import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { MotorizedCategory } from './types';

export const MOCK_PLAN_VTCG_EQUIPMENTS = [
	// Scania
	{
		id: 'eq-scania-01',
		code: 'SCANIA-01',
		name: 'Xe ô tô Scania 30T số 01',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'eq-scania-02',
		code: 'SCANIA-02',
		name: 'Xe ô tô Scania 30T số 02',
		category: 'scania' as MotorizedCategory,
	},
	// Excavator / Dozer
	{
		id: 'eq-exc-dx210',
		code: '14LA-2689',
		name: 'Máy xúc TL 14LA-2689 (Doosan DX210)',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'eq-exc-jcb',
		code: 'JCB-01',
		name: 'Máy đào liên hợp JCB',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'eq-doz-d7r-01',
		code: 'D7RII-01',
		name: 'Máy gạt D7RII số 01',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'eq-doz-d7r-02',
		code: 'D7RII-02',
		name: 'Máy gạt D7RII số 02',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'eq-exc-01',
		code: 'KOMATSU-PC1250',
		name: 'Máy xúc thủy lực Komatsu PC1250',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'eq-doz-01',
		code: 'KOMATSU-D85',
		name: 'Máy gạt Komatsu D85A-21',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	// Service Crane / Forklift / Service vehicle
	{
		id: 'eq-forklift-ds01',
		code: 'FORKLIFT-DS01',
		name: 'Xe nâng DS số 01 (5 tấn)',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-forklift-ds02',
		code: 'FORKLIFT-DS02',
		name: 'Xe nâng DS số 02 (5 tấn)',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-forklift-ds03',
		code: 'FORKLIFT-DS03',
		name: 'Xe nâng DS số 03 (5 tấn)',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-forklift-ds04',
		code: 'FORKLIFT-DS04',
		name: 'Xe nâng DS số 04 (5 tấn)',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-crane-01',
		code: 'CRANE-KATO-25T',
		name: 'Xe cẩu tự hành Kato 25T',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-water-01',
		code: 'WATER-TRUCK-15M3',
		name: 'Xe tưới đường mỏ Dongfeng 15m3',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'eq-pickup-01',
		code: 'PICKUP-FORD',
		name: 'Xe bán tải phục vụ Ford Ranger',
		category: 'service_crane' as MotorizedCategory,
	},
	// Vacuum Truck
	{
		id: 'eq-vac-01',
		code: 'VACUUM-DONGFENG-10M3',
		name: 'Xe hút chất thải Dongfeng 10m3',
		category: 'vacuum_truck' as MotorizedCategory,
	},
];

export const MOCK_PLAN_VTCG_PROCESSES = [
	// Scania
	{
		id: 'proc-scania-01',
		code: 'VC_THAN',
		name: 'Vận chuyển than nguyên khai',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'proc-scania-02',
		code: 'VC_DAT_DA',
		name: 'Vận chuyển đất đá khai trường',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'proc-scania-03',
		code: 'VC_BUN',
		name: 'Vận chuyển bùn bể lắng',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'proc-scania-04',
		code: 'VC_THAN_CAM',
		name: 'Vận chuyển than cám & các loại cục',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'proc-scania-pv-bun',
		code: 'GPV_HUT_BUN',
		name: 'Giờ phục vụ - hút bùn đổ thải',
		category: 'scania' as MotorizedCategory,
	},
	{
		id: 'proc-scania-pv-dv',
		code: 'GPV_DON_VI',
		name: 'Giờ phục vụ - phục vụ các đơn vị',
		category: 'scania' as MotorizedCategory,
	},
	// Excavator / Dozer
	{
		id: 'proc-exc-xuc-kho',
		code: 'XUC_THAN_KHO',
		name: 'Xúc than các kho',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-01',
		code: 'XUC_DAT_DA',
		name: 'Xúc đất đá (Xúc khối lượng)',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-03',
		code: 'GIO_PV_XUC',
		name: 'Giờ phục vụ máy xúc',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-pv-vuot',
		code: 'GIO_PV_VUOT',
		name: 'Giờ phục vụ vượt',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-04',
		code: 'GIO_DC_XUC',
		name: 'Giờ di chuyển máy xúc',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-05',
		code: 'GIO_GAT_PV',
		name: 'Giờ gạt phục vụ',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-06',
		code: 'GIO_GAT_DC',
		name: 'Giờ gạt di chuyển',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	// Service Crane / Forklift / Service vehicle
	{
		id: 'proc-crane-01',
		code: 'GIO_PV_CAU',
		name: 'Giờ phục vụ xe cẩu',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'proc-crane-pv-forklift',
		code: 'GIO_PV_XE_NANG',
		name: 'Giờ phục vụ xe nâng',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'proc-crane-02',
		code: 'TUOI_DUONG',
		name: 'Tưới đường mỏ',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'proc-crane-03',
		code: 'GIO_DC_CAU',
		name: 'Giờ di chuyển xe cẩu',
		category: 'service_crane' as MotorizedCategory,
	},
	{
		id: 'proc-crane-04',
		code: 'GIO_PV_XE_BAN_TAI',
		name: 'Giờ phục vụ xe bán tải',
		category: 'service_crane' as MotorizedCategory,
	},
	// Vacuum Truck
	{
		id: 'proc-vac-01',
		code: 'HUT_CHAT_THAI',
		name: 'Hút và vận chuyển chất thải',
		category: 'vacuum_truck' as MotorizedCategory,
	},
	{
		id: 'proc-vac-02',
		code: 'HUT_BUN_HO_LANG',
		name: 'Hút bùn hố lắng',
		category: 'vacuum_truck' as MotorizedCategory,
	},
];

export const MOCK_PLAN_HAUL_DISTANCES = [
	{ id: 'dist-02', value: '0.2', name: '0.2 km', factor: 1.18 },
	{ id: 'dist-03', value: '0.3', name: '0.3 km', factor: 1.17 },
	{ id: 'dist-04', value: '0.4', name: '0.4 km', factor: 1.16 },
	{ id: 'dist-05', value: '0.5', name: '0.5 km', factor: 1.15 },
	{ id: 'dist-06', value: '0.6', name: '0.6 km', factor: 1.14 },
	{ id: 'dist-07', value: '0.7', name: '0.7 km', factor: 1.13 },
	{ id: 'dist-08', value: '0.8', name: '0.8 km', factor: 1.12 },
	{ id: 'dist-09', value: '0.9', name: '0.9 km', factor: 1.11 },
	{ id: 'dist-10', value: '1.0', name: '1.0 km', factor: 1.10 },
	{ id: 'dist-15', value: '1.5', name: '1.5 km', factor: 1.05 },
	{ id: 'dist-20', value: '2.0', name: '2.0 km', factor: 1.00 },
	{ id: 'dist-30', value: '3.0', name: '3.0 km', factor: 1.00 },
	{ id: 'dist-50', value: '5.0', name: '5.0 km', factor: 1.00 },
	{ id: 'dist-70', value: '7.0', name: '7.0 km', factor: 1.00 },
];

export const MOCK_PLAN_CARGO_TYPES = [
	{ id: 'cargo-01', code: 'TNK', name: 'Than nguyên khai' },
	{ id: 'cargo-02', code: 'DD', name: 'Đất đá' },
	{ id: 'cargo-03', code: 'BUN', name: 'Bùn lắng' },
	{ id: 'cargo-04', code: 'TCAM', name: 'Than cám & các loại cục' },
	{ id: 'cargo-05', code: 'DAS', name: 'Đá sàng, bã sàng' },
];

export const MOCK_PLAN_LOCATIONS = [
	{ id: 'loc-rec-01', name: 'Khai trường vỉa 11', locationType: 1 },
	{ id: 'loc-rec-02', name: 'Máng rót MB +38', locationType: 1 },
	{ id: 'loc-rec-03', name: 'Máy xúc PC1250 số 1', locationType: 1 },
	{ id: 'loc-dmp-01', name: 'Kho than số 5 (Kho BHN)', locationType: 2 },
	{ id: 'loc-dmp-02', name: 'Kho than số 6 (mức +75)', locationType: 2 },
	{ id: 'loc-dmp-03', name: 'Bãi thải Đông Cao Sơn', locationType: 2 },
	{ id: 'loc-dmp-04', name: 'Bãi thải Tây Khe Sim', locationType: 2 },
];

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
			const d = typeof distanceValue === 'string' ? parseFloat(distanceValue) : distanceValue;
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
		if (eq.includes('JCB') && (n.includes('phục vụ') || n.includes('vượt'))) return 0.85;
		if (eq.includes('D7R') && n.includes('phục vụ')) return 0.85;
		return 1.0;
	}

	// 3. Xe cẩu / Xe nâng / Xe dịch vụ
	if (category === 'service_crane') {
		if (eq.includes('FORKLIFT') || eq.includes('DS') || n.includes('xe nâng')) {
			if (n.includes('phục vụ')) return 0.85;
		}
		return 1.0;
	}

	return 1.0;
}

export const MOCK_UNITS_OF_MEASURE = [
	{ value: '%', label: '%' },
	{ value: 'bộ', label: 'bộ' },
	{ value: 'cái', label: 'cái' },
	{ value: 'can', label: 'can' },
	{ value: 'cặp', label: 'cặp' },
	{ value: 'cầu', label: 'cầu' },
	{ value: 'chiếc', label: 'chiếc' },
	{ value: 'tấn', label: 'tấn' },
	{ value: 'm³', label: 'm³' },
	{ value: 'giờ', label: 'giờ' },
	{ value: 'tkm', label: 'tkm' },
	{ value: 'chuyến', label: 'chuyến' },
	{ value: 'ca', label: 'ca' },
	{ value: 'm', label: 'm' },
	{ value: 'kg', label: 'kg' },
	{ value: 'lít', label: 'lít' },
];

export function getUnitForMotorizedProcess(processName?: string, category?: MotorizedCategory): string {
	const n = (processName || '').toLowerCase();
	if (n.includes('giờ') || n.includes('phục vụ') || n.includes('di chuyển') || n.includes('gạt')) {
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
