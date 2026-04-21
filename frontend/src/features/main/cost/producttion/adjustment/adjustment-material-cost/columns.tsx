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
	stoneClampRatio: string;
	normFactorValue: string;
	akRatePercent: number;
};

export const ADJUSTMENT_MATERIAL_COST_SUMMARY_COLUMNS: ColumnDef<AdjustmentMaterialCostSummary>[] =
	[
		{
			accessorKey: 'materialCode',
			header: () => (
				<span className='whitespace-normal'>Mã định mức đơn giá vật liệu</span>
			),
		},
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
		{
			accessorKey: 'akRatePercent',
			header: () => (
				<span className='whitespace-normal'>Tỉ lệ điều chỉnh doanh thu</span>
			),
			cell: ({ row }) => `${formatNumber(row.original.akRatePercent)}%`,
		},
	];
