import { LumpSumFinalSettlement } from './types';

function combineCodeName(code?: string, name?: string, fallback = 'Chưa xác định') {
	return [code?.trim(), name?.trim()].filter(Boolean).join(' - ') || fallback;
}

// So sánh tự nhiên (A < B < C, "2" < "10") theo tiếng Việt — dùng để sắp xếp Công đoạn/Tuyến/
// Thiết bị/Đơn vị/Chất lượng theo ABC thay vì giữ nguyên thứ tự BE trả về.
function compareLabel(a?: string | null, b?: string | null) {
	return (a ?? '').localeCompare(b ?? '', 'vi', { numeric: true, sensitivity: 'base' });
}

type CostTotals = {
	plannedQuantity: number;
	actualQuantity: number;
	materialsTotal: number;
	maintainsTotal: number;
	electricitiesTotal: number;
	totalAmount: number;
};

function sumCostTotals(items: LumpSumFinalSettlement[]): CostTotals {
	return {
		plannedQuantity: items.reduce((sum, x) => sum + (x.plannedQuantity ?? 0), 0),
		actualQuantity: items.reduce((sum, x) => sum + (x.actualQuantity ?? 0), 0),
		materialsTotal: items.reduce(
			(sum, x) => sum + (x.materials?.totalAmount ?? 0),
			0,
		),
		maintainsTotal: items.reduce(
			(sum, x) => sum + (x.maintains?.totalAmount ?? 0),
			0,
		),
		electricitiesTotal: items.reduce(
			(sum, x) => sum + (x.electricities?.totalAmount ?? 0),
			0,
		),
		totalAmount: items.reduce((sum, x) => sum + (x.totalAmount ?? 0), 0),
	};
}

function buildSubHeaderRow(
	id: string,
	sttLabel: string,
	productName: string,
	unitOfMeasureName: string | undefined,
	items: LumpSumFinalSettlement[],
): LumpSumFinalSettlement {
	const totals = sumCostTotals(items);
	return {
		id,
		sttLabel,
		isBold: true,
		excludeFromSummary: true,
		productName,
		unitOfMeasureName,
		plannedQuantity: totals.plannedQuantity,
		actualQuantity: totals.actualQuantity,
		materials: { totalAmount: totals.materialsTotal },
		maintains: { totalAmount: totals.maintainsTotal },
		electricities: { totalAmount: totals.electricitiesTotal },
		totalAmount: totals.totalAmount,
	};
}

// VTL/VTCG — trong 1 nhóm công đoạn, tách 3 cấp: Công đoạn sản xuất (đánh số) > Tuyến vận tải
// (băng tải/trục, con là Đơn vị áp dụng) hoặc Thiết bị (Monoray, con là Chất lượng thiết bị)
// (đánh số) > Đơn vị/Chất lượng (không đánh số, chỉ đánh dấu "-"), khớp mẫu bảng kê PM gửi.
// Dòng nào không có đủ cặp Tuyến+Đơn vị hoặc Thiết bị+Chất lượng (VD: Thiết bị khác) thì giữ
// nguyên 1 cấp ở vị trí Tuyến/Thiết bị, không tách thêm cấp dưới.
function pushTransportGroupItems(
	result: LumpSumFinalSettlement[],
	groupItems: LumpSumFinalSettlement[],
	stt: number,
) {
	// Chi phí vật tư mau hỏng rẻ tiền là khoản trọn gói theo THÁNG (1 dòng cho cả Đơn vị+Nhóm
	// công đoạn), không thuộc về 1 Công đoạn/Tuyến/Thiết bị cụ thể nào — tách ra hiển thị phẳng,
	// không đi qua gộp cây Công đoạn > Tuyến/Thiết bị bên dưới.
	const lowValueRows = groupItems.filter((x) => x.isLowValuePerishableSupplyRow);
	const regularItems = groupItems.filter((x) => !x.isLowValuePerishableSupplyRow);

	const processGroups = new Map<string, LumpSumFinalSettlement[]>();
	const processOrder: string[] = [];

	for (const item of regularItems) {
		const key =
			item.productionProcessCode || item.productionProcessName || 'process';
		if (!processGroups.has(key)) {
			processGroups.set(key, []);
			processOrder.push(key);
		}
		processGroups.get(key)!.push(item);
	}

	processOrder.sort((a, b) => {
		const itemsA = processGroups.get(a) ?? [];
		const itemsB = processGroups.get(b) ?? [];
		return compareLabel(
			itemsA[0]?.productionProcessCode || itemsA[0]?.productionProcessName,
			itemsB[0]?.productionProcessCode || itemsB[0]?.productionProcessName,
		);
	});

	let processStt = 1;

	for (const processKey of processOrder) {
		const processItems = processGroups.get(processKey) ?? [];
		const first = processItems[0];
		const processLabel = combineCodeName(
			first.productionProcessCode,
			first.productionProcessName,
			'Công đoạn chưa xác định',
		);
		const processSttLabel = `${stt}.${processStt}`;

		result.push(
			buildSubHeaderRow(
				`${first.id ?? processKey}-process`,
				processSttLabel,
				processLabel,
				first.unitOfMeasureName,
				processItems,
			),
		);

		pushRouteOrEquipmentGroups(result, processItems, processSttLabel);
		processStt++;
	}

	for (const item of lowValueRows) {
		result.push({
			...item,
			sttLabel: `${stt}.${processStt}`,
		});
		processStt++;
	}
}

