import { MainCostLayout } from '@/features/main/cost/layout';
import { MainCostLumpSumFinalSettlementPage } from '@/features/main/cost/lump-sum-final-settlement/page';
import { MainCostPlanPage } from '@/features/main/cost/plan/page';
import { MainCostProductionRouter } from '@/features/main/cost/producttion/router';
import { Navigate, RouteObject } from 'react-router-dom';

export const MainCostRouter: RouteObject = {
	path: 'cost',
	element: <MainCostLayout />,
	handle: { breadcrumb: 'Thống kê vận hành' },
	children: [
		{
			index: true,
			element: <Navigate replace to='/cost/plan' />,
		},
		{
			path: 'plan',
			handle: {
				breadcrumb: 'Kế hoạch sản xuất',
				title: 'Kế hoạch sản xuất',
			},
			element: <MainCostPlanPage />,
		},
		MainCostProductionRouter,
		{
			path: 'lump-sum-final-settlement',
			handle: {
				breadcrumb: 'Quyết toán giao khoán',
				title: 'Quyết toán giao khoán',
			},
			element: <MainCostLumpSumFinalSettlementPage />,
		},
	],
};
