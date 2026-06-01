import { ColumnDef } from '@tanstack/react-table';
import { formatNumber } from '@/lib/utils';

export type ContractCode = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId?: string | null;
	unitOfMeasureName?: string | null;
	currentPrice?: number | null;
};

export type ContractCodeMaterialDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName?: string | null;
	materialType?: number | null;
	costAmount?: number | null;
	actualAmount?: number | null;
};

export const CATALOG_CONTRACT_CODE_COLUMNS: ColumnDef<ContractCode>[] = [
	{
		accessorKey: 'code',
		header: 'Nhóm vật tư, tài sản',
	},
	{
		accessorKey: 'name',
		header: 'Tên nhóm vật tư, tài sản',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'currentPrice',
		header: 'Đơn giá điện năng (đ/kWh)',
		cell: ({ row }) => formatNumber(Number(row.original.currentPrice ?? 0)),
	},
];

export const CATALOG_CONTRACT_CODE_EXPAND_COLUMNS: ColumnDef<ContractCodeMaterialDetail>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã vật tư, tài sản',
		},
		{
			accessorKey: 'name',
			header: 'Tên vật tư ,tài sản',
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: 'Đơn vị tính',
		},
		{
			accessorKey: 'costAmount',
			header: 'Đơn giá kế hoạch (đ)',
			cell: ({ row }) => formatNumber(Number(row.original.costAmount ?? 0)),
		},
		{
			accessorKey: 'actualAmount',
			header: 'Đơn giá thực tế (đ)',
			cell: ({ row }) => formatNumber(Number(row.original.actualAmount ?? 0)),
		},
	];
