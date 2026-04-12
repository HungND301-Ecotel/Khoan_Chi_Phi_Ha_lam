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
