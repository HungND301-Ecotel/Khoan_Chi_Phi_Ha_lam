import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';

export type FixedKey = {
	id: string;
	key: string;
	name: string;
	type: ProcessGroupType;
};

const PROCESS_GROUP_TYPE_LABELS: Record<ProcessGroupType, string> = {
	[ProcessGroupType.None]: 'Chưa xác định',
	[ProcessGroupType.DL]: 'Đào lò',
	[ProcessGroupType.LC]: 'Lò chợ',
	[ProcessGroupType.XL]: 'Xén lò',
};

export const CATALOG_FIXED_KEY_COLUMNS: ColumnDef<FixedKey>[] = [
	{
		accessorKey: 'key',
		header: 'Code',
	},
	{
		accessorKey: 'name',
		header: 'Tên khóa cấu hình',
	},
	{
		accessorKey: 'type',
		header: 'Loại nghiệp vụ',
		cell: ({ row }) => PROCESS_GROUP_TYPE_LABELS[row.original.type],
	},
];

export const PROCESS_GROUP_TYPE_OPTIONS = [
	{ value: ProcessGroupType.DL, label: 'Đào lò' },
	{ value: ProcessGroupType.LC, label: 'Lò chợ' },
	{ value: ProcessGroupType.XL, label: 'Xén lò' },
];
