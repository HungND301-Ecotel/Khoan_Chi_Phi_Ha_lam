import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';

const TABS: { title: string; href: string }[] = [
	{ title: 'Hệ số điều chỉnh', href: '/catalogs/factors/adjustment-factors' },
	{
		title: 'Diễn giải hệ số điều chỉnh',
		href: '/catalogs/factors/adjustment-interpreters',
	},
	{
		title: 'Hệ số điều chỉnh định mức',
		href: '/catalogs/factors/norm-factors',
	},
	{
		title: 'Hệ số tiết kiệm được chấp nhận',
		href: '/catalogs/factors/savings-rates',
	},
	{ title: 'Hệ số Ak', href: '/catalogs/factors/ak-factors' },
	{
		title: 'Giá trị tiết kiệm được cộng/trừ vào thu nhập',
		href: '/catalogs/factors/revenue-cost-adjustments',
	},
] as const;

export function MainCatalogFactorLayout() {
	const location = useLocation();
	const navigate = useNavigate();
	const path = location.pathname;
	const [isMobile, setIsMobile] = useState(false);

	useEffect(() => {
		const checkMobile = () => {
			setIsMobile(window.innerWidth < 768);
		};

		checkMobile();
		window.addEventListener('resize', checkMobile);

		return () => window.removeEventListener('resize', checkMobile);
	}, []);

	const currentTab = TABS.find((tab) => tab.href === path);

	if (isMobile) {
		return (
			<>
				<div className='mb-4'>
					<Select value={path} onValueChange={(value) => navigate(value)}>
						<SelectTrigger className='w-full'>
							<SelectValue>{currentTab?.title || 'Chọn hệ số'}</SelectValue>
						</SelectTrigger>
						<SelectContent>
							{TABS.map((tab) => (
								<SelectItem key={tab.href} value={tab.href}>
									{tab.title}
								</SelectItem>
							))}
						</SelectContent>
					</Select>
				</div>
				<Outlet />
			</>
		);
	}

	return (
		<>
			<Tabs value={path}>
				<TabsList className='inline-flex h-auto min-h-10 flex-wrap items-center justify-center'>
					{TABS.map((tab) => (
						<TabsTrigger
							key={tab.href}
							value={tab.href}
							className='min-h-[47px]'
						>
							<Link to={tab.href}>{tab.title}</Link>
						</TabsTrigger>
					))}
				</TabsList>
			</Tabs>
			<Outlet />
		</>
	);
}

export default MainCatalogFactorLayout;
