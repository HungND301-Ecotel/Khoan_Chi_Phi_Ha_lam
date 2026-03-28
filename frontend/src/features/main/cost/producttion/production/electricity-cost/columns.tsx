import { ProductionElectricityCostItem } from '@/features/main/cost/producttion/production/electricity-cost/types';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const PRODUCTION_ELECTRICITY_COST_COLUMNS: ColumnDef<ProductionElectricityCostItem>[] =
	[
		{
			accessorKey: 'equipmentCode',
			header: 'Mã thiết bị',
		},
		{
			accessorKey: 'equipmentName',
			header: 'Tên thiết bị',
		},
		{
			accessorKey: 'electricityUnitPrice',
			header: 'Đơn giá điện năng (đ/kWh)',
			cell: ({ row }) => formatNumber(row.original.electricityUnitPrice ?? 0),
		},
		{
			accessorKey: 'electricityConsumption',
			header: 'Điện năng tiêu thụ (kWh)',
			cell: ({ row }) => formatNumber(row.original.electricityConsumption ?? 0),
		},
		{
			accessorKey: 'electricityCost',
			header: 'Chi phí điện năng (đ)',
			cell: ({ row }) => formatNumber(Math.round(row.original.electricityCost)),
		},
	];
