import { ProcessGroupType } from '@/constants/process-group';
import z from 'zod';

export const planMaterialCostSchema = z
	.object({
		productUnitPriceId: z.string().nonempty({
			error: 'Mã giá sản phẩm không được để trống',
		}),
		materialUnitPriceId: z.string().nonempty({
			error: 'Mã giá vật tư không được để trống',
		}),
		slideUnitPriceAssignmentCodeId: z
			.string()
			.nullable()
			.transform((val) => (val === '' ? null : val)),
		normFactorId: z
			.string()
			.nullable()
			.transform((val) => (val === '' ? null : val)),
		outputId: z.string().nonempty({
			error: 'Mã kế hoạch không được để trống',
		}),
		processGroupType: z.number().optional().nullable(),
	})
	.refine(
		(data) => {
			if (data.processGroupType === ProcessGroupType.DL) {
				return data.normFactorId !== null && data.normFactorId !== '';
			}
			return true;
		},
		{
			message: 'Độ kiên cố đá không được để trống',
			path: ['normFactorId'],
		},
	);

export type PlanMaterialCostSchema = z.infer<typeof planMaterialCostSchema>;

export const PLAN_MATERIAL_COST_DEFAULT: PlanMaterialCostSchema = {
	productUnitPriceId: '',
	materialUnitPriceId: '',
	slideUnitPriceAssignmentCodeId: null,
	normFactorId: null,
	outputId: '',
};
