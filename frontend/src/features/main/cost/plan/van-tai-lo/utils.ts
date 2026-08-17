import { formatNumber } from '@/lib/utils';
import { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';

export function detectTransportMode(code?: string, name?: string) {
	const c = (code || '').toUpperCase();
	const n = (name || '').toLowerCase();

	if (c.startsWith('BT') || n.includes('băng tải')) return 'conveyor';
	if (c.startsWith('TV') || n.includes('trục vọt') || n.includes('trục tải'))
		return 'shaft';
	if (c.startsWith('TC') || n.includes('tời cáp') || n.includes('tời nâng'))
		return 'cable_winch';
	if (c.startsWith('MV') || n.includes('monoray') || n.includes('monorail'))
		return 'monorail';
	if (c === 'TBK' || n.includes('thiết bị khác')) return 'other';
	return 'conveyor';
}

export function formatAdjustmentOptionLabel(description: string, value?: number | null) {
	if (value === null || value === undefined) return description;
	return `${description} - ${formatNumber(value)}`;
}

export function isConveyorMode(mode: string) {
	return mode === 'conveyor';
}

export function isShaftMode(mode: string) {
	return mode === 'shaft' || mode === 'cable_winch';
}

export function isMonorailMode(mode: string) {
	return mode === 'monorail';
}

export function isOtherMode(mode: string) {
	return mode === 'other';
}

export function buildTransportMonthsPayload(
	transportMonths: DepartmentPlanFormSchema['transportMonths'],
) {
	return transportMonths.map((m) => {
		const itemsPayload = (m.processes || []).flatMap((p) => {
			const processId = p.productionProcessId;
			if (!processId) return [];

			return (p.items || [])
				.filter(
					(item) =>
						typeof item.productionMeters === 'number' &&
						!Number.isNaN(item.productionMeters) &&
						item.productionMeters > 0,
				)
				.map((item) => {
					const k1Selection = item.k1Factor;
					const k2Selection = item.k2Factor;

					const formatFactor = (factor?: typeof k1Selection) => {
						if (
							!factor ||
							(!factor.adjustmentFactorDescriptionId &&
								(factor.customValue === null || factor.customValue === undefined))
						) {
							return undefined;
						}
						return {
							adjustmentFactorId: factor.adjustmentFactorId || undefined,
							adjustmentFactorDescriptionId:
								factor.adjustmentFactorDescriptionId || undefined,
							customValue: factor.customValue ?? undefined,
						};
					};

					return {
						id: item.id || undefined,
						transportPlanLineId: item.id || undefined,
						productionProcessId: processId,
						transportRouteId: item.transportRouteId || undefined,
						routeDepartmentId: item.departmentId || undefined,
						contractCodeId: item.contractCodeId || undefined,
						equipmentQuality: item.equipmentQuality || undefined,
						productionMeters: item.productionMeters,
						unitOfMeasureId: item.unitOfMeasureId || undefined,
						k1: formatFactor(k1Selection),
						k2: formatFactor(k2Selection),
					};
				});
		});

		return {
			month: m.month,
			lowValuePerishableSupply: m.lowValuePerishableSupply,
			items: itemsPayload,
		};
	});
}
