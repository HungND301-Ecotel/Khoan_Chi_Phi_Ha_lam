import { ColumnDef } from '@tanstack/react-table';
import { formatNumber } from '@/lib/utils';

export type ProductionOutputTransportLineRow = {
	productionProcessId: string;
	productionProcessCode?: string;
	productionProcessName?: string;
	equipmentId?: string;
	equipmentCode?: string;
	equipmentName?: string;
	equipmentQuality?: string;
	transportRouteId?: string;
	transportRouteCode?: string;
	transportRouteName?: string;
	routeDepartmentId?: string;
	routeDepartmentCode?: string;
	routeDepartmentName?: string;
	productionMeters: number;
};

export type TransportLineRow = {
	id: string;
	productionProcessCode: string;
	productionProcessName: string;
	routeOrEquipmentCode: string;
	routeOrEquipmentName: string;
	// Chiều khoán chi tiết bên trong Tuyến/Thiết bị: "Đơn vị áp dụng cho tuyến" (công đoạn Băng
	// tải) hoặc "Chất lượng thiết bị" (công đoạn Monoray) — 2 công đoạn còn lại không có.
	detailLabel?: string;
	productionMeters: number;
};

// Bảng xem lại "Vận hành sản xuất" cho nhóm công đoạn VTL/VTCG — thay cột Mã/Tên sản phẩm bằng
// Công đoạn + Tuyến/Thiết bị, song song PRODUCTION_GROUP_PRODUCT_COLUMNS bên Khai thác.
export const TRANSPORT_LINE_COLUMNS: ColumnDef<TransportLineRow>[] = [
	{
		accessorKey: 'productionProcessCode',
		header: () => <span>Mã công đoạn</span>,
	},
	{
		accessorKey: 'productionProcessName',
		header: () => <span>Tên công đoạn</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.productionProcessName}
			</span>
		),
	},
	{
		accessorKey: 'routeOrEquipmentCode',
		header: () => <span>Mã tuyến/thiết bị</span>,
	},
	{
		accessorKey: 'routeOrEquipmentName',
		header: () => <span>Tên tuyến/thiết bị</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.routeOrEquipmentName}
			</span>
		),
	},
	{
		accessorKey: 'detailLabel',
		header: () => <span>Đơn vị / Chất lượng thiết bị</span>,
		cell: ({ row }) => row.original.detailLabel || '-',
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => formatNumber(row.original.productionMeters ?? 0),
	},
];

export function toTransportLineRows(
	groupKey: string,
	transportLines: ProductionOutputTransportLineRow[],
): TransportLineRow[] {
	return transportLines.map((line, index) => ({
		id: `${groupKey}-${line.productionProcessId}-${line.transportRouteId ?? line.equipmentId ?? index}`,
		productionProcessCode: line.productionProcessCode ?? '-',
		productionProcessName: line.productionProcessName ?? '-',
		routeOrEquipmentCode:
			line.transportRouteCode ?? line.equipmentCode ?? '-',
		routeOrEquipmentName:
			line.transportRouteName ?? line.equipmentName ?? '-',
		detailLabel: line.equipmentQuality
			? `Thiết bị loại ${line.equipmentQuality}`
			: line.routeDepartmentCode || line.routeDepartmentName
				? `Đơn vị: ${line.routeDepartmentCode ?? ''} ${line.routeDepartmentName ?? ''}`.trim()
				: undefined,
		productionMeters: line.productionMeters ?? 0,
	}));
}
