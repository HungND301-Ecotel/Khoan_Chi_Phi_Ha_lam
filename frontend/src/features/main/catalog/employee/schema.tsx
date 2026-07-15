import z from 'zod';

export const employeeFormSchema = z.object({
	fullName: z.string().min(1, {
		message: 'Họ tên không được để trống',
	}),
	userName: z.string().min(1, {
		message: 'Tên đăng nhập không được để trống',
	}),
	positionId: z.coerce.number().min(1, {
		message: 'Vui lòng chọn chức vụ',
	}),
	departmentId: z.string().min(1, {
		message: 'Vui lòng chọn đơn vị',
	}),
	email: z
		.string()
		.email({
			message: 'Email không hợp lệ',
		})
		.optional()
		.or(z.literal('')),
	phoneNumber: z.string().optional().or(z.literal('')),
	cccd: z.string().optional().or(z.literal('')),
	dob: z.string().optional().or(z.literal('')),
	genre: z.any().optional(),
});

export type EmployeeFormSchema = z.infer<typeof employeeFormSchema>;

export const EMPLOYEE_FORM_DEFAULT: EmployeeFormSchema = {
	fullName: '',
	userName: '',
	positionId: 0,
	departmentId: '',
	email: '',
	phoneNumber: '',
	cccd: '',
	dob: '',
	genre: true,
};
