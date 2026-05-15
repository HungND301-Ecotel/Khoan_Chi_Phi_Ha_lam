import z from 'zod';

export const materialFormSchema = z.object({
	code: z
		.string()
		.nonempty({ error: 'Mã định mức vật liệu không được để trống' }),
	processId: z.string().nonempty({ error: 'Công đoạn không được để trống' }),
	passportId: z
		.string()
		.nonempty({ error: 'Hộ chiếu, Sđ, Sc không được để trống' }),
	hardnessId: z
		.string()
		.nonempty({ error: 'Độ kiên cố đá/ than (f) không được để trống' }),
	insertItemId: z.string().nonempty({ error: 'Chèn không được để trống' }),
	supportStepId: z
		.string()
		.nonempty({ error: 'Bước chống không được để trống' }),
	startMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	endMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	costs: z.array(
		z.object({
			assignmentCodeId: z
				.string()
				.nonempty({ error: 'ID Nhóm vật tư, tài sản không được để trống' }),
			totalPrice: z
				.number({ message: 'Đơn giá không được để trống' })
				.min(0, { message: 'Đơn giá phải lớn hơn hoặc bằng 0' }),
		}),
	),
	otherMaterialValue: z
		.number({ message: 'Tỷ lệ vật tư khác không được để trống' })
		.min(0, { message: 'Tỷ lệ phải lớn hơn hoặc bằng 0' })
		.optional(),
});

export type MaterialFormSchema = z.infer<typeof materialFormSchema>;

export const MATERIAL_FORM_DEFAULT: MaterialFormSchema = {
	code: '',
	processId: '',
	passportId: '',
	hardnessId: '',
	insertItemId: '',
	supportStepId: '',
	startMonth: '',
	endMonth: '',
	costs: [],
	otherMaterialValue: undefined,
};
