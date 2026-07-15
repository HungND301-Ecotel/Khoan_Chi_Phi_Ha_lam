'use client';

import { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { usePermission } from '@/hooks/use-permission';
import { ReportSidebar } from './sidebar';
import { REPORT_CATEGORIES } from './types';

// const DEFAULT_CATEGORY_ID = REPORT_CATEGORIES[0]?.id ?? '';

export function MainReportPage() {
	const navigate = useNavigate();
	const location = useLocation();
	const { hasPermission } = usePermission();

	const authorizedCategories = useMemo(() => {
		return REPORT_CATEGORIES.filter(
			(c) => !c.permission || hasPermission(c.permission),
		);
	}, [hasPermission]);

	const selectedCategory = useMemo(() => {
		const pathSegments = location.pathname.split('/').filter(Boolean);
		const currentCategoryId = pathSegments[1];

		const defaultCategoryId = authorizedCategories[0]?.id ?? '';

		const isValidCategory = authorizedCategories.some(
			(category) => category.id === currentCategoryId,
		);

		return isValidCategory ? currentCategoryId : defaultCategoryId;
	}, [location.pathname, authorizedCategories]);

	const handleSelectCategory = (nextCategoryId: string) => {
		if (!nextCategoryId || nextCategoryId === selectedCategory) {
			return;
		}

		navigate(`/report/${nextCategoryId}`);
	};

	return (
		<div className='mb-14 flex min-w-0 items-stretch gap-4'>
			<ReportSidebar
				selectedCategory={selectedCategory}
				onSelectCategory={handleSelectCategory}
			/>
			<div className='min-w-0 flex-1'>
				<Outlet />
			</div>
		</div>
	);
}
