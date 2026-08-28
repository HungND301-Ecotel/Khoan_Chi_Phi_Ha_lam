import { LumpSumFinalSettlement, RevenueCostAdjustmentConfig } from './types';

export function resolveRevenueCostAdjustmentRate(
	profit: number,
	configs?: RevenueCostAdjustmentConfig[],
): number {
	if (configs && configs.length > 0) {
		const matched = configs.find((x) => {
			const aboveMin = x.minProfit == null || profit >= x.minProfit;
			const belowMax = x.maxProfit == null || profit <= x.maxProfit;
			return aboveMin && belowMax;
		});
		if (matched && matched.rate !== undefined && matched.rate !== null) {
			return matched.rate > 1 ? matched.rate / 100 : matched.rate;
		}
	}
	return profit >= 0 ? 0.6 : 1.0;
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
		const processLabel = (
			first.productionProcessName ||
			first.productionProcessCode ||
			'Công đoạn chưa xác định'
		).toUpperCase();
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
			? (first.transportRouteName || first.transportRouteCode || 'Tuyến chưa xác định')
			: (first.equipmentName || first.equipmentCode || 'Thiết bị chưa xác định');

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
				? (child.routeDepartmentName || child.routeDepartmentCode || 'Đơn vị chưa xác định')
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

function formatVtcgParameterName(
	cargo?: string,
	rec?: string,
	dump?: string,
	fallback = 'Thông số',
) {
	const parts: string[] = [];
	if (cargo?.trim()) parts.push(cargo.trim());
	const locs: string[] = [];
	if (rec?.trim()) locs.push(rec.trim());
	if (dump?.trim()) locs.push(`về ${dump.trim()}`);
	if (locs.length > 0) {
		parts.push(`(${locs.join(' ')})`);
	}
	return parts.join(' ') || fallback;
}

function combineVehicleName(
	code?: string,
	name?: string,
	quality?: string,
) {
	const baseName = name?.trim() || code?.trim() || 'Thiết bị';
	if (!quality?.trim()) return baseName;
	const qTrim = quality.trim();
	const qLabel = qTrim.toUpperCase().startsWith('LOẠI') ? qTrim : `LOẠI ${qTrim}`;
	return `${baseName} (${qLabel})`;
}

// VTCG — Gom 4 cấp:
// Cấp 1: Thiết bị (Xe) — Đánh số [STT.0], tên [Tên xe] (LOẠI [Chất lượng])
// Cấp 2: Công đoạn sản xuất — Đánh số [1], [2], [3]...
// Cấp 3: Thông số (Chủng loại hàng, Vị trí nhận, Vị trí đổ) — Không đánh số
// Cấp 4: Cung độ (L <= ... Km) — Không đánh số
// Cuối mỗi xe: 6 dòng tổng kết tài chính (Doanh thu xe theo giá khoán, Tổng doanh thu xe,
// Tổng chi phí xe, Tiết kiệm(+)/Bội chi(-), Tiết kiệm được chấp nhận (khống chế 8%), Cộng trừ thu nhập)
function pushVtcgGroupItems(
	result: LumpSumFinalSettlement[],
	groupItems: LumpSumFinalSettlement[],
	stt: number,
	month = 1,
	revenueCostAdjustmentConfigs?: RevenueCostAdjustmentConfig[],
) {
	const lowValueRows = groupItems.filter((x) => x.isLowValuePerishableSupplyRow);
	const regularItems = groupItems.filter((x) => !x.isLowValuePerishableSupplyRow);

	const vehicleMap = new Map<string, LumpSumFinalSettlement[]>();
	const vehicleOrder: string[] = [];

	for (const item of regularItems) {
		const equipKey =
			item.equipmentId ||
			item.equipmentCode ||
			item.equipmentName ||
			'unknown-vehicle';
		const qualityKey = (item.equipmentQuality || '').trim();
		const vKey = `${equipKey}__${qualityKey}`;
		if (!vehicleMap.has(vKey)) {
			vehicleMap.set(vKey, []);
			vehicleOrder.push(vKey);
		}
		vehicleMap.get(vKey)!.push(item);
	}

	vehicleOrder.sort((a, b) => {
		const itemsA = vehicleMap.get(a) ?? [];
		const itemsB = vehicleMap.get(b) ?? [];
		const labelA = combineVehicleName(
			itemsA[0]?.equipmentCode,
			itemsA[0]?.equipmentName,
			itemsA[0]?.equipmentQuality,
		);
		const labelB = combineVehicleName(
			itemsB[0]?.equipmentCode,
			itemsB[0]?.equipmentName,
			itemsB[0]?.equipmentQuality,
		);
		return compareLabel(labelA, labelB);
	});

	let vehicleIdx = 0;

	for (const vKey of vehicleOrder) {
		const vehicleItems = vehicleMap.get(vKey) ?? [];
		const firstVehicleItem = vehicleItems[0];
		vehicleIdx++;
		const vehicleSttLabel = `${stt}.${vehicleIdx}`;
		const vehicleTitle = combineVehicleName(
			firstVehicleItem.equipmentCode,
			firstVehicleItem.equipmentName,
			firstVehicleItem.equipmentQuality,
		);

		// Cấp 1 Header Row
		result.push({
			...buildSubHeaderRow(
				`vtcg-v-${firstVehicleItem.equipmentId ?? vehicleIdx}-${firstVehicleItem.equipmentQuality ?? ''}`,
				vehicleSttLabel,
				vehicleTitle,
				undefined,
				vehicleItems,
			),
			isVehicleHeaderRow: true,
		});

		// Group by Production Process (Cấp 2)
		const processMap = new Map<string, LumpSumFinalSettlement[]>();
		const processOrder: string[] = [];

		for (const item of vehicleItems) {
			const pKey = item.productionProcessCode || item.productionProcessName || 'process';
			if (!processMap.has(pKey)) {
				processMap.set(pKey, []);
				processOrder.push(pKey);
			}
			processMap.get(pKey)!.push(item);
		}

		processOrder.sort((a, b) => {
			const itemsA = processMap.get(a) ?? [];
			const itemsB = processMap.get(b) ?? [];
			return compareLabel(
				itemsA[0]?.productionProcessName || itemsA[0]?.productionProcessCode,
				itemsB[0]?.productionProcessName || itemsB[0]?.productionProcessCode,
			);
		});

		let processIdx = 1;

		for (const pKey of processOrder) {
			const pItems = processMap.get(pKey) ?? [];
			const firstPItem = pItems[0];
			const processSttLabel = `${stt}.${vehicleIdx}.${processIdx}`;
			const processTitle = (
				firstPItem.productionProcessName ||
				firstPItem.productionProcessCode ||
				'Công đoạn chưa xác định'
			).toUpperCase();

			// Cấp 2 Header Row
			result.push(
				buildSubHeaderRow(
					`vtcg-p-${firstPItem.id ?? `${vehicleIdx}-${processIdx}`}`,
					processSttLabel,
					processTitle,
					firstPItem.unitOfMeasureName,
					pItems,
				),
			);

			// Group by Parameter (Cấp 3)
			const paramMap = new Map<string, LumpSumFinalSettlement[]>();
			const paramOrder: string[] = [];

			for (const item of pItems) {
				const paramName = formatVtcgParameterName(
					item.cargoTypeName,
					item.receivingLocationName,
					item.dumpingLocationName,
					item.haulDistanceId || item.haulDistanceValue ? 'Thông số vận chuyển' : '',
				);
				const paramKey = paramName || '__no_param__';
				if (!paramMap.has(paramKey)) {
					paramMap.set(paramKey, []);
					paramOrder.push(paramKey);
				}
				paramMap.get(paramKey)!.push(item);
			}

			for (const paramKey of paramOrder) {
				const paramItems = paramMap.get(paramKey) ?? [];
				const isNamedParam = paramKey !== '__no_param__' && paramKey.trim() !== '';
				const hasSubBreakdown =
					paramItems.length > 1 ||
					paramItems.some(
						(x) => x.haulDistanceValue?.trim() || x.transportRouteName?.trim(),
					);
				const hasParamHeader = isNamedParam && hasSubBreakdown;

				if (hasParamHeader) {
					// Cấp 3 Header Row (bold, no STT)
					result.push(
						buildSubHeaderRow(
							`vtcg-param-${paramItems[0]?.id ?? `${vehicleIdx}-${processIdx}-${paramKey}`}`,
							'',
							paramKey,
							paramItems[0]?.unitOfMeasureName,
							paramItems,
						),
					);
				}

				// Cấp 4 Leaf Rows (Cung độ / Tuyến / Chi tiết)
				for (const child of paramItems) {
					let leafTitle = child.haulDistanceValue?.trim();
					if (leafTitle) {
						if (!leafTitle.startsWith('L') && !leafTitle.startsWith('l')) {
							if (leafTitle.startsWith('≤') || leafTitle.startsWith('<=')) {
								leafTitle = `L ${leafTitle}`;
							} else {
								leafTitle = `L ≤ ${leafTitle}`;
							}
						}
					} else if (child.transportRouteName?.trim()) {
						leafTitle = child.transportRouteName.trim();
					} else if (child.routeDepartmentName?.trim()) {
						leafTitle = child.routeDepartmentName.trim();
					} else if (isNamedParam) {
						leafTitle = paramKey;
					} else if (child.productionProcessName?.trim()) {
						leafTitle = child.productionProcessName.trim();
					} else {
						leafTitle = child.productName || 'Chi tiết';
					}

					result.push({
						...child,
						sttLabel: '',
						productName: leafTitle,
					});
				}
			}

			processIdx++;
		}

		// 6 Dòng tổng kết tài chính cho xe này
		const vehicleRevenue = vehicleItems.reduce(
			(sum, x) => sum + (x.totalAmount ?? 0),
			0,
		);
		const vehicleMaterialRevenue = vehicleItems.reduce(
			(sum, x) => sum + (x.materials?.totalAmount ?? 0),
			0,
		);
		const vehicleMaintainRevenue = vehicleItems.reduce(
			(sum, x) => sum + (x.maintains?.totalAmount ?? 0),
			0,
		);
		const vehicleElectricityRevenue = vehicleItems.reduce(
			(sum, x) => sum + (x.electricities?.totalAmount ?? 0),
			0,
		);

		const vehicleCost = firstVehicleItem.vehicleTotalTransferredCost ?? 0;
		const vehicleMaterialCost = firstVehicleItem.vehicleTransferredMaterialAmount ?? 0;
		const vehicleMaintainCost = firstVehicleItem.vehicleTransferredMaintainAmount ?? 0;
		const vehicleElectricityCost = firstVehicleItem.vehicleTransferredElectricityAmount ?? 0;

		const calcAcceptedSaving = (rev: number, cost: number) => {
			const sav = rev - cost;
			if (rev > 0 && sav / rev >= 0.08) {
				return rev * 0.08;
			}
			return sav;
		};

		const calcIncomeAdjustment = (accepted: number) => {
			const r = resolveRevenueCostAdjustmentRate(
				accepted,
				revenueCostAdjustmentConfigs,
			);
			return accepted * r;
		};

		const vehicleMaterialSaving = vehicleMaterialRevenue - vehicleMaterialCost;
		const vehicleMaintainSaving = vehicleMaintainRevenue - vehicleMaintainCost;
		const vehicleElectricitySaving = vehicleElectricityRevenue - vehicleElectricityCost;
		const vehicleSaving = vehicleRevenue - vehicleCost;

		const acceptedMaterialSaving = calcAcceptedSaving(
			vehicleMaterialRevenue,
			vehicleMaterialCost,
		);
		const acceptedMaintainSaving = calcAcceptedSaving(
			vehicleMaintainRevenue,
			vehicleMaintainCost,
		);
		const acceptedElectricitySaving = calcAcceptedSaving(
			vehicleElectricityRevenue,
			vehicleElectricityCost,
		);
		let acceptedSaving = vehicleSaving;
		if (vehicleRevenue > 0 && vehicleSaving / vehicleRevenue >= 0.08) {
			acceptedSaving = vehicleRevenue * 0.08;
		}

		const incomeAdjustmentMaterial = calcIncomeAdjustment(acceptedMaterialSaving);
		const incomeAdjustmentMaintain = calcIncomeAdjustment(acceptedMaintainSaving);
		const incomeAdjustmentElectricity = calcIncomeAdjustment(acceptedElectricitySaving);
		const rate = resolveRevenueCostAdjustmentRate(
			acceptedSaving,
			revenueCostAdjustmentConfigs,
		);
		const incomeAdjustment = acceptedSaving * rate;

		// 1. Doanh thu xe theo giá khoán
		result.push({
			id: `vtcg-summary-rev-khoan-${vKey}`,
			sttLabel: '',
			productName: 'Doanh thu xe theo giá khoán',
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: vehicleMaterialRevenue },
			maintains: { totalAmount: vehicleMaintainRevenue },
			electricities: { totalAmount: vehicleElectricityRevenue },
			totalAmount: vehicleRevenue,
		});

		// 2. Tổng doanh thu xe tháng X
		result.push({
			id: `vtcg-summary-total-rev-${vKey}`,
			sttLabel: '',
			productName: `Tổng doanh thu xe tháng ${month}`,
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: vehicleMaterialRevenue },
			maintains: { totalAmount: vehicleMaintainRevenue },
			electricities: { totalAmount: vehicleElectricityRevenue },
			totalAmount: vehicleRevenue,
		});

		// 3. Tổng chi phí xe tháng X
		result.push({
			id: `vtcg-summary-cost-${vKey}`,
			sttLabel: '',
			productName: `Tổng chi phí xe tháng ${month}`,
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: vehicleMaterialCost },
			maintains: { totalAmount: vehicleMaintainCost },
			electricities: { totalAmount: vehicleElectricityCost },
			totalAmount: vehicleCost,
		});

		// 4. Tiết kiệm(+) bội chi (-) tháng X
		result.push({
			id: `vtcg-summary-saving-${vKey}`,
			sttLabel: '',
			productName: `Tiết kiệm(+) bội chi (-) tháng ${month}`,
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: vehicleMaterialSaving },
			maintains: { totalAmount: vehicleMaintainSaving },
			electricities: { totalAmount: vehicleElectricitySaving },
			totalAmount: vehicleSaving,
		});

		// 5. Tiết kiệm(+) bội chi (-) được chấp nhận tháng X
		result.push({
			id: `vtcg-summary-accepted-${vKey}`,
			sttLabel: '',
			productName: `Tiết kiệm(+) bội chi (-) được chấp nhận tháng ${month}`,
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: acceptedMaterialSaving },
			maintains: { totalAmount: acceptedMaintainSaving },
			electricities: { totalAmount: acceptedElectricitySaving },
			totalAmount: acceptedSaving,
		});

		// 6. Cộng trừ vào thu nhập tháng X
		result.push({
			id: `vtcg-summary-income-${vKey}`,
			sttLabel: '',
			productName: `Cộng trừ vào thu nhập tháng ${month}`,
			isBold: true,
			isVehicleSummaryRow: true,
			excludeFromSummary: true,
			materials: { totalAmount: incomeAdjustmentMaterial },
			maintains: { totalAmount: incomeAdjustmentMaintain },
			electricities: { totalAmount: incomeAdjustmentElectricity },
			totalAmount: incomeAdjustment,
		});
	}

	// Chi phí vật tư mau hỏng rẻ tiền (nếu có)
	for (const item of lowValueRows) {
		result.push({
			...item,
			sttLabel: `${stt}.${vehicleIdx + 1}`,
		});
		vehicleIdx++;
	}
}

