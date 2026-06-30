import { LumpSumFinalSettlement } from './types';

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

		stt += 1;
	}

	return result;
}
