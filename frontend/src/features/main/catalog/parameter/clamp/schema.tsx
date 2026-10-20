import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const clampSchema = z.object({
	value: z.string().nonempty({
		error: 'Tỷ lệ đá kẹp (Ckẹp) không được để trống.',
	}),
	note: z.string().optional(),
});

export type ClampSchema = z.infer<typeof clampSchema>;

export const CLAMP_SCHEMA_DEFAULT: ClampSchema = {
	value: '',
	note: '',
};

export const clampBatchSchema = batchSchema(clampSchema, {
	uniqueKey: 'value',
	uniqueMessage: 'Tỷ lệ đá kẹp (Ckẹp) bị trùng',
});

export type ClampBatchSchema = z.infer<typeof clampBatchSchema>;
