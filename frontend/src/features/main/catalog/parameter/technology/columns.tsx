import { ColumnDef } from '@tanstack/react-table';

export type Technology = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_TECHNOLOGY_COLUMNS: ColumnDef<Technology>[] = [
	{
		accessorKey: 'value',
		header: 'Công nghệ khai thác',
	},
];
