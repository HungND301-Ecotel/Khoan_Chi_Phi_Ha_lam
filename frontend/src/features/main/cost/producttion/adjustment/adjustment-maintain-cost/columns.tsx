import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AdjustmentMaintainCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	costs: AdjustmentMaintainCostItem[];
};

export type AdjustmentMaintainCostItem = {
	maintainUnitPriceId: string;
	maintainUnitPrice: number;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	quantity: number;
	totalPrice: number;
	k6AdjustmentFactorValue: number;
	adjustmentFactorDescriptions: AdjustmentMaintainCostItemDescription[];
};

export type AdjustmentMaintainCostItemDescription = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	maintenanceAdjustmentValue: number;
};

export const ADJUSTMENT_MAINTAIN_COST_COLUMNS: ColumnDef<AdjustmentMaintainCostItem>[] =
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
			accessorKey: 'maintainUnitPrice',
			header: 'Đơn giá (đ)',
			cell: ({ row }) =>
				formatNumber(row.original.maintainUnitPrice, {
					maximumFractionDigits: 0,
				}),
		},
		{
			accessorKey: 'quantity',
			header: 'Số lượng',
			cell: ({ row }) => formatNumber(row.original.quantity),
		},
		...Array.from({ length: 7 }).map<ColumnDef<AdjustmentMaintainCostItem>>(
			(_, idx) => {
				const name = `K${idx + 1}`;
				return {
					id: name,
					header: name,
					cell: ({ row }) => {
						if (idx === 5) {
							return formatNumber(row.original.k6AdjustmentFactorValue);
						}

						const sortedDescriptions =
							row.original.adjustmentFactorDescriptions.sort((a, b) =>
								a.adjustmentFactorCode.localeCompare(b.adjustmentFactorCode),
							);

						const arrayIndex = idx > 5 ? idx - 1 : idx;
						const item = sortedDescriptions[arrayIndex];
						return item
							? formatNumber(item.maintenanceAdjustmentValue)
							: formatNumber(0);
					},
				};
			},
		),
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá SCTX (đ/m) ',
			cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
		},
	];
