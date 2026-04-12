export type CostProduct = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupType: number;
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
	processGroupType: number;
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

export type ProductCostFormProps = Omit<ProductCostExpandProps, 'isOpen'>;
