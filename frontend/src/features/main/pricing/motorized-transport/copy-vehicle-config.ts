/** Trường xác định một dòng đơn giá trong khối của một xe (ngoài xe) */
const ITEM_IDENTITY_FIELDS = [
	'productionProcessId',
	'equipmentQuality',
	'cargoTypeId',
	'dumpingLocationId',
	'haulDistanceId',
] as const;

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- dữ liệu khoảng thời gian của từng loại xe có cấu trúc riêng
type AnyItem = Record<string, any>;

const itemIdentity = (item: AnyItem) =>
	ITEM_IDENTITY_FIELDS.map((field) => item[field] ?? '').join('|');

const belongsTo = (key: string, vehicleId: string) =>
	key === vehicleId || key.startsWith(`${vehicleId}_`);

const renameKey = (key: string, from: string, to: string) =>
	to + key.slice(from.length);

const isRecord = (value: unknown): value is Record<string, unknown> =>
	!!value && typeof value === 'object' && !Array.isArray(value);

/**
 * Chép toàn bộ cấu hình (công đoạn, chất lượng, cung độ, loại hàng, vị trí...)
 * và đơn giá của xe `sourceId` sang các xe `targetIds` trong cùng khoảng thời gian.
 * Cấu hình cũ của xe đích bị ghi đè; dòng đơn giá trùng khớp giữ lại id để khi
 * cập nhật không tạo bản ghi mới.
 */
export function copyVehicleConfig<T extends AnyItem>(
	period: T,
	sourceId: string,
	targetIds: string[],
	/** Các trường hiển thị theo xe của dòng đơn giá, vd: { title, materialName } */
	getItemFields: (vehicleId: string) => AnyItem,
): T {
	const next: AnyItem = { ...period };

	for (const [field, value] of Object.entries(period)) {
		// Map cấu hình khoá theo "xeId" hoặc "xeId_congDoanId"
		if (isRecord(value)) {
			const map: Record<string, unknown> = {};
			for (const [key, val] of Object.entries(value)) {
				if (!targetIds.some((target) => belongsTo(key, target))) {
					map[key] = val;
				}
			}
			for (const [key, val] of Object.entries(value)) {
				if (!belongsTo(key, sourceId)) continue;
				for (const target of targetIds) {
					map[renameKey(key, sourceId, target)] = Array.isArray(val)
						? [...val]
						: val;
				}
			}
			next[field] = map;
		}
	}

	// Các dòng cung độ đã bị xoá thủ công (khoá bắt đầu bằng "xeId_")
	if (Array.isArray(period.excludedItemKeys)) {
		const keys = (period.excludedItemKeys as string[]).filter(
			(key) => !targetIds.some((target) => belongsTo(key, target)),
		);
		for (const key of period.excludedItemKeys as string[]) {
			if (!belongsTo(key, sourceId)) continue;
			for (const target of targetIds) {
				keys.push(renameKey(key, sourceId, target));
			}
		}
		next.excludedItemKeys = keys;
	}

	if (Array.isArray(period.items)) {
		const items = period.items as AnyItem[];
		const sourceItems = items.filter((it) => it.assignmentCodeId === sourceId);
		const result = items.filter(
			(it) => !targetIds.includes(it.assignmentCodeId),
		);

		for (const target of targetIds) {
			const existingTargetItems = items.filter(
				(it) => it.assignmentCodeId === target,
			);
			for (const source of sourceItems) {
				const existing = existingTargetItems.find(
					(it) => itemIdentity(it) === itemIdentity(source),
				);
				result.push({
					...source,
					...getItemFields(target),
					assignmentCodeId: target,
					tempId:
						existing?.tempId ||
						`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
					id: existing?.id,
					detailId: existing?.detailId,
				});
			}
		}
		next.items = result;
	}

	return next as T;
}
