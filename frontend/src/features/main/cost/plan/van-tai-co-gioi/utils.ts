import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { MotorizedCategory } from './types';

export function getSuggestedFuelAdjustmentFactor(
	category?: MotorizedCategory,
	processName?: string,
	equipmentCode?: string,
	distanceValue?: string | number,
): number {
	const n = (processName || '').toLowerCase();
	const eq = (equipmentCode || '').toUpperCase();

	// 1. Scania Giờ phục vụ
	if (category === 'scania') {
		if (n.includes('hút bùn đổ thải')) return 1.0;
		if (n.includes('phục vụ các đơn vị') || n.includes('đơn vị')) return 0.7;
		if (distanceValue) {
			const d =
				typeof distanceValue === 'string'
					? parseFloat(distanceValue)
					: distanceValue;
			if (!Number.isNaN(d) && d < 2.0 && d >= 0.2) {
				// Cung độ < 2.0 km: hệ số từ 1.18 xuống 1.00
				const step = Math.round(d * 10);
				const f = 1.0 + (20 - step) * 0.01;
				return Math.round(f * 100) / 100;
			}
		}
		return 1.0;
	}

	// 2. Máy xúc / Máy gạt
	if (category === 'excavator_dozer') {
		if (eq.includes('2689') || eq.includes('DX210')) {
			if (n.includes('xúc than') || n.includes('phục vụ')) return 0.85;
		}
		if (eq.includes('JCB') && (n.includes('phục vụ') || n.includes('vượt')))
			return 0.85;
		if (eq.includes('D7R') && n.includes('phục vụ')) return 0.85;
		return 1.0;
	}

	// 3. Xe cẩu / Xe nâng / Xe dịch vụ
	if (category === 'service_crane') {
		if (
			eq.includes('FORKLIFT') ||
			eq.includes('DS') ||
			n.includes('xe nâng')
		) {
			if (n.includes('phục vụ')) return 0.85;
		}
		return 1.0;
	}

	return 1.0;
}

export function getUnitForMotorizedProcess(
	processName?: string,
	category?: MotorizedCategory,
): string {
	const n = (processName || '').toLowerCase();
	if (
		n.includes('giờ') ||
		n.includes('phục vụ') ||
		n.includes('di chuyển') ||
		n.includes('gạt')
	) {
		return 'giờ';
	}
	if (n.includes('tưới đường')) {
		return 'tkm';
	}
	if (category === 'scania') {
		return 'tấn';
	}
	if (category === 'vacuum_truck') {
		return 'm³';
	}
	if (n.includes('xúc')) {
		return 'm³';
	}
	return 'tấn';
}

export function buildMotorizedTransportMonthsPayload(
	motorizedMonths: DepartmentPlanFormSchema['motorizedMonths'] = [],
) {
	return motorizedMonths.map((m) => {
		const itemsPayload = (m.items || [])
			.filter(
				(item) =>
					typeof item.productionMeters === 'number' &&
					!Number.isNaN(item.productionMeters) &&
					item.productionMeters > 0,
			)
			.map((item) => ({
				id: item.id || undefined,
				transportPlanLineId: item.id || undefined,
				productionProcessId: item.productionProcessId,
				equipmentId: item.equipmentId || undefined,
				equipmentQuality: item.equipmentQuality || undefined,
				haulDistanceId: item.haulDistanceId || undefined,
				cargoTypeId: item.cargoTypeId || undefined,
				receivingLocationId: item.receivingLocationId || undefined,
				dumpingLocationId: item.dumpingLocationId || undefined,
				productionMeters: item.productionMeters,
				unitOfMeasureId: item.unitOfMeasureId || undefined,
				fuelAdjustmentFactor: item.fuelAdjustmentFactor ?? 1.0,
			}));

		return {
			month: m.month,
			lowValuePerishableSupply: m.lowValuePerishableSupply ?? false,
			items: itemsPayload,
		};
	});
}
