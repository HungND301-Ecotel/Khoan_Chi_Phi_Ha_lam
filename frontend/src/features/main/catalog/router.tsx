import { MainCatalogAdjustmentRouter } from '@/features/main/catalog/adjustment/router';
import MainCatalogAssetInternalPage from '@/features/main/catalog/asset/internal/page';
import MainCatalogContractCodePage from '@/features/main/catalog/contract-code/page';
import { MainCatalogDepartmentPage } from '@/features/main/catalog/department/page';
import { MainCatalogLayout } from '@/features/main/catalog/layout';
import { MainCatalogParameterRouter } from '@/features/main/catalog/parameter/router';
import { MainCatalogProcessRouter } from '@/features/main/catalog/process/router';
import { MainCatalogProductPage } from '@/features/main/catalog/product/page';
import MainCatalogUnitPage from '@/features/main/catalog/unit/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';
import { MainCatalogParameterProductionOrderPage } from './production-order/page';
import { MainCatalogNormFactorPage } from './norm-factor/page';
import { MainCatalogRevenueCostAdjustmentConfigPage } from './revenue-cost-adjustment-config/page';
import { MainCatalogSavingsRateConfigPage } from './savings-rate-config/page';
import { MainCatalogAkFactorConfigPage } from './ak-factor-config/page';

const MainCatalogRouter: RouteObject = {
	path: 'catalogs',
	handle: {
		breadcrumb: 'Danh mục',
	},
	element: <MainCatalogLayout />,
	children: [
		MainCatalogProcessRouter,
		MainCatalogParameterRouter,
		MainCatalogAdjustmentRouter,
		{
			path: 'units',
			element: <MainCatalogUnitPage />,
			handle: { breadcrumb: 'Đơn vị tính', title: 'Đơn vị tính' },
		},
		{
			path: 'departments',
			element: <MainCatalogDepartmentPage />,
			handle: { breadcrumb: 'Đơn vị', title: 'Đơn vị' },
		},
		{
			path: 'contract-codes',
			element: <MainCatalogContractCodePage />,
			handle: {
				breadcrumb: 'Nhóm vật tư, tài sản',
				title: 'Nhóm vật tư, tài sản',
			},
		},
		{
			path: 'assets',
			element: <Outlet />,
			handle: { breadcrumb: 'Vật tư, tài sản' },
			children: [
				{
					index: true,
					element: <Navigate replace to='internal' />,
				},
				{
					path: 'internal',
					element: <MainCatalogAssetInternalPage />,
					handle: {
						breadcrumb: 'Vật tư, tài sản',
						title: 'Vật tư, tài sản',
					},
				},
			],
		},
		{
			path: 'products',
			element: <MainCatalogProductPage />,
			handle: { breadcrumb: 'Sản phẩm', title: 'Sản phẩm' },
		},
		{
			path: 'production-orders',
			element: <MainCatalogParameterProductionOrderPage />,
			handle: {
				breadcrumb: 'Quyết định, lệnh sản xuất',
				title: 'Quyết định, lệnh sản xuất',
			},
		},
		{
			path: 'norm-factors',
			element: <MainCatalogNormFactorPage />,
			handle: {
				breadcrumb: 'Hệ số điều chỉnh định mức',
				title: 'Hệ số điều chỉnh định mức',
			},
		},
		{
			path: 'accepted-savings-rates',
			element: <MainCatalogSavingsRateConfigPage />,
			handle: {
				breadcrumb: 'Hệ số tiết kiệm được chấp nhận',
				title: 'Hệ số tiết kiệm được chấp nhận',
			},
		},
		{
			path: 'ak-factors',
			element: <MainCatalogAkFactorConfigPage />,
			handle: {
				breadcrumb: 'Hệ số Ak',
				title: 'Hệ số Ak',
			},
		},
		{
			path: 'revenue-cost-adjustment-configs',
			element: <MainCatalogRevenueCostAdjustmentConfigPage />,
			handle: {
				breadcrumb: 'Giá trị tiết kiệm được cộng/trừ vào thu nhập',
				title: 'Giá trị tiết kiệm được cộng/trừ vào thu nhập',
			},
		},
	],
};

export default MainCatalogRouter;
