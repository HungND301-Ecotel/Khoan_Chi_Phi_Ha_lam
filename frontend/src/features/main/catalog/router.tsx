import { MainCatalogAdjustmentRouter } from '@/features/main/catalog/adjustment/router';
import MainCatalogAssetExternalPage from '@/features/main/catalog/asset/external/page';
import MainCatalogAssetInternalPage from '@/features/main/catalog/asset/internal/page';
import MainCatalogContractCodePage from '@/features/main/catalog/contract-code/page';
import MainCatalogEquipmentPage from '@/features/main/catalog/equipment/page';
import { MainCatalogLayout } from '@/features/main/catalog/layout';
import { MainCatalogParameterRouter } from '@/features/main/catalog/parameter/router';
import { MainCatalogPartPage } from '@/features/main/catalog/part/page';
import { MainCatalogProcessRouter } from '@/features/main/catalog/process/router';
import { MainCatalogProductPage } from '@/features/main/catalog/product/page';
import MainCatalogUnitPage from '@/features/main/catalog/unit/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';

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
						breadcrumb: 'Vật tư, tài sản ngoài khoán',
						title: 'Vật tư, tài sản ngoài khoán',
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
			element: <MainCatalogPartPage />,
			handle: { breadcrumb: 'Phụ tùng', title: 'Phụ tùng' },
		},
		{
			path: 'products',
			element: <MainCatalogProductPage />,
			handle: { breadcrumb: 'Sản phẩm', title: 'Sản phẩm' },
		},
	],
};

export default MainCatalogRouter;
