// Base financial fields shared by all item types
export type FinancialFields = {
	// Đơn giá
	priceKH: number; // Kế hoạch
	priceTT: number; // Thực tế

	// Tồn đầu kỳ - Tổng cộng
	openingBalanceTotalQty: number; // SL
	openingBalanceTotalAmount: number; // Thành tiền

	// Tồn đầu kỳ - Tồn tại khai trường
	openingBalanceOnSiteQty: number;
	openingBalanceOnSiteAmount: number;

	// Tồn đầu kỳ - Chi phí chờ hạch toán
	openingBalancePendingQty: number;
	openingBalancePendingAmount: number;

	// Tồn đầu kỳ - Quyết định giao khoán công trình
	openingBalanceContractQty: number;
	openingBalanceContractAmount: number;

	// Lĩnh trong kỳ - Tổng cộng
	receiptTotalQty: number;
	receiptTotalAmountKH: number;

	// Lĩnh trong kỳ - Lĩnh vật tư (trả phiếu)
	receiptWithReceiptQty: number;
	receiptWithReceiptAmountKH: number;
	receiptWithReceiptAmountTT: number;

	// Lĩnh trong kỳ - Vay chưa trả phiếu
	receiptBorrowedQty: number;
	receiptBorrowedAmount: number;

	// Lĩnh trong kỳ - Trả phiếu tháng trước
	receiptReturnPrevMonthQty: number;
	receiptReturnPrevMonthAmount: number;

	// Lĩnh trong kỳ - Nhận bàn giao
	receiptHandoverQty: number;
	receiptHandoverPercent: number; // % còn lại
	receiptHandoverAmount: number;

	// Xuất trong kỳ - Tổng cộng
	issueTotalQty: number;
	issueTotalAmount: number;

	// Xuất trong kỳ - Xuất cho sản xuất
	issueForProductionQty: number;
	issueForProductionAmount: number;

	// Xuất trong kỳ - Chi phí vật tư dài kỳ hạch toán
	issueLongtermQty: number;
	issueLongtermAmount: number;

	// Xuất trong kỳ - Xuất khác số
	issueOtherQty: number;
	issueOtherAmount: number;

	// Xuất trong kỳ - Quyết định, giao khoán công trình
	issueContractQty: number;
	issueContractAmount: number;

	// Tồn cuối kỳ - Tổng cộng
	closingBalanceTotalQty: number;
	closingBalanceTotalAmount: number;

	// Tồn cuối kỳ - Tồn tại khai trường
	closingBalanceOnSiteQty: number;
	closingBalanceOnSiteAmount: number;

	// Tồn cuối kỳ - Giá trị cuối kỳ chờ hạch toán
	closingBalancePendingQty: number;
	closingBalancePendingAmount: number;

	// Tồn cuối kỳ - Quyết định, giao khoán công trình
	closingBalanceContractQty: number;
	closingBalanceContractAmount: number;
};

// Base item type for materials
export type MaterialItem = {
	id: string;
	assignmentCode: string; // Mã giao khoán (deprecated - use groupCode)
	groupCode: string; // New hierarchical group code
	groupName: string; // New hierarchical group name
	materialCode: string; // Mã vật tư
	materialName: string; // Tên vật tư
	unit: string; // ĐVT
} & FinancialFields;

// SCTX item type
export type SCTXItem = {
	id: string;
	assignmentCode: string; // Mã giao khoán
	assignmentName?: string; // Tên giao khoán
	equipmentCode: string; // Mã thiết bị (deprecated - use groupCode)
	groupCode: string; // New hierarchical group code
	groupName: string; // New hierarchical group name
	materialCode: string; // Mã vật tư
	materialName: string; // Tên vật tư
	sparePartCode: string; // Mã phụ tùng
	sparePartName: string; // Tên phụ tùng
	unit: string; // ĐVT
	classification?: 'KHVT' | 'LONGTERM' | null; // SCTX theo KHVT, SCTX dài kỳ phân bổ
} & FinancialFields;

// Asset item type
export type AssetItem = {
	id: string;
	assetGroup: string; // Deprecated - use groupCode
	groupCode: string; // New hierarchical group code
	groupName: string; // New hierarchical group name
	assetCode: string; // Mã tài sản
	assetName: string; // Tên tài sản
	unit: string;
} & FinancialFields;

