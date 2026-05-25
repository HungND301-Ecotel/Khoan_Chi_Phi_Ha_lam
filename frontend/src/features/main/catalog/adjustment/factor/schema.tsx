import z from 'zod';

export const factorSchema = z.object({
	fixedKeyId: z.string().nonempty({
		error: 'Hệ số điều chỉnh không được để trống.',
	}),
	name: z.string().nonempty({
		error: 'Tên hệ số điều chỉnh không được để trống.',
	}),
	processGroupId: z.string().nonempty({
		error: 'Nhóm công đoạn sản xuất không được để trống.',
	}),
});

export type FactorSchema = z.infer<typeof factorSchema>;

export const FACTOR_SCHEMA_DEFAULT: FactorSchema = {
	fixedKeyId: '',
	name: '',
	processGroupId: '',
};
