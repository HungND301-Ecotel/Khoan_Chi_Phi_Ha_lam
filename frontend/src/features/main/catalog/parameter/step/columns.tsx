import { ColumnDef } from '@tanstack/react-table';

export type Step = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_STEP_COLUMNS: ColumnDef<Step>[] = [
	{
		accessorKey: 'value',
		header: 'Bước chống',
	},
];
