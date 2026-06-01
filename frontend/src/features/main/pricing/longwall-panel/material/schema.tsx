import z from 'zod';

export const longwallMaterialFormSchema = z.object({
	id: z.string().optional(),
	code: z
		.string()
		.nonempty({ error: 'Mã định mức vật liệu không được để trống' }),
	processId: z.string().nonempty({ error: 'Công đoạn không được để trống' }),
	longwallParametersId: z
		.string()
		.nonempty({ error: 'Thông số lò chợ không được để trống' }),
	cuttingThicknessId: z
		.string()
		.nonempty({ error: 'Chiều dày lớp khấu không được để trống' }),
	seamFaceId: z.string().nullable().optional(),
	technologyId: z
		.string()
		.nonempty({ error: 'Công nghệ khai thác không được để trống' }),
	powerId: z.string().nullable().optional(),
	hardnessId: z.string().nullable().optional(),
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

export type LongwallMaterialFormSchema = z.infer<
	typeof longwallMaterialFormSchema
>;

export const LONGWALL_MATERIAL_FORM_DEFAULT: LongwallMaterialFormSchema = {
	code: '',
	processId: '',
	longwallParametersId: '',
	cuttingThicknessId: '',
	seamFaceId: '',
	technologyId: '',
	powerId: '',
	hardnessId: '',
	startMonth: '',
	endMonth: '',
	costs: [],
	otherMaterialValue: undefined,
};
