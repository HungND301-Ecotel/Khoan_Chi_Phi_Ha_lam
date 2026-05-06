import type { ColumnDef } from '@tanstack/react-table';

export type Department = {
	id: string;
	code: string;
	name: string;
};

export const CATALOG_DEPARTMENT_COLUMNS: ColumnDef<Department>[] = [
	{
		accessorKey: 'code',
		header: 'Mã đơn vị',
	},
	{
		accessorKey: 'name',
		header: 'Tên đơn vị',
	},
];
