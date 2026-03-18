export type PlanedElectricityCostDetailDescription = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	electricityAdjustmentValue: number;
};

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
	costs: PlanedElectricityCostDetailCost[];
};
