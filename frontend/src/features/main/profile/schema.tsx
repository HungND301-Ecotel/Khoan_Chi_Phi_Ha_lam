import { z } from 'zod';

export const profileFormSchema = z.object({
	fullName: z.string().min(1, 'Họ và tên không được để trống'),
	email: z.string().email('Email không đúng định dạng'),
	phoneNumber: z
		.string()
		.regex(/^\d{10,11}$/, 'Số điện thoại phải gồm 10-11 chữ số'),
	cccd: z
		.string()
		.min(9, 'CCCD phải từ 9-12 chữ số')
		.max(12, 'CCCD phải từ 9-12 chữ số'),
	dob: z.string().nullable().optional(),
	gender: z.enum(['true', 'false']).nullable().optional(),
	positionId: z.number().int(),
	departmentId: z.string(),
});

export type ProfileFormValues = z.infer<typeof profileFormSchema>;

export const changePasswordSchema = z
	.object({
		currentPassword: z
			.string()
			.min(6, 'Mật khẩu hiện tại phải từ 6 ký tự trở lên'),
		newPassword: z.string().min(6, 'Mật khẩu mới phải từ 6 ký tự trở lên'),
		confirmNewPassword: z
			.string()
			.min(6, 'Xác nhận mật khẩu mới phải từ 6 ký tự trở lên'),
	})
	.refine((data) => data.newPassword === data.confirmNewPassword, {
		message: 'Mật khẩu xác nhận không trùng khớp',
		path: ['confirmNewPassword'],
	});

export type ChangePasswordValues = z.infer<typeof changePasswordSchema>;

export const PROFILE_FORM_DEFAULT: ProfileFormValues = {
	fullName: '',
	email: '',
	phoneNumber: '',
	cccd: '',
	dob: null,
	gender: 'true' as 'true' | 'false',
	positionId: 0,
	departmentId: '',
};

export const PASSWORD_FORM_DEFAULT: ChangePasswordValues = {
	currentPassword: '',
	newPassword: '',
	confirmNewPassword: '',
};
