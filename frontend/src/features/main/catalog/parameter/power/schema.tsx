import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const powerSchema = z.object({
	value: z.string().nonempty({
		error: 'Tên Công suất không được để trống.',
	}),
	note: z.string().optional(),
});

export type PowerSchema = z.infer<typeof powerSchema>;

export const POWER_SCHEMA_DEFAULT: PowerSchema = {
	value: '',
	note: '',
};

export const powerBatchSchema = batchSchema(powerSchema, {
	uniqueKey: 'value',
	uniqueMessage: 'Công suất bị trùng',
});

export type PowerBatchSchema = z.infer<typeof powerBatchSchema>;
