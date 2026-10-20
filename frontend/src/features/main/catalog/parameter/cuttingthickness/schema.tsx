import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const cuttingthicknessSchema = z.object({
	value: z.string().nonempty({
		error: 'Chiều dày lớp khấu không được để trống',
	}),
	note: z.string().optional(),
});

export type CuttingthicknessSchema = z.infer<typeof cuttingthicknessSchema>;

export const CUTTINGTHICKNESS_SCHEMA_DEFAULT: CuttingthicknessSchema = {
	value: '',
	note: '',
};

export const cuttingthicknessBatchSchema = batchSchema(cuttingthicknessSchema, {
	uniqueKey: 'value',
	uniqueMessage: 'Chiều dày lớp khấu bị trùng',
});

export type CuttingthicknessBatchSchema = z.infer<
	typeof cuttingthicknessBatchSchema
>;
