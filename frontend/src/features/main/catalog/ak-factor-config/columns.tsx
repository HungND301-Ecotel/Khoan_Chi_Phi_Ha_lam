import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AkFactorConfig = {
	id: string;
	processGroupId: string;
	processGroupCode?: string | null;
	processGroupName?: string | null;
	minAkDiff?: number;
	maxAkDiff?: number;
	minAdjustmentRate?: number;
	maxAdjustmentRate?: number;
	akDiffDisplay?: string | null;
	adjustmentRateDisplay?: string | null;
	description?: string | null;
};

export const CATALOG_AK_FACTOR_CONFIG_COLUMNS: ColumnDef<AkFactorConfig>[] =
	[
		{
			accessorKey: 'processGroupCode',
			header: 'Mã nhóm công đoạn',
			cell: ({ row }) => row.original.processGroupCode || '',
		},
		{
			accessorKey: 'processGroupName',
			header: 'Tên nhóm công đoạn',
			cell: ({ row }) => row.original.processGroupName || '',
		},
		{
			accessorKey: 'akDiffDisplay',
			header: 'Chênh lệch Ak',
			cell: ({ row }) => {
				if (row.original.akDiffDisplay) {
					return row.original.akDiffDisplay;
				}

				const min = row.original.minAkDiff;
				const max = row.original.maxAkDiff;
				if (min == null && max == null) {
					return 'Không giới hạn';
				}
				if (min != null && max != null) {
					return min === max
						? formatNumber(min)
						: `${formatNumber(min)} - ${formatNumber(max)}`;
				}
				if (min != null) {
					return `≥ ${formatNumber(min)}`;
				}
				return `≤ ${formatNumber(max ?? 0)}`;
			},
		},
		{
			accessorKey: 'adjustmentRateDisplay',
			header: 'Tỷ lệ điều chỉnh doanh thu',
			cell: ({ row }) => {
				if (row.original.adjustmentRateDisplay) {
					return row.original.adjustmentRateDisplay;
				}
				const min = row.original.minAdjustmentRate;
				const max = row.original.maxAdjustmentRate;
				if (min == null && max == null) {
					return '0%';
				}
				if (min != null && max != null) {
					if (min === max) {
						return `${formatNumber(min * 100)}%`;
					}
					return `${formatNumber(min * 100)} - ${formatNumber(max * 100)}%`;
				}
				if (min != null) {
					return `≥ ${formatNumber(min * 100)}%`;
				}
				return `≤ ${formatNumber((max ?? 0) * 100)}%`;
			},
		},
		{
			accessorKey: 'description',
			header: 'Mô tả',
		},
	];
