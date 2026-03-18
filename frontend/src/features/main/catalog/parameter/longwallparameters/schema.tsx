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
});

export type LongwallparametersSchema = z.infer<typeof longwallparametersSchema>;

export const LONGWALLPARAMETERS_SCHEMA_DEFAULT: LongwallparametersSchema = {
	llc: '',
	lkc: '',
	mk: '',
};
