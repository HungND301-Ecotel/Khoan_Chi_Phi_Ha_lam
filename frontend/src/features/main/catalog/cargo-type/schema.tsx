import { batchSchema } from '@/components/form/form-batch';
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

export const cargoTypeBatchSchema = batchSchema(cargoTypeFormSchema, {
	uniqueKey: 'code',
	uniqueMessage: 'Mã chủng loại hàng bị trùng',
});

export type CargoTypeBatchSchema = z.infer<typeof cargoTypeBatchSchema>;
