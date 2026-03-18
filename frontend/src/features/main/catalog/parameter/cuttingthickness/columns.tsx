import { ColumnDef } from '@tanstack/react-table';

export type Cuttingthickness = {
	id: string;
	value: string;
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
	];
