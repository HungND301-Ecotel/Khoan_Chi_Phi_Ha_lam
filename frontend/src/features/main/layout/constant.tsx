/* eslint-disable react-refresh/only-export-components */
import {
	BadgeRussianRubleIcon,
	BoxesIcon,
	ClipboardListIcon,
	FileBarChart2Icon,
	FileChartColumnIcon,
} from 'lucide-react';
import { IconName } from 'lucide-react/dynamic';
import { type JSX } from 'react';

export type Navigation = {
	type: 'link' | 'dropdown' | 'sub-menu';
	name: string;
	icon?: () => JSX.Element;
	href?: string;
	items?: Navigation[];
};

export const NAVIGATIONS: Navigation[] = [
	{
		type: 'link',
		name: 'dash board',
		href: '/',
		icon: () => (
			<FileChartColumnIcon className='size-5 text-[#9114cc]' strokeWidth={2} />
		),
	},
	{
		type: 'dropdown',
		name: 'danh mục',
		icon: () => (
			<ClipboardListIcon className='size-5 text-[#4caf50]' strokeWidth={2} />
		),
		items: [
			{ type: 'link', name: 'Đơn vị tính', href: '/catalogs/units' },
			{ type: 'link', name: 'Công đoạn sản xuất', href: '/catalogs/processes' },
			{ type: 'link', name: 'Mã giao khoán', href: '/catalogs/contract-codes' },
			{
				type: 'sub-menu',
				name: 'Vật tư, tài sản',
				items: [
					{
						type: 'link',
						name: 'Vật tư, tài sản trong khoán',
						href: '/catalogs/assets/internal',
					},
					{
						type: 'link',
						name: 'Vật tư, tài sản ngoài khoán',
						href: '/catalogs/assets/external',
					},
				],
			},
			{ type: 'link', name: 'Thiết bị', href: '/catalogs/equipments' },
			// { type: 'link', name: 'Phụ tùng', href: '/catalogs/spare-parts' },
			{
				type: 'sub-menu',
				name: 'Phụ tùng',
				items: [
					{
						type: 'link',
						name: 'Phụ tùng theo thiết bị',
						href: '/catalogs/spare-parts/main',
					},
					{
						type: 'link',
						name: 'Phụ tùng khác',
						href: '/catalogs/spare-parts/other',
					},
				],
			},
			{ type: 'link', name: 'Sản phẩm', href: '/catalogs/products' },
			{ type: 'link', name: 'Thông số', href: '/catalogs/parameters' },
			{
				type: 'link',
				name: 'Hệ số điều chỉnh',
				href: '/catalogs/adjustments',
			},
		],
	},
	{
		type: 'dropdown',
		name: 'đơn giá và định mức',
		icon: () => (
			<BadgeRussianRubleIcon
				className='size-5 text-[#cc146c]'
				strokeWidth={2}
			/>
		),
		items: [
			{
				type: 'sub-menu',
				name: 'Đào lò',
				items: [
					{
						type: 'link',
						name: 'Đơn giá và định mức vật liệu',
						href: '/pricing/tunneling/material',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức máng trượt',
						href: '/pricing/tunneling/slide',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức SCTX',
						href: '/pricing/tunneling/maintenance',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức điện năng',
						href: '/pricing/tunneling',
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
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức SCTX',
						href: '/pricing/longwall-panel/maintenance',
					},
					{
						type: 'link',
						name: 'Đơn giá và định mức điện năng',
						href: '/pricing/longwall-panel/electricity',
					},
				],
			},
		],
	},
	{
		type: 'dropdown',
		name: 'thống kê vận hành',
		icon: () => <BoxesIcon className='size-5 text-[#f3d016]' strokeWidth={2} />,
		items: [
			{ type: 'link', name: 'Kế hoạch sản xuất', href: '/cost/plan' },
			{ type: 'link', name: 'Vận hành sản xuất', href: '/cost/production' },
			{
				type: 'link',
				name: 'Quyết toán giao khoán',
				href: '/cost/lump-sum-final-settlement',
			},
		],
	},
	{
		type: 'link',
		name: 'Báo cáo',
		href: '/report',
		icon: () => (
			<FileBarChart2Icon className='size-5 text-blue-500' strokeWidth={2} />
		),
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