function pushRouteOrEquipmentGroups(
	result: LumpSumFinalSettlement[],
	items: LumpSumFinalSettlement[],
	sttPrefix: string,
) {
	const subGroups = new Map<string, LumpSumFinalSettlement[]>();
	const subGroupOrder: string[] = [];
	const flatItems: LumpSumFinalSettlement[] = [];

	for (const item of items) {
		let key: string | null = null;
		if (item.transportRouteId && item.routeDepartmentId) {
			key = `route:${item.transportRouteId}`;
		} else if (
			item.equipmentId &&
			(item.equipmentQuality || item.haulDistanceId || item.haulDistanceValue)
		) {
			key = `equipment:${item.equipmentId}`;
		}

		if (!key) {
			flatItems.push(item);
			continue;
		}

		const existing = subGroups.get(key);
		if (existing) {
			existing.push(item);
			continue;
		}
		subGroups.set(key, [item]);
		subGroupOrder.push(key);
	}

	subGroupOrder.sort((a, b) => {
		const firstA = (subGroups.get(a) ?? [])[0];
		const firstB = (subGroups.get(b) ?? [])[0];
		const isRoute = a.startsWith('route:');
		return isRoute
			? compareLabel(firstA?.transportRouteCode, firstB?.transportRouteCode)
			: compareLabel(firstA?.equipmentCode, firstB?.equipmentCode);
	});

	let subStt = 1;

	for (const key of subGroupOrder) {
		const children = (subGroups.get(key) ?? []).slice();
		const first = children[0];
		const isRoute = key.startsWith('route:');

		children.sort((a, b) =>
			isRoute
				? compareLabel(a.routeDepartmentCode, b.routeDepartmentCode)
				: compareLabel(
						a.haulDistanceValue || a.equipmentQuality,
						b.haulDistanceValue || b.equipmentQuality,
					),
		);

		const parentLabel = isRoute
			? combineCodeName(
					first.transportRouteCode,
					first.transportRouteName,
					'Tuyến chưa xác định',
				)
			: combineCodeName(
					first.equipmentCode,
					first.equipmentName,
					'Thiết bị chưa xác định',
				);

		result.push(
			buildSubHeaderRow(
				`${first.id ?? key}-group`,
				`${sttPrefix}.${subStt}`,
				parentLabel,
				first.unitOfMeasureName,
				children,
			),
		);

		for (const child of children) {
			const leafLabel = isRoute
				? combineCodeName(
						child.routeDepartmentCode,
						child.routeDepartmentName,
						'Đơn vị chưa xác định',
					)
				: [
						child.haulDistanceValue && `Cung độ: ${child.haulDistanceValue}`,
						child.equipmentQuality && `Loại ${child.equipmentQuality}`,
					]
						.filter(Boolean)
						.join(' - ') || child.productName || 'Chi tiết';

			result.push({
				...child,
				sttLabel: '-',
				productName: leafLabel,
			});
		}

		subStt++;
	}

	flatItems.sort((a, b) =>
		compareLabel(
			a.equipmentCode || a.transportRouteCode,
			b.equipmentCode || b.transportRouteCode,
		),
	);
	for (const item of flatItems) {
		result.push({
			...item,
			sttLabel: `${sttPrefix}.${subStt}`,
		});
		subStt++;
	}
}

