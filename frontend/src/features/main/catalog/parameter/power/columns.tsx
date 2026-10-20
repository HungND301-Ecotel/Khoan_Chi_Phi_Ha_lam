import { ColumnDef } from '@tanstack/react-table';

export type Power = {
	id: string;
	value: string;
	note?: string;
};

export const CATALOG_PARAMETER_POWER_COLUMNS: ColumnDef<Power>[] = [
	{
		accessorKey: 'value',
		header: 'Công suất',
	},
	{
		accessorKey: 'note',
		header: 'Ghi chú',
	},
];
