import z from 'zod';

export const insertSchema = z.object({
	value: z.string().nonempty({
		error: 'Chèn không được để trống.',
	}),
});

export type InsertSchema = z.infer<typeof insertSchema>;

export const INSERT_SCHEMA_DEFAULT: InsertSchema = {
	value: '',
};
