import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Part = {
	id: string;
	equipmentPartId: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	equipmentId: string;
	equipmentCode: string;
	replacementTimeStandard: number;
	costAmount: number;
	actualAmount: number;
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
		accessorKey: 'replacementTimeStandard',
		header: 'Định mức thời gian thay thế (tháng)',
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
