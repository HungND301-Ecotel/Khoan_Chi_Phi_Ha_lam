/* eslint-disable react-refresh/only-export-components */
import z from 'zod';

export const AkFactorConfigSchema = z
	.object({
		processGroupId: z.string().trim(),
		akDiffDisplay: z.string().trim(),
		adjustmentRateDisplay: z.string().trim(),
		description: z.string().trim(),
	})
	.superRefine((values, ctx) => {
		const numberPattern = '-?(?:\\d+)(?:[\\.,]\\d+)?';
		const akConditionRegex = new RegExp(
			`^\\s*(?:[≥>]=?\\s*${numberPattern}|[≤<]=?\\s*${numberPattern}|=\\s*${numberPattern}|${numberPattern})\\s*$`,
		);
		const percentValueRegex = new RegExp(`^\\s*${numberPattern}\\s*%\\s*$`);

		if (!values.processGroupId) {
			ctx.addIssue({
				code: 'custom',
				path: ['processGroupId'],
				message: 'Nhóm công đoạn sản xuất không được để trống.',
			});
		}

		if (!values.akDiffDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['akDiffDisplay'],
				message: 'Chênh lệch Ak không được để trống.',
			});
		}

		if (values.akDiffDisplay && !akConditionRegex.test(values.akDiffDisplay)) {
			ctx.addIssue({
				code: 'custom',
				path: ['akDiffDisplay'],
				message:
					'Chênh lệch Ak không đúng định dạng. Ví dụ: "> 0", "≤ -0,5", "= 1", "-1,5".',
			});
		}

		if (!values.adjustmentRateDisplay) {
			ctx.addIssue({
				code: 'custom',
				path: ['adjustmentRateDisplay'],
				message: 'Tỷ lệ điều chỉnh doanh thu không được để trống.',
			});
		}

		if (
			values.adjustmentRateDisplay &&
			!percentValueRegex.test(values.adjustmentRateDisplay)
		) {
			ctx.addIssue({
				code: 'custom',
				path: ['adjustmentRateDisplay'],
				message:
					'Tỷ lệ điều chỉnh doanh thu không đúng định dạng. Ví dụ: "1,5%", "-0,5%".',
			});
		}
	});

export type AkFactorConfigSchema = z.infer<typeof AkFactorConfigSchema>;

export const AK_FACTOR_CONFIG_SCHEMA_DEFAULT: AkFactorConfigSchema = {
	processGroupId: '',
	akDiffDisplay: '',
	adjustmentRateDisplay: '',
	description: '',
};
