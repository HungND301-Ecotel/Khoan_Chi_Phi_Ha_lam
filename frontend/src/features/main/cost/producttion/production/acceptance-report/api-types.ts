/**
 * API Response DTOs for PRODUCTION.ACCEPTANCE_REPORT.DETAIL
 * Response structure with hierarchical categorization of materials
 */

// Inventory information for materials
export type InventoryInfo = {
	quantity: number;
	amount: number;
};

export type IssuedInPeriodInfo = {
	received: {
		quantity: number;
		plannedAmount: number;
		actualAmount: number;
	};
	borrowedNoVoucher?: {
		quantity: number;
		amount: number;
	};
	returnPreviousMonthVoucher?: {
		quantity: number;
		amount: number;
	};
	otherReceipt?: {
		quantity: number;
		amount: number;
	};
	total: {
		quantity: number;
		amount: number;
	};
};

export type ExportedInPeriodInfo = {
	exportedToProduction: {
		quantity: number;
		amount: number;
	};
	otherExport?: {
		quantity: number;
		amount: number;
	};
	contractSettlement?: {
		quantity: number;
		amount: number;
	};
	longTermExpense: {
		amount: number;
	} | null;
	total: {
		quantity: number;
		amount: number;
	};
};

export type InventoryBalanceInfo = {
	quantity: number;
	amount: number;
};

export type BeginningInventoryInfo = {
	remainingAtSite?: {
		quantity: number;
		amount: number;
	} | null;
	remainingByOrder?: {
		quantity: number;
		amount: number;
	} | null;
	pendingValue: number;
	total: {
		quantity: number;
		amount: number;
	};
} | null;

export type EndingInventoryInfo = {
	remainingAtSite?: {
		quantity: number;
		amount: number;
	} | null;
	remainingByOrder?: {
		quantity: number;
		amount: number;
	} | null;
	pendingValue?: number | null;
	total: {
		quantity: number;
		amount: number;
	};
} | null;

// Material detail within a material group
export type MaterialDetailDto = {
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
	plannedUnitPrice: number;
	actualUnitPrice: number;
	issuedInPeriod: IssuedInPeriodInfo;
	exportedInPeriod: ExportedInPeriodInfo;
	beginningInventory: BeginningInventoryInfo;
	endingInventory: EndingInventoryInfo;
	state: string | null;
};

// Sub group for materials (used in category type 3)
export type SubGroupDto = {
	subGroupCode: string;
	subGroupName?: string;
	materials: MaterialDetailDto[];
};

// Material group containing materials with same equipment/assignment code
export type MaterialGroupDto = {
	groupCode: string;
	groupName: string;
	materialType: string;
	sectionAType?: number | null;
	additionalCostType?: number | null;
	productionOrderId?: string | null;
	otherMaterialDetail?: string | number | null;
	materials: MaterialDetailDto[];
	subGroups: SubGroupDto[];
};

// Item category (e.g., CategoryType 1-4)
export type ProductionOutputDetailItemDto = {
	categoryType: number;
	categoryName: string;
	materialGroups: MaterialGroupDto[];
};

export type ProductionOutputProcessGroupProductDto = {
	productId: string;
	productCode?: string;
	productName?: string;
	productionMeters: number;
};

export type ProductionOutputProcessGroupDto = {
	processGroupId: string;
	processGroupCode?: string;
	processGroupName?: string;
	standardProductionMeters?: number;
	productionMeters?: number;
	products?: ProductionOutputProcessGroupProductDto[];
};

export type AcceptanceReportSectionKey =
	| 'sectionA'
	| 'sectionB'
	| 'sectionC'
	| 'sectionD';

// Production output information
export type ProductionOutputDto = {
	productionOutputId: string;
	startMonth: string;
	endMonth: string;
	productionMeters: number;
	standardProductionMeters: number;
	sectionA?: MaterialGroupDto[];
	sectionB?: MaterialGroupDto[];
	sectionC?: MaterialGroupDto[];
	sectionD?: MaterialGroupDto[];
	items?: ProductionOutputDetailItemDto[];
	processGroups?: ProductionOutputProcessGroupDto[];
};

// API Response wrapper
export type AcceptanceReportDetailResponseDto = {
	result: ProductionOutputDto;
	success: boolean;
	message: string;
};
