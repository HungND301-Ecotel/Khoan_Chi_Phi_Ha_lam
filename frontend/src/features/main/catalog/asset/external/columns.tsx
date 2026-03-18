import { Asset } from '@/features/main/catalog/asset/types';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const CATALOG_ASSET_EXTERNAL_COLUMNS: ColumnDef<Asset>[] = [
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
		accessorKey: 'usageTime',
		header: 'Thời gian sử dụng (tháng)',
		cell: ({ row }) => formatNumber(row.original.usageTime),
	},
	{
		accessorKey: 'costAmount',
		header: 'Đơn giá vật tư (đ)',
		cell: ({ row }) => formatNumber(row.original.costAmount),
	},
];
