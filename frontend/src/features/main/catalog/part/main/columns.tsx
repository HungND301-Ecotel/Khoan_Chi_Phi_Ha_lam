import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Part = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	partType: number;
	equipmentIds: string[];
	equipmentCodes: string[];
	costAmount: number;
	actualAmount: number;
};

export const CATALOG_PART_COLUMNS: ColumnDef<Part>[] = [
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
		header: 'Đơn giá kế hoạch (đ)',
		cell: ({ row }) => formatNumber(row.original.costAmount),
	},
	{
		accessorKey: 'actualAmount',
		header: 'Đơn giá thực tế (đ)',
		cell: ({ row }) => formatNumber(row.original.actualAmount),
	},
];