export function groupByProcessGroup(
	items: LumpSumFinalSettlement[],
	sttStart = 1,
): LumpSumFinalSettlement[] {
	const groups = new Map<string, LumpSumFinalSettlement[]>();

	for (const item of items) {
		const key =
			item.processGroupId ||
			`${item.processGroupCode}|${item.processGroupName}`;
		const existing = groups.get(key);
		if (existing) {
			existing.push(item);
			continue;
		}
		groups.set(key, [item]);
	}

	const result: LumpSumFinalSettlement[] = [];
	let stt = sttStart;

	for (const [, groupItems] of groups) {
		const first = groupItems[0];
		const code = first.processGroupCode?.trim() ?? '';
		const name = first.processGroupName?.trim() ?? '';
		const groupTitle =
			[code, name].filter(Boolean).join(' - ') || 'Chưa phân nhóm';

		result.push({
			id: `group-${first.processGroupId ?? stt}`,
			sttLabel: `${stt}`,
			isBold: true,
			isProcessGroupRow: true,
			productName: groupTitle,
			plannedQuantity: groupItems.reduce(
				(sum, item) => sum + (item.plannedQuantity ?? 0),
				0,
			),
			actualQuantity: groupItems.reduce(
				(sum, item) => sum + (item.actualQuantity ?? 0),
				0,
			),
			materials: {
				totalAmount: groupItems.reduce(
					(sum, item) =>
						sum +
						(item.materials?.totalAmount ?? 0) +
						(item.ashContentMaterials?.totalAmount ?? 0),
					0,
				),
			},
			maintains: {
				totalAmount: groupItems.reduce(
					(sum, item) =>
						sum +
						(item.maintains?.totalAmount ?? 0) +
						(item.ashContentMaintains?.totalAmount ?? 0),
					0,
				),
			},
			electricities: {
				totalAmount: groupItems.reduce(
					(sum, item) =>
						sum +
						(item.electricities?.totalAmount ?? 0) +
						(item.ashContentElectricities?.totalAmount ?? 0),
					0,
				),
			},
			totalAmount: groupItems.reduce(
				(sum, item) =>
					sum + (item.totalAmount ?? 0) + (item.ashContentTotalAmount ?? 0),
				0,
			),
		});

		const isTransportGroup = groupItems.some(
			(item) =>
				item.transportRouteId ||
				item.equipmentId ||
				item.haulDistanceId ||
				item.isLowValuePerishableSupplyRow,
		);

		if (isTransportGroup) {
			pushTransportGroupItems(result, groupItems, stt);
		} else {
			let subStt = 1;
			for (const item of groupItems) {
				result.push({
					...item,
					sttLabel: `${stt}.${subStt}`,
				});
				subStt++;

				if (item.ashContentDeltaPercent) {
					result.push({
						id: `${item.id}-ak`,
						processGroupId: item.processGroupId,
						processGroupCode: item.processGroupCode,
						processGroupName: item.processGroupName,
						sttLabel: `${stt}.${subStt}`,
						productName: 'Tăng giảm AK theo kế hoạch',
						unitOfMeasureName: '%',
						plannedQuantity: item.planAshContent,
						actualQuantity: item.actualAshContent,
						materials: {
							unitPrice: item.materials?.unitPrice,
							totalAmount: item.ashContentMaterials?.totalAmount,
						},
						maintains: item.ashContentMaintains,
						electricities: item.ashContentElectricities,
						totalAmount: item.ashContentTotalAmount,
					});
					subStt++;
				}
			}
		}

		stt += 1;
	}

	return result;
}
