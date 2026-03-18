import MainCatalogProcessGroupPage from '@/features/main/catalog/process/group/page';
import { MainCatalogProcessLayout } from '@/features/main/catalog/process/layout';
import MainCatalogProcessStepPage from '@/features/main/catalog/process/step/page';
import { Navigate, RouteObject } from 'react-router-dom';

export const MainCatalogProcessRouter: RouteObject = {
	path: 'processes',
	element: <MainCatalogProcessLayout />,
	handle: {
		breadcrumb: 'Công đoạn sản xuất',
		title: 'Công đoạn sản xuất',
	},
	children: [
		{
			index: true,
			element: <Navigate replace to='groups' />,
		},
		{
			path: 'groups',
			element: <MainCatalogProcessGroupPage />,
			handle: { breadcrumb: 'Nhóm công đoạn sản xuất' },
		},

		{
			path: 'steps',
			element: <MainCatalogProcessStepPage />,
			handle: { breadcrumb: 'Công đoạn sản xuất' },
		},
	],
};
