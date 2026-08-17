import { ColumnDef } from '@tanstack/react-table';

export type TransportMode =
	| 'conveyor'
	| 'shaft'
	| 'cable_winch'
	| 'monorail'
	| 'other';

export type TransportUnitPrice = {
	id: string;
	code?: string;
	displayCode?: string;
	transportMode?: TransportMode;
	productionProcessId: string;
	productionProcessCode?: string;
	productionProcessName?: string;
	transportRouteId?: string;
	transportRouteCode?: string;
	transportRouteName?: string;
	departmentId?: string;
	departmentName?: string;
	contractCodeId?: string;
	contractCodeName?: string;
	equipmentId?: string;
	equipmentName?: string;
	materialId?: string;
	materialName?: string;
	equipmentQuality?: string;
	quantity?: number;
	unitOfMeasureId?: string;
	unitOfMeasureName?: string;
	materialFuelUnitPrice?: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice?: number;
	isLowVolumeCase?: boolean;
	startMonth: string;
	endMonth: string;
	// Nhóm theo (Công đoạn sản xuất, Thời gian) — 1 hàng danh sách = 1 lần tạo mới trên form,
	// gồm nhiều dòng Tuyến/Đơn vị/Nhóm vật tư/Chất lượng thiết bị con nằm trong items.
	itemCount?: number;
	items?: TransportUnitPrice[];
};

export const formatMoney = (value?: number) => {
	if (value === undefined || value === null) return '-';
	return new Intl.NumberFormat('vi-VN').format(value);
};

// Đúng logic suy luận trong form.tsx (tạo mới/sửa) — giữ riêng ở đây (file không phải
// component) để bảng Xem (expand.tsx) chỉ đọc, không cần state/effect như form.
export function detectTransportMode(code?: string, name?: string): TransportMode {
	const c = (code || '').toUpperCase();
	const n = (name || '').toLowerCase();

	if (c === 'TKVVCVL' || n.includes('trục kéo')) return 'cable_winch';
	if (c === 'VTT' || (n.includes('vận tải trục') && !n.includes('trục kéo')))
		return 'shaft';
	if (c.startsWith('MV') || n.includes('monoray') || n.includes('monorail'))
		return 'monorail';
	if (c === 'TBK' || n.includes('thiết bị khác')) return 'other';
	return 'conveyor';
}

export const MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS: ColumnDef<TransportUnitPrice>[] =
	[
		{
			accessorKey: 'startMonth',
			header: 'Thời gian',
			cell: ({ row }) => (
				<div className='flex flex-col text-xs font-medium'>
					<span>{row.original.startMonth}</span>
					<span className='text-gray-500'>{row.original.endMonth}</span>
				</div>
			),
		},
		{
			accessorKey: 'productionProcessName',
			header: 'Công đoạn sản xuất',
		},
	];
