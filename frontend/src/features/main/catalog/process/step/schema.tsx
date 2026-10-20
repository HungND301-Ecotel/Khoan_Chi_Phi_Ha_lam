import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const processStepSchema = z.object({
	processGroupId: z.string().min(1, {
		message: 'Nhóm công đoạn sản xuất không được để trống',
	}),
	code: z.string().min(1, {
		message: 'Mã công đoạn sản xuất không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên công đoạn sản xuất không được để trống',
	}),
	unitOfMeasureId: z.string().min(1, {
		message: 'Đơn vị tính không được để trống',
	}),
	note: z.string().optional(),
});

export type ProcessStepSchema = z.infer<typeof processStepSchema>;

export const PROCESS_STEP_SCHEMA_DEFAULT: ProcessStepSchema = {
	processGroupId: '',
	code: '',
	name: '',
	unitOfMeasureId: '',
	note: '',
} as const;

export const processStepBatchSchema = batchSchema(processStepSchema, {
	uniqueKey: 'code',
	uniqueMessage: 'Mã công đoạn sản xuất bị trùng',
});

export type ProcessStepBatchSchema = z.infer<typeof processStepBatchSchema>;
