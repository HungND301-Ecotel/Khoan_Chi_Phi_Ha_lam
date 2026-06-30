// Request payload to list lump-sum final settlement
export interface LumpSumFinalSettlementListRequest {
	month: string; // "1".."12"
	year: string; // e.g. "2024"
	processGroupId?: string | null;
	departmentId?: string | null;
}

export interface LumpSumFinalSettlementQuarterListRequest {
	quarter: string; // "1".."4"
	year: string; // e.g. "2024"
	processGroupId?: string | null;
	departmentId?: string | null;
}

export interface LumpSumFinalSettlementQuarterResponse {
	items: LumpSumFinalSettlement[];
	monthBreakdowns?: LumpSumFinalSettlementMonthResponse[];
	revenuesByMonth: LumpSumQuarterRevenueByMonth[];
	costsByMonth?: LumpSumQuarterRevenueByMonth[];
	savingsByMonth?: LumpSumQuarterRevenueByMonth[];
	transferredCosts?: LumpSumQuarterTransferredCost[];
	customCosts?: LumpSumQuarterCustomCost[];
	revenueQuarter?: LumpSumQuarterRevenueByMonth | null;
	costQuarter?: LumpSumQuarterRevenueByMonth | null;
	savingQuarter?: LumpSumQuarterRevenueByMonth | null;
	coalExcavationActualQuantity?: number;
	coalCrosscutActualQuantity?: number;
	meterExcavationActualQuantity?: number;
	meterCrosscutActualQuantity?: number;
	totalSavingQuarter?: number;
	acceptedSavingQuarter?: number;
	savingsValue?: number;
	revenueAdjustmentRate?: number;
	savingAddedToIncomeQuarter?: number;
}

export interface LumpSumFinalSettlementMonthResponse {
	items: LumpSumFinalSettlement[];
	revenue?: LumpSumQuarterRevenueByMonth | null;
	cost?: LumpSumQuarterRevenueByMonth | null;
	saving?: LumpSumQuarterRevenueByMonth | null;
	transferredCost?: LumpSumQuarterTransferredCost | null;
	customCosts?: LumpSumQuarterCustomCost[];
	coalExcavationActualQuantity?: number;
	coalCrosscutActualQuantity?: number;
	meterExcavationActualQuantity?: number;
	meterCrosscutActualQuantity?: number;
	totalSavingMonth?: number;
	savingsValue?: number;
	quyetToanSavingsLimit?: number;
	acceptedSavingMonth?: number;
	revenueAdjustmentRate?: number;
	savingAddedToIncomeMonth?: number;
	savingCarryForwardByMonths?: LumpSumSavingCarryForwardByMonth[];
	savingCarryForwardToNextMonths?: number;
}

export interface LumpSumSavingCarryForwardByMonth {
	month: number;
	value: number;
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
	month?: number | string;
	quarter?: number | string;
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
	month?: string;
	processGroupId: string;
}

export interface UpsertLumpSumQuarterCustomCostRequest {
	id?: string;
	year: string;
	month: string;
	quarter?: string;
	processGroupId: string;
	customName?: string;
	actualQuantity: number;
	materialUnitPrice: number;
	maintainUnitPrice: number;
	electricityUnitPrice: number;
}

export interface UpdateLumpSumMonthSpecialQuantityRequest {
	month: string;
	year: string;
	processGroupId?: string | null;
	coalExcavationActualQuantity: number;
	coalCrosscutActualQuantity: number;
}

export interface UpdateLumpSumMonthCarryForwardRequest {
	month: string;
	year: string;
	processGroupId?: string | null;
	savingCarryForwardToNextMonths: number;
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
	isSavingCarryForwardInputRow?: boolean;
	mergedValue?: number;
	isCustomCostRow?: boolean;
	isEditing?: boolean;
	isTransferredDefaultRow?: boolean;
	isSpecialQuantityRow?: boolean;
	specialQuantityField?:
		| 'coalExcavationActualQuantity'
		| 'coalCrosscutActualQuantity';
	month?: number;
	productName?: string;
	productCode?: string;
	unitOfMeasureId?: string;
	unitOfMeasureName?: string;
	plannedQuantity?: number;
	actualQuantity?: number;
	planAshContent?: number;
	actualAshContent?: number;
	ashContentDeltaPercent?: number;
	materials?: { unitPrice?: number; totalAmount?: number } | null;
	maintains?: { unitPrice?: number; totalAmount?: number } | null;
	electricities?: { unitPrice?: number; totalAmount?: number } | null;
	ashContentMaterials?: { unitPrice?: number; totalAmount?: number } | null;
	ashContentMaintains?: { unitPrice?: number; totalAmount?: number } | null;
	ashContentElectricities?: { unitPrice?: number; totalAmount?: number } | null;
	ashContentTotalAmount?: number;
	totalAmount?: number;
}

export interface YearFilterForm {
	month?: string;
	year?: string;
	processGroup?: string;
	department?: string;
}

export interface QuarterFilterForm {
	quarter?: string;
	year?: string;
	processGroup?: string;
	department?: string;
}

export interface ProcessGroup {
	id: string;
	code: string;
	name: string;
}
