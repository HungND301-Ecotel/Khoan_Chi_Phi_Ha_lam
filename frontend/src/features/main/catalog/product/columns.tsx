import { ColumnDef } from '@tanstack/react-table';

export type Product = {
	id: string;
	startMonth: string;
	endMonth: string;
	code: string;
	name: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
};

export const CATALOG_PRODUCT_COLUMNS: ColumnDef<Product>[] = [
	{
		accessorKey: 'processGroupCode',
		header: 'Mã nhóm công đoạn sản xuất',
	},
	{
		accessorKey: 'code',
		header: 'Mã sản phẩm',
	},
	{
		accessorKey: 'name',
		header: 'Tên sản phẩm',
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.name}</span>
		),
	},
];
