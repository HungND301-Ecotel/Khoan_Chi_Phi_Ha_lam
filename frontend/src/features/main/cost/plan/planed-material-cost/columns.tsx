import { formatNumber } from '@/lib/utils';
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
	materialReferenceId?: string;
	normFactorId: string;
	stoneClampRatioReferenceId?: string;
	outputId: string;
	otherMaterialValue?: number;
	materialCost?: number;
	slideUnitPriceCost?: number;
	normFactorValue?: string;
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
	materialUnitPriceCost: number;
	slideUsage: string;
	slideUnitPriceCost: number;
	stoneClampRatio: string;
	normFactorValue: string;
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
			accessorKey: 'materialUnitPriceCost',
			header: () => (
				<span className='whitespace-normal'>Đơn giá vật liệu (đ/m)</span>
			),
			cell: ({ row }) => formatNumber(row.original.materialUnitPriceCost),
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
	];
