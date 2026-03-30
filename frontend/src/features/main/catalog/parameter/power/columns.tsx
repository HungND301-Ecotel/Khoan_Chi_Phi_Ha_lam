import { ColumnDef } from '@tanstack/react-table';

export type Power = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_POWER_COLUMNS: ColumnDef<Power>[] = [
	{
		accessorKey: 'value',
		header: 'Công suất',
	},
];
