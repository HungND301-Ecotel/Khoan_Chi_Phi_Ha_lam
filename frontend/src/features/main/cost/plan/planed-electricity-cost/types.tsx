import {
	CostPlanAdjustmentDetail,
	CostPlanAdjustmentSelection,
} from '@/features/main/cost/plan/types';

export type PlanedElectricityCostDetailDescription = CostPlanAdjustmentDetail;

export type PlanedElectricityCostAdjustmentSelection = CostPlanAdjustmentSelection;

export type PlanedElectricityCostDetailCost = {
	electricityUnitPriceEquipmentId: string;
	electricityUnitPrice: number;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	quantity: number;
	totalPrice: number;
	adjustmentFactorDescriptions: PlanedElectricityCostDetailDescription[];
};

export type PlanedElectricityCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	trimmingCoefficient: number;
	costs: PlanedElectricityCostDetailCost[];
};
