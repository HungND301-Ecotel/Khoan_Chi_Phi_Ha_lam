import { MainPricingLayout } from '@/features/main/pricing/layout';
import { LongwallPanelMaterialPage } from '@/features/main/pricing/longwall-panel/material/page';
import { MainPricingElectricityPage } from '@/features/main/pricing/tunneling/electricity/page';
import { MainPricingMaterialPage } from '@/features/main/pricing/tunneling/material/page';
import { MainPricingSlidePage } from '@/features/main/pricing/tunneling/slide/page';
import { RouteObject } from 'react-router-dom';
import { MainPricingLongwallElectricityPage } from './longwall-panel/electricity/page';
import { MainPricingMaintenanceLongwallPanelPage } from './longwall-panel/maintenance/page';
import { MainPricingMaintenanceTunnelingPage } from './tunneling/maintenance/page';

const MainPricingRouter: RouteObject = {
	path: 'pricing',
	handle: { breadcrumb: 'Đơn giá và định mức' },
	element: <MainPricingLayout />,
	children: [
		{
			path: 'tunneling',
			handle: { breadcrumb: 'Đào/Xén lò' },
			children: [
				{
					index: true,
					element: <MainPricingElectricityPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức điện năng',
						title: 'Đơn giá và định mức điện năng',
					},
				},
				{
					path: 'maintenance',
					element: <MainPricingMaintenanceTunnelingPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức SCTX',
						title: 'Đơn giá và định mức SCTX',
					},
				},
				{
					path: 'material',
					element: <MainPricingMaterialPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức vật liệu',
						title: 'Đơn giá và định mức vật liệu',
					},
				},
				{
					path: 'slide',
					element: <MainPricingSlidePage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức máng trượt',
						title: 'Đơn giá và định mức máng trượt',
					},
				},
			],
		},
		{
			path: 'longwall-panel',
			handle: { breadcrumb: 'Lò chợ' },
			children: [
				{
					path: 'electricity',
					element: <MainPricingLongwallElectricityPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức điện năng',
						title: 'Đơn giá và định mức điện năng',
					},
				},
				{
					path: 'material',
					element: <LongwallPanelMaterialPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức vật liệu',
						title: 'Đơn giá và định mức vật liệu',
					},
				},
				{
					path: 'maintenance',
					element: <MainPricingMaintenanceLongwallPanelPage />,
					handle: {
						breadcrumb: 'Đơn giá và định mức SCTX',
						title: 'Đơn giá và định mức SCTX',
					},
				},
			],
		},
	],
};

export default MainPricingRouter;
