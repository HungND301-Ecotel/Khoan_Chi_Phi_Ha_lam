import MainCatalogRouter from '@/features/main/catalog/router';
import { MainCostRouter } from '@/features/main/cost/router';
import MainLayout from '@/features/main/layout';
import MainPricingRouter from '@/features/main/pricing/router';
import DashboardPage from '@/features/main/dashboard/page';
import ProfilePage from '@/features/main/profile/page';
import type { RouteObject } from 'react-router-dom';
import { MainReportRouter } from './report/router';
import { MainSystemRouter } from './system/router';
import { ProtectedRoute } from '@/components/protected-route';

const MainRouter: RouteObject = {
	path: '/',
	element: (
		<ProtectedRoute>
			<MainLayout />
		</ProtectedRoute>
	),
	children: [
		{
			index: true,
			element: <DashboardPage />,
		},
		{
			path: 'profile',
			element: <ProfilePage />,
			handle: { breadcrumb: 'Thông tin cá nhân', title: 'Thông tin cá nhân' },
		},
		MainCatalogRouter,
		MainPricingRouter,
		MainCostRouter,
		MainReportRouter,
		MainSystemRouter,
	],
};

export default MainRouter;
