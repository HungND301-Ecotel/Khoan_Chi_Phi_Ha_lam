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
	{ title: 'Hộ chiếu, Sđ, Sc', href: '/catalogs/parameters/passports' },
	{ title: 'Độ kiên cố than đá (f)', href: '/catalogs/parameters/strengths' },
	{ title: 'Tỷ lệ đá kẹp (Ckẹp)', href: '/catalogs/parameters/clamps' },
	{ title: 'Chèn', href: '/catalogs/parameters/inserts' },
	{ title: 'Bước chống', href: '/catalogs/parameters/steps' },
	{ title: 'Công nghệ khai thác', href: '/catalogs/parameters/technologies' },
	{ title: 'Mặt vỉa (M)', href: '/catalogs/parameters/seamfaces' },
	{
		title: 'Chiều dày lớp khấu',
		href: '/catalogs/parameters/cuttingthicknesses',
	},
	{ title: 'Thông số lò chợ', href: '/catalogs/parameters/longwallparameters' },
	{
		title: 'Quyết định, lệnh sản xuất',
		href: '/catalogs/parameters/production-orders',
	},
] as const;

export function MainCatalogParameterLayout() {
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
							<SelectValue>{currentTab?.title || 'Chọn danh mục'}</SelectValue>
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
