import z from 'zod';

export const positionFormSchema = z.object({
	name: z.string().min(1, {
		message: 'Tên chức vụ không được để trống',
	}),
	level: z.coerce.number().min(0, {
		message: 'Cấp bậc phải là số dương',
	}),
	description: z.string().optional(),
});

export type PositionFormSchema = z.infer<typeof positionFormSchema>;

export const POSITION_FORM_DEFAULT: PositionFormSchema = {
	name: '',
	level: 0,
	description: '',
};
