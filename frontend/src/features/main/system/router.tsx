import { MainSystemFixedKeyPage } from '@/features/main/system/fixed-key/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';
import { MainSystemPermissionsPage } from '@/features/main/system/permissions/page';

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
			path: 'permissions',
			element: <MainSystemPermissionsPage />,
			handle: { breadcrumb: 'Phân quyền', title: 'Phân quyền' },
		},
		{
			path: 'fixed-keys',
			element: <MainSystemFixedKeyPage />,
			handle: { breadcrumb: 'Khóa cấu hình', title: 'Khóa cấu hình' },
		},
	],
};
