import { MainCostProductionRevenueAdjustmentPage } from '@/features/main/cost/producttion/adjustment/page';
import { MainCostProductionCostPage } from '@/features/main/cost/producttion/production/page';
import { MainCostProductionLayout } from '@/features/main/cost/producttion/layout';
import { Navigate, RouteObject } from 'react-router-dom';

export const MainCostProductionRouter: RouteObject = {
	path: 'production',
	element: <MainCostProductionLayout />,
	handle: { breadcrumb: 'Vận hành sản xuất', title: 'Vận hành sản xuất' },
	children: [
		{
			index: true,
			element: <Navigate replace to='cost' />,
		},
		{
			path: 'cost',
			element: <MainCostProductionCostPage />,
			handle: { breadcrumb: 'Chi phí' },
		},
		{
			path: 'revenue-adjustment',
			element: <MainCostProductionRevenueAdjustmentPage />,
			handle: { breadcrumb: 'Doanh thu điều chỉnh' },
		},
	],
};
