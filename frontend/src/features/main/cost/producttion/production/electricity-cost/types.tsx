export type ProductionElectricityCostItem = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	electricityUnitPrice: number;
	electricityConsumption: number;
	electricityCost: number;
};

export type ProductionActualElectricityEquipmentDetail = {
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	electricityUnitPrice: number;
	actualElectricityConsumption: number;
	totalPrice: number;
};

export type ProductionActualElectricityCostDetail = {
	id: string;
	acceptanceReportId: string;
	equipments: ProductionActualElectricityEquipmentDetail[];
};
