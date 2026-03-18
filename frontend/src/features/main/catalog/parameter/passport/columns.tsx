import { ColumnDef } from '@tanstack/react-table';

export type Passport = {
	id: string;
	name: string;
	sd: string;
	sc: string;
};

export const CATALOG_PARAMETER_PASSPORT_COLUMNS: ColumnDef<Passport>[] = [
	{
		accessorKey: 'name',
		header: 'Hộ chiếu, Sđ, Sc',
		cell: ({ row }) => {
			return `H/c ${row.original.name}; ${row.original.sd}; ${row.original.sc}`;
		},
	},
];
