import { z } from 'zod';

export const actualFormSchema = z.object({
	productId: z.string().nonempty({
		error: 'Mã sản phẩm không được để trống',
	}),
	unitOfMeasureId: z.string().nonempty({
		error: 'Đơn vị tính không được để trống',
	}),
	productionOutputs: z.array(z.string()),
	outputs: z.array(
		z
			.object({
				id: z.string().optional(),
				productionMeters: z.coerce
					.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
					.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' }),
				startMonth: z
					.string()
					.nonempty({ error: 'Thời gian không được để trống' }),
				outputType: z.number(),
				endMonth: z
					.string()
					.nonempty({ error: 'Thời gian không được để trống' }),
			})
			.superRefine((data, ctx) => {
				const startMonth = data.startMonth;
				const endMonth = data.endMonth;

				if (startMonth > endMonth) {
					ctx.addIssue({
						code: 'custom',
						message: 'Thời gian kết thúc phải sau Thời gian bắt đầu.',
						path: ['endMonth'],
					});
				}
			}),
	),
});

export type ActualFormSchema = z.infer<typeof actualFormSchema>;

export const ACTUAL_FORM_DEFAULT: ActualFormSchema = {
	productId: '',
	unitOfMeasureId: '',
	productionOutputs: [''],
	outputs: [
		{
			productionMeters: 0,
			startMonth: '',
			endMonth: '',
			outputType: 1,
		},
	],
};
