export type LongTermAnchorSeedItem = {
	id: string;
	materialId: string;
	trackedMaterialId?: string;
	partId?: string;
	processGroupId: string;
	materialCode: string;
	materialName: string;
	trackedMaterialCode?: string;
	trackedMaterialName?: string;
	partCode?: string;
	partName?: string;
	unitOfMeasureName: string;
	processGroupCode: string;
	processGroupName: string;
	issuedQuantity: number;
	unitPrice: number;
	pendingValueStartPeriod: number;
	usageTime: number;
	allocatedTime: number;
	remainingTime: number;
	allocationRatio: number;
	originAmount: number;
	totalAmount: number;
	totalValueToAccount: number;
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
