import z from 'zod';

export const masterDataSchema = z.object({
	code: z.string().min(1, { message: 'Mã fixed key không được để trống' }),
	name: z.string().min(1, { message: 'Tên fixed key không được để trống' }),
	type: z.string().min(1, { message: 'Loại fixed key không được để trống' }),
	isSystem: z.boolean(),
});

export type MasterDataSchema = z.infer<typeof masterDataSchema>;

export const MASTER_DATA_SCHEMA_DEFAULT: MasterDataSchema = {
	code: '',
	name: '',
	type: '',
	isSystem: true,
};