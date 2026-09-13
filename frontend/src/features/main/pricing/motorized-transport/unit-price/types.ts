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
	receivingLocationIds?: string[];
	receivingLocationNames?: string[];
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

// ====== 4-Tier Types ======

export type ExpandPriceRow = {
	headerId: string;
	detailId: string;
	equipmentQuality: string;
	haulDistanceValue?: string;
	fuelUnitPrice?: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice?: number;
};

// Cấp 3: Công đoạn sản xuất & Thông số
export type MotorizedC3Item = {
	id: string;
	c2Id: string;
	vehicleType: number;
	productionProcessId: string;
	productionProcessName: string;
	cargoTypeName?: string;
	receivingLocationName?: string;
	dumpingLocationName?: string;
	params: string;
	headerIds: string[];
	rows: ExpandPriceRow[];
};

// Cấp 2: Thời gian & Nhóm xe
export type MotorizedC2Item = {
	id: string;
	parentAssignmentCodeId: string;
	startMonth: string;
	endMonth: string;
	vehicleType: number;
	vehicleLabel: string;
	c3Items: MotorizedC3Item[];
};

// Cấp 1: Nhóm vật tư, tài sản
export type MechanizedTransportAssignmentGroup = MechanizedTransportUnitPriceGroupDto & {
	id?: string;
	c2Items: MotorizedC2Item[];
	rawGroups?: MechanizedTransportUnitPriceGroupDto[];
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

export type PeriodItem = {
	id?: string;
	startMonth: string;
	endMonth: string;
};

export const getInitialPeriods = (
	row: any,
	isDuplicate?: boolean,
): PeriodItem[] => {
	const periodList: PeriodItem[] = [];
	if (row) {
		const anyRow = row as any;
		const seen = new Set<string>();

		if (anyRow.rawGroups && anyRow.rawGroups.length > 0) {
			anyRow.rawGroups.forEach((rg: any) => {
				const s = rg.startMonth?.substring(0, 7) || '';
				const e = rg.endMonth?.substring(0, 7) || '';
				const key = `${s}_${e}`;
				if (s && !seen.has(key)) {
					seen.add(key);
					periodList.push({
						id: isDuplicate ? undefined : rg.id,
						startMonth: s,
						endMonth: e,
					});
				}
			});
		} else if (anyRow.c2Items && anyRow.c2Items.length > 0) {
			anyRow.c2Items.forEach((c2: any) => {
				const s = c2.startMonth?.substring(0, 7) || '';
				const e = c2.endMonth?.substring(0, 7) || '';
				const key = `${s}_${e}`;
				if (s && !seen.has(key)) {
					seen.add(key);
					periodList.push({
						id: isDuplicate ? undefined : c2.id,
						startMonth: s,
						endMonth: e,
					});
				}
			});
		}

		if (periodList.length === 0 && row.startMonth) {
			periodList.push({
				id: isDuplicate ? undefined : anyRow.id,
				startMonth: row.startMonth.substring(0, 7),
				endMonth: row.endMonth?.substring(0, 7) || '',
			});
		}
	}

	if (periodList.length === 0) {
		periodList.push({
			startMonth: '',
			endMonth: '',
		});
	}

	return periodList;
};

export type ScaniaPeriodData = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	assignmentCodeIds: string[];
	equipmentProcesses: Record<string, string[]>;
	equipmentQualities: Record<string, string[]>;
	equipmentDistances: Record<string, string[]>;
	processCargoTypes: Record<string, string[]>;
	processPickupLocations: Record<string, string[]>;
	processDropoffLocations: Record<string, string[]>;
	items: any[];
};

export type ExcavatorPeriodData = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	assignmentCodeIds: string[];
	equipmentProcesses: Record<string, string[]>;
	equipmentQualities: Record<string, string[]>;
	items: any[];
};

export type ServiceCranePeriodData = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	assignmentCodeIds: string[];
	equipmentProcesses: Record<string, string[]>;
	equipmentQualities: Record<string, string[]>;
	equipmentDistances: Record<string, string[]>;
	items: any[];
};

export type VacuumTruckPeriodData = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	assignmentCodeIds: string[];
	equipmentProcesses: Record<string, string[]>;
	equipmentQualities: Record<string, string[]>;
	equipmentDistances: Record<string, string[]>;
	items: any[];
};

export const computeNextPeriodMonths = (lastEndMonth?: string) => {
	if (!lastEndMonth) {
		const now = new Date();
		const s = now.toISOString().substring(0, 7);
		return { startMonth: s, endMonth: s };
	}
	try {
		const [y, m] = lastEndMonth.split('-').map(Number);
		if (!y || !m) return { startMonth: '', endMonth: '' };
		const nextStart = new Date(y, m, 1);
		const nextEnd = new Date(y + 1, m, 0);
		const s = `${nextStart.getFullYear()}-${String(nextStart.getMonth() + 1).padStart(2, '0')}`;
		const e = `${nextEnd.getFullYear()}-${String(nextEnd.getMonth() + 1).padStart(2, '0')}`;
		return { startMonth: s, endMonth: e };
	} catch {
		return { startMonth: '', endMonth: '' };
	}
};
