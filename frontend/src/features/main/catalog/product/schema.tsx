import z from 'zod';

export const productFormSchema = z.object({
	code: z.string().min(1, {
		message: 'Mã sản phẩm không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên sản phẩm không được để trống',
	}),
	processGroupId: z.string().min(1, {
		message: 'Mã nhóm công đoạn sản xuất không được để trống',
	}),
});

export type ProductFormSchema = z.infer<typeof productFormSchema>;

export const PRODUCT_FORM_DEFAULT: ProductFormSchema = {
	code: '',
	name: '',
	processGroupId: '',
};
