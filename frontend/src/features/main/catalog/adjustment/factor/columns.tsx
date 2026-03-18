import { ColumnDef } from '@tanstack/react-table';

export type Factor = {
	id: string;
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
		accessorKey: 'code',
		header: 'Mã hệ số điều chỉnh',
	},
	{
		accessorKey: 'name',
		header: 'Tên hệ số điều chỉnh',
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.name}</span>
		),
	},
];
