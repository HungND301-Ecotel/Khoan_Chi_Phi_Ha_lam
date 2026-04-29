import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type DepartmentPlanGroup = {
	id: string;
	code: string;
	name: string;
	startMonth?: string;
	endMonth?: string;
	productUnitPriceIds: string[];
};

export const PLAN_DEPARTMENT_COLUMNS: ColumnDef<DepartmentPlanGroup>[] = [
	{
		accessorKey: 'code',
		header: () => <span className='whitespace-normal'>Mã đơn vị</span>,
	},
	{
		accessorKey: 'name',
		header: () => <span className='whitespace-normal'>Tên đơn vị</span>,
	},
	{
		id: 'time',
		header: () => <span className='whitespace-normal'>Thời gian</span>,
		cell: ({ row }) => {
			const { startMonth, endMonth } = row.original;

			if (!startMonth && !endMonth) return '-';
			if (!startMonth) return formatDate(endMonth!);
			if (!endMonth) return formatDate(startMonth);

			return `${formatDate(startMonth)} - ${formatDate(endMonth)}`;
		},
	},
];

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
		cell: ({ row }) => formatDate(row.original.startMonth),
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
