import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type ProductionAdjustment = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName?: string;
	processGroupType?: number;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	totalProductionMeters: number;
	plannedTotalCost: number;
	actualTotalCost: number;
	adjustmentTotalCost: number;
	startMonth: string;
	endMonth: string;
};

export type DepartmentAdjustmentGroup = {
	id: string;
	code: string;
	name: string;
	startMonth?: string;
	endMonth?: string;
	productUnitPriceIds: string[];
};

export const ADJUSTMENT_DEPARTMENT_COLUMNS: ColumnDef<DepartmentAdjustmentGroup>[] =
	[
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

export const MAIN_COST_ADJUSTMENT_COLUMNS: ColumnDef<ProductionAdjustment>[] = [
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
				Sản lượng <br /> thực tế
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.totalProductionMeters),
	},
	{
		accessorKey: 'adjustmentTotalCost',
		header: () => <span>Doanh thu điều chỉnh (đ)</span>,
		cell: ({ row }) =>
			formatNumber(Math.round(row.original.adjustmentTotalCost)),
	},
];
