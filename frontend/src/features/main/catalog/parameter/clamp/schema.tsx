import z from 'zod';

export const clampSchema = z.object({
	value: z.string().nonempty({
		error: 'Tỷ lệ đá kẹp (Ckẹp) không được để trống.',
	}),
});

export type ClampSchema = z.infer<typeof clampSchema>;

export const CLAMP_SCHEMA_DEFAULT: ClampSchema = {
	value: '',
};
