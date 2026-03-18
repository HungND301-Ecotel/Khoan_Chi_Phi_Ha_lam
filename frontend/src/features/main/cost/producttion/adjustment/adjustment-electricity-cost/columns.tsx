import { AdjustmentElectricityCostDetailCost } from '@/features/main/cost/producttion/adjustment/adjustment-electricity-cost/types';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const ADJUSTMENT_ELECTRICITY_COST_COLUMNS: ColumnDef<AdjustmentElectricityCostDetailCost>[] =
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
			header: 'Đơn giá (đ)',
			cell: ({ row }) =>
				formatNumber(row.original.electricityUnitPrice, {
					maximumFractionDigits: 0,
				}),
		},
		{
			accessorKey: 'quantity',
			header: 'Số lượng',
			cell: ({ row }) => formatNumber(row.original.quantity),
		},
		...Array.from({ length: 3 }).map<
			ColumnDef<AdjustmentElectricityCostDetailCost>
		>((_, idx) => {
			const name = `K${idx + 1}`;
			return {
				id: name,
				header: name,
				cell: ({ row }) =>
					formatNumber(
						row.original.adjustmentFactorDescriptions.sort((a, b) =>
							a.adjustmentFactorCode.localeCompare(b.adjustmentFactorCode),
						)[idx].electricityAdjustmentValue,
					),
			};
		}),
		{
			accessorKey: 'totalPrice',
			header: 'Thành tiền (đ)',
			cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
		},
	];
