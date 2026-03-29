import { MainCatalogAdjustmentRouter } from '@/features/main/catalog/adjustment/router';
import MainCatalogAssetExternalPage from '@/features/main/catalog/asset/external/page';
import MainCatalogAssetInternalPage from '@/features/main/catalog/asset/internal/page';
import MainCatalogAssetQuotaMaterialsPage from '@/features/main/catalog/asset/quota-materials/page';
import MainCatalogAssetResourcePage from '@/features/main/catalog/asset/resource/page';
import MainCatalogAssetSafetyAndWelfarePage from '@/features/main/catalog/asset/safety-and-welfare/page';
import MainCatalogContractCodePage from '@/features/main/catalog/contract-code/page';
import MainCatalogEquipmentPage from '@/features/main/catalog/equipment/page';
import { MainCatalogLayout } from '@/features/main/catalog/layout';
import { MainCatalogParameterRouter } from '@/features/main/catalog/parameter/router';
import { MainCatalogPartPage } from '@/features/main/catalog/part/main/page';
import { MainCatalogProcessRouter } from '@/features/main/catalog/process/router';
import { MainCatalogProductPage } from '@/features/main/catalog/product/page';
import MainCatalogUnitPage from '@/features/main/catalog/unit/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';
import { MainCatalogOtherPartPage } from './part/other/page';
import { MainCatalogParameterProductionOrderPage } from './production-order/page';
import { MainCatalogNormFactorPage } from './norm-factor/page';

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
			path: 'contract-codes',
			element: <MainCatalogContractCodePage />,
			handle: { breadcrumb: 'Mã giao khoán', title: 'Mã giao khoán' },
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
						breadcrumb: 'Vật tư, tài sản trong khoán',
						title: 'Vật tư, tài sản trong khoán',
					},
				},
				{
					path: 'external',
					element: <MainCatalogAssetExternalPage />,
					handle: {
						breadcrumb: 'Vật tư, tài sản khác',
						title: 'Vật tư, tài sản khác',
					},
				},
				{
					path: 'safety-and-welfare',
					element: <MainCatalogAssetSafetyAndWelfarePage />,
					handle: {
						breadcrumb:
							'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
						title:
							'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
					},
				},
				{
					path: 'resource',
					element: <MainCatalogAssetResourcePage />,
					handle: {
						breadcrumb: 'Tài sản',
						title: 'Tài sản',
					},
				},
				{
					path: 'quota-materials',
					element: <MainCatalogAssetQuotaMaterialsPage />,
					handle: {
						breadcrumb: 'Vật tư theo hạn mức',
						title: 'Vật tư theo hạn mức',
					},
				},
			],
		},
		{
			path: 'equipments',
			element: <MainCatalogEquipmentPage />,
			handle: { breadcrumb: 'Thiết bị', title: 'Thiết bị' },
		},
		{
			path: 'spare-parts',
			element: <Outlet />,
			handle: { breadcrumb: 'Phụ tùng' },
			children: [
				{
					index: true,
					element: <Navigate replace to='main' />,
				},
				{
					path: 'main',
					element: <MainCatalogPartPage />,
					handle: {
						breadcrumb: 'Phụ tùng theo thiết bị',
						title: 'Phụ tùng theo thiết bị',
					},
				},
				{
					path: 'other',
					element: <MainCatalogOtherPartPage />,
					handle: { breadcrumb: 'Phụ tùng khác', title: 'Phụ tùng khác' },
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
	],
};

export default MainCatalogRouter;
