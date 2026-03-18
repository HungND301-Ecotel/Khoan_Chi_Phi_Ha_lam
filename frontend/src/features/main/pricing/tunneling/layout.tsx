import { Outlet } from 'react-router-dom';

export function MainPricingTunnelingLayout() {
	return (
		<div className='flex flex-col gap-4'>
			<Outlet />
		</div>
	);
}
