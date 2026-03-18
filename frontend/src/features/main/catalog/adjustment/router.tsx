import { MainCatalogAdjustmentFactorPage } from '@/features/main/catalog/adjustment/factor/page';
import { MainCatalogAdjustmentInterpreterPage } from '@/features/main/catalog/adjustment/interpreter/page';
import { MainCatalogAdjustmentLayout } from '@/features/main/catalog/adjustment/layout';
import { Navigate, RouteObject } from 'react-router-dom';

export const MainCatalogAdjustmentRouter: RouteObject = {
	path: 'adjustments',
	element: <MainCatalogAdjustmentLayout />,
	handle: { breadcrumb: 'Hệ số điều chỉnh', title: 'Hệ số điều chỉnh' },
	children: [
		{
			index: true,
			element: <Navigate replace to='factors' />,
		},
		{
			path: 'factors',
			element: <MainCatalogAdjustmentFactorPage />,
			handle: { breadcrumb: 'Hệ số điều chỉnh' },
		},
		{
			path: 'interpreters',
			element: <MainCatalogAdjustmentInterpreterPage />,
			handle: { breadcrumb: 'Diễn giải' },
		},
	],
};
