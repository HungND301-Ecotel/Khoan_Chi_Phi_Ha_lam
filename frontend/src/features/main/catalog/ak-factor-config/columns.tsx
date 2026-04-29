import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AkFactorConfig = {
	id: string;
	processGroupId: string;
	processGroupCode?: string | null;
	processGroupName?: string | null;
	akDiffOperator?: string | null;
	akDiffValue?: number | null;
	adjustmentRate?: number | null;
	akDiffDisplay?: string | null;
	adjustmentRateDisplay?: string | null;
	description?: string | null;
};

export const CATALOG_AK_FACTOR_CONFIG_COLUMNS: ColumnDef<AkFactorConfig>[] = [
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

			const operator = row.original.akDiffOperator;
			const value = row.original.akDiffValue;
			if (!operator || value == null) {
				return 'Không giới hạn';
			}

			const displayOperator =
				operator === '>=' ? '≥' : operator === '<=' ? '≤' : operator;

			return displayOperator === '='
				? formatNumber(value)
				: `${displayOperator} ${formatNumber(value)}`;
		},
	},
	{
		accessorKey: 'adjustmentRateDisplay',
		header: 'Tỷ lệ điều chỉnh doanh thu',
		cell: ({ row }) => {
			if (row.original.adjustmentRateDisplay) {
				return row.original.adjustmentRateDisplay;
			}
			const adjustmentRate = row.original.adjustmentRate;
			if (adjustmentRate == null) {
				return '0%';
			}

			return `${formatNumber(adjustmentRate * 100)}%`;
		},
	},
	{
		accessorKey: 'description',
		header: 'Mô tả',
	},
];
