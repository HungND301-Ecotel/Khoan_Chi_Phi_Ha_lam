export type AdjustmentElectricityCostDetailDescription = {
	id: string;
	adjustmentFactorDescriptionId?: string | null;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	electricityAdjustmentValue?: number | null;
	customValue?: number | null;
	effectiveValue: number;
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
	akRate: number;
	akRatePercent: number;
	costs: AdjustmentElectricityCostDetailCost[];
};

export type AdjustmentElectricityCostSummary = {
	akRatePercent: number;
};
