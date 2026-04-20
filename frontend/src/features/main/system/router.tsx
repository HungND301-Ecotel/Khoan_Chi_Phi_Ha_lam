import { Navigate, RouteObject } from 'react-router-dom';
import { MainSystemLayout } from './layout';
import { MainSystemPage } from './page';
import { MasterDataPage } from './master-data/page';

export const MainSystemRouter: RouteObject = {
	path: 'system',
	element: <MainSystemLayout />,
	handle: { breadcrumb: 'Hệ thống' },
	children: [
		{
			element: <MainSystemPage />,
			children: [
				{
					index: true,
					element: <Navigate replace to='master-data' />,
				},
				{
					path: 'master-data',
					element: <MasterDataPage />,
					handle: { breadcrumb: 'Master data', title: 'Master data' },
				},
				{
					path: '*',
					element: <Navigate replace to='master-data' />,
				},
			],
		},
	],
};