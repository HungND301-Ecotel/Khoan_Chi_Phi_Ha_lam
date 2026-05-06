import z from 'zod';

export const departmentFormSchema = z.object({
	code: z.string().min(1, {
		message: 'Mã đơn vị không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên đơn vị không được để trống',
	}),
});

export type DepartmentFormSchema = z.infer<typeof departmentFormSchema>;

export const DEPARTMENT_FORM_DEFAULT: DepartmentFormSchema = {
	code: '',
	name: '',
};
