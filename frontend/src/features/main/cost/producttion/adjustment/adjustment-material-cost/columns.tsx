import { ProcessGroupType } from '@/constants/process-group';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AdjustmentMaterialCostItem = {
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
	materialCost: number;
	materialUnitPriceCost: number;
	originalQuantity: number;
	coefficientValue: number;
	finalQuantity: number;
	totalPrice: number;
};

export type AdjustmentMaterialContract = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	costs: AdjustmentMaterialCostItem[];
};

export type AdjustmentMaterialCostType = {
	id: string;
	productUnitPriceId: string;
	materialUnitPriceId: string;
	slideUnitPriceAssignmentCodeId: string;
	materialReferenceId?: string;
	normFactorId: string;
	stoneClampRatioReferenceId?: string;
	outputId: string;
	otherMaterialValue?: number;
	materialCost?: number;
	slideUnitPriceCost?: number;
	lowValuePerishableSupplyUnitPriceCost?: number;
	akRate: number;
	akRatePercent: number;
	normFactorValue?: string;
	totalPlannedMaterialPrice: number;
	adjustmentMaterialCostAssignmentCodes: AdjustmentMaterialContract[];
};

export type FlatAdjustmentMaterialCost = {
	isGroupRow: boolean;
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	materialId?: string;
	materialCode?: string;
	materialName?: string;
	unitOfMeasureName?: string;
	materialCost?: number;
	materialUnitPriceCost?: number;
	originalQuantity?: number;
	coefficientValue?: number;
	finalQuantity?: number;
	totalPrice: number;
};

export type AdjustmentMaterialCostSummary = {
	materialCode: string;
	materialUnitPriceCost: number;
	slideUsage: string;
	slideUnitPriceCost: number;
	lowValuePerishableSupplyUnitPriceCost?: number;
	stoneClampRatio: string;
	normFactorValue: string;
	akRatePercent: number;
};

export const getAdjustmentMaterialCostSummaryColumns = (
	processGroupType?: number,
): ColumnDef<AdjustmentMaterialCostSummary>[] => {
	const columns: ColumnDef<AdjustmentMaterialCostSummary>[] = [
		{
			accessorKey: 'materialCode',
			header: () => (
				<span className='whitespace-normal'>Mã định mức đơn giá vật liệu</span>
			),
		},
	];

	if (processGroupType === ProcessGroupType.DL) {
		columns.push(
			{
				accessorKey: 'slideUsage',
				header: () => (
					<span className='whitespace-normal'>Sử dụng máng trượt</span>
				),
			},
			{
				accessorKey: 'slideUnitPriceCost',
				header: () => (
					<span className='whitespace-normal'>Đơn giá máng trượt (đ/m)</span>
				),
				cell: ({ row }) => formatNumber(row.original.slideUnitPriceCost),
			},
		);
	}

	if (
		processGroupType === ProcessGroupType.DL ||
		processGroupType === ProcessGroupType.LC
	) {
		columns.push({
			accessorKey: 'lowValuePerishableSupplyUnitPriceCost',
			header: () => (
				<span className='whitespace-normal'>
					Đơn giá vật tư mau hỏng rẻ tiền (đ/m)
				</span>
			),
			cell: ({ row }) =>
				formatNumber(row.original.lowValuePerishableSupplyUnitPriceCost || 0),
		});
	}

	columns.push(
		{
			accessorKey: 'stoneClampRatio',
			header: () => <span className='whitespace-normal'>Tỷ lệ đá kẹp</span>,
		},
		{
			accessorKey: 'normFactorValue',
			header: () => (
				<span className='whitespace-normal'>Hệ số điều chỉnh định mức</span>
			),
		},
		{
			accessorKey: 'materialUnitPriceCost',
			header: () => (
				<span className='whitespace-normal'>Đơn giá vật liệu (đ/m)</span>
			),
			cell: ({ row }) => formatNumber(row.original.materialUnitPriceCost),
		},
	);

	columns.push({
		accessorKey: 'akRatePercent',
		header: () => (
			<span className='whitespace-normal'>Tỉ lệ điều chỉnh doanh thu</span>
		),
		cell: ({ row }) => `${formatNumber(row.original.akRatePercent)}%`,
	});

	return columns;
};
