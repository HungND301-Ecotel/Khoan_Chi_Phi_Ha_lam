// ====== Backend DTO types ======

export type MechanizedTransportUnitPriceRowDto = {
	headerId: string;
	detailId: string;
	equipmentQuality: string;
	haulDistanceId?: string;
	haulDistanceValue?: string;
	fuelUnitPrice: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice: number;
};

export type MechanizedTransportUnitPriceSectionDto = {
	vehicleType: number; // 1=Scania, 2=VacuumTruck, 3=ServiceCrane, 4=ExcavatorDozer
	productionProcessId: string;
	productionProcessName: string;
	// Scania-specific fields
	cargoTypeId?: string;
	cargoTypeName?: string;
	receivingLocationId?: string;
	receivingLocationName?: string;
	dumpingLocationId?: string;
	dumpingLocationName?: string;
	rows: MechanizedTransportUnitPriceRowDto[];
};

export type MechanizedTransportUnitPriceGroupDto = {
	assignmentCodeId: string;
	assignmentCodeName: string;
	startMonth: string;
	endMonth: string;
	sections: MechanizedTransportUnitPriceSectionDto[];
};

// ====== Frontend display types ======

export type MechanizedTransportUnitPriceDetail = {
	id: string;
	haulDistanceId?: string;
	haulDistanceValue?: string;
	fuelUnitPrice: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice: number;
};

export type MechanizedTransportUnitPriceItem = {
	id: string;
	vehicleType: number;
	assignmentCodeId: string;
	assignmentCodeName: string;
	equipmentQuality: string;
	productionProcessId: string;
	productionProcessName: string;
	startMonth: string;
	endMonth: string;
	details: MechanizedTransportUnitPriceDetail[];
};

// ====== Constants ======

export const VEHICLE_TYPE_LABELS: Record<number, string> = {
	1: 'Vận chuyển (Xe Scania)',
	2: 'Hút bùn, chất thải (Xe hút bùn, chất thải)',
	3: 'Phục vụ (Xe phục vụ)',
	4: 'Xúc, gạt, nâng (Máy xúc, máy gạt, xe nâng)',
};

export const VEHICLE_TYPE_OPTIONS = [
	{ label: 'Vận chuyển (Xe Scania)', value: 'scania', apiType: 1 },
	{ label: 'Hút bùn, chất thải (Xe hút bùn, chất thải)', value: 'vacuum-truck', apiType: 2 },
	{ label: 'Phục vụ (Xe phục vụ)', value: 'service-crane', apiType: 3 },
	{ label: 'Xúc, gạt, nâng (Máy xúc, máy gạt, xe nâng)', value: 'excavator-dozer', apiType: 4 },
];

export const getVehicleApi = (vehicleType: number, API: any) => {
	switch (vehicleType) {
		case 1:
			return API.PRICING.MOTORIZED_TRANSPORT.SCANIA;
		case 2:
			return API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK;
		case 3:
			return API.PRICING.MOTORIZED_TRANSPORT.SERVICE_CRANE;
		case 4:
			return API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER;
		default:
			return API.PRICING.MOTORIZED_TRANSPORT.SCANIA;
	}
};

export const getVehicleLabel = (vehicleType: number) => {
	const opt = VEHICLE_TYPE_OPTIONS.find((o) => o.apiType === vehicleType);
	return opt?.label || 'Phương tiện';
};
