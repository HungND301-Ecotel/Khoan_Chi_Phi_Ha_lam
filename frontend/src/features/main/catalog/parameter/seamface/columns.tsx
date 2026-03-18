import { ColumnDef } from '@tanstack/react-table';

export type Seamface = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_SEAMFACE_COLUMNS: ColumnDef<Seamface>[] = [
	{
		accessorKey: 'value',
		header: 'Mặt vỉa (M)',
	},
];
