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
