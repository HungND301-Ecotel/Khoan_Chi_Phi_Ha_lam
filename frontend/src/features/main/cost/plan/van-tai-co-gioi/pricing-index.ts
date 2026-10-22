import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';
import type { MotorizedCategory } from './types';

// Tra từ Đơn giá VTCG: Nhóm vật tư có đơn giá ở loại xe nào, với những Công đoạn nào — để form
// Kế hoạch tự hiện đúng khối nhập theo loại xe, không bắt người dùng chọn "Nhóm Vận tải cơ giới".
export type MotorizedPricingIndex = Record<
	string,
	Partial<Record<MotorizedCategory, string[]>>
>;

// Cùng thứ tự hiển thị các khối trên form
export const MOTORIZED_CATEGORY_ORDER: MotorizedCategory[] = [
	'scania',
	'excavator_dozer',
	'service_crane',
	'vacuum_truck',
];

// MechanizedTransportUnitPriceType (BE) ↔ loại xe (khối nhập)
export const VEHICLE_TYPE_TO_CATEGORY: Record<number, MotorizedCategory> = {
	1: 'scania',
	2: 'vacuum_truck',
	3: 'service_crane',
	4: 'excavator_dozer',
};

export const CATEGORY_TO_VEHICLE_TYPE: Record<MotorizedCategory, number> = {
	scania: 1,
	vacuum_truck: 2,
	service_crane: 3,
	excavator_dozer: 4,
};

// Khoá lưu loại xe người dùng chọn cho Công đoạn có đơn giá ở nhiều loại xe của 1 Nhóm vật tư
export const processCategoryKey = (
	assignmentCodeId: string,
	processId: string,
) => `${assignmentCodeId}::${processId}`;

// Dựng lại lựa chọn loại xe từ các dòng đã lưu (VehicleType) khi mở form sửa
export function buildProcessCategories(
	lines: {
		equipmentId?: string | null;
		productionProcessId?: string | null;
		vehicleType?: number | null;
	}[],
): Record<string, string> {
	const result: Record<string, string> = {};
	lines.forEach((line) => {
		const category = line.vehicleType
			? VEHICLE_TYPE_TO_CATEGORY[line.vehicleType]
			: undefined;
		if (category && line.equipmentId && line.productionProcessId) {
			result[processCategoryKey(line.equipmentId, line.productionProcessId)] =
				category;
		}
	});
	return result;
}

type PricingGroup = {
	assignmentCodeId: string;
	sections?: { vehicleType: number; productionProcessId: string }[];
};

let cachedIndex: Promise<MotorizedPricingIndex> | null = null;
let cachedAt = 0;
// Đơn giá có thể vừa được thêm ở màn Đơn giá — chỉ dùng lại bản đã tải trong thời gian ngắn
const CACHE_TTL_MS = 60_000;

async function loadIndex(): Promise<MotorizedPricingIndex> {
	const res = await api.pagging<PricingGroup>(
		API.PRICING.MOTORIZED_TRANSPORT.ALL.LIST,
		{ ignorePagination: true },
	);
	const index: MotorizedPricingIndex = {};
	(res.result?.data || []).forEach((group) => {
		(group.sections || []).forEach((section) => {
			const category = VEHICLE_TYPE_TO_CATEGORY[section.vehicleType];
			if (!category || !group.assignmentCodeId) return;
			const byCategory = (index[group.assignmentCodeId] ??= {});
			const procs = (byCategory[category] ??= []);
			if (
				section.productionProcessId &&
				!procs.includes(section.productionProcessId)
			) {
				procs.push(section.productionProcessId);
			}
		});
	});
	return index;
}

export function useMotorizedPricingIndex() {
	const [index, setIndex] = useState<MotorizedPricingIndex | null>(null);

	useEffect(() => {
		let mounted = true;
		if (!cachedIndex || Date.now() - cachedAt > CACHE_TTL_MS) {
			cachedAt = Date.now();
			cachedIndex = loadIndex().catch((error) => {
				cachedIndex = null;
				throw error;
			});
		}
		cachedIndex
			.then((value) => mounted && setIndex(value))
			.catch(() => mounted && setIndex({}));
		return () => {
			mounted = false;
		};
	}, []);

	return index;
}

// Các loại xe của 1 Nhóm vật tư. Chưa có đơn giá VTCG nào thì mặc định Scania để vẫn nhập được.
export function getAssetCategories(
	index: MotorizedPricingIndex,
	assignmentCodeId: string,
): MotorizedCategory[] {
	const byCategory = index[assignmentCodeId] || {};
	const claimed = new Set<string>();
	const categories = MOTORIZED_CATEGORY_ORDER.filter((category) => {
		const own = (byCategory[category] || []).filter((id) => !claimed.has(id));
		own.forEach((id) => claimed.add(id));
		return own.length > 0;
	});
	if (categories.length > 0) return categories;
	// Chưa có đơn giá: dùng loại xe người dùng chọn (gắn vào index qua withCategoryOverrides)
	const declared = MOTORIZED_CATEGORY_ORDER.find(
		(category) => byCategory[category],
	);
	return [declared || 'scania'];
}

