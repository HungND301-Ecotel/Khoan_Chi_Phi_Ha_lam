import { LumpSumFinalSettlement } from './types';

export function groupByProcessGroup(
	items: LumpSumFinalSettlement[],
): LumpSumFinalSettlement[] {
	const groups = new Map<string, LumpSumFinalSettlement[]>();

	for (const item of items) {
		const key = item.processGroupId || `${item.processGroupCode}|${item.processGroupName}`;
		const existing = groups.get(key);
		if (existing) {
			existing.push(item);
			continue;
		}
		groups.set(key, [item]);
	}

	const result: LumpSumFinalSettlement[] = [];
	let stt = 1;

	for (const [, groupItems] of groups) {
		const first = groupItems[0];
		const code = first.processGroupCode?.trim() ?? '';
		const name = first.processGroupName?.trim() ?? '';
		const groupTitle = [code, name].filter(Boolean).join(' - ') || 'Chưa phân nhóm';

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
					(sum, item) => sum + (item.materials?.totalAmount ?? 0),
					0,
				),
			},
			maintains: {
				totalAmount: groupItems.reduce(
					(sum, item) => sum + (item.maintains?.totalAmount ?? 0),
					0,
				),
			},
			electricities: {
				totalAmount: groupItems.reduce(
					(sum, item) => sum + (item.electricities?.totalAmount ?? 0),
					0,
				),
			},
			totalAmount: groupItems.reduce(
				(sum, item) => sum + (item.totalAmount ?? 0),
				0,
			),
		});

		result.push(
			...groupItems.map((item, index) => ({
				...item,
				sttLabel: `${stt}.${index + 1}`,
			})),
		);
		stt += 1;
	}

	return result;
}
