import { ColumnDef } from '@tanstack/react-table';

export type Clamp = {
	id: string;
	value: string;
};

export const CATALOG_PARAMETER_CLAMP_COLUMNS: ColumnDef<Clamp>[] = [
	{
		accessorKey: 'value',
		header: 'Tỷ lệ đá kẹp (Ckẹp)',
	},
];
