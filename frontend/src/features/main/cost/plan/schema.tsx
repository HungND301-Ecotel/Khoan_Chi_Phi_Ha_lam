import { z } from 'zod';

export const planFormSchema = z.object({
	productId: z.string().nonempty({
		error: 'Mã sản phẩm không được để trống',
	}),
	unitOfMeasureId: z.string().nonempty({
		error: 'Đơn vị tính không được để trống',
	}),
	departmentId: z.string().nonempty({
		error: 'Đơn vị không được để trống',
	}),
	outputs: z.array(
		z.object({
			id: z.string().optional(),
			productionMeters: z.coerce
				.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
				.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' }),
			planAshContent: z.coerce
				.number<number>({ error: 'Ak kế hoạch phải là số' })
				.min(0, { error: 'Ak kế hoạch không được âm' })
				.optional(),
			startMonth: z
				.string()
				.nonempty({ error: 'Thời gian không được để trống' }),
			outputType: z.number(),
			endMonth: z.string().optional(),
		}),
	),
});

export type PlanFormSchema = z.infer<typeof planFormSchema>;

export const PLAN_FORM_DEFAULT: PlanFormSchema = {
	productId: '',
	unitOfMeasureId: '',
	departmentId: '',
	outputs: [
		{
			productionMeters: NaN,
			planAshContent: 0,
			startMonth: '',
			endMonth: '',
			outputType: 1,
		},
	],
};
