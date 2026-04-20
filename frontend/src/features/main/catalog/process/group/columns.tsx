import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';

export type ProcessGroup = {
	id: string;
	code: string;
	name: string;
	type: ProcessGroupType;
	fixedKeyId?: string | null;
	fixedKeyCode?: string | null;
	fixedKeyName?: string | null;
};

export const CATALOG_PROCESS_GROUP_COLUMNS: ColumnDef<ProcessGroup>[] = [
	{
		accessorKey: 'code',
		header: 'Mã nhóm công đoạn sản xuất',
	},
	{
		accessorKey: 'name',
		header: 'Tên nhóm công đoạn sản xuất',
	},
	{
		accessorKey: 'fixedKeyName',
		header: 'Khóa hệ thống',
		cell: ({ row }) =>
			row.original.fixedKeyName || row.original.fixedKeyCode || 'Chưa gán',
	},
];
