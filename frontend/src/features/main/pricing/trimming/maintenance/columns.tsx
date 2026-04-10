import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Tunneling = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	processGroupTypes: number[];
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export const MAIN_PRICING_TUNNELING_COLUMNS: ColumnDef<Tunneling>[] = [
	{
		accessorKey: 'equipmentCode',
		header: 'Mã thiết bị',
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
		header: 'Đơn giá SCTX (đ/m)',
		cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
	},
];

export type MaintainUnitPriceEquipment = {
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

export const MAIN_PRICING_TUNNELING_EXPAND_COLUMNS: ColumnDef<MaintainUnitPriceEquipment>[] =
	[
		{
			accessorKey: 'partCode',
			header: () => <span className='whitespace-normal'>{'Mã phụ tùng'}</span>,
		},
		{
			accessorKey: 'partName',
			header: () => <span className='whitespace-normal'>{'Tên phụ tùng'}</span>,
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: () => <span className='h-fit whitespace-normal'>{'ĐVT'}</span>,
		},
		{
			accessorKey: 'partCost',
			header: () => (
				<span className='h-fit whitespace-normal'>{'Đơn giá (đ)'}</span>
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
					{'Sản lượng xén lò bình quân (m)'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(row.original.averageMonthlyTunnelProduction),
		},
		{
			accessorKey: 'materialRatePerMetres',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Định mức vật tư SCTX'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(Number(row.original.materialRatePerMetres.toFixed(4))),
		},
		{
			accessorKey: 'materialCostPerMetres',
			header: () => (
				<span className='h-fit whitespace-normal'>
					{'Chi phí vật tư SCTX (đ)'}
				</span>
			),
			cell: ({ row }) =>
				formatNumber(Math.round(row.original.materialCostPerMetres)),
		},
	];

