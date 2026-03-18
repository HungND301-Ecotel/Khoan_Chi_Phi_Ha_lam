export type AdjustmentElectricityCostDetailDescription = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	electricityAdjustmentValue: number;
};

export type AdjustmentElectricityCostDetailCost = {
	electricityUnitPriceEquipmentId: string;
	electricityUnitPrice: number;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	quantity: number;
	totalPrice: number;
	adjustmentFactorDescriptions: AdjustmentElectricityCostDetailDescription[];
};

export type AdjustmentElectricityCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	costs: AdjustmentElectricityCostDetailCost[];
};
