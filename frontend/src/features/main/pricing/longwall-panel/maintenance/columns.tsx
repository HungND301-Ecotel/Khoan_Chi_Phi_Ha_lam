import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type LongwallPanel = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export const MAIN_PRICING_LONGWALL_PANEL_COLUMNS: ColumnDef<LongwallPanel>[] = [
	{
		accessorKey: 'equipmentCode',
		header: 'Nhóm vật tư, tài sản',
		cell: ({ row }) =>
			`${row.original.equipmentCode} - ${row.original.equipmentName}`,
	},
	{
		accessorKey: 'time',
		header: 'Thời gian',
		cell: ({ row }) => (
			<span>
				<span>{formatDate(row.original.startMonth)}</span>
				<br />
				<span>{formatDate(row.original.endMonth)}</span>
			</span>
		),
	},
	{
		accessorKey: 'totalPrice',
		header: 'Chi phí vật tư SCTX cho 1 thiết bị /1 tấn than NK (đ/t)',
		cell: ({ row }) => formatNumber(row.original.totalPrice),
	},
];

export type MaintainUnitPriceLongwallPanel = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	partId: string;
	partCode: string;
	partName: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	partCost: number;
	replacementTimeStandard: number;
	averageMonthlyTunnelProduction: number;
	quantity: number;
	materialRatePerMetres: number;
	materialCostPerMetres: number;
};

export const MAIN_PRICING_LONGWALL_PANEL_EXPAND_COLUMNS: ColumnDef<MaintainUnitPriceLongwallPanel>[] =
	[
		{
			accessorKey: 'partCode',
			header: () => <span className='whitespace-normal'>{'Mã vật tư'}</span>,
		},
		{
			accessorKey: 'partName',
			header: () => <span className='whitespace-normal'>{'Tên vật tư'}</span>,
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: () => <span className='h-fit whitespace-normal'>{'ĐVT'}</span>,
		},
		{
			accessorKey: 'partCost',
			header: () => (
				<span className='h-fit whitespace-normal'>{'Đơn giá vật tư (đ/t'}</span>
			),
			cell: ({ row }) => formatNumber(row.original.partCost),
		},
		{
			accessorKey: 'replacementTimeStandard',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Định mức thời gian thay thế (tháng)'}
				</span>
			),
			cell: ({ row }) => formatNumber(row.original.replacementTimeStandard),
		},
		{
			accessorKey: 'quantity',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Số lượng vật tư 1 lần thay thế'}
				</span>
			),
			cell: ({ row }) => formatNumber(row.original.quantity),
		},
		{
			accessorKey: 'averageMonthlyTunnelProduction',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Sản lượng than bình quân tháng (1000 tấn)'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(row.original.averageMonthlyTunnelProduction),
		},
		{
			accessorKey: 'materialRatePerMetres',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Định mức vật tư SCTX cho 1 thiết bị /1000 tấn than NK'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(Number(row.original.materialRatePerMetres.toFixed(4))),
		},
		{
			accessorKey: 'materialCostPerMetres',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Chi phí vật tư SCTX cho 1 phụ tùng/1 tấn than NK (đ/t)'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(row.original.materialCostPerMetres),
		},
	];
