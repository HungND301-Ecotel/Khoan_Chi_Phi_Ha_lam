export type RawAcceptanceReportItem = {
	id: string;
	acceptanceReportId: string;
	materialId: string | null;
	trackedMaterialId?: string | null;
	partId?: string | null;
	partType?: number | null;
	usageTime?: number;
	categoryAllocations?: CategoryAllocation[];
	categoryProductionOrderId?: string | null;
	categoryAssignmentCodeId?: string | null;
	categoryEquipmentId?: string | null;
	additionalCostProductionOrderId?: string | null;
	additionalCostAssignmentCodeId?: string | null;
	additionalCostEquipmentId?: string | null;
	materialCode: string | null;
	materialName: string | null;
	trackedMaterialCode?: string | null;
	trackedMaterialName?: string | null;
	partCode?: string | null;
	partName?: string | null;
	unitOfMeasureName: string | null;
	planCost: number;
	actualCost: number;
	issuedQuantity: number;
	shippedQuantity: number;
	type: number;
	materialsIncludedInContractRevenue: number;
	isLongTermTracking?: boolean;
	processGroupId: string | null;
	processGroupCode: string | null;
	processGroupName: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	otherMaterialDetail: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity?: number;
	quotaBasedMaterialQuantities?: QuotaBasedMaterialQuantityDetail[] | null;
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
	materialId: string | null;
	trackedMaterialId?: string | null;
	partId?: string | null;
	partType?: number | null;
	usageTime?: number;
	categoryAllocations?: CategoryAllocation[];
	categoryProductionOrderId?: string | null;
	categoryAssignmentCodeId?: string | null;
	categoryEquipmentId?: string | null;
	additionalCostProductionOrderId?: string | null;
	additionalCostAssignmentCodeId?: string | null;
	additionalCostEquipmentId?: string | null;
	itemType?: number;
	materialCode: string | null;
	materialName: string | null;
	trackedMaterialCode?: string | null;
	trackedMaterialName?: string | null;
	partCode?: string | null;
	partName?: string | null;
	unitOfMeasureName: string | null;
	planCost: number;
	actualCost: number;
	issuedQuantity: number;
	shippedQuantity: number;
	issuedDetails?: QuantityDetail[];
	shippedDetails?: QuantityDetail[];
	type: number;
	materialsIncludedInContractRevenue: number;
	isLongTermTracking?: boolean;
	processGroupId: string | null;
	processGroupCode: string | null;
	processGroupName: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	otherMaterialDetail: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity?: number;
	quotaBasedMaterialQuantities?: QuotaBasedMaterialQuantityDetail[] | null;
	assetMaterialQuantity: number;
	asset: number;
};

export type AcceptanceReportDetail = {
	id: string;
	productionOutputId: string;
	filePath: string;
	items: AcceptanceReportItem[];
};

