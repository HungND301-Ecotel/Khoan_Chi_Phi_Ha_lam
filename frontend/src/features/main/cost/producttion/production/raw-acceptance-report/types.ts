export type RawAcceptanceReportItem = {
	id: string;
	acceptanceReportId: string;
	materialId: string;
	maintainUnitPriceEquipmentId: string | null;
	materialCode: string | null;
	materialName: string | null;
	partCode: string | null;
	partName: string | null;
	unitOfMeasureName: string | null;
	planCost: number;
	actualCost: number;
	issuedQuantity: number;
	shippedQuantity: number;
	type: number;
	materialsIncludedInContractRevenue: number;
	processGroupId: string | null;
	processGroupCode: string | null;
	processGroupName: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity: number;
	assetMaterialQuantity: number;
	asset: number;
};

export type RawAcceptanceReportDetail = {
	id: string;
	productionId: string;
	items: RawAcceptanceReportItem[];
};

// API Response Types
export type AcceptanceReportItem = {
	id: string;
	acceptanceReportId: string;
	materialId: string;
	maintainUnitPriceEquipmentId: string | null;
	materialCode: string | null;
	materialName: string | null;
	partCode: string | null;
	partName: string | null;
	unitOfMeasureName: string | null;
	planCost: number;
	actualCost: number;
	issuedQuantity: number;
	shippedQuantity: number;
	type: number;
	materialsIncludedInContractRevenue: number;
	processGroupId: string | null;
	processGroupCode: string | null;
	processGroupName: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity: number;
	assetMaterialQuantity: number;
	asset: number;
};

export type AcceptanceReportDetail = {
	id: string;
	productionOutputId: string;
	filePath: string;
	items: AcceptanceReportItem[];
};

// Enums as const objects
export const MaterialsIncludedInContractRevenue = {
	None: 1,
	Material: 2,
	Maintain: 3,
} as const;

export const AdditionalCost = {
	None: 1,
	Material: 2,
	Maintain: 3,
	OtherMaterial: 4,
} as const;

export const QuotaBasedMaterial = {
	None: 1,
	MineSupport: 2,
	SupportAccessories: 3,
	MineTimber: 4,
} as const;

export const Asset = {
	None: 1,
	True: 2,
} as const;

// Material type for row highlighting
export const MaterialType = {
	Material: 1, // Vật liệu
	SparePart: 2, // Phụ tùng (SCTX)
} as const;

// Dropdown options
export const CATEGORY_OPTIONS = [
	{ value: MaterialsIncludedInContractRevenue.Material, label: 'Vật liệu' },
	{ value: MaterialsIncludedInContractRevenue.Maintain, label: 'SCTX' },
];

export const ADDITIONAL_COST_OPTIONS = [
	{ value: AdditionalCost.Material, label: 'Vật liệu' },
	{ value: AdditionalCost.Maintain, label: 'SCTX' },
	{ value: AdditionalCost.OtherMaterial, label: 'Vật tư khác' },
];

export const CONTRACT_LIMIT_OPTIONS = [
	{ value: QuotaBasedMaterial.MineSupport, label: 'Vì chống lò' },
	{ value: QuotaBasedMaterial.SupportAccessories, label: 'Phụ kiện' },
	{ value: QuotaBasedMaterial.MineTimber, label: 'Gỗ lò' },
];

export const CONTRACT_LIMIT_SECONDARY_OPTIONS = [
	{ value: 1, label: 'Lĩnh mới' },
	{ value: 2, label: 'Lĩnh tái sử dụng' },
];

// export const ASSET_OPTIONS = [
// 	{ value: 'Vật liệu', label: 'Vật liệu' },
// 	{ value: 'SCTX', label: 'SCTX' },

// Mock data for testing (no longer needed as we use API)
export const MOCK_RAW_ACCEPTANCE_REPORT_ITEMS: RawAcceptanceReportItem[] = [];

export const MOCK_RAW_ACCEPTANCE_REPORT_DETAIL: RawAcceptanceReportDetail = {
	id: 'raw-001',
	productionId: 'prod-001',
	items: MOCK_RAW_ACCEPTANCE_REPORT_ITEMS,
};
