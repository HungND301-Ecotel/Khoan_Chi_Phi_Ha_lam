import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Link, Outlet, useLocation } from 'react-router-dom';

const TABS: { title: string; href: string }[] = [
	{ title: 'Chi phí', href: '/cost/production/cost' },
	{
		title: 'Doanh thu điều chỉnh',
		href: '/cost/production/revenue-adjustment',
	},
] as const;

export function MainCostProductionLayout() {
	const location = useLocation();
	const path = location.pathname;

	return (
		<>
			<Tabs value={path}>
				<TabsList>
					{TABS.map((tab) => (
						<TabsTrigger key={tab.href} value={tab.href}>
							<Link to={tab.href}>{tab.title}</Link>
						</TabsTrigger>
					))}
				</TabsList>
			</Tabs>
			<Outlet />
		</>
	);
}
