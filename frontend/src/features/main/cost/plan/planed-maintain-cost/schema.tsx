import z from 'zod';

export const planMaintainCostSchema = z.object({
	productUnitPriceId: z.string().nonempty({
		error: 'Mã giá sản phẩm không được để trống',
	}),
	outputId: z.string().nonempty({
		error: 'Mã kế hoạch không được để trống',
	}),
	maintainUnitPriceIds: z
		.array(
			z.string().nonempty({
				error: 'Mã thiết bị không được để trống',
			}),
		)
		.nonempty({
			error: 'Mã thiết bị không được để trống',
		}),
	costs: z
		.array(
			z.object({
				maintainUnitPriceId: z.string().nonempty({
					error: 'Mã định mức SCTX không được để trống',
				}),
				quantity: z.coerce
					.number<number>({ error: 'Số lượng không được để trống' })
					.gt(0, { error: 'Không được để trống' }),
				adjustmentFactorDescriptions: z.array(z.string()),
				k6AdjustmentFactorValue: z.number(),
			}),
		)
		.superRefine((costs, ctx) => {
			costs.forEach((cost, costIndex) => {
				cost.adjustmentFactorDescriptions.forEach((desc, descIndex) => {
					if (!desc || desc === '') {
						ctx.addIssue({
							code: 'custom',
							message: 'Không được để trống',
							path: [costIndex, 'adjustmentFactorDescriptions', descIndex],
						});
					}
				});
			});
		}),
});

export type PlanMaintainCostSchema = z.infer<typeof planMaintainCostSchema>;

export const PLAN_MAINTAIN_COST_DEFAULT: PlanMaintainCostSchema = {
	productUnitPriceId: '',
	outputId: '',
	maintainUnitPriceIds: [],
	costs: [],
};
