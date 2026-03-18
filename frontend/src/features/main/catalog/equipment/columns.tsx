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
