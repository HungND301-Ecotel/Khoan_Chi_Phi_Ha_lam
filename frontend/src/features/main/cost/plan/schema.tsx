import { z } from 'zod';

const planOutputSchema = z.object({
	id: z.string().optional(),
	productionMeters: z.coerce
		.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
		.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' }),
	planAshContent: z.coerce
		.number<number>({ error: 'Ak kế hoạch phải là số' })
		.min(0, { error: 'Ak kế hoạch không được âm' })
		.optional(),
	startMonth: z.string().nonempty({ error: 'Thời gian không được để trống' }),
	outputType: z.number(),
	endMonth: z.string().optional(),
});

const departmentPlannedItemSchema = z.object({
	productUnitPriceId: z.string().optional(),
	outputId: z.string().optional(),
	productId: z.string().nonempty({
		error: 'Mã sản phẩm không được để trống',
	}),
	unitOfMeasureId: z.string().nonempty({
		error: 'Đơn vị tính không được để trống',
	}),
	productionMeters: z.coerce
		.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
		.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' }),
	planAshContent: z.coerce
		.number<number>({ error: 'Ak kế hoạch phải là số' })
		.min(0, { error: 'Ak kế hoạch không được âm' })
		.optional(),
});

const departmentPlannedMonthSchema = z.object({
	month: z.string().nonempty({ error: 'Thời gian không được để trống' }),
	items: z
		.array(departmentPlannedItemSchema)
		.min(1, { error: 'Danh sách sản phẩm không được để trống' }),
});

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
	outputs: z.array(planOutputSchema),
});

export const departmentPlanFormSchema = z
	.object({
		departmentId: z.string().nonempty({
			error: 'Đơn vị không được để trống',
		}),
		months: z
			.array(departmentPlannedMonthSchema)
			.min(1, { error: 'Danh sách thời gian không được để trống' }),
	})
	.superRefine((data, ctx) => {
		const monthIndexes = new Map<string, number>();
		const productUnits = new Map<string, string>();

		data.months.forEach((month, monthIndex) => {
			if (monthIndexes.has(month.month)) {
				ctx.addIssue({
					code: 'custom',
					message: 'Thời gian không được trùng',
					path: ['months', monthIndex, 'month'],
				});
			} else {
				monthIndexes.set(month.month, monthIndex);
			}

			const productIndexes = new Map<string, number>();
			month.items.forEach((item, itemIndex) => {
				if (productIndexes.has(item.productId)) {
					ctx.addIssue({
						code: 'custom',
						message: 'Sản phẩm không được trùng trong cùng tháng',
						path: ['months', monthIndex, 'items', itemIndex, 'productId'],
					});
				} else {
					productIndexes.set(item.productId, itemIndex);
				}

				const existingUnit = productUnits.get(item.productId);
				if (!existingUnit) {
					productUnits.set(item.productId, item.unitOfMeasureId);
				} else if (existingUnit !== item.unitOfMeasureId) {
					ctx.addIssue({
						code: 'custom',
						message:
							'Cùng một sản phẩm trong cùng đơn vị phải dùng một đơn vị tính',
						path: ['months', monthIndex, 'items', itemIndex, 'unitOfMeasureId'],
					});
				}
			});
		});
	});

export type PlanFormSchema = z.infer<typeof planFormSchema>;
export type DepartmentPlanFormSchema = z.infer<typeof departmentPlanFormSchema>;

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

export const DEPARTMENT_PLAN_FORM_DEFAULT: DepartmentPlanFormSchema = {
	departmentId: '',
	months: [
		{
			month: '',
			items: [
				{
					productId: '',
					unitOfMeasureId: '',
					productionMeters: NaN,
					planAshContent: 0,
				},
			],
		},
	],
};
