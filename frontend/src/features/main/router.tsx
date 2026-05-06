import MainCatalogRouter from '@/features/main/catalog/router';
import { MainCostRouter } from '@/features/main/cost/router';
import MainLayout from '@/features/main/layout';
import MainPricingRouter from '@/features/main/pricing/router';
import DashboardPage from '@/features/main/dashboard/page';
import type { RouteObject } from 'react-router-dom';
import { MainReportRouter } from './report/router';
import { MainSystemRouter } from './system/router';

const MainRouter: RouteObject = {
	path: '/',
	element: <MainLayout />,
	children: [
		{
			index: true,
			element: <DashboardPage />,
		},
		MainCatalogRouter,
		MainPricingRouter,
		MainCostRouter,
		MainReportRouter,
		MainSystemRouter,
	],
};

export default MainRouter;
