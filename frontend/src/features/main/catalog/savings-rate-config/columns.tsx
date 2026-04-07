import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type SavingsRateConfig = {
	id: string;
	maxRevenue?: number;
	maxSavingsRate?: number;
	description?: string | null;
};

export const CATALOG_SAVINGS_RATE_CONFIG_COLUMNS: ColumnDef<SavingsRateConfig>[] =
	[
		{
			accessorKey: 'maxRevenue',
			header: 'Tổng doanh thu 3 yếu tố',
			cell: ({ row }) => {
				const value = row.original.maxRevenue;
				if (value === null || value === undefined) {
					return 'Không giới hạn';
				}
				return formatNumber(value);
			},
		},
		{
			accessorKey: 'maxSavingsRate',
			header: 'Giá trị tiết kiệm',
			cell: ({ row }) => `${formatNumber(row.original.maxSavingsRate ?? 0)}%`,
		},
		{
			accessorKey: 'description',
			header: 'Mô tả',
		},
	];
