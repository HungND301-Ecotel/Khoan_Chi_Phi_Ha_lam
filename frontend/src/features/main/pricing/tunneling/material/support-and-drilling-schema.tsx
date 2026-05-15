import z from 'zod';

export const supportAndDrillingFormSchema = z.object({
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
	code: z.string().nonempty({ error: 'Mã đơn giá không được để trống' }),
	processId: z
		.string()
		.nonempty({ error: 'Công đoạn sản xuất không được để trống' }),
	technologyId: z.string().nonempty({ error: 'Công nghệ không được để trống' }),
	passportId: z.string().nonempty({ error: 'Hộ chiếu không được để trống' }),
	hardnessId: z
		.string()
		.nonempty({ error: 'Độ kiên cố than đá không được để trống' }),
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

export type SupportAndDrillingFormSchema = z.infer<
	typeof supportAndDrillingFormSchema
>;

export const SUPPORT_AND_DRILLING_FORM_DEFAULT: SupportAndDrillingFormSchema = {
	startMonth: '',
	endMonth: '',
	code: '',
	processId: '',
	technologyId: '',
	passportId: '',
	hardnessId: '',
	costs: [],
	otherMaterialValue: undefined,
};
