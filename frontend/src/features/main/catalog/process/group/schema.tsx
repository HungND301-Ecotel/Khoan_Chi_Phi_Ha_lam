import z from 'zod';

export const processGroupSchema = z.object({
	code: z.string().min(1, {
		message: 'Mã nhóm công đoạn sản xuất không được để trống',
	}),
	fixedKeyId: z.string().min(1, {
		message: 'Khóa hệ thống không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên nhóm công đoạn sản xuất không được để trống',
	}),
});

export type ProcessGroupSchema = z.infer<typeof processGroupSchema>;

export const PROCESS_GROUP_SCHEMA_DEFAULT: ProcessGroupSchema = {
	code: '',
	fixedKeyId: '',
	name: '',
} as const;
