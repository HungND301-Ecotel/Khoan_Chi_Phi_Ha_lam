import { ProcessGroupType } from '@/constants/process-group';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AdjustmentMaintainCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	akRate: number;
	akRatePercent: number;
	costs: AdjustmentMaintainCostItem[];
};

export type AdjustmentMaintainCostSummary = {
	akRatePercent: number;
};

export type AdjustmentMaintainCostItem = {
	maintainUnitPriceId: string;
	maintainUnitPrice: number;
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	equipmentId?: string;
	equipmentCode?: string;
	equipmentName?: string;
	quantity: number;
	totalPrice: number;
	k6AdjustmentFactorValue: number;
	adjustmentFactorDescriptions: AdjustmentMaintainCostItemDescription[];
};

export type AdjustmentMaintainCostItemDescription = {
	id: string;
	adjustmentFactorDescriptionId?: string | null;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	maintenanceAdjustmentValue?: number | null;
	customValue?: number | null;
	effectiveValue: number;
};

export const getAdjustmentMaintainCostColumns = (
	fixedKeyType?: ProcessGroupType,
): ColumnDef<AdjustmentMaintainCostItem>[] => {
	const getLength = () => {
		if (
			fixedKeyType === ProcessGroupType.DL ||
			fixedKeyType === ProcessGroupType.XL
		) {
			return 7;
		}
		return 8;
	};

	return [
		{
			accessorKey: 'assignmentCode',
			header: 'Mã giao khoán',
		},
		{
			accessorKey: 'assignmentCodeName',
			header: 'Tên giao khoán',
		},
		{
			accessorKey: 'maintainUnitPrice',
			header: 'Đơn giá (đ)',
			cell: ({ row }) => formatNumber(row.original.maintainUnitPrice),
		},
		{
			accessorKey: 'quantity',
			header: 'Số lượng',
			cell: ({ row }) => formatNumber(row.original.quantity),
		},
		...Array.from({ length: getLength() }).map<
			ColumnDef<AdjustmentMaintainCostItem>
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
						sortedDescriptions[adjustedIdx]?.effectiveValue ?? 0,
					);
				},
			};
		}),
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá SCTX (đ/m) ',
			cell: ({ row }) => formatNumber(row.original.totalPrice),
		},
	];
};

export const getAdjustmentMaintainCostSummaryColumns =
	(): ColumnDef<AdjustmentMaintainCostSummary>[] => {
		return [
			{
				accessorKey: 'akRatePercent',
				header: () => (
					<span className='whitespace-normal'>Tỉ lệ điều chỉnh doanh thu</span>
				),
				cell: ({ row }) => `${formatNumber(row.original.akRatePercent)}%`,
			},
		];
	};
