import z from 'zod';

export const savingsRateConfigSchema = z
	.object({
		revenueDisplay: z.string().trim(),
		savingsRateDisplay: z.string().trim(),
		description: z.string().trim(),
	})
	.superRefine((values, ctx) => {
		const numberPattern = '(?:\\d{1,3}(?:,\\d{3})*|\\d+)(?:\\.\\d+)?';
		const revenueRegex = new RegExp(
			`^\\s*(?:${numberPattern}\\s*[-–]\\s*${numberPattern}|[≥>]=?\\s*${numberPattern}|[≤<]=?\\s*${numberPattern}|${numberPattern})\\s*$`,
		);
	const savingsRateRegex = new RegExp(
		`^\\s*(?:${numberPattern}\\s*[-–]\\s*${numberPattern}|[≥>]=?\\s*${numberPattern}|[≤<]=?\\s*${numberPattern}|${numberPattern})\\s*%\\s*$`,
	);

		if (!values.revenueDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['revenueDisplay'],
				message: 'Tổng doanh thu 3 yếu tố không được để trống.',
			});
		}

		if (values.revenueDisplay && !revenueRegex.test(values.revenueDisplay)) {
			ctx.addIssue({
				code: 'custom',
				path: ['revenueDisplay'],
				message:
					'Tổng doanh thu 3 yếu tố không đúng định dạng. Ví dụ: "≥ 300000", "< 500000", "300000 - 500000".',
			});
		}

		if (!values.savingsRateDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['savingsRateDisplay'],
				message: 'Giá trị tiết kiệm không được để trống.',
			});
		}

		if (
			values.savingsRateDisplay &&
			!savingsRateRegex.test(values.savingsRateDisplay)
		) {
			ctx.addIssue({
				code: 'custom',
				path: ['savingsRateDisplay'],
				message:
					'Giá trị tiết kiệm không đúng định dạng. Ví dụ đúng: "≥ 8%", "< 12%", "8 - 12%".',
			});
		}
	});

export type SavingsRateConfigSchema = z.infer<typeof savingsRateConfigSchema>;

export const SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT: SavingsRateConfigSchema = {
	revenueDisplay: '',
	savingsRateDisplay: '',
	description: '',
};
