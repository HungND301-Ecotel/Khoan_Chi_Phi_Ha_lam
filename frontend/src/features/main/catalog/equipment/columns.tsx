import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Equipment = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	currentPrice: number;
};

export type EquipmentPartDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName: string;
	replacementTimeStandard: number;
	currentCost: number;
	actualAmount: number;
};
export const CATALOG_EQUIPMENT_EXPAND_COLUMNS: ColumnDef<EquipmentPartDetail>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã phụ tùng',
		},
		{
			accessorKey: 'name',
			header: 'Tên phụ tùng',
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: 'Đơn vị tính',
		},
		{
			accessorKey: 'currentCost',
			header: 'Đơn giá kế hoạch (đ)',
			cell: ({ row }) => formatNumber(row.original.currentCost),
		},
		{
			accessorKey: 'actualAmount',
			header: 'Đơn giá thực tế (đ)',
			cell: ({ row }) => formatNumber(row.original.actualAmount),
		},
	];

export const CATALOG_EQUIPMENT_COLUMNS: ColumnDef<Equipment>[] = [
	{
		accessorKey: 'code',
		header: 'Mã thiết bị',
	},
	{
		accessorKey: 'name',
		header: 'Tên thiết bị',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'currentPrice',
		header: 'Đơn giá điện năng (đ/kWh)',
		cell: ({ row }) => formatNumber(row.original.currentPrice),
	},
];
