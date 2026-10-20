import type { ColumnDef } from '@tanstack/react-table';

export type Position = {
	id: number;
	code?: string | null;
	name: string;
	level: number;
	description?: string | null;
};

export const CATALOG_POSITION_COLUMNS: ColumnDef<Position>[] = [
	{
		accessorKey: 'code',
		header: 'Mã chức vụ',
	},
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
		header: 'Ghi chú',
	},
];