// Quota material item with classification
export type QuotaMaterialItem = MaterialItem & {
	classification?: 'NEW' | 'REUSE' | null; // Lĩnh mới, Lĩnh tái sử dụng
};

// Unified item type that can represent any item type
export type UnifiedItem =
	| MaterialItem
	| SCTXItem
	| AssetItem
	| QuotaMaterialItem;

// Row type for hierarchical table
export type RowType = 'category' | 'type' | 'group' | 'item';

// Hierarchical row structure for rendering
export type HierarchicalRow = {
	id: string;
	rowType: RowType;
	label: string; // Legacy display text for hierarchical label
	level: number; // Indentation level (0-3)

	// Display metadata used by the table
	code?: string;
	name?: string;

	// Item data (only for 'item' rows)
	itemCode?: string; // Mã vật tư / Mã phụ tùng / Mã tài sản
	itemName?: string; // Tên vật tư / Tên phụ tùng / Tên tài sản
	unit?: string;

	// Financial data (for 'item' rows, or totals for 'category'/'type' rows)
	data?: Partial<FinancialFields>;
};

// New hierarchy structure types
export type GroupCodeGroup = {
	groupCode: string;
	groupName: string;
	displayCode?: string;
	isProductionOrder?: boolean;
	items: UnifiedItem[];
	showTotals?: boolean; // If true, group row shows totals like a type row (for "Lĩnh mới", "Lĩnh tái sử dụng")
	childGroups?: GroupCodeGroup[];
};

export type TypeGroup = {
	typeName: string; // 'Vật liệu', 'SCTX', 'Tài sản', etc.
	groups: GroupCodeGroup[]; // Can be empty for flat lists
	flatItems?: UnifiedItem[]; // For types without grouping (e.g., "Vật tư khác")
};

export type CategoryGroup = {
	categoryName: string; // 'Vật tư đã tính vào doanh thu khoán', etc.
	types: TypeGroup[];
};

// Main acceptance report data structure (new hierarchical)
export type HierarchicalAcceptanceReport = {
	id: string;
	categories: CategoryGroup[];
};

// Legacy group types (keep for backward compatibility during migration)
export type MaterialGroup = {
	assignmentCode: string; // Mã giao khoán (or "VTK" for other materials)
	assignmentName: string;
	items: MaterialItem[];
};

export type SCTXGroup = {
	assignmentCode: string; // Mã giao khoán
	assignmentName: string; // Tên giao khoán
	equipmentCode: string; // Mã thiết bị
	equipmentName: string;
	items: SCTXItem[];
};

export type QuotaMaterialGroup = {
	assignmentCode: string;
	assignmentName: string;
	items: QuotaMaterialItem[];
};

export type AssetGroup = {
	groupCode: string;
	groupName: string;
	items: AssetItem[];
};

// Category 1: Vật tư tính vào doanh thu khoán
export type ContractedRevenueCategory = {
	id: string;
	materialGroups: MaterialGroup[]; // Vật liệu (grouped by mã giao khoán)
	sctxGroups: SCTXGroup[]; // SCTX (grouped by thiết bị, with default classification rows)
};

// Category 2: Bổ sung chi phí
export type AdditionalCostCategory = {
	id: string;
	materialGroups: MaterialGroup[]; // Vật liệu (grouped by mã giao khoán)
	sctxGroups: SCTXGroup[]; // SCTX (grouped by thiết bị)
	otherMaterials: MaterialItem[]; // Vật tư khác (no grouping)
};

// Category 3: Vật tư theo hạn mức
export type QuotaCategory = {
	id: string;
	supportBeamGroups: QuotaMaterialGroup[]; // Vì chống lò (with classification rows)
	accessoriesGroups: QuotaMaterialGroup[]; // Phụ kiện chống lò (with classification rows)
	woodGroups: QuotaMaterialGroup[]; // Gỗ lò
};

// Category 4: Tài sản
export type AssetCategory = {
	id: string;
	assetGroups: AssetGroup[]; // Tài sản
};

// Main acceptance report type
export type AcceptanceReportDetail = {
	id: string;
	contractedRevenue: ContractedRevenueCategory;
	additionalCost: AdditionalCostCategory;
	quota: QuotaCategory;
	assets: AssetCategory;
};

// Flattened row type for rendering with rowSpan (deprecated - use HierarchicalRow)
export type FlatRow<T> = T & {
	rowSpan?: number; // For merged cells (group column)
	isFirstInGroup?: boolean; // Whether this is first row in group
};
