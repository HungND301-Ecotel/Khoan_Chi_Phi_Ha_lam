import { ColumnDef } from '@tanstack/react-table';

export type Cuttingthickness = {
	id: string;
	value: string;
	note?: string;
};

export const CATALOG_PARAMETER_CUTTINGTHICKNESS_COLUMNS: ColumnDef<Cuttingthickness>[] =
	[
		{
			accessorKey: 'value',
			header: 'Chiều dày lớp khấu',
			cell: ({ row }) => {
				return row.original.value;
			},
		},
		{
			accessorKey: 'note',
			header: 'Ghi chú',
		},
	];
