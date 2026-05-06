import { ColumnDef } from '@tanstack/react-table';
import {
	ADJUSTMENT_FACTOR_TYPE_LABELS,
	ADJUSTMENT_FACTOR_TYPE_OPTIONS,
	isAdjustmentFactorFixedKey,
} from '@/constants/adjustment-factor-type';
import { ProcessGroupType } from '@/constants/process-group';

export type FixedKey = {
	id: string;
	key: string;
	name: string;
	type: number;
};

const PROCESS_GROUP_TYPE_LABELS: Record<ProcessGroupType, string> = {
	[ProcessGroupType.None]: 'Chưa xác định',
	[ProcessGroupType.DL]: 'Đào lò',
	[ProcessGroupType.LC]: 'Lò chợ',
	[ProcessGroupType.XL]: 'Xén lò',
};

function getFixedKeyTypeLabel(fixedKey: FixedKey) {
	if (isAdjustmentFactorFixedKey(fixedKey.key)) {
		return ADJUSTMENT_FACTOR_TYPE_LABELS[fixedKey.type] ?? fixedKey.key;
	}

	return (
		PROCESS_GROUP_TYPE_LABELS[fixedKey.type as ProcessGroupType] ??
		String(fixedKey.type)
	);
}

export const CATALOG_FIXED_KEY_COLUMNS: ColumnDef<FixedKey>[] = [
	{
		accessorKey: 'key',
		header: 'Mã khóa cấu hình',
	},
	{
		accessorKey: 'name',
		header: 'Tên khóa cấu hình',
	},
	{
		accessorKey: 'type',
		header: 'Loại nghiệp vụ',
		cell: ({ row }) => getFixedKeyTypeLabel(row.original),
	},
];

export const FIXED_KEY_TYPE_OPTIONS = [
	{ value: ProcessGroupType.DL, label: 'Đào lò' },
	{ value: ProcessGroupType.LC, label: 'Lò chợ' },
	{ value: ProcessGroupType.XL, label: 'Xén lò' },
	...ADJUSTMENT_FACTOR_TYPE_OPTIONS,
];
