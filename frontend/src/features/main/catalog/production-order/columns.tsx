import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type ProductionOrder = {
	id: string;
	code: string;
	name: string;
	startMonth: string;
	endMonth: string;
};

export const CATALOG_PARAMETER_PRODUCTION_ORDER_COLUMNS: ColumnDef<ProductionOrder>[] =
	[
		{
			accessorKey: 'code',
			header: 'Số Quyết định, lệnh sản xuất',
		},
		{
			accessorKey: 'name',
			header: 'Tên Quyết định, lệnh sản xuất',
		},
		{
			accessorKey: 'startMonth',
			header: 'Thời gian',
			cell: ({ row }) => (
				<span>
					<span>{formatDate(row.original.startMonth)}</span>
					<br />
					<span>{formatDate(row.original.endMonth)}</span>
				</span>
			),
		},
	];
