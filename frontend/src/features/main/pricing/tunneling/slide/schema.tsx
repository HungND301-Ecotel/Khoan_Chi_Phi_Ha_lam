import z from 'zod';

export const slideFormSchema = z.object({
	startMonth: z.iso
		.date({ error: 'Định dạng ngày không hợp lệ' })
		.nonempty({ error: 'Ngày bắt đầu không được để trống' }),
	endMonth: z.iso
		.date({ error: 'Định dạng ngày không hợp lệ' })
		.nonempty({ error: 'Ngày kết thúc không được để trống' }),
	code: z
		.string()
		.nonempty({ error: 'Mã định mức vật liệu không được để trống' }),
	processGroupId: z
		.string()
		.nonempty({ error: 'Nhóm công đoạn sản xuất không được để trống' }),
	passportId: z
		.string()
		.nonempty({ error: 'Hộ chiếu, Sđ, Sc không được để trống' }),
	hardnessId: z
		.string()
		.nonempty({ error: 'Độ kiên cố đá/ than (f) không được để trống' }),
	costs: z
		.array(
			z.object({
				assignmentCodeId: z
					.string()
					.nonempty({ error: 'ID mã số không được để trống' }),
				materialId: z
					.string()
					.nonempty({ error: 'ID vật liệu không được để trống' }),
				norm: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val), {
						message: 'Định mức không được để trống',
					}),
				amount: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val) && val >= 0, {
						message: 'Đơn giá máng trượt phải lớn hơn hoặc bằng 0',
					}),
			}),
		)
		.nonempty({ error: 'Mục đầu vào không được để trống' }),
});

export type SlideFormSchema = z.infer<typeof slideFormSchema>;

export const SLIDE_FORM_DEFAULT: SlideFormSchema = {
	startMonth: new Date().toISOString().substring(0, 10),
	endMonth: new Date().toISOString().substring(0, 10),
	code: '',
	processGroupId: '',
	passportId: '',
	hardnessId: '',
	costs: [],
};
