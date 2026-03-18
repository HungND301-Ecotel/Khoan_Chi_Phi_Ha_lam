import { ColumnDef } from '@tanstack/react-table';

export type Strength = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_STRENGTH_COLUMNS: ColumnDef<Strength>[] = [
	{
		accessorKey: 'value',
		header: 'Độ kiên cố than đá (f)',
	},
];
