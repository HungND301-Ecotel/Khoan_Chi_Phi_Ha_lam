import z from 'zod';

export const normFactorSchema = z.object({
	processGroupId: z.string().optional(),
	productionProcessId: z.string().nonempty({
		error: 'Công đoạn sản xuất không được để trống.',
	}),
	stoneClampRatioId: z.string().nonempty({
		error: 'Tỷ lệ đá kẹp không được để trống.',
	}),
	hardnessId: z.string().nonempty({
		error: 'Độ kiên cố than đá không được để trống.',
	}),
	assignmentCodeIds: z.array(z.string()).min(1, {
		message: 'Mã giao khoán không được để trống',
	}),
	value: z.coerce
		.number<number>({
			error: 'Hệ số điều chỉnh định mức không được để trống.',
		})
		.min(0, {
			error: 'Hệ số điều chỉnh định mức không được để trống.',
		}),
	targetHardnessId: z.string().optional(),
});

export type NormFactorSchema = z.infer<typeof normFactorSchema>;

export const NORM_FACTOR_SCHEMA_DEFAULT: NormFactorSchema = {
	productionProcessId: '',
	hardnessId: '',
	stoneClampRatioId: '',
	assignmentCodeIds: [],
	value: NaN,
	targetHardnessId: '',
};
