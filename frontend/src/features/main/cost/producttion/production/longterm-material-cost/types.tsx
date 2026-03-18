export type LongtermMaterialDetailItem = {
	id: string;
	acceptanceReportItemId: string;
	processGroupId?: string;
	processGroupCode?: string;
	processGroupName?: string;
	partCode: string; // Mã phụ tùng
	partName: string; // Tên phụ tùng
	unitOfMeasureName: string; // ĐVT
	pendingValueStartPeriod: number; // GIÁ TRỊ CHỜ HẠCH TOÁN ĐẦU KỲ (Đồng)
	issuedQuantity: number; // SỐ LƯỢNG (trong GIÁ TRỊ PHÁT SINH TRONG KỲ)
	unitPrice: number; // ĐƠN GIÁ (trong GIÁ TRỊ PHÁT SINH TRONG KỲ)
	totalAmount: number; // THÀNH TIỀN (trong GIÁ TRỊ PHÁT SINH TRONG KỲ)
	totalValueToAccount: number; // TỔNG GIÁ TRỊ CẦN HẠCH TOÁN (Đồng)
	usageTime: number; // THỜI GIAN SỬ DỤNG (Ti)
	allocatedTime: number; // THỜI GIAN ĐÃ PHÂN BỔ
	remainingTime: number; // THỜI GIAN CÒN LẠI
	actualOutput?: number; // SẢN LƯỢNG THỰC TẾ
	plannedOutput?: number; // SẢN LƯỢNG THỰC TẾ
	standardOutput?: number; // SẢN LƯỢNG TIÊU CHUẨN
	valueByStandard: number; // GIÁ TRỊ CẦN HẠCH TOÁN THEO ĐỊNH MỨC (Đồng)
	allocationRatio: number; // TỶ LỆ PHÂN BỔ
	accountedValueThisPeriod: number; // GIÁ TRỊ DÀI KỲ HẠCH TOÁN KỲ NÀY (Đồng)
	pendingValueEndPeriod: number; // GIÁ TRỊ CUỐI KỲ CHỜ HẠCH TOÁN KỲ SAU (Đồng)
	isNewItem?: boolean;
	note?: string; // Ghi chú
};

export type LongTermTrackingProcessGroup = {
	processGroupId: string;
	processGroupCode?: string;
	processGroupName?: string;
	items: LongtermMaterialDetailItem[];
};

// API Response type
export type LongTermTrackingResponse = {
	acceptanceReportId: string;
	periodStartMonth: string;
	periodEndMonth: string;
	items: LongtermMaterialDetailItem[];
	processGroups?: LongTermTrackingProcessGroup[];
};

export type LongtermMaterialCostDetail = {
	acceptanceReportId: string;
	periodStartMonth: string;
	periodEndMonth: string;
	items: LongtermMaterialDetailItem[];
	processGroups?: LongTermTrackingProcessGroup[];
};

// Danh sách vật tư có thể select (giống Asset trong material-cost)
export type LongtermMaterialCostItem = {
	id: string;
	code: string;
	name: string;
	unit: string;
	unitPrice: number;
};
