import type { ColumnDef } from '@tanstack/react-table';

export type Position = {
	id: number;
	name: string;
	level: number;
	description: string;
};

export const CATALOG_POSITION_COLUMNS: ColumnDef<Position>[] = [
	{
		accessorKey: 'name',
		header: 'Tên chức vụ',
	},
	{
		accessorKey: 'level',
		header: 'Cấp bậc',
	},
	{
		accessorKey: 'description',
		header: 'Mô tả',
	},
];
