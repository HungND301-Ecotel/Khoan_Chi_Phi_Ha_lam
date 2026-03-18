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
	stoneClampRatioId: string;
	outputId: string;
	otherMaterialValue?: number;
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
	slideUsage: string;
	stoneClampRatio: string;
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
			accessorKey: 'stoneClampRatio',
			header: () => <span className='whitespace-normal'>Tỷ lệ đá kẹp</span>,
		},
	];
