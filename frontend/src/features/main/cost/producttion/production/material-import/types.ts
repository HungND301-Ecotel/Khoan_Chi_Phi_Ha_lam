// Raw data from Excel file
export type MaterialImportRow = {
	materialCode: string;
	quantityReceived: number;
	quantityExported: number;
};

// API Response types
export type AcceptanceReportItemDto = {
	reportItemId: string | null;
	materialOrPartId: string;
	materialCode: string;
	unitOfMeasureName: string;
	type: number;
	issuedQuantity: number;
	shippedQuantity: number;
};

export type UploadAcceptanceReportResponseDto = {
	filePath: string;
	acceptanceReports: AcceptanceReportItemDto[];
};

// Enums as const objects (for TypeScript erasableSyntaxOnly)
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

export const QuotaBasedMaterialType = {
	New: 1,
	Reusable: 2,
} as const;

export const Asset = {
	None: 1,
	True: 2,
} as const;

// Create request types
export type CreateAcceptanceReportItem = {
	acceptanceReportItemId: string | null;
	materialOrPartId: string;
	type: number;
	issuedQuantity: number;
	shippedQuantity: number;
	materialsIncludedInContractRevenue: number;
	processGroupId: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity: number;
	asset: number;
	assetMaterialQuantity: number;
};

export type CreateAcceptanceReportRequest = {
	productionOutputId: string;
	filePath: string;
	items: CreateAcceptanceReportItem[];
};

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

// Material type for row highlighting
export const MaterialType = {
	Material: 1, // Vật liệu
	SparePart: 2, // Phụ tùng (SCTX)
} as const;

// Secondary options for contract limit (MineSupport / SupportAccessories only)
export const CONTRACT_LIMIT_SECONDARY_OPTIONS = [
	{ value: QuotaBasedMaterialType.New, label: 'Lĩnh mới' },
	{ value: QuotaBasedMaterialType.Reusable, label: 'Lĩnh tái sử dụng' },
];
