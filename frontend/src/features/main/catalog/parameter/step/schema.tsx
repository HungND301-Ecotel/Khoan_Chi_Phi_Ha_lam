import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const stepSchema = z.object({
	value: z.string().nonempty({
		error: 'Bước chống không được để trống.',
	}),
	note: z.string().optional(),
});

export type StepSchema = z.infer<typeof stepSchema>;

export const STEP_SCHEMA_DEFAULT: StepSchema = {
	value: '',
	note: '',
};

export const stepBatchSchema = batchSchema(stepSchema, {
	uniqueKey: 'value',
	uniqueMessage: 'Bước chống bị trùng',
});

export type StepBatchSchema = z.infer<typeof stepBatchSchema>;
