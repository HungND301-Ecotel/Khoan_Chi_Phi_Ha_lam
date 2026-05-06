import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Production = {
	id: string;
	startMonth: string;
	endMonth: string;
	acceptanceReportId: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	productionMeters?: number;
	standardProductionMeters?: number;
};

export type DepartmentProductionGroup = {
	id: string;
	code: string;
	name: string;
	startMonth?: string;
	endMonth?: string;
	productionOutputIds: string[];
};

export const PRODUCTION_DEPARTMENT_COLUMNS: ColumnDef<DepartmentProductionGroup>[] =
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

export const MAIN_COST_PRODUCTION_COLUMNS: ColumnDef<Production>[] = [
	{
		accessorKey: 'time',
		header: () => <span>Thời gian</span>,
		cell: ({ row }) => <span>{formatDate(row.original.startMonth)}</span>,
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => (
			<span>{formatNumber(row.original.productionMeters ?? 0)}</span>
		),
	},
	{
		accessorKey: 'standardProductionMeters',
		header: () => <span>Sản lượng định mức</span>,
		cell: ({ row }) => (
			<span>{formatNumber(row.original.standardProductionMeters ?? 0)}</span>
		),
	},
];
