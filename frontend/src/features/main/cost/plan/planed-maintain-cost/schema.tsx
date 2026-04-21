import z from 'zod';
import { PlannedMaintainCostAdjustmentSelection } from '@/features/main/cost/plan/planed-maintain-cost/types';

const plannedMaintainAdjustmentFactorSchema = z
	.object({
		adjustmentFactorDescriptionId: z.string(),
		adjustmentFactorId: z.string(),
		customValue: z.number().nullable(),
	})
	.superRefine((value, ctx) => {
		const hasDescription = value.adjustmentFactorDescriptionId !== '';
		const hasCustomValue = value.customValue !== null;

		if (hasDescription === hasCustomValue) {
			ctx.addIssue({
				code: 'custom',
				message: 'Chọn hệ số hoặc nhập giá trị tùy chỉnh',
				path: ['adjustmentFactorDescriptionId'],
			});
		}

		if (hasCustomValue && value.adjustmentFactorId === '') {
			ctx.addIssue({
				code: 'custom',
				message: 'Thiếu loại hệ số',
				path: ['customValue'],
			});
		}
	});

export const planMaintainCostSchema = z.object({
	productUnitPriceId: z.string().nonempty({
		error: 'Mã giá sản phẩm không được để trống',
	}),
	outputId: z.string().nonempty({
		error: 'Mã kế hoạch không được để trống',
	}),
	trimmingCoefficient: z.coerce
		.number<number>({ error: 'Hệ số không hợp lệ' })
		.gt(0, { error: 'Hệ số phải lớn hơn 0' }),
	maintainUnitPriceIds: z
		.array(
			z.string().nonempty({
				error: 'Mã thiết bị không được để trống',
			}),
		)
		.nonempty({
			error: 'Mã thiết bị không được để trống',
		}),
	costs: z.array(
		z.object({
			maintainUnitPriceId: z.string().nonempty({
				error: 'Mã định mức SCTX không được để trống',
			}),
			quantity: z.coerce
				.number<number>({ error: 'Số lượng không được để trống' })
				.gt(0, { error: 'Không được để trống' }),
			adjustmentFactorDescriptions: z.array(
				plannedMaintainAdjustmentFactorSchema,
			),
			k6AdjustmentFactorValue: z.number(),
		}),
	),
});

export type PlanMaintainCostSchema = z.infer<typeof planMaintainCostSchema>;

export const PLAN_MAINTAIN_COST_DEFAULT: PlanMaintainCostSchema = {
	productUnitPriceId: '',
	outputId: '',
	trimmingCoefficient: 100,
	maintainUnitPriceIds: [],
	costs: [],
};

export const PLAN_MAINTAIN_ADJUSTMENT_DEFAULT: PlannedMaintainCostAdjustmentSelection =
	{
		adjustmentFactorDescriptionId: '',
		adjustmentFactorId: '',
		customValue: null,
	};
