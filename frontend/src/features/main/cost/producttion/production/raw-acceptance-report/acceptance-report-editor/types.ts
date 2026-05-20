export type AcceptanceReportEditorMode = 'import' | 'edit';

export type ProcessGroupOption = {
	value: string;
	label: string;
	type?: number;
};

export type ProductionOrderOption = {
	value: string;
	label: string;
};

export type MaterialLookupOption = {
	value: string;
	label: string;
	materialOrPartId: string;
	type: number;
	itemType: number;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
};

export type ImportedItemMeta = {
	materialOrPartId: string;
	type: number;
};

export type AssignmentCodeOption = {
	id: string;
	code: string;
	name: string;
};

export type Equipment = AssignmentCodeOption;

export type AcceptanceReportItemDto = {
	reportItemId: string | null;
	rowNumber: number;
	trackedMaterialId?: string | null;
	materialId?: string | null;
	partId?: string | null;
	partType?: number | null;
	materialCode: string;
	materialName?: string | null;
	trackedMaterialCode?: string | null;
	trackedMaterialName?: string | null;
	partName?: string | null;
	unitOfMeasureName: string;
	type: number;
	itemType: number;
	issuedQuantity: number;
	shippedQuantity: number;
};

export type UnresolvedAcceptanceReportItemDto = {
	rowNumber: number;
	reportItemId: string | null;
	materialCode: string;
	materialName?: string | null;
	unitOfMeasureName: string;
	issuedQuantity: number;
	shippedQuantity: number;
	unresolvedReason: string;
};

export type UploadAcceptanceReportResponseDto = {
	filePath: string;
	acceptanceReports: AcceptanceReportItemDto[];
	unresolvedAcceptanceReports: UnresolvedAcceptanceReportItemDto[];
};

export const ImportResolutionStatus = {
	Resolved: 'resolved',
	Unresolved: 'unresolved',
} as const;

export type ImportResolutionStatusValue =
	(typeof ImportResolutionStatus)[keyof typeof ImportResolutionStatus];

export type ProductionOrder = {
	id: string;
	code: string;
	name: string;
	startMonth: string;
	endMonth: string;
};

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

export const MaterialType = {
	Material: 1,
	SparePart: 2,
} as const;

export const ItemType = {
	InContract: 1,
	OutContract: 2,
	SafetyAndWelfare: 3,
	Resource: 4,
	QuotaMaterials: 5,
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

export type CategoryAllocation = {
	processGroupId: string | null;
	quantity: number | null;
	assignmentCodeIds?: string[];
	equipmentIds: string[];
};

export type QuotaBasedMaterialQuantityDetail = {
	type: number;
	quantity: number;
};

export type CreateAcceptanceReportItem = {
	acceptanceReportItemId: string | null;
	trackedMaterialId?: string | null;
	materialId?: string | null;
	partId?: string | null;
	usageTime: number;
	type: number;
	itemType: number;
	categoryAllocations?: CategoryAllocation[] | null;
	categoryProductionOrderId: string | null;
	categoryAssignmentCodeId?: string | null;
	categoryEquipmentId: string | null;
	additionalCostProductionOrderId: string | null;
	additionalCostAssignmentCodeId?: string | null;
	additionalCostEquipmentId: string | null;
	issuedDetails: QuantityDetail[];
	shippedDetails: QuantityDetail[];
	materialsIncludedInContractRevenue: number;
	isLongTermTracking: boolean;
	processGroupId: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	otherMaterialDetail: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity: number;
	quotaBasedMaterialQuantities?: QuotaBasedMaterialQuantityDetail[] | null;
	asset: number;
	assetMaterialQuantity: number;
};

export type CreateAcceptanceReportRequest = {
	productionOutputId: string;
	filePath: string;
	items: CreateAcceptanceReportItem[];
};

export type UpdateAcceptanceReportItem = {
	id?: string;
	trackedMaterialId?: string | null;
	materialId?: string | null;
	partId?: string | null;
	usageTime: number;
	type: number;
	itemType: number;
	categoryAllocations?: CategoryAllocation[] | null;
	categoryProductionOrderId: string | null;
	categoryAssignmentCodeId?: string | null;
	categoryEquipmentId: string | null;
	additionalCostProductionOrderId: string | null;
	additionalCostAssignmentCodeId?: string | null;
	additionalCostEquipmentId: string | null;
	issuedQuantity: number;
	shippedQuantity: number;
	issuedDetails: QuantityDetail[];
	shippedDetails: QuantityDetail[];
	materialsIncludedInContractRevenue: number;
	isLongTermTracking: boolean;
	processGroupId: string | null;
	materialsIncludedInContractRevenueQuantity: number;
	additionalCost: number;
	otherMaterialDetail: number;
	additionalCostQuantity: number;
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantities?: QuotaBasedMaterialQuantityDetail[] | null;
	asset: number;
	assetMaterialQuantity: number;
};

export type UpdateAcceptanceReportRequest = {
	id: string;
	filePath: string;
	items: UpdateAcceptanceReportItem[];
};

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
