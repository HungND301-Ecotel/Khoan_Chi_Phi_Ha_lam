import { Navigate, RouteObject } from 'react-router-dom';
import { MainCatalogFactorLayout } from './layout';
import { MainCatalogAdjustmentFactorPage } from '@/features/main/catalog/adjustment/factor/page';
import { MainCatalogAdjustmentInterpreterPage } from '@/features/main/catalog/adjustment/interpreter/page';
import { MainCatalogNormFactorPage } from '@/features/main/catalog/norm-factor/page';
import { MainCatalogSavingsRateConfigPage } from '@/features/main/catalog/savings-rate-config/page';
import { MainCatalogAkFactorConfigPage } from '@/features/main/catalog/ak-factor-config/page';
import { MainCatalogRevenueCostAdjustmentConfigPage } from '@/features/main/catalog/revenue-cost-adjustment-config/page';

export const MainCatalogFactorRouter: RouteObject = {
	path: 'factors',
	element: <MainCatalogFactorLayout />,
	handle: {
		breadcrumb: 'Hệ số',
		title: 'Hệ số',
	},
	children: [
		{
			index: true,
			element: <Navigate replace to='adjustment-factors' />,
		},
		{
			path: 'adjustment-factors',
			element: <MainCatalogAdjustmentFactorPage />,
			handle: { breadcrumb: 'Hệ số điều chỉnh' },
		},
		{
			path: 'adjustment-interpreters',
			element: <MainCatalogAdjustmentInterpreterPage />,
			handle: { breadcrumb: 'Diễn giải hệ số điều chỉnh' },
		},
		{
			path: 'norm-factors',
			element: <MainCatalogNormFactorPage />,
			handle: { breadcrumb: 'Hệ số điều chỉnh định mức' },
		},
		{
			path: 'savings-rates',
			element: <MainCatalogSavingsRateConfigPage />,
			handle: { breadcrumb: 'Hệ số tiết kiệm được chấp nhận' },
		},
		{
			path: 'ak-factors',
			element: <MainCatalogAkFactorConfigPage />,
			handle: { breadcrumb: 'Hệ số Ak' },
		},
		{
			path: 'revenue-cost-adjustments',
			element: <MainCatalogRevenueCostAdjustmentConfigPage />,
			handle: { breadcrumb: 'Giá trị tiết kiệm được cộng/trừ vào thu nhập' },
		},
	],
};

export default MainCatalogFactorRouter;
