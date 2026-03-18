import {
	Breadcrumb,
	BreadcrumbItem,
	BreadcrumbList,
	BreadcrumbPage,
	BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'; // Giả định bạn có các component Breadcrumb này
import { useMeta } from '@/data/meta/meta-hook';
import React from 'react';

export type DynamicBreadCrumbsProps = {
	children?: string[];
};

export function DynamicBreadCrumbs({ children }: DynamicBreadCrumbsProps) {
	const { breadcrumbs } = useMeta();

	const crumbs = breadcrumbs?.filter((crumb) => !!crumb);

	if (children) crumbs.push(...children);

	if (!breadcrumbs || breadcrumbs.length === 0) {
		return null;
	}

	return (
		<Breadcrumb className='border-primary'>
			<BreadcrumbList className='flex-nowrap gap-2 whitespace-nowrap'>
				{crumbs.map((crumb, index) => {
					const isLast = index === crumbs.length - 1;
					return (
						<React.Fragment key={`${crumb}-${index}`}>
							<BreadcrumbItem>
								<BreadcrumbPage className='text-[1rem] text-[#00000099]'>
									{crumb}
								</BreadcrumbPage>
							</BreadcrumbItem>
							{!isLast && (
								<BreadcrumbSeparator
									className='text-[1rem] text-[#00000099]'
									children={'/'}
								/>
							)}
						</React.Fragment>
					);
				})}
			</BreadcrumbList>
		</Breadcrumb>
	);
}
