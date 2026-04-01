import z from 'zod';

export const powerSchema = z.object({
	value: z.string().nonempty({
		error: 'Tên Công suất không được để trống.',
	}),
});

export type PowerSchema = z.infer<typeof powerSchema>;

export const POWER_SCHEMA_DEFAULT: PowerSchema = {
	value: '',
};
