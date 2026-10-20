import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const unitFormSchema = z.object({
	name: z.string().min(1, {
		message: 'Đơn vị tính không được để trống',
	}),
	note: z.string().optional(),
});

export type UnitFormSchema = z.infer<typeof unitFormSchema>;

export const UNIT_FORM_DEFAULT: UnitFormSchema = {
	name: '',
	note: '',
};

export const unitBatchSchema = batchSchema(unitFormSchema, {
	uniqueKey: 'name',
	uniqueMessage: 'Đơn vị tính bị trùng',
});

export type UnitBatchSchema = z.infer<typeof unitBatchSchema>;
