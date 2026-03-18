import { AuthLayout } from '@/features/auth/layout';
import SignInPage from '@/features/auth/sign-in/page';
import { RouteObject } from 'react-router-dom';

export const AuthRouter: RouteObject = {
	path: '/auth',
	element: <AuthLayout />,
	children: [
		{
			index: true,
			path: 'sign-in',
			element: <SignInPage />,
		},
	],
};
