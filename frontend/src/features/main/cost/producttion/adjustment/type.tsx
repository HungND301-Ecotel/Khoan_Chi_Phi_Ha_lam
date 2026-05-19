import { ProcessGroupType } from '@/constants/process-group';

export type AdjustmentCostProductDetail = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	fixedKeyType: ProcessGroupType;
	processGroupType?: ProcessGroupType;
	processGroupName: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;

	outputs: AdjustmentOutput[];
	productionOutputs: AdjustmentProductionOutput[];
};

export type AdjustmentOutput = {
	id: string;
	productUnitPriceId: string;
	productionMeters: number;
	outputType: number;
	startMonth: string;
	endMonth: string;
};

export type AdjustmentProductionOutput = {
	id: string;
	productionOutputId: string;
	startMonth: string;
	endMonth: string;
	productionMeters: number;
	adjTotalPrice: number;
	standardProductionMeters: number;
	actualAshContent?: number;
	akRate: number;
	akRatePercent: number;
};

export type DepartmentAdjustmentItem = {
	productUnitPriceId: string;
	plannedOutputId?: string;
	productionOutputId: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	fixedKeyType?: ProcessGroupType;
	processGroupType?: ProcessGroupType;
	unitOfMeasureId?: string;
	unitOfMeasureName: string;
	productionMeters: number;
	standardProductionMeters: number;
	actualAshContent: number;
	adjustmentTotalCost: number;
	akRate: number;
	akRatePercent: number;
};

export type DepartmentAdjustmentMonth = {
	month: string;
	items: DepartmentAdjustmentItem[];
};

export type DepartmentAdjustmentDetail = {
	departmentId: string;
	departmentCode: string;
	departmentName: string;
	months: DepartmentAdjustmentMonth[];
};

export function mapAdjustmentCostProductDetail(
	detail: AdjustmentCostProductDetail,
): AdjustmentCostProductDetail {
	return {
		...detail,
		fixedKeyType:
			detail.fixedKeyType ??
			(detail.processGroupType as ProcessGroupType) ??
			ProcessGroupType.None,
	};
}

export function mapDepartmentAdjustmentDetail(
	detail: DepartmentAdjustmentDetail,
): DepartmentAdjustmentDetail {
	return {
		...detail,
		months: detail.months.map((month) => ({
			...month,
			items: month.items.map((item) => ({
				...item,
				fixedKeyType:
					item.fixedKeyType ??
					(item.processGroupType as ProcessGroupType) ??
					ProcessGroupType.None,
			})),
		})),
	};
}
