import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';

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
	lowValuePerishableSupplyInclusion?: number;
	outputId: string;
	otherMaterialValue?: number;
	materialCost?: number;
	slideUnitPriceCost?: number;
	lowValuePerishableSupplyUnitPriceCost?: number;
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
	lowValuePerishableSupplyUsage?: string;
	lowValuePerishableSupplyUnitPriceCost?: number;
	stoneClampRatio: string;
	normFactorValue: string;
};

export const getPlanedMaterialCostSummaryColumns = (
	processGroupType?: number,
): ColumnDef<PlanedMaterialCostSummary>[] => {
	const columns: ColumnDef<PlanedMaterialCostSummary>[] = [
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

	return columns;
};
