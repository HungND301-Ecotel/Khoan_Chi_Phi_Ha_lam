import z from 'zod';

export const passportSchema = z.object({
	name: z.string().nonempty({
		error: 'Hộ chiếu không được để trống',
	}),
	sd: z.string().nonempty({
		error: 'Sđ không được để trống',
	}),
	sc: z.string().nonempty({
		error: 'Sc không được để trống',
	}),
});

export type PassportSchema = z.infer<typeof passportSchema>;

export const PASSPORT_SCHEMA_DEFAULT: PassportSchema = {
	name: '',
	sd: '',
	sc: '',
};
