import { Asset } from '@/features/main/catalog/asset/types';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const CATALOG_ASSET_INTERNAL_COLUMNS: ColumnDef<Asset>[] = [
	{
		accessorKey: 'code',
		header: 'Mã vật tư, tài sản',
	},
	{
		accessorKey: 'name',
		header: 'Tên vật tư, tài sản',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'costAmount',
		header: 'Đơn giá kế hoạch (đ)',
		cell: ({ row }) => formatNumber(row.original.costAmount),
	},
	{
		accessorKey: 'actualCostAmount',
		header: 'Đơn giá thực tế (đ)',
		cell: ({ row }) => formatNumber(row.original.actualAmount ?? 0),
	},
];
