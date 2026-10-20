import type { ColumnDef } from '@tanstack/react-table';

export type Unit = {
	id: string;
	name: string;
	note?: string;
	index?: number;
};

export const CATALOG_UNIT_COLUMNS: ColumnDef<Unit>[] = [
	{
		accessorKey: 'name',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'note',
		header: 'Ghi chú',
	},
];
