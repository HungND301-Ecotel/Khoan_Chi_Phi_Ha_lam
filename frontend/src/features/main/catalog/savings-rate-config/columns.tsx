import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type SavingsRateConfig = {
	id: string;
	minRevenue?: number;
	maxRevenue?: number;
	minSavingsRate?: number;
	maxSavingsRate?: number;
	revenueDisplay?: string | null;
	savingsRateDisplay?: string | null;
	description?: string | null;
};

export const CATALOG_SAVINGS_RATE_CONFIG_COLUMNS: ColumnDef<SavingsRateConfig>[] =
	[
		{
			accessorKey: 'revenueDisplay',
			header: 'Tổng doanh thu 3 yếu tố',
			cell: ({ row }) => {
				if (row.original.revenueDisplay) {
					return row.original.revenueDisplay;
				}

				const value = row.original.maxRevenue;
				if (value === null || value === undefined) {
					return 'Không giới hạn';
				}
				return formatNumber(value);
			},
		},
		{
			accessorKey: 'savingsRateDisplay',
			header: 'Giá trị tiết kiệm',
			cell: ({ row }) => {
				if (row.original.savingsRateDisplay) {
					return row.original.savingsRateDisplay;
				}
				return `${formatNumber(row.original.maxSavingsRate ?? 0)}%`;
			},
		},
		{
			accessorKey: 'description',
			header: 'Mô tả',
		},
	];
