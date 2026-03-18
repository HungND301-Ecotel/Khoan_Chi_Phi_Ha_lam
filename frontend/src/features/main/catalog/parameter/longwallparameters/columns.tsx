import { ColumnDef } from '@tanstack/react-table';

export type Longwallparameters = {
	id: string;
	llc: string;
	lkc: number;
	mk: number;
};

export const CATALOG_PARAMETER_LONGWALLPARAMETERS_COLUMNS: ColumnDef<Longwallparameters>[] =
	[
		{
			accessorKey: 'llc',
			header: 'Thông số lò chợ',
			cell: ({ row }) => {
				return `${row.original.llc}; ${row.original.lkc}; ${row.original.mk}`;
			},
		},
	];
