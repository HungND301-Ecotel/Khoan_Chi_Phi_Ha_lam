import { ColumnDef } from '@tanstack/react-table';

export type Factor = {
	id: string;
	fixedKeyId?: string;
	fixedKeyKey?: string;
	fixedKeyType?: number;
	code: string;
	name: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	type: number;
};

export const CATALOG_ADJUSTMENT_FACTOR_COLUMNS: ColumnDef<Factor>[] = [
	{
		accessorKey: 'processGroupCode',
		header: 'Mã nhóm công đoạn sản xuất',
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.processGroupCode}</span>
		),
	},
	{
		accessorKey: 'fixedKeyKey',
		header: 'Hệ số điều chỉnh',
		cell: ({ row }) => row.original.fixedKeyKey ?? row.original.code,
	},
	{
		accessorKey: 'name',
		header: 'Tên hệ số điều chỉnh',
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.name}</span>
		),
	},
];
