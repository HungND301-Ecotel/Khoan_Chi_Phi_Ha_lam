import z from 'zod';

export const unitFormSchema = z.object({
	name: z.string().min(1, {
		message: 'Đơn vị tính không được để trống',
	}),
});

export type UnitFormSchema = z.infer<typeof unitFormSchema>;

export const UNIT_FORM_DEFAULT: UnitFormSchema = {
	name: '',
};
