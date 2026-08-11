import { ColumnDef } from '@tanstack/react-table';

export type CargoType = {
	id: string;
	code: string;
	name: string;
	note?: string;
};

export const CATALOG_CARGO_TYPE_COLUMNS: ColumnDef<CargoType>[] = [
	{
		accessorKey: 'code',
		header: 'Mã chủng loại hàng',
	},
	{
		accessorKey: 'name',
		header: 'Chủng loại hàng',
	},
	{
		accessorKey: 'note',
		header: 'Ghi chú',
		cell: ({ row }) => row.original.note || '-',
	},
];
