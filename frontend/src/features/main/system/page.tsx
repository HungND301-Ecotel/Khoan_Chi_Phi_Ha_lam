'use client';

import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { SystemSidebar } from './sidebar';
import { SYSTEM_CATEGORIES } from './types';

const DEFAULT_CATEGORY_ID = SYSTEM_CATEGORIES[0]?.id ?? 'master-data';

export function MainSystemPage() {
	const navigate = useNavigate();
	const location = useLocation();

	const selectedCategory = useMemo(() => {
		const pathSegments = location.pathname.split('/').filter(Boolean);
		const currentCategoryId = pathSegments[1] ?? DEFAULT_CATEGORY_ID;
		const isValidCategory = SYSTEM_CATEGORIES.some(
			(category) => category.id === currentCategoryId,
		);
		return isValidCategory ? currentCategoryId : DEFAULT_CATEGORY_ID;
	}, [location.pathname]);

	const handleSelectCategory = (categoryId: string) => {
		if (!categoryId || categoryId === selectedCategory) return;
		navigate(`/system/${categoryId}`);
	};

	return (
		<div className='mb-14 flex min-w-0 items-stretch gap-4'>
			<SystemSidebar
				selectedCategory={selectedCategory}
				onSelectCategory={handleSelectCategory}
			/>
			<div className='min-w-0 flex-1'>
				<Outlet />
			</div>
		</div>
	);
}