import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Link, Outlet, useLocation } from 'react-router-dom';

const TABS: { title: string; href: string }[] = [
	{ title: 'Bảng Thanh toán', href: '/cost/lump-sum-final-settlement/month' },
	{ title: 'Bảng Quyết toán', href: '/cost/lump-sum-final-settlement/quarter' },
] as const;

export function MainCostLumpSumFinalSettlementPage() {
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
