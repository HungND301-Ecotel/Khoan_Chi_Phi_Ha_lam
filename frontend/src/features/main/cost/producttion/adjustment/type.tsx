import { ProcessGroupType } from '@/constants/process-group';

export type AdjustmentCostProductDetail = {
	id: string;
	productId: string;
	productCode: string;
	productName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupType: ProcessGroupType;
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
