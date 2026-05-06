import { MainSystemFixedKeyPage } from '@/features/main/system/fixed-key/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';

export const MainSystemRouter: RouteObject = {
	path: 'system',
	handle: {
		breadcrumb: 'Hệ thống',
	},
	element: <Outlet />,
	children: [
		{
			index: true,
			element: <Navigate replace to='fixed-keys' />,
		},
		{
			path: 'fixed-keys',
			element: <MainSystemFixedKeyPage />,
			handle: { breadcrumb: 'Khóa cấu hình', title: 'Khóa cấu hình' },
		},
	],
};
