import { z } from 'zod';

export const longTermAnchorSeedItemSchema = z.object({
	id: z.string(),
	materialId: z.string(),
	partId: z.string().optional(),
	processGroupId: z.string(),
	categoryAssignmentCodeId: z.string().nullable().optional(),
	categoryProductionOrderId: z.string().nullable().optional(),
	issuedQuantity: z.number().min(0),
	unitPrice: z.number().min(0),
	pendingValueStartPeriod: z
		.number()
		.positive('Tổng giá trị cần hạch toán phải > 0'),
	usageTime: z.number().min(0, 'Thời gian sử dụng phải >= 0'),
	allocatedTime: z.number().min(0, 'Thời gian đã phân bổ phải >= 0'),
	allocationRatio: z.number().min(0, 'Tỷ lệ phân bổ phải >= 0'),
	note: z.string().optional(),
});

export const longTermAnchorSeedProcessGroupMetricSchema = z.object({
	id: z.string(),
	processGroupId: z.string(),
	plannedOutput: z.number().min(0, 'Sản lượng kế hoạch phải >= 0'),
	standardOutput: z.number().min(0, 'Sản lượng định mức phải >= 0'),
});

export const longTermAnchorSeedSchema = z.object({
	departmentId: z.string(),
	processGroupMetrics: z.array(longTermAnchorSeedProcessGroupMetricSchema),
	items: z.array(longTermAnchorSeedItemSchema),
});

export type LongTermAnchorSeedSchema = z.infer<typeof longTermAnchorSeedSchema>;

export const LONG_TERM_ANCHOR_SEED_DEFAULT: LongTermAnchorSeedSchema = {
	departmentId: '',
	processGroupMetrics: [],
	items: [],
};
