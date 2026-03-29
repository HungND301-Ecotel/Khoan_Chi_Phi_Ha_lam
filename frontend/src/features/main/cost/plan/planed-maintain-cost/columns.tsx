import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';

export type PlanedMaintainCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	costs: PlanedMaintainCostItem[];
};

export type PlanedMaintainCostItem = {
	maintainUnitPriceId: string;
	maintainUnitPrice: number;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	quantity: number;
	k6AdjustmentFactorValue: number;
	totalPrice: number;
	adjustmentFactorDescriptions: PlanedMaintainCostItemDescription[];
};

export type PlanedMaintainCostItemDescription = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	maintenanceAdjustmentValue: number;
};

export const getPlanedMaintainCostColumns = (
	processGroupType?: ProcessGroupType,
): ColumnDef<PlanedMaintainCostItem>[] => {
	const getLength = () => {
		if (processGroupType === ProcessGroupType.DL) {
			return 7;
		}
		return 8; // LONGWALL or default
	};

	return [
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
		...Array.from({ length: getLength() }).map<
			ColumnDef<PlanedMaintainCostItem>
		>((_, idx) => {
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

					const adjustedIdx = idx > 5 ? idx - 1 : idx;

					return formatNumber(
						sortedDescriptions[adjustedIdx]?.maintenanceAdjustmentValue ?? 0,
					);
				},
			};
		}),
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá SCTX (đ/m) ',
			cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
		},
	];
};
