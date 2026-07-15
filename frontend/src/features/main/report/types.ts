// Entity types for the report system
export interface ReportEntity {
	id: string;
	code: string;
	name: string;
	description?: string;
}

import { PERMISSIONS } from '@/constants/permissions';

// Category type for sidebar navigation
export interface ReportCategory {
	id: string;
	label: string;
	group?: string;
	permission?: string;
}

// Category groups for organizing the sidebar
export const REPORT_CATEGORY_GROUPS = [
	{ id: 'reports', label: 'Báo cáo' },
] as const;

// Sample categories - replace with your actual categories
export const REPORT_CATEGORIES: ReportCategory[] = [
	// {
	// 	id: 'raw-acceptance-report',
	// 	label: 'Biên bản nghiệm thu',
	// 	group: 'reports',
	// },
	{
		id: 'electricity-and-maintainance-report',
		label: 'Bảng tính đơn giá SCTX và điện năng',
		group: 'reports',
		permission: PERMISSIONS.REPORT.PRODUCT_UNIT_PRICE.EXPORT,
	},
	{
		id: 'longterm-material-cost',
		label: 'Bảng hạch toán chi phí dài kỳ',
		group: 'reports',
		permission: PERMISSIONS.REPORT.LONG_TERM_TRACKING.EXPORT,
	},
	{
		id: 'acceptance-report',
		label: 'Bảng nghiệm thu vật tư và kết chuyển chi phí',
		group: 'reports',
		permission: PERMISSIONS.REPORT.ACCEPTANCE_REPORT.EXPORT,
	},
	{
		id: 'lump-sum-final-settlement',
		label: 'Bảng quyết toán',
		group: 'reports',
		permission: PERMISSIONS.REPORT.LUMP_SUM_FINAL_SETTLEMENT.EXPORT,
	},
	{
		id: 'lump-sum-final-settlement-month',
		label: 'Bảng thanh toán',
		group: 'reports',
		permission: PERMISSIONS.REPORT.LUMP_SUM_FINAL_SETTLEMENT.EXPORT,
	},
	{
		id: 'sctx-revenue-report',
		label: 'Báo cáo doanh thu SCTX',
		group: 'reports',
		permission: PERMISSIONS.REPORT.SCTX_REVENUE_BY_ASSIGNMENT_CODE.EXPORT,
	},
];
