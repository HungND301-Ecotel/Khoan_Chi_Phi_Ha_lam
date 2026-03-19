import { MainCatalogParameterClampPage } from '@/features/main/catalog/parameter/clamp/page';
import { MainCatalogParameterCuttingthicknessPage } from '@/features/main/catalog/parameter/cuttingthickness/page';
import { MainCatalogParameterInsertPage } from '@/features/main/catalog/parameter/insert/page';
import { MainCatalogParameterLayout } from '@/features/main/catalog/parameter/layout';
import { MainCatalogParameterLongwallparametersPage } from '@/features/main/catalog/parameter/longwallparameters/page';
import { MainCatalogParameterPassportPage } from '@/features/main/catalog/parameter/passport/page';
import { MainCatalogParameterSeamfacePage } from '@/features/main/catalog/parameter/seamface/page';
import { MainCatalogParameterStepPage } from '@/features/main/catalog/parameter/step/page';
import { MainCatalogParameterStrengthPage } from '@/features/main/catalog/parameter/strength/page';
import { MainCatalogParameterTechnologyPage } from '@/features/main/catalog/parameter/technology/page';
import { Navigate, RouteObject } from 'react-router-dom';
import { MainCatalogParameterProductionOrderPage } from './production-order/page';

export const MainCatalogParameterRouter: RouteObject = {
	path: 'parameters',
	element: <MainCatalogParameterLayout />,
	handle: {
		breadcrumb: 'Thông số',
		title: 'Thông số',
	},
	children: [
		{
			index: true,
			element: <Navigate replace to='passports' />,
		},
		{
			path: 'passports',
			element: <MainCatalogParameterPassportPage />,
			handle: { breadcrumb: 'Hộ chiếu, Sđ, Sc' },
		},
		{
			path: 'strengths',
			element: <MainCatalogParameterStrengthPage />,
			handle: { breadcrumb: 'Độ kiên cố than, đá (f)' },
		},
		{
			path: 'clamps',
			element: <MainCatalogParameterClampPage />,
			handle: { breadcrumb: 'Tỷ lệ đá kẹp (Ckẹp)' },
		},
		{
			path: 'inserts',
			element: <MainCatalogParameterInsertPage />,
			handle: { breadcrumb: 'Chèn' },
		},
		{
			path: 'production-orders',
			element: <MainCatalogParameterProductionOrderPage />,
			handle: { breadcrumb: 'Quyết định, lệnh sản xuất' },
		},
		{
			path: 'steps',
			element: <MainCatalogParameterStepPage />,
			handle: { breadcrumb: 'Bước chống' },
		},
		{
			path: 'technologies',
			element: <MainCatalogParameterTechnologyPage />,
			handle: { breadcrumb: 'Công nghệ khai thác' },
		},
		{
			path: 'seamfaces',
			element: <MainCatalogParameterSeamfacePage />,
			handle: { breadcrumb: 'Mặt vỉa' },
		},
		{
			path: 'cuttingthicknesses',
			element: <MainCatalogParameterCuttingthicknessPage />,
			handle: { breadcrumb: 'Chiều dày lớp khấu' },
		},
		{
			path: 'longwallparameters',
			element: <MainCatalogParameterLongwallparametersPage />,
			handle: { breadcrumb: 'Thông số lò chợ' },
		},
	],
};