export function groupByProcessGroup(
	items: LumpSumFinalSettlement[],
	sttStart = 1,
	month = 1,
	revenueCostAdjustmentConfigs?: RevenueCostAdjustmentConfig[],
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
		let name = first.processGroupName?.trim() ?? '';

		const isVtcg = groupItems.some(
			(item) =>
				(item.processGroupCode || '').toUpperCase().includes('VTCG') ||
				(item.processGroupName || '').toLowerCase().includes('cơ giới'),
		);

		const isVtl = groupItems.some(
			(item) =>
				(item.processGroupCode || '').toUpperCase().includes('VTL') ||
				(item.processGroupName || '').toLowerCase().includes('vận tải lò'),
		);

		if (name.startsWith('VTCG - ') || name.startsWith('vtcg - ')) {
			name = name.substring(7).trim();
		} else if (name.startsWith('VTL - ') || name.startsWith('vtl - ')) {
			name = name.substring(6).trim();
		}

		let groupTitle = (name || code || 'Chưa phân nhóm').toUpperCase();
		if (isVtcg) {
			groupTitle = (name || 'VẬN TẢI CƠ GIỚI').toUpperCase();
		} else if (isVtl) {
			groupTitle = (name || 'VẬN TẢI LÒ').toUpperCase();
		}

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

		if (isVtcg) {
			pushVtcgGroupItems(
				result,
				groupItems,
				stt,
				month,
				revenueCostAdjustmentConfigs,
			);
		} else if (isTransportGroup) {
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
