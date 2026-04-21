import z from 'zod';
import { PlanedElectricityCostAdjustmentSelection } from '@/features/main/cost/plan/planed-electricity-cost/types';

const plannedElectricityAdjustmentFactorSchema = z
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

export const planElectricityCostSchema = z.object({
	productUnitPriceId: z.string().nonempty({
		error: 'Mã giá sản phẩm không được để trống',
	}),
	outputId: z.string().nonempty({
		error: 'Mã kế hoạch không được để trống',
	}),
	trimmingCoefficient: z.coerce
		.number<number>({ error: 'Hệ số không hợp lệ' })
		.gt(0, { error: 'Hệ số phải lớn hơn 0' }),
	electricityUnitPriceIds: z
		.array(
			z.string().nonempty({
				error: 'Không được để trống',
			}),
		)
		.nonempty({
			error: 'Không được để trống',
		}),
	costs: z.array(
		z.object({
			electricityUnitPriceEquipmentId: z.string().nonempty({
				error: 'Không được để trống',
			}),
			quantity: z.coerce
				.number<number>({ error: 'Không phải là số' })
				.gt(0, { error: 'Phải lớn hơn 0' }),
			adjustmentFactorDescriptions: z.array(
				plannedElectricityAdjustmentFactorSchema,
			),
		}),
	),
});

export type PlanElectricityCostSchema = z.infer<
	typeof planElectricityCostSchema
>;

export const PLAN_ELECTRICITY_COST_DEFAULT: PlanElectricityCostSchema = {
	productUnitPriceId: '',
	outputId: '',
	trimmingCoefficient: 100,
	electricityUnitPriceIds: [],
	costs: [],
};

export const PLAN_ELECTRICITY_ADJUSTMENT_DEFAULT: PlanedElectricityCostAdjustmentSelection = {
	adjustmentFactorDescriptionId: '',
	adjustmentFactorId: '',
	customValue: null,
};
