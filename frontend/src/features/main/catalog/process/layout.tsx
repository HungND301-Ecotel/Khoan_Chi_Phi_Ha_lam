import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Link, Outlet, useLocation } from 'react-router-dom';

const TABS: { title: string; href: string }[] = [
	{ title: 'Nhóm công đoạn sản xuất', href: '/catalogs/processes/groups' },
	{ title: 'Công đoạn sản xuất', href: '/catalogs/processes/steps' },
] as const;

export function MainCatalogProcessLayout() {
	const location = useLocation();
	const path = location.pathname;

	return (
		<>
			<Tabs
				value={path}
				className='w-fit rounded-sm border border-[#e6e9ee] p-1'
			>
				<TabsList className='gap-3 rounded-none border-none p-1'>
					{TABS.map((tab) => (
						<TabsTrigger
							key={tab.href}
							value={tab.href}
							className='px-[18px] text-sm data-[state=active]:bg-[#f1f3f5]'
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
