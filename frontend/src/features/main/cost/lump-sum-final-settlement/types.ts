// Request payload to list lump-sum final settlement
export interface LumpSumFinalSettlementListRequest {
	month: string; // "1".."12"
	year: string; // e.g. "2024"
	processGroupId: string;
}

export interface LumpSumFinalSettlementQuarterListRequest {
	quarter: string; // "1".."4"
	year: string; // e.g. "2024"
	processGroupId: string;
}

export interface LumpSumFinalSettlementQuarterResponse {
	items: LumpSumFinalSettlement[];
	revenuesByMonth: LumpSumQuarterRevenueByMonth[];
	transferredCost: LumpSumQuarterTransferredCost;
	customCosts?: LumpSumQuarterCustomCost[];
}

export interface LumpSumQuarterRevenueByMonth {
	month: number;
	materials?: { unitPrice?: number; totalAmount?: number } | null;
	maintains?: { unitPrice?: number; totalAmount?: number } | null;
	electricities?: { unitPrice?: number; totalAmount?: number } | null;
	totalAmount?: number;
}

export interface LumpSumQuarterTransferredCost {
	month: number;
	materials?: { unitPrice?: number; totalAmount?: number } | null;
	maintains?: { unitPrice?: number; totalAmount?: number } | null;
	electricities?: { unitPrice?: number; totalAmount?: number } | null;
	totalAmount?: number;
}

export interface LumpSumQuarterCustomCost {
	id: string;
	year: number | string;
	quarter: number | string;
	processGroupId?: string | null;
	customName?: string;
	actualQuantity: number;
	materialUnitPrice: number;
	maintainUnitPrice: number;
	electricityUnitPrice: number;
}

export interface LumpSumQuarterCustomCostListRequest {
	year: string;
	quarter: string;
	processGroupId: string;
}

export interface UpsertLumpSumQuarterCustomCostRequest {
	id?: string;
	year: string;
	quarter: string;
	processGroupId: string;
	customName?: string;
	actualQuantity: number;
	materialUnitPrice: number;
	maintainUnitPrice: number;
	electricityUnitPrice: number;
}

export interface LumpSumFinalSettlement {
	id?: string;
	processGroupId?: string;
	processGroupCode?: string;
	processGroupName?: string;
	sttLabel?: string;
	isBold?: boolean;
	isProcessGroupRow?: boolean;
	excludeFromSummary?: boolean;
	isMergedValueRow?: boolean;
	mergedValue?: number;
	isCustomCostRow?: boolean;
	isEditing?: boolean;
	isTransferredDefaultRow?: boolean;
	productName?: string;
	productCode?: string;
	unitOfMeasureId?: string;
	unitOfMeasureName?: string;
	plannedQuantity?: number;
	actualQuantity?: number;
	materials?: { unitPrice?: number; totalAmount?: number } | null;
	maintains?: { unitPrice?: number; totalAmount?: number } | null;
	electricities?: { unitPrice?: number; totalAmount?: number } | null;
	totalAmount?: number;
}

export interface YearFilterForm {
	month?: string;
	year?: string;
	processGroup?: string;
}

export interface QuarterFilterForm {
	quarter?: string;
	year?: string;
	processGroup?: string;
}

export interface ProcessGroup {
	id: string;
	code: string;
	name: string;
}
