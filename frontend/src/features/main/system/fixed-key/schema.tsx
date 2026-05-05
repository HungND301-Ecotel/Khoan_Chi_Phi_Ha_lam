import { ProcessGroupType } from '@/constants/process-group';
import z from 'zod';

export const fixedKeySchema = z.object({
	key: z.string().min(1, {
		message: 'Key không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên khóa cấu hình không được để trống',
	}),
	type: z.number().refine((value) => value !== ProcessGroupType.None, {
		message: 'Loại nghiệp vụ không được để trống',
	}),
});

export type FixedKeySchema = z.infer<typeof fixedKeySchema>;

export const FIXED_KEY_SCHEMA_DEFAULT: FixedKeySchema = {
	key: '',
	name: '',
	type: ProcessGroupType.DL,
};