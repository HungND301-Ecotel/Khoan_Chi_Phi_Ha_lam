// Entity types for the report system
export interface ReportEntity {
	id: string;
	code: string;
	name: string;
	description?: string;
}

// Category type for sidebar navigation
export interface ReportCategory {
	id: string;
	label: string;
	group?: string;
}

// Category groups for organizing the sidebar
export const REPORT_CATEGORY_GROUPS = [
	{ id: 'reports', label: 'Báo cáo' },
] as const;

// Sample categories - replace with your actual categories
export const REPORT_CATEGORIES: ReportCategory[] = [
	{
		id: 'raw-acceptance-report',
		label: 'Biên bản nghiệm thu',
		group: 'reports',
	},
	{
		id: 'electricity-and-maintainance-report',
		label: 'Bảng tính đơn giá SCTX và điện năng',
		group: 'reports',
	},
	{
		id: 'longterm-material-cost',
		label: 'Bảng hạch toán chi phí dài kỳ',
		group: 'reports',
	},
	{
		id: 'acceptance-report',
		label: 'Bảng nghiệm thu vật tư và kết chuyển chi phí',
		group: 'reports',
	},
	{
		id: 'lump-sum-final-settlement',
		label: 'Bảng thanh toán',
		group: 'reports',
	},
];
