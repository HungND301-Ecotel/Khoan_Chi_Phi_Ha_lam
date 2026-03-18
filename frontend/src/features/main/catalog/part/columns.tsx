import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Part = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	equipmentId: string;
	equipmentCode: string;
	costAmount: number;
};

export const CATALOG_PART_COLUMNS: ColumnDef<Part>[] = [
	{
		accessorKey: 'equipmentCode',
		header: 'Mã thiết bị',
	},
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
		accessorKey: 'costAmount',
		header: 'Đơn giá vật tư (đ)',
		cell: ({ row }) => formatNumber(row.original.costAmount),
	},
];
