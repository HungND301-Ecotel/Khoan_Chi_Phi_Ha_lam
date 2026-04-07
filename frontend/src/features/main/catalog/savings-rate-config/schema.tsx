import z from 'zod';

export const savingsRateConfigSchema = z.object({
	maxRevenue: z.number().nullable().optional(),
	isUnlimited: z.boolean(),
	maxSavingsRate: z.number().nullable().optional(),
	description: z.string().trim(),
}).superRefine((values, ctx) => {
	if (!values.isUnlimited && values.maxRevenue == null) {
		ctx.addIssue({
			code: 'custom',
			path: ['maxRevenue'],
			message: 'Tổng doanh thu 3 yếu tố không được để trống.',
		});
	}

	if (values.maxRevenue != null && values.maxRevenue < 0) {
		ctx.addIssue({
			code: 'custom',
			path: ['maxRevenue'],
			message: 'Tổng doanh thu 3 yếu tố phải lớn hơn hoặc bằng 0.',
		});
	}

	if (values.maxSavingsRate == null) {
		ctx.addIssue({
			code: 'custom',
			path: ['maxSavingsRate'],
			message: 'Giá trị tiết kiệm không được để trống.',
		});
	}

	if (values.maxSavingsRate != null && values.maxSavingsRate < 0) {
		ctx.addIssue({
			code: 'custom',
			path: ['maxSavingsRate'],
			message: 'Giá trị tiết kiệm phải lớn hơn hoặc bằng 0.',
		});
	}
});

export type SavingsRateConfigSchema = z.infer<typeof savingsRateConfigSchema>;

export const SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT: SavingsRateConfigSchema = {
	maxRevenue: null,
	isUnlimited: false,
	maxSavingsRate: null,
	description: '',
};
