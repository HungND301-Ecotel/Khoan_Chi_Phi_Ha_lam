/* eslint-disable react-refresh/only-export-components */
import {
	BadgeRussianRubleIcon,
	BoxesIcon,
	ClipboardListIcon,
	FileBarChart2Icon,
	FileChartColumnIcon,
	Settings2Icon,
} from 'lucide-react';
import { IconName } from 'lucide-react/dynamic';
import { type JSX } from 'react';
import { PERMISSIONS } from '@/constants/permissions';

export type Navigation = {
	type: 'link' | 'dropdown' | 'sub-menu';
	name: string;
	icon?: () => JSX.Element;
	href?: string;
	permission?: string | string[];
	items?: Navigation[];
};

export const NAVIGATIONS: Navigation[] = [
	{
		type: 'link',
		name: 'dash board',
		href: '/',
		icon: () => <FileChartColumnIcon className='size-5' strokeWidth={2} />,
	},
	{
		type: 'dropdown',
		name: 'danh mục',
		icon: () => <ClipboardListIcon className='size-5' strokeWidth={2} />,
		items: [
			{ type: 'link', name: 'Chức vụ', href: '/catalogs/positions', permission: PERMISSIONS.CATALOG.POSITION.READ },
			{ type: 'link', name: 'Cán bộ, nhân viên', href: '/catalogs/employees', permission: PERMISSIONS.CATALOG.EMPLOYEE.READ },
			{ type: 'link', name: 'Đơn vị tính', href: '/catalogs/units', permission: PERMISSIONS.CATALOG.UNIT.READ },
			{ type: 'link', name: 'Đơn vị', href: '/catalogs/departments', permission: PERMISSIONS.CATALOG.DEPARTMENT.READ },
			{ type: 'link', name: 'Công đoạn sản xuất', href: '/catalogs/processes', permission: PERMISSIONS.CATALOG.PROCESS_STEP.READ },
			{
				type: 'link',
				name: 'Nhóm vật tư, tài sản',
				href: '/catalogs/contract-codes',
				permission: PERMISSIONS.CATALOG.CONTRACT_CODE.READ,
			},
			{
				type: 'link',
				name: 'Vật tư, tài sản',
				href: '/catalogs/assets/internal',
				permission: PERMISSIONS.CATALOG.ASSET.READ,
			},
			{ type: 'link', name: 'Sản phẩm', href: '/catalogs/products', permission: PERMISSIONS.CATALOG.PRODUCT.READ },
			{ type: 'link', name: 'Thông số', href: '/catalogs/parameters', permission: PERMISSIONS.CATALOG.PARAMETER_PASSPORT.READ },
			{
				type: 'link',
				name: 'Hệ số điều chỉnh',
				href: '/catalogs/adjustments',
				permission: PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.READ,
			},
			{
				type: 'link',
				name: 'Quyết định, lệnh sản xuất',
				href: '/catalogs/production-orders',
				permission: PERMISSIONS.CATALOG.PRODUCTION_ORDER.READ,
			},
			{
				type: 'link',
				name: 'Hệ số điều chỉnh định mức',
				href: '/catalogs/norm-factors',
				permission: PERMISSIONS.CATALOG.NORM_FACTOR.READ,
			},
			{
				type: 'link',
				name: 'Hệ số tiết kiệm được chấp nhận',
				href: '/catalogs/accepted-savings-rates',
				permission: PERMISSIONS.CATALOG.SAVINGS_RATE.READ,
			},
			{
				type: 'link',
				name: 'Hệ số Ak',
				href: '/catalogs/ak-factors',
				permission: PERMISSIONS.CATALOG.AK_FACTOR.READ,
			},
			{
				type: 'link',
				name: 'Giá trị tiết kiệm được cộng/trừ vào thu nhập',
				href: '/catalogs/revenue-cost-adjustment-configs',
				permission: PERMISSIONS.CATALOG.REVENUE_COST.READ,
			},
		],
	},
	{
		type: 'dropdown',
		name: 'đơn giá và định mức',
		icon: () => <BadgeRussianRubleIcon className='size-5' strokeWidth={2} />,
		items: [
			{
				type: 'sub-menu',
				name: 'Đào lò',
				items: [
					{
						type: 'link',
						name: 'Đơn giá và định mức vật liệu',
						href: '/pricing/tunneling/material',
						permission: 'pricing.materialunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá khoán vật tư mau hỏng rẻ tiền',
						href: '/pricing/tunneling/low-value-perishable-supply',
						permission: 'pricing.tunnellowvalueperishablesupplyunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức máng trượt',
						href: '/pricing/tunneling/slide',
						permission: 'pricing.slideunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức SCTX',
						href: '/pricing/tunneling/maintenance',
						permission: 'pricing.maintainunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức điện năng',
						href: '/pricing/tunneling',
						permission: 'pricing.tunnerelectricityunitprice.read',
					},
				],
			},
			{
				type: 'sub-menu',
				name: 'Xén lò',
				items: [
					{
						type: 'link',
						name: 'Đơn giá và định mức vật liệu',
						href: '/pricing/trimming/material',
						permission: 'pricing.trimmingmaterialunitpricing.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức SCTX',
						href: '/pricing/trimming/maintenance',
						permission: 'pricing.trimmingmaintainunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức điện năng',
						href: '/pricing/trimming',
						permission: 'pricing.trimmingelectricityunitprice.read',
					},
				],
			},
			{
				type: 'sub-menu',
				name: 'Lò chợ',
				href: '/pricing/longwall-panel',
				items: [
					{
						type: 'link',
						name: 'Đơn giá và định mức vật liệu',
						href: '/pricing/longwall-panel/material',
						permission: 'pricing.longwallmaterialunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá khoán vật tư mau hỏng rẻ tiền',
						href: '/pricing/longwall-panel/low-value-perishable-supply',
						permission: 'pricing.longwalllowvalueperishablesupplyunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức SCTX',
						href: '/pricing/longwall-panel/maintenance',
						permission: 'pricing.longwallmaintainunitprice.read',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức điện năng',
						href: '/pricing/longwall-panel/electricity',
						permission: 'pricing.longwallelectricityunitprice.read',
					},
				],
			},
		],
	},
	{
		type: 'dropdown',
		name: 'thống kê vận hành',
		icon: () => <BoxesIcon className='size-5' strokeWidth={2} />,
		items: [
			{ type: 'link', name: 'Kế hoạch sản xuất', href: '/cost/plan', permission: 'production.productunitprice.read' },
			{ type: 'link', name: 'Vận hành sản xuất', href: '/cost/production', permission: 'production.productionoutput.read' },
			{
				type: 'link',
				name: 'Quyết toán giao khoán',
				href: '/cost/lump-sum-final-settlement',
				permission: 'production.lumpsumfinalsettlement.read',
			},
		],
	},
	{
		type: 'link',
		name: 'Báo cáo',
		href: '/report',
		icon: () => <FileBarChart2Icon className='size-5' strokeWidth={2} />,
		permission: [
			PERMISSIONS.REPORT.PRODUCT_UNIT_PRICE.EXPORT,
			PERMISSIONS.REPORT.LONG_TERM_TRACKING.EXPORT,
			PERMISSIONS.REPORT.ACCEPTANCE_REPORT.EXPORT,
			PERMISSIONS.REPORT.LUMP_SUM_FINAL_SETTLEMENT.EXPORT,
			PERMISSIONS.REPORT.SCTX_REVENUE_BY_ASSIGNMENT_CODE.EXPORT,
		],
	},
	{
		type: 'dropdown',
		name: 'hệ thống',
		icon: () => <Settings2Icon className='size-5' strokeWidth={2} />,
		items: [
			{ type: 'link', name: 'Phân quyền', href: '/system/permissions', permission: PERMISSIONS.SYSTEM.PERMISSION.READ },
			{ type: 'link', name: 'Khóa cấu hình', href: '/system/fixed-keys', permission: 'system.fixkey.read' },
		],
	},
] as const;

export type Information = {
	title: string;
	icon: IconName;
	description: string;
};

export const topInfo: Information[] = [
	{
		title: 'Email',
		icon: 'mail',
		description: 'lienhe@halamcoal.com.vn',
	},
	{
		title: 'Số điện thoại',
		icon: 'phone',
		description: '(02033).825399',
	},
	{
		title: 'Website',
		icon: 'globe',
		description: 'halamcoal.com.vn',
	},
];

export const bottomInfo: Information[] = [
	{
		title: 'MST',
		description: '5700101637',
		icon: 'landmark',
	},
	{
		title: 'Địa chỉ',
		description: '	Số 1, phố Tân Lập, Phường Hà Lầm, Tỉnh Quảng Ninh, Việt Nam',
		icon: 'map-pin',
	},
];
