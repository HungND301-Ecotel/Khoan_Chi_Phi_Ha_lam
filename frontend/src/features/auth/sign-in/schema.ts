import z from 'zod';

export const SignInSchema = z.object({
	username: z.string().nonempty({
		error: 'Tên đăng nhập không được để trống',
	}),
	password: z.string().nonempty({
		error: 'Mật khẩu không được để trống',
	}),
});

export type SignInValues = z.infer<typeof SignInSchema>;

export const SignInDefault: SignInValues = {
	username: '',
	password: '',
};
