import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const processGroupSchema = z.object({
	fixedKeyId: z.string().min(1, {
		message: 'Mã nhóm công đoạn sản xuất không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên nhóm công đoạn sản xuất không được để trống',
	}),
	note: z.string().optional(),
});

export type ProcessGroupSchema = z.infer<typeof processGroupSchema>;

export const PROCESS_GROUP_SCHEMA_DEFAULT: ProcessGroupSchema = {
	fixedKeyId: '',
	name: '',
	note: '',
} as const;

export const processGroupBatchSchema = batchSchema(processGroupSchema, {
	uniqueKey: 'fixedKeyId',
	uniqueMessage: 'Mã nhóm công đoạn sản xuất bị trùng',
});

export type ProcessGroupBatchSchema = z.infer<typeof processGroupBatchSchema>;
