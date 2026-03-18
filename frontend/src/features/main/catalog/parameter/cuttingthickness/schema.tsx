import z from 'zod';

export const cuttingthicknessSchema = z.object({
	value: z.string().nonempty({
		error: 'Chiều dày lớp khấu không được để trống',
	}),
});

export type CuttingthicknessSchema = z.infer<typeof cuttingthicknessSchema>;

export const CUTTINGTHICKNESS_SCHEMA_DEFAULT: CuttingthicknessSchema = {
	value: '',
};
