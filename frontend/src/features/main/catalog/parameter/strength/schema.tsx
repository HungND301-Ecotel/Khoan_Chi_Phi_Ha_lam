import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const strengthSchema = z.object({
	value: z.string().nonempty({
		error: 'Độ kiên cố không được để trống.',
	}),
	note: z.string().optional(),
});

export type StrengthSchema = z.infer<typeof strengthSchema>;

export const STRENGTH_SCHEMA_DEFAULT: StrengthSchema = {
	value: '',
	note: '',
};

export const strengthBatchSchema = batchSchema(strengthSchema, {
	uniqueKey: 'value',
	uniqueMessage: 'Độ kiên cố than, đá (f) bị trùng',
});

export type StrengthBatchSchema = z.infer<typeof strengthBatchSchema>;
