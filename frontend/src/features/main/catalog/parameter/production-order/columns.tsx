import { ColumnDef } from '@tanstack/react-table';

export type ProductionOrder = {
	id: string;
	value: string;
	coefficientValue: number;
};

export const CATALOG_PARAMETER_PRODUCTION_ORDER_COLUMNS: ColumnDef<ProductionOrder>[] =
	[
		{
			accessorKey: 'value',
			header: 'Quyết định, lệnh sản xuất',
		},
	];
