import z from 'zod';

export const processStepSchema = z.object({
	processGroupId: z.string().min(1, {
		message: 'Mã nhóm công đoạn sản xuất không được để trống',
	}),
	code: z.string().min(1, {
		message: 'Mã nhóm công đoạn sản xuất không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên nhóm công đoạn sản xuất không được để trống',
	}),
});

export type ProcessStepSchema = z.infer<typeof processStepSchema>;

export const PROCESS_STEP_SCHEMA_DEFAULT: ProcessStepSchema = {
	processGroupId: '',
	code: '',
	name: '',
} as const;
