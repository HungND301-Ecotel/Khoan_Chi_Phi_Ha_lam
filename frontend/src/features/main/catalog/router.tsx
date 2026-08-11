import { MainCatalogAdjustmentRouter } from '@/features/main/catalog/adjustment/router';
import MainCatalogAssetInternalPage from '@/features/main/catalog/asset/internal/page';
import MainCatalogContractCodePage from '@/features/main/catalog/contract-code/page';
import { MainCatalogDepartmentPage } from '@/features/main/catalog/department/page';
import { MainCatalogFactorRouter } from '@/features/main/catalog/factor/router';
import { MainCatalogLayout } from '@/features/main/catalog/layout';
import { MainCatalogParameterRouter } from '@/features/main/catalog/parameter/router';
import { MainCatalogProcessRouter } from '@/features/main/catalog/process/router';
import { MainCatalogProductPage } from '@/features/main/catalog/product/page';
import MainCatalogUnitPage from '@/features/main/catalog/unit/page';
import { Navigate, Outlet, type RouteObject } from 'react-router-dom';
import { MainCatalogParameterProductionOrderPage } from './production-order/page';
import { MainCatalogEmployeePage } from '@/features/main/catalog/employee/page';
import { MainCatalogPositionPage } from '@/features/main/catalog/position/page';
import { MainCatalogTransportRoutePage } from '@/features/main/catalog/transport-route/page';
import { MainCatalogCargoTypePage } from '@/features/main/catalog/cargo-type/page';

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
		MainCatalogFactorRouter,
		{
			path: 'positions',
			element: <MainCatalogPositionPage />,
			handle: { breadcrumb: 'Chức vụ', title: 'Chức vụ' },
		},
		{
			path: 'employees',
			element: <MainCatalogEmployeePage />,
			handle: { breadcrumb: 'Cán bộ, nhân viên', title: 'Cán bộ, nhân viên' },
		},
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
			path: 'transport-routes',
			element: <MainCatalogTransportRoutePage />,
			handle: {
				breadcrumb: 'Tuyến vận tải',
				title: 'Tuyến vận tải',
			},
		},
		{
			path: 'cargo-types',
			element: <MainCatalogCargoTypePage />,
			handle: {
				breadcrumb: 'Chủng loại hàng',
				title: 'Chủng loại hàng',
			},
		},
	],
};

export default MainCatalogRouter;
