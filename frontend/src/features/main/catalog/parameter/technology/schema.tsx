import z from 'zod';

export const technologySchema = z.object({
	value: z.string().nonempty({
		error: 'Tên Công nghệ khai thác không được để trống.',
	}),
	note: z.string().optional(),
});

export type TechnologySchema = z.infer<typeof technologySchema>;

export const TECHNOLOGY_SCHEMA_DEFAULT: TechnologySchema = {
	value: '',
	note: '',
};
