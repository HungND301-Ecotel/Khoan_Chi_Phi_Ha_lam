import z from 'zod';

export const planElectricityCostSchema = z.object({
	productUnitPriceId: z.string().nonempty({
		error: 'Mã giá sản phẩm không được để trống',
	}),
	outputId: z.string().nonempty({
		error: 'Mã kế hoạch không được để trống',
	}),
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
				z.string().nonempty({
					error: 'Không được để trống',
				}),
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
	electricityUnitPriceIds: [],
	costs: [],
};
