// Request payload to list lump-sum final settlement
export interface LumpSumFinalSettlementListRequest {
	month: string; // "1".."12"
	year: string; // e.g. "2024"
	processGroupId: string;
}

export interface LumpSumFinalSettlement {
	id?: string;
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

export interface FilterForm {
	month?: string;
	year?: string;
	processGroup?: string;
}

export interface ProcessGroup {
	id: string;
	code: string;
	name: string;
}
