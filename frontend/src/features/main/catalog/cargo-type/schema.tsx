import { z } from 'zod';

export const cargoTypeFormSchema = z.object({
	code: z.string().min(1, 'Vui lòng nhập mã chủng loại hàng'),
	name: z.string().min(1, 'Vui lòng nhập tên chủng loại hàng'),
	note: z.string().optional(),
});

export type CargoTypeFormSchema = z.infer<typeof cargoTypeFormSchema>;

export const CARGO_TYPE_FORM_DEFAULT: CargoTypeFormSchema = {
	code: '',
	name: '',
	note: '',
};
