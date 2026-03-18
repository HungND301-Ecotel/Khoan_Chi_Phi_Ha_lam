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
	seamFaceId: z.string().nonempty({ error: 'Mặt vỉa không được để trống' }),
	technologyId: z
		.string()
		.nonempty({ error: 'Công nghệ khai thác không được để trống' }),
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
	totalPrice: z
		.number()
		.min(0, { message: 'Giá trị tối thiểu là 0' })
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
	startMonth: '',
	endMonth: '',
	totalPrice: 0,
};
