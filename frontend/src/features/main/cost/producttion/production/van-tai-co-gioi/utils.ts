import { MotorizedCategory } from './types';

export const MOCK_VTCG_EQUIPMENTS = [
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
	// Service Crane / Service vehicle
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

export const MOCK_VTCG_PROCESSES = [
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
	// Excavator / Dozer
	{
		id: 'proc-exc-01',
		code: 'XUC_DAT_DA',
		name: 'Xúc đất đá (Xúc khối lượng)',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-02',
		code: 'XUC_THAN',
		name: 'Xúc than kho (Xúc khối lượng)',
		category: 'excavator_dozer' as MotorizedCategory,
	},
	{
		id: 'proc-exc-03',
		code: 'GIO_PV_XUC',
		name: 'Giờ phục vụ máy xúc',
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
	// Service Crane / Service vehicle
	{
		id: 'proc-crane-01',
		code: 'GIO_PV_CAU',
		name: 'Giờ phục vụ xe cẩu',
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

export const MOCK_HAUL_DISTANCES = [
	{ id: 'dist-02', value: '0.2', name: '0.2 km' },
	{ id: 'dist-05', value: '0.5', name: '0.5 km' },
	{ id: 'dist-10', value: '1.0', name: '1.0 km' },
	{ id: 'dist-15', value: '1.5', name: '1.5 km' },
	{ id: 'dist-20', value: '2.0', name: '2.0 km' },
	{ id: 'dist-30', value: '3.0', name: '3.0 km' },
	{ id: 'dist-50', value: '5.0', name: '5.0 km' },
	{ id: 'dist-70', value: '7.0', name: '7.0 km' },
];

export const MOCK_CARGO_TYPES = [
	{ id: 'cargo-01', code: 'TNK', name: 'Than nguyên khai' },
	{ id: 'cargo-02', code: 'DD', name: 'Đất đá' },
	{ id: 'cargo-03', code: 'BUN', name: 'Bùn lắng' },
	{ id: 'cargo-04', code: 'TCAM', name: 'Than cám & các loại cục' },
	{ id: 'cargo-05', code: 'DAS', name: 'Đá sàng, bã sàng' },
];

export const MOCK_LOCATIONS = [
	{ id: 'loc-rec-01', name: 'Khai trường vỉa 11', locationType: 1 },
	{ id: 'loc-rec-02', name: 'Máng rót MB +38', locationType: 1 },
	{ id: 'loc-rec-03', name: 'Máy xúc PC1250 số 1', locationType: 1 },
	{ id: 'loc-dmp-01', name: 'Kho than số 5 (Kho BHN)', locationType: 2 },
	{ id: 'loc-dmp-02', name: 'Kho than số 6 (mức +75)', locationType: 2 },
	{ id: 'loc-dmp-03', name: 'Bãi thải Đông Cao Sơn', locationType: 2 },
	{ id: 'loc-dmp-04', name: 'Bãi thải Tây Khe Sim', locationType: 2 },
];

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

export function getUnitForProcess(processName?: string, category?: MotorizedCategory): string {
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
