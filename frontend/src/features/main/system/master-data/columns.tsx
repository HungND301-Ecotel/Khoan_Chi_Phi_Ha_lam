import { Badge } from '@/components/ui/badge';
import {
	FixedKey,
	getFixedKeyTypeLabel,
} from '@/constants/fixed-key';
import { ColumnDef } from '@tanstack/react-table';

export const MASTER_DATA_COLUMNS: ColumnDef<FixedKey>[] = [
	{
		accessorKey: 'code',
		header: 'Mã fixed key',
	},
	{
		accessorKey: 'name',
		header: 'Tên fixed key',
	},
	{
		accessorKey: 'type',
		header: 'Loại',
		cell: ({ row }) => getFixedKeyTypeLabel(row.original.type),
	},
	{
		accessorKey: 'isSystem',
		header: 'Hệ thống',
		cell: ({ row }) => (
			<Badge variant={row.original.isSystem ? 'default' : 'secondary'}>
				{row.original.isSystem ? 'Hệ thống' : 'Người dùng'}
			</Badge>
		),
	},
];