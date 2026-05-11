import z from 'zod';

export const unresolvedCreateSchema = z
	.object({
		entityGroup: z.enum(['material', 'part'], {
			message: 'Phải chọn nhóm đối tượng',
		}),
		specificType: z.number().nullable(),
	})
	.refine((data) => data.specificType != null, {
		message: 'Phải chọn loại',
		path: ['specificType'],
	});

export type UnresolvedCreateSchema = z.input<typeof unresolvedCreateSchema>;

export const UNRESOLVED_CREATE_DEFAULT: UnresolvedCreateSchema = {
	entityGroup: 'material',
	specificType: 1,
};
