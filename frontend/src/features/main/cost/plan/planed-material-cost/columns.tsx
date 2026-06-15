import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { ProcessGroupType } from '@/constants/process-group';
import { UnifiedMaterial } from './type';

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
	materialDetail: string;
	materialUnitPriceCost: number;
	slideUsage: string;
	slideUnitPriceCost: number;
	lowValuePerishableSupplyUsage?: string;
	lowValuePerishableSupplyUnitPriceCost?: number;
	stoneClampRatio: string;
	normFactorValue: string;
};

export function getPlannedMaterialDetail(
	material?: UnifiedMaterial,
	fixedKeyType?: number,
) {
	if (!material) return '-';

	if (
		fixedKeyType === ProcessGroupType.DL ||
		fixedKeyType === ProcessGroupType.XL
	) {
		return (
			[
				material.hardnessName,
				material.passportName,
				material.insertItemName,
				material.supportStepName,
			]
			.filter(Boolean)
			.join(' | ') || '-'
		);
	}

	if (fixedKeyType === ProcessGroupType.LC) {
		const hardnessOrPowerText =
			material.powerName?.trim() || material.hardnessName?.trim();

		return (
			[
				material.technologyName,
				hardnessOrPowerText,
				material.seamFaceName,
				material.longwallParametersName,
				material.cuttingThicknessName,
			]
			.filter(Boolean)
			.join(' | ') || '-'
		);
	}

	return '-';
}

export const getPlanedMaterialCostSummaryColumns = (
	fixedKeyType?: number,
): ColumnDef<PlanedMaterialCostSummary>[] => {
	const columns: ColumnDef<PlanedMaterialCostSummary>[] = [
		{
			accessorKey: 'materialCode',
			header: () => (
				<span className='whitespace-normal'>Mã định mức đơn giá vật liệu</span>
			),
		},
		{
			accessorKey: 'materialDetail',
			header: () => <span className='whitespace-normal'>Thông số</span>,
			cell: ({ row }) => (
				<div className='flex min-w-90 flex-wrap items-center gap-x-2 text-sm'>
					{String(row.original.materialDetail || '-')
						.split(' | ')
						.map((item, index, items) => (
							<div key={`${item}-${index}`} className='contents'>
								<span>{item}</span>
								{index < items.length - 1 && <span>|</span>}
							</div>
						))}
				</div>
			),
		},
	];

	if (fixedKeyType === ProcessGroupType.DL) {
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
		fixedKeyType === ProcessGroupType.DL ||
		fixedKeyType === ProcessGroupType.LC
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

export type PlannedMaterialBreakdownRow = {
	rowType: 'group-summary' | 'material-item';
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	materialId?: string;
	materialCode?: string;
	materialName?: string;
	unitPrice?: number | null;
	originalQuantity?: number | null;
	coefficientValue?: number | null;
	totalPrice: number;
};

export const PLANNED_MATERIAL_BREAKDOWN_COLUMNS: ColumnDef<PlannedMaterialBreakdownRow>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: () => <span>Mã nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold'>{row.original.assignmentCode}</span>
				) : (
					''
				),
		},
		{
			accessorKey: 'assignmentCodeName',
			header: () => <span>Tên nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? row.original.assignmentCodeName
					: '',
		},
		{
			accessorKey: 'materialCode',
			header: () => <span>Mã vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: row.original.materialCode,
		},
		{
			accessorKey: 'materialName',
			header: () => <span>Tên vật tư, tài sản</span>,
			cell: ({ row }) => (
				<span className='whitespace-normal'>
					{row.original.rowType === 'group-summary'
						? ''
						: row.original.materialName}
				</span>
			),
		},
		{
			accessorKey: 'unitPrice',
			header: 'Đơn giá gốc (đ)',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.unitPrice === null ||
				row.original.unitPrice === undefined
					? ''
					: formatNumber(row.original.unitPrice),
		},
		{
			accessorKey: 'originalQuantity',
			header: 'Định mức gốc',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.originalQuantity === null ||
				row.original.originalQuantity === undefined
					? ''
					: formatNumber(row.original.originalQuantity),
		},
		{
			accessorKey: 'coefficientValue',
			header: 'Hệ số ĐCĐM',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.coefficientValue === null ||
				row.original.coefficientValue === undefined
					? ''
					: formatNumber(row.original.coefficientValue),
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá vật liệu (đ/m)',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold'>
						{formatNumber(row.original.totalPrice)}
					</span>
				) : (
					formatNumber(row.original.totalPrice)
				),
		},
	];
