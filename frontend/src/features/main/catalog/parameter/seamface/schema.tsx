import z from 'zod';

export const seamfaceSchema = z.object({
	value: z.string().nonempty({
		error: 'Tên mặt vỉa (M) không được để trống.',
	}),
});

export type SeamfaceSchema = z.infer<typeof seamfaceSchema>;

export const SEAMFACE_SCHEMA_DEFAULT: SeamfaceSchema = {
	value: '',
};
