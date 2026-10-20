import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const longwallparametersSchema = z.object({
	llc: z.string().nonempty({
		error: 'LLC không được để trống',
	}),
	lkc: z.string().nonempty({
		error: 'LKC không được để trống',
	}),
	mk: z.string().nonempty({
		error: 'MK không được để trống',
	}),
	note: z.string().optional(),
});

export type LongwallparametersSchema = z.infer<typeof longwallparametersSchema>;

export const LONGWALLPARAMETERS_SCHEMA_DEFAULT: LongwallparametersSchema = {
	llc: '',
	lkc: '',
	mk: '',
	note: '',
};

export const longwallparametersBatchSchema = batchSchema(
	longwallparametersSchema,
);

export type LongwallparametersBatchSchema = z.infer<
	typeof longwallparametersBatchSchema
>;
