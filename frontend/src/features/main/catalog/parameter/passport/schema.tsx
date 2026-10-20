import { batchSchema } from '@/components/form/form-batch';
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
	note: z.string().optional(),
});

export type PassportSchema = z.infer<typeof passportSchema>;

export const PASSPORT_SCHEMA_DEFAULT: PassportSchema = {
	name: '',
	sd: '',
	sc: '',
	note: '',
};

export const passportBatchSchema = batchSchema(passportSchema, {
	uniqueKey: 'name',
	uniqueMessage: 'Hộ chiếu bị trùng',
});

export type PassportBatchSchema = z.infer<typeof passportBatchSchema>;
