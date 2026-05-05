import { ProcessGroupType } from '@/constants/process-group';

export type CostProduct = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	fixedKeyType: ProcessGroupType;
	processGroupType?: number;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	totalProductionMeters: number;
	plannedTotalCost: number;
	startMonth: string;
	endMonth: string;
};

export type CostProductDetail = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	fixedKeyType: ProcessGroupType;
	processGroupType?: number;
	processGroupName: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	outputs: CostProductDetailOutput[];
};

export type CostProductDetailOutput = {
	id: string;
	acceptanceReportId?: string;
	productionMeters: number;
	standardProductionMeters?: number;
	plannedMaterialCostId?: string;
	plannedMaintainCostId?: string;
	plannedElectricityCostId?: string;
	actualMaterialCostId?: string;
	actualMaintainCostId?: string;
	actualElectricityCostId?: string;
	outputType: number;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
	adjTotalPrice?: number;
	planAshContent?: number;
};

export type ProductCostExpandProps = {
	id?: string;
	output?: CostProductDetailOutput;
	plan?: CostProductDetail;
	actual?: CostProductDetail;
	callback?: () => void | Promise<void>;
	isOpen?: boolean;
	reloadKey?: number;
};

export function mapCostProduct(product: CostProduct): CostProduct {
	return {
		...product,
		fixedKeyType:
			product.fixedKeyType ??
			(product.processGroupType as ProcessGroupType) ??
			ProcessGroupType.None,
	};
}

export function mapCostProductDetail(
	detail: CostProductDetail,
): CostProductDetail {
	return {
		...detail,
		fixedKeyType:
			detail.fixedKeyType ??
			(detail.processGroupType as ProcessGroupType) ??
			ProcessGroupType.None,
	};
}

export type ProductCostFormProps = Omit<ProductCostExpandProps, 'isOpen'>;

export type CostPlanAdjustmentSelection = {
	adjustmentFactorDescriptionId: string;
	adjustmentFactorId: string;
	customValue: number | null;
};

export type CostPlanAdjustmentDetail = {
	id: string;
	adjustmentFactorDescriptionId?: string | null;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	maintenanceAdjustmentValue?: number | null;
	electricityAdjustmentValue?: number | null;
	customValue?: number | null;
	effectiveValue: number;
};

export type CostPlanAdjustmentOption = {
	value: string;
	label: string;
};
