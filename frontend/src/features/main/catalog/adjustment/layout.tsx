import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Link, Outlet, useLocation } from 'react-router-dom';

const TABS: { title: string; href: string }[] = [
	{ title: 'Hệ số điều chỉnh', href: '/catalogs/adjustments/factors' },
	{ title: 'Diễn giải', href: '/catalogs/adjustments/interpreters' },
] as const;

export function MainCatalogAdjustmentLayout() {
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
