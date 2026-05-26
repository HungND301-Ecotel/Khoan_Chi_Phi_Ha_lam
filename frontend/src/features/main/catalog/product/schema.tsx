import z from 'zod';

const monthField = (message: string) =>
	z
		.string()
		.nonempty({
			message,
		})
		.refine((value) => /^\d{4}-\d{2}-\d{2}$/.test(value), {
			message: 'Tháng không hợp lệ',
		});

export const productFormSchema = z
	.object({
		code: z.string().min(1, {
			message: 'Mã sản phẩm không được để trống',
		}),
		name: z.string().min(1, {
			message: 'Tên sản phẩm không được để trống',
		}),
		processGroupId: z.string().min(1, {
			message: 'Mã nhóm công đoạn sản xuất không được để trống',
		}),
		startMonth: monthField('Thời gian bắt đầu không được để trống'),
		endMonth: monthField('Thời gian kết thúc không được để trống'),
	})
	.superRefine((data, ctx) => {
		if (data.startMonth > data.endMonth) {
			ctx.addIssue({
				code: 'custom',
				path: ['startMonth'],
				message: 'Thời gian bắt đầu phải nhỏ hơn hoặc bằng thời gian kết thúc',
			});
			ctx.addIssue({
				code: 'custom',
				path: ['endMonth'],
				message: 'Thời gian kết thúc phải lớn hơn hoặc bằng thời gian bắt đầu',
			});
		}
	});

export type ProductFormSchema = z.infer<typeof productFormSchema>;

export const PRODUCT_FORM_DEFAULT: ProductFormSchema = {
	code: '',
	name: '',
	processGroupId: '',
	startMonth: '',
	endMonth: '',
};
