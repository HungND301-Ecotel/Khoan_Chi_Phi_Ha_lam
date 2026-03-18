import z from 'zod';

export const stepSchema = z.object({
	value: z.string().nonempty({
		error: 'Bước chống không được để trống.',
	}),
});

export type StepSchema = z.infer<typeof stepSchema>;

export const STEP_SCHEMA_DEFAULT: StepSchema = {
	value: '',
};
