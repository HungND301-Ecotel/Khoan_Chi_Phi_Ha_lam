import { ColumnDef } from '@tanstack/react-table';

export type RevenueCostAdjustmentConfig = {
	id: string;
	profitConditionDisplay?: string | null;
	minProfit?: number | null;
	maxProfit?: number | null;
	rateDisplay?: string | null;
	rate?: number;
	description?: string | null;
};

export const CATALOG_REVENUE_COST_ADJUSTMENT_CONFIG_COLUMNS: ColumnDef<RevenueCostAdjustmentConfig>[] =
	[
		{
			accessorKey: 'profitConditionDisplay',
			header: 'Lợi nhuận',
		},
		{
			accessorKey: 'rateDisplay',
			header: 'Tỷ lệ điều chỉnh',
		},
		{
			accessorKey: 'description',
			header: 'Mô tả',
		},
	];
