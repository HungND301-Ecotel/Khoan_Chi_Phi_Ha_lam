import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const MAIN_COST_PLAN_COLUMNS: ColumnDef<CostProduct>[] = [
	{
		accessorKey: 'productCode',
		header: () => <span className='whitespace-normal'>Mã sản phẩm</span>,
	},
	{
		accessorKey: 'productName',
		header: () => <span className='whitespace-normal'>Tên sản phẩm</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.productName}</span>
		),
	},
	{
		accessorKey: 'processGroupCode',
		header: () => <span className='whitespace-normal'>Mã nhóm CĐSX</span>,
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: () => <span className='whitespace-normal'>Đơn vị tính</span>,
	},
	{
		id: 'time',
		header: () => <span>Thời gian</span>,
		cell: ({ row }) => (
			<span>
				<span>{formatDate(row.original.startMonth)}</span>
				<br />
				<span>{formatDate(row.original.endMonth)}</span>
			</span>
		),
	},
	{
		accessorKey: 'totalProductionMeters',
		header: () => (
			<span>
				Sản lượng <br /> kế hoạch <br /> ban đầu
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.totalProductionMeters),
	},
	{
		accessorKey: 'plannedTotalCost',
		header: () => (
			<span>
				Doanh thu <br /> kế hoạch <br /> ban đầu (đ)
			</span>
		),
		cell: ({ row }) =>
			formatNumber(row.original.plannedTotalCost, { maximumFractionDigits: 0 }),
	},
];
