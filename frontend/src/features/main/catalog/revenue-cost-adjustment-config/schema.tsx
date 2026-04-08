import z from 'zod';

export const revenueCostAdjustmentConfigSchema = z
	.object({
		profitConditionDisplay: z.string().trim(),
		rateDisplay: z.string().trim(),
		description: z.string().trim(),
	})
	.superRefine((values, ctx) => {
		const numberPattern = '(?:\\d{1,3}(?:,\\d{3})*|\\d+)(?:\\.\\d+)?';
		const signedNumberPattern = `-?${numberPattern}`;
		const profitConditionRegex = new RegExp(
			`^\\s*(?:${signedNumberPattern}\\s*[-–]\\s*${signedNumberPattern}|[≥>]=?\\s*${signedNumberPattern}|[≤<]=?\\s*${signedNumberPattern}|${signedNumberPattern})\\s*$`,
		);
		const rateRegex = new RegExp(
			`^\\s*${signedNumberPattern}\\s*%?\\s*$`,
		);

		if (!values.profitConditionDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['profitConditionDisplay'],
				message: 'Điều kiện lợi nhuận không được để trống.',
			});
		}

		if (
			values.profitConditionDisplay &&
			!profitConditionRegex.test(values.profitConditionDisplay)
		) {
			ctx.addIssue({
				code: 'custom',
				path: ['profitConditionDisplay'],
				message:
					'Điều kiện lợi nhuận không đúng định dạng. Ví dụ: "≥ 0", "< 100000", "0 - 500000".',
			});
		}

		if (!values.rateDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['rateDisplay'],
				message: 'Tỷ lệ điều chỉnh không được để trống.',
			});
		}

		if (values.rateDisplay && !rateRegex.test(values.rateDisplay)) {
			ctx.addIssue({
				code: 'custom',
				path: ['rateDisplay'],
				message: 'Tỷ lệ điều chỉnh không đúng định dạng. Ví dụ: "60%" hoặc "-1".',
			});
		}

	});

export type RevenueCostAdjustmentConfigSchema = z.infer<
	typeof revenueCostAdjustmentConfigSchema
>;

export const REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT: RevenueCostAdjustmentConfigSchema =
	{
		profitConditionDisplay: '',
		rateDisplay: '',
		description: '',
	};
