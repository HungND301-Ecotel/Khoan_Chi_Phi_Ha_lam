import { ColumnDef } from '@tanstack/react-table';

export type PlanedMaterialCostItem = {
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

export type PlanedMaterialContract = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	costs: PlanedMaterialCostItem[];
};

export type PlanedMaterialCostType = {
	id: string;
	productUnitPriceId: string;
	materialUnitPriceId: string;
	slideUnitPriceAssignmentCodeId: string;
	normFactorId: string;
	outputId: string;
	otherMaterialValue?: number;
	totalPlannedMaterialPrice: number;
	plannedMaterialCostAssignmentCodes: PlanedMaterialContract[];
};

export type FlatPlannedMaterialCost = {
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

export type PlanedMaterialCostSummary = {
	materialCode: string;
	slideUsage: string;
	stoneClampRatio: string;
};

export const PLANED_MATERIAL_COST_SUMMARY_COLUMNS: ColumnDef<PlanedMaterialCostSummary>[] =
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