export type ProductionOrder = {
	id: string;
	code: string;
	name: string;
	startMonth: string;
	endMonth: string;
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

export const QuotaBasedMaterialType = {
	New: 1,
	Reusable: 2,
} as const;

export const Asset = {
	None: 1,
	True: 2,
} as const;

export const OtherMaterialDetail = {
	None: 1,
	BaoHoLaoDong: 2,
	VatTuPhucVuCongTacAnToan: 3,
} as const;

// Material type for row highlighting
export const MaterialType = {
	Material: 1, // Vật liệu
	SparePart: 2, // Phụ tùng (SCTX)
} as const;

export const IssuedQuantityType = {
	LinhVatTuTraPhieu: 1,
	VayVhuaTraPhieu: 2,
	TraPhieuThangTruoc: 3,
	LinhKhac: 4,
} as const;

export const ShippedQuantityType = {
	XuatChoSanXuat: 1,
	XuatKhac: 2,
	QuyetToanGiaoKhoan: 3,
} as const;

export type QuantityDetail = {
	type: number;
	quantity: number;
};

export type QuotaBasedMaterialQuantityDetail = {
	type: number;
	quantity: number;
};

export type CategoryAllocation = {
	processGroupId: string | null;
	quantity: number | null;
	assignmentCodeIds: string[];
	equipmentIds: string[];
};

// Dropdown options
export const CATEGORY_OPTIONS = [
	{ value: MaterialsIncludedInContractRevenue.Material, label: 'Vật liệu' },
	{ value: MaterialsIncludedInContractRevenue.Maintain, label: 'SCTX' },
];

export const ADDITIONAL_COST_OPTIONS = [
	{ value: AdditionalCost.Material, label: 'Vật liệu' },
	{ value: AdditionalCost.Maintain, label: 'SCTX' },
	{
		value: AdditionalCost.OtherMaterial,
		label:
			'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
	},
];

export const CONTRACT_LIMIT_OPTIONS = [
	{ value: QuotaBasedMaterial.MineSupport, label: 'Vì chống lò' },
	{ value: QuotaBasedMaterial.SupportAccessories, label: 'Phụ kiện' },
	{ value: QuotaBasedMaterial.MineTimber, label: 'Gỗ lò' },
];

export const CONTRACT_LIMIT_SECONDARY_OPTIONS = [
	{ value: QuotaBasedMaterialType.New, label: 'Lĩnh mới' },
	{ value: QuotaBasedMaterialType.Reusable, label: 'Lĩnh tái sử dụng' },
];

export const OTHER_MATERIAL_DETAIL_OPTIONS = [
	{ value: OtherMaterialDetail.BaoHoLaoDong, label: 'Bảo hộ lao động' },
	{
		value: OtherMaterialDetail.VatTuPhucVuCongTacAnToan,
		label: 'Vật tư phục vụ công tác an toàn',
	},
];

export const RECEIVED_TYPE_OPTIONS = [
	{ value: 'receipt_voucher', label: 'Lĩnh vật tư (trả phiếu)' },
	{ value: 'loan_no_voucher', label: 'Vay chưa trả phiếu' },
	{ value: 'prev_month_voucher', label: 'Trả phiếu tháng trước' },
	{ value: 'other_received', label: 'Lĩnh khác' },
];

export const EXPORTED_TYPE_OPTIONS = [
	{ value: 'production', label: 'Xuất cho sản xuất' },
	{ value: 'other_export', label: 'Xuất khác' },
	{ value: 'settlement', label: 'Quyết toán, giao khoán công trình' },
];

export const ISSUED_DETAIL_TYPE_BY_KEY = {
	receipt_voucher: IssuedQuantityType.LinhVatTuTraPhieu,
	loan_no_voucher: IssuedQuantityType.VayVhuaTraPhieu,
	prev_month_voucher: IssuedQuantityType.TraPhieuThangTruoc,
	other_received: IssuedQuantityType.LinhKhac,
} as const;

export const SHIPPED_DETAIL_TYPE_BY_KEY = {
	production: ShippedQuantityType.XuatChoSanXuat,
	other_export: ShippedQuantityType.XuatKhac,
	settlement: ShippedQuantityType.QuyetToanGiaoKhoan,
} as const;

export const ISSUED_DETAIL_KEY_BY_TYPE: Record<number, string> = {
	[IssuedQuantityType.LinhVatTuTraPhieu]: 'receipt_voucher',
	[IssuedQuantityType.VayVhuaTraPhieu]: 'loan_no_voucher',
	[IssuedQuantityType.TraPhieuThangTruoc]: 'prev_month_voucher',
	[IssuedQuantityType.LinhKhac]: 'other_received',
};

export const SHIPPED_DETAIL_KEY_BY_TYPE: Record<number, string> = {
	[ShippedQuantityType.XuatChoSanXuat]: 'production',
	[ShippedQuantityType.XuatKhac]: 'other_export',
	[ShippedQuantityType.QuyetToanGiaoKhoan]: 'settlement',
};

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
