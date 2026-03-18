import { DynamicBreadCrumbs } from '@/features/main/layout/breadcrumbs';
import { DynamicTitle } from '@/features/main/layout/title';
import { Outlet } from 'react-router-dom';

export function MainCatalogLayout() {
	return (
		<div className='flex flex-col gap-4'>
			<div className='flex flex-col gap-6'>
				<DynamicBreadCrumbs />
				<DynamicTitle />
			</div>
			<Outlet />
		</div>
	);
}
