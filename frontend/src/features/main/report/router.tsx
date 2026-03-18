import { Navigate, RouteObject } from 'react-router-dom';
import { MainReportLayout } from './layout';
import { AcceptanceReportPage } from './acceptance-report/page';
import { ElectricityAndMaintainanceReportPage } from './electricity-and-maintainance-report/page';
import { LumpSumFinalSettlementPage } from './lump-sum-final-settlement/page';
import { MainReportPage } from './page';
import { LongtermMaterialCostPage } from './longterm-material-cost/page';
import { RawAcceptanceReportPage } from './raw-acceptance-report/page';
import { REPORT_CATEGORIES } from './types';
import { ReportCategoryPlaceholderPage } from './placeholder-page';

const defaultCategoryId = REPORT_CATEGORIES[0]?.id || 'raw-acceptance-report';

const reportCategoryRoutes: RouteObject[] = REPORT_CATEGORIES.map(
	(category) => ({
		path: category.id,
		handle: {
			breadcrumb: category.label,
			title: category.label,
		},
		element:
			category.id === 'raw-acceptance-report' ? (
				<RawAcceptanceReportPage />
			) : category.id === 'acceptance-report' ? (
				<AcceptanceReportPage />
			) : category.id === 'electricity-and-maintainance-report' ? (
				<ElectricityAndMaintainanceReportPage />
			) : category.id === 'longterm-material-cost' ? (
				<LongtermMaterialCostPage />
			) : category.id === 'lump-sum-final-settlement' ? (
				<LumpSumFinalSettlementPage />
			) : (
				<ReportCategoryPlaceholderPage categoryLabel={category.label} />
			),
	}),
);

export const MainReportRouter: RouteObject = {
	path: 'report',
	element: <MainReportLayout />,
	handle: { breadcrumb: 'Báo cáo' },
	children: [
		{
			element: <MainReportPage />,
			children: [
				{
					index: true,
					element: <Navigate replace to={defaultCategoryId} />,
				},
				...reportCategoryRoutes,
				{
					path: '*',
					element: <Navigate replace to={defaultCategoryId} />,
				},
			],
		},
	],
};
