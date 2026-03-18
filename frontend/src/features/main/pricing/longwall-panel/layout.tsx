import { Outlet } from 'react-router-dom';

export function MainPricingLongwallPanelLayout() {
	return (
		<div className='flex flex-col gap-4'>
			<Outlet />
		</div>
	);
}
