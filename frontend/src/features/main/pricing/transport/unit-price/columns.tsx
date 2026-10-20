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
	departmentCode?: string;
	departmentName?: string;
	contractCodeId?: string;
	contractCodeName?: string;
	equipmentId?: string;
	equipmentCode?: string;
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
	note?: string;
	startMonth: string;
	endMonth: string;
	itemCount?: number;
	items?: TransportUnitPrice[];
	periods?: TransportPeriod[];
};

export type TransportPeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	itemCount?: number;
	items: TransportUnitPrice[];
	productionProcessId?: string;
	productionProcessCode?: string;
	productionProcessName?: string;
};

export const formatMoney = (value?: number) => {
	if (value === undefined || value === null) return '-';
	return new Intl.NumberFormat('vi-VN').format(value);
};

// Hiển thị dạng "Mã - Tên" (bỏ phần thiếu)
export const formatCodeName = (code?: string, name?: string) =>
	[code, name].filter(Boolean).join(' - ') || '-';

// Đơn vị tính của các cột đơn giá theo phương thức vận tải (dùng chung form và bảng Xem)
export function getTransportUnitLabel(mode: TransportMode): string {
	switch (mode) {
		case 'conveyor':
			return 'đ/tấn';
		case 'shaft':
			return 'đ/xe';
		case 'cable_winch':
		case 'other':
			return 'đ/tháng';
		case 'monorail':
			return 'đ/h';
		default:
			return 'đ';
	}
}

// Đúng logic suy luận trong form.tsx (tạo mới/sửa) — giữ riêng ở đây (file không phải
// component) để bảng Xem (expand.tsx) chỉ đọc, không cần state/effect như form.
export function detectTransportMode(
	code?: string,
	name?: string,
): TransportMode {
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
			accessorKey: 'productionProcessName',
			header: 'Công đoạn sản xuất',
			cell: ({ row }) => row.original.productionProcessName || '-',
		},
	];
