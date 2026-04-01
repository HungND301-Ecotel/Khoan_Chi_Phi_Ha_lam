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
		materialReferenceId: z
			.string()
			.nullable()
			.transform((val) => (val === '' ? null : val)),
		normFactorId: z
			.string()
			.nullable()
			.transform((val) => (val === '' ? null : val)),
		stoneClampRatioReferenceId: z
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
				return (
					data.stoneClampRatioReferenceId !== null &&
					data.stoneClampRatioReferenceId !== ''
				);
			}
			return true;
		},
		{
			message: 'Tỷ lệ đá kẹp không được để trống',
			path: ['stoneClampRatioReferenceId'],
		},
	);

export type PlanMaterialCostSchema = z.infer<typeof planMaterialCostSchema>;

export const PLAN_MATERIAL_COST_DEFAULT: PlanMaterialCostSchema = {
	productUnitPriceId: '',
	materialUnitPriceId: '',
	slideUnitPriceAssignmentCodeId: null,
	materialReferenceId: null,
	normFactorId: null,
	stoneClampRatioReferenceId: null,
	outputId: '',
};