// Nhóm vật tư đã có đơn giá VTCG ở ít nhất 1 Công đoạn
export function hasAssetPricing(
	index: MotorizedPricingIndex,
	assignmentCodeId: string,
): boolean {
	return Object.values(index[assignmentCodeId] || {}).some(
		(procs) => (procs || []).length > 0,
	);
}

// Đoán loại xe theo mã / tên Nhóm vật tư khi chưa có đơn giá — người dùng sửa lại được trên form
export function guessAssetCategory(asset?: {
	code?: string;
	name?: string;
	label?: string;
}): MotorizedCategory {
	const text =
		`${asset?.code || ''} ${asset?.name || asset?.label || ''}`.toLowerCase();
	if (
		['hút bùn', 'chất thải', 'xe bồn', 'bồn hút'].some((k) => text.includes(k))
	) {
		return 'vacuum_truck';
	}
	if (
		['xúc', 'gạt', 'ủi', 'dx210', 'jcb', 'd7r', 'komatsu'].some((k) =>
			text.includes(k),
		)
	) {
		return 'excavator_dozer';
	}
	if (
		['cẩu', 'nâng', 'forklift', 'dịch vụ', 'tưới', 'cứu hỏa', 'phục vụ'].some(
			(k) => text.includes(k),
		)
	) {
		return 'service_crane';
	}
	return 'scania';
}

// Kế hoạch / vận hành được lập trước khi có đơn giá: Nhóm vật tư chưa có đơn giá được gắn loại xe
// người dùng chọn (mặc định đoán theo tên) để vẫn hiện khối nhập, đơn giá sẽ tự khớp khi được tạo.
// Công đoạn có đơn giá ở nhiều loại xe: chỉ giữ ở loại xe người dùng chọn (processCategories).
export function withCategoryOverrides(
	index: MotorizedPricingIndex,
	assignmentCodeIds: string[],
	overrides: Record<string, string>,
	assets: {
		id?: string;
		value?: string;
		code?: string;
		name?: string;
		label?: string;
	}[],
	processCategories: Record<string, string> = {},
): MotorizedPricingIndex {
	const next: MotorizedPricingIndex = { ...index };
	assignmentCodeIds.forEach((id) => {
		if (!hasAssetPricing(index, id)) {
			const category = (MOTORIZED_CATEGORY_ORDER as string[]).includes(
				overrides[id],
			)
				? (overrides[id] as MotorizedCategory)
				: guessAssetCategory(assets.find((a) => a.id === id || a.value === id));
			next[id] = { [category]: [] };
			return;
		}
		const byCategory = { ...index[id] };
		let changed = false;
		Object.entries(processCategories).forEach(([key, chosen]) => {
			const prefix = `${id}::`;
			if (!key.startsWith(prefix)) return;
			const procId = key.slice(prefix.length);
			if (!(byCategory[chosen as MotorizedCategory] || []).includes(procId)) {
				return;
			}
			MOTORIZED_CATEGORY_ORDER.forEach((category) => {
				if (category !== chosen && byCategory[category]?.includes(procId)) {
					byCategory[category] = byCategory[category]!.filter(
						(other) => other !== procId,
					);
					changed = true;
				}
			});
		});
		if (changed) next[id] = byCategory;
	});
	return next;
}

// Loại xe (khối nhập) mà Công đoạn đã chọn của 1 Nhóm vật tư thuộc về — chọn Nhóm vật tư + Công đoạn
// là biết dòng đó khớp đơn giá loại xe nào, chỉ hiện khối có Công đoạn được chọn.
export function getProcessCategories(
	index: MotorizedPricingIndex,
	assignmentCodeId: string,
	selectedProcessIds: string[],
): MotorizedCategory[] {
	return getAssetCategories(index, assignmentCodeId).filter((category) =>
		getAllowedProcessIds(
			index,
			category,
			assignmentCodeId,
			selectedProcessIds,
		).some((procId) => selectedProcessIds.includes(procId)),
	);
}

// Công đoạn được hiện trong khối của 1 loại xe. Công đoạn đã chọn mà không có đơn giá ở loại xe
// nào (vd. đơn giá bị xoá sau khi lập kế hoạch) thì dồn về khối đầu tiên của nhóm vật tư để không
// bị ẩn mất.
export function getAllowedProcessIds(
	index: MotorizedPricingIndex,
	category: MotorizedCategory,
	assignmentCodeId: string,
	selectedProcessIds: string[] = [],
): string[] {
	const byCategory = index[assignmentCodeId] || {};
	// Cùng 1 Công đoạn có đơn giá ở nhiều loại xe → chỉ thuộc khối đứng trước, tránh 2 khối cùng
	// sinh dòng trùng nhau
	const earlierCategories = MOTORIZED_CATEGORY_ORDER.slice(
		0,
		MOTORIZED_CATEGORY_ORDER.indexOf(category),
	);
	const pricedEarlier = new Set(
		earlierCategories.flatMap((earlier) => byCategory[earlier] || []),
	);
	const allowed = (byCategory[category] || []).filter(
		(procId) => !pricedEarlier.has(procId),
	);
	if (getAssetCategories(index, assignmentCodeId)[0] === category) {
		const pricedAnywhere = new Set(Object.values(byCategory).flat());
		selectedProcessIds.forEach((procId) => {
			if (!pricedAnywhere.has(procId) && !allowed.includes(procId)) {
				allowed.push(procId);
			}
		});
	}
	return allowed;
}
