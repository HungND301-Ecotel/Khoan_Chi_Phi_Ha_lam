import { ColumnDef } from '@tanstack/react-table';

export type Insert = {
	id: string;
	value: string;
	coefficientValue: number;
	note?: string;
};

export const CATALOG_PARAMETER_INSERT_COLUMNS: ColumnDef<Insert>[] = [
	{
		accessorKey: 'value',
		header: 'Chèn',
	},
	{
		accessorKey: 'note',
		header: 'Ghi chú',
	},
];
