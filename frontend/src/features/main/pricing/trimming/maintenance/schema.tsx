import z from 'zod';

export const trimmingCommonFormSchema = z.object({
	equipmentId: z
		.string()
		.nonempty({ error: 'Nhóm vật tư, tài sản không được để trống' }),
});

export type TrimmingCommonFormSchema = z.infer<typeof trimmingCommonFormSchema>;

export const TRIMMING_COMMON_FORM_DEFAULT: TrimmingCommonFormSchema = {
	equipmentId: '',
};

// Aliases for compatibility
export const trimmingFormSchema = trimmingCommonFormSchema;
export type TrimmingFormSchema = TrimmingCommonFormSchema;
export const TRIMMING_FORM_DEFAULT = TRIMMING_COMMON_FORM_DEFAULT;

export interface SCTXPeriodCost {
	partId: string;
	replacementTimeStandard: number;
	quantity: number;
	averageMonthlyTunnelProduction: number;
	equipmentId?: string;
}

export interface SCTXPeriodItemData {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	selectedPartIds: string[];
	costs: SCTXPeriodCost[];
	otherMaterialValue?: number;
}

export function validateSCTXPeriods(
	periods: SCTXPeriodItemData[],
): string | null {
	if (periods.length === 0) {
		return 'Mã đơn giá cần có ít nhất một khoảng thời gian áp dụng.';
	}

	for (let i = 0; i < periods.length; i++) {
		const period = periods[i];
		const label = `Khoảng thời gian thứ ${i + 1}`;

		if (!period.startMonth) {
			return `${label}: Thời gian bắt đầu không được để trống.`;
		}
		if (!period.endMonth) {
			return `${label}: Thời gian kết thúc không được để trống.`;
		}
		if (period.startMonth > period.endMonth) {
			return `${label}: Thời gian bắt đầu không được lớn hơn thời gian kết thúc.`;
		}

		if (!period.costs || period.costs.length === 0) {
			return `${label}: Vui lòng chọn ít nhất một vật tư, phụ tùng.`;
		}

		for (let j = 0; j < period.costs.length; j++) {
			const cost = period.costs[j];
			if (
				cost.replacementTimeStandard === undefined ||
				isNaN(cost.replacementTimeStandard) ||
				cost.replacementTimeStandard <= 0
			) {
				return `${label}: Định mức thời gian thay thế phải lớn hơn 0.`;
			}
			if (
				cost.quantity === undefined ||
				isNaN(cost.quantity) ||
				cost.quantity <= 0
			) {
				return `${label}: Số lượng vật tư 1 lần thay thế phải lớn hơn 0.`;
			}
			if (
				cost.averageMonthlyTunnelProduction === undefined ||
				isNaN(cost.averageMonthlyTunnelProduction) ||
				cost.averageMonthlyTunnelProduction <= 0
			) {
				return `${label}: Sản lượng xén lò bình quân phải lớn hơn 0.`;
			}
		}

		if (
			period.otherMaterialValue !== undefined &&
			(isNaN(period.otherMaterialValue) || period.otherMaterialValue < 0)
		) {
			return `${label}: Định mức vật tư khác không được âm.`;
		}
	}

	// Kiểm tra trùng lặp thời gian áp dụng giữa các kỳ
	for (let i = 0; i < periods.length; i++) {
		for (let j = i + 1; j < periods.length; j++) {
			const p1 = periods[i];
			const p2 = periods[j];
			if (p1.startMonth <= p2.endMonth && p1.endMonth >= p2.startMonth) {
				return `Khoảng thời gian thứ ${i + 1} và thứ ${j + 1} bị trùng lặp thời gian áp dụng.`;
			}
		}
	}

	return null;
}

