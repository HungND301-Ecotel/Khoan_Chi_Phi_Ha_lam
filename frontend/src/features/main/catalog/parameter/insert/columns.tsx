import { ColumnDef } from '@tanstack/react-table';

export type Insert = {
	id: string;
	value: string;
	coefficientValue: number;
};

export const CATALOG_PARAMETER_INSERT_COLUMNS: ColumnDef<Insert>[] = [
	{
		accessorKey: 'value',
		header: 'Chèn',
	},
];
