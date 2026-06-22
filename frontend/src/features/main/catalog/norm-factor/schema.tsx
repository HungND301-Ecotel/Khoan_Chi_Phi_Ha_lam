import z from 'zod';

const assignmentCodeConfigSchema = z.object({
	assignmentCodeId: z.string().nonempty({
		error: 'Nhóm vật tư, tài sản không được để trống',
	}),
	materialId: z.string().nonempty({
		error: 'Vật tư chi tiết không được để trống.',
	}),
	value: z.coerce
		.number<number>({
			error: 'Hệ số điều chỉnh định mức không được để trống.',
		})
		.min(0, {
			error: 'Hệ số điều chỉnh định mức không được để trống.',
		}),
	targetHardnessId: z.string().nullable().optional(),
});

export const normFactorSchema = z
	.object({
		processGroupId: z.string().optional(),
		productionProcessId: z.string().nonempty({
			error: 'Công đoạn sản xuất không được để trống.',
		}),
		stoneClampRatioId: z.string().nullable().optional(),
		hardnessId: z.string().nullable().optional(),
		isMechanizedLongwall: z.boolean(),
		steelMeshType: z.coerce.number<number>(),
		assignmentCodeIds: z.array(z.string()).min(1, {
			message: 'Nhóm vật tư, tài sản không được để trống',
		}),
		assignmentCodeConfigs: z.array(assignmentCodeConfigSchema).min(1, {
			message: 'Cấu hình Nhóm vật tư, tài sản không được để trống',
		}),
	})
	.superRefine((data, ctx) => {
		if (!data.isMechanizedLongwall && !data.hardnessId) {
			ctx.addIssue({
				code: 'custom', // ✅ dùng string literal thay vì enum
				message: 'Độ kiên cố than đá không được để trống.',
				path: ['hardnessId'],
			});
		}

		if (data.isMechanizedLongwall && data.steelMeshType <= 1) {
			ctx.addIssue({
				code: 'custom',
				message: 'Chọn lớp lưới thép không được để trống.',
				path: ['steelMeshType'],
			});
		}

		if (!data.isMechanizedLongwall && data.steelMeshType !== 1) {
			ctx.addIssue({
				code: 'custom',
				message: 'Khi không chọn CGH thì lớp lưới thép phải là Không áp dụng.',
				path: ['steelMeshType'],
			});
		}

		for (const assignmentCodeId of data.assignmentCodeIds) {
			const hasConfig = data.assignmentCodeConfigs.some(
				(config) => config.assignmentCodeId === assignmentCodeId,
			);

			if (!hasConfig) {
				ctx.addIssue({
					code: 'custom',
					message: 'Thiếu cấu hình cho Nhóm vật tư, tài sản đã chọn.',
					path: ['assignmentCodeConfigs'],
				});
				break;
			}
		}
	});

export type NormFactorSchema = z.infer<typeof normFactorSchema>;

export const NORM_FACTOR_SCHEMA_DEFAULT: NormFactorSchema = {
	productionProcessId: '',
	hardnessId: '',
	isMechanizedLongwall: false,
	steelMeshType: 1,
	stoneClampRatioId: '',
	assignmentCodeIds: [],
	assignmentCodeConfigs: [],
};
