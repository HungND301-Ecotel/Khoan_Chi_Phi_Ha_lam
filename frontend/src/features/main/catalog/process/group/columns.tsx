import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';

export type ProcessGroup = {
	id: string;
	code: string;
	name: string;
	type: ProcessGroupType;
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
];
