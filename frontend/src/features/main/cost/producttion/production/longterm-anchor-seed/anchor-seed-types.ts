export type LongTermAnchorSeedItem = {
	id: string;
	materialId: string;
	trackedMaterialId?: string;
	partId?: string;
	processGroupId: string;
	categoryAssignmentCodeId?: string | null;
	categoryEquipmentId?: string | null;
	categoryProductionOrderId?: string | null;
	materialCode: string;
	materialName: string;
	trackedMaterialCode?: string;
	trackedMaterialName?: string;
	partCode?: string;
	partName?: string;
	unitOfMeasureName: string;
	processGroupCode: string;
	processGroupName: string;
	categoryAssignmentCode?: string;
	categoryAssignmentCodeName?: string;
	categoryProductionOrderCode?: string;
	categoryProductionOrderName?: string;
	pendingValueStartPeriod: number;
	usageTime: number;
	allocatedTime: number;
	remainingTime: number;
	allocationRatio: number;
	note?: string;
};

export type LongTermAnchorSeedProcessGroupMetric = {
	id: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	plannedOutput: number;
	standardOutput: number;
};

export type LongTermAnchorSeedDetail = {
	departmentId: string;
	departmentCode: string;
	departmentName: string;
	effectiveMonth?: string;
	processGroupMetrics: LongTermAnchorSeedProcessGroupMetric[];
	items: LongTermAnchorSeedItem[];
};
