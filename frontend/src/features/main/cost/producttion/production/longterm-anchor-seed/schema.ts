import { z } from 'zod';

export const longTermAnchorSeedItemSchema = z.object({
	id: z.string(),
	materialId: z.string(),
	partId: z.string().optional(),
	processGroupId: z.string(),
	categoryAssignmentCodeId: z.string().nullable().optional(),
	categoryProductionOrderId: z.string().nullable().optional(),
	issuedQuantity: z.number().min(0, 'Số lượng phải >= 0'),
	unitPrice: z.number().min(0, 'Đơn giá phải >= 0'),
	pendingValueStartPeriod: z
		.number()
		.min(0, 'Giá trị chờ hạch toán đầu kỳ phải >= 0'),
	usageTime: z.number().min(0, 'Thời gian sử dụng phải >= 0'),
	allocatedTime: z.number().min(0, 'Thời gian đã phân bổ phải >= 0'),
	allocationRatio: z.number().min(0, 'Tỷ lệ phân bổ phải >= 0'),
	note: z.string().optional(),
}).superRefine((value, context) => {
	const hasPendingValueStartPeriod = value.pendingValueStartPeriod > 0;
	const hasIssuedQuantity = value.issuedQuantity > 0;
	const hasUnitPrice = value.unitPrice > 0;

	if (hasPendingValueStartPeriod && (hasIssuedQuantity || hasUnitPrice)) {
		context.addIssue({
			code: z.ZodIssueCode.custom,
			path: ['pendingValueStartPeriod'],
			message:
				'Không được nhập đồng thời giá trị chờ hạch toán đầu kỳ với số lượng hoặc đơn giá',
		});
	}

	if (!hasPendingValueStartPeriod && hasIssuedQuantity !== hasUnitPrice) {
		context.addIssue({
			code: z.ZodIssueCode.custom,
			path: ['issuedQuantity'],
			message: 'Phải nhập đồng thời số lượng và đơn giá khi không nhập giá trị đầu kỳ',
		});
	}

	if (!hasPendingValueStartPeriod && !hasIssuedQuantity && !hasUnitPrice) {
		context.addIssue({
			code: z.ZodIssueCode.custom,
			path: ['pendingValueStartPeriod'],
			message: 'Phải nhập giá trị đầu kỳ hoặc đồng thời số lượng và đơn giá',
		});
	}
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
