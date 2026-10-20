import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const departmentFormSchema = z.object({
	code: z.string().min(1, {
		message: 'Mã đơn vị không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên đơn vị không được để trống',
	}),
	note: z.string().optional(),
});

export type DepartmentFormSchema = z.infer<typeof departmentFormSchema>;

export const DEPARTMENT_FORM_DEFAULT: DepartmentFormSchema = {
	code: '',
	name: '',
	note: '',
};

export const departmentBatchSchema = batchSchema(departmentFormSchema, {
	uniqueKey: 'code',
	uniqueMessage: 'Mã đơn vị bị trùng',
});

export type DepartmentBatchSchema = z.infer<typeof departmentBatchSchema>;
