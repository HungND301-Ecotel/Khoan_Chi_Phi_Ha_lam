'use client';

import { cn } from '@/lib/utils';
import { SYSTEM_CATEGORIES } from './types';

type SystemSidebarProps = {
	selectedCategory: string;
	onSelectCategory: (categoryId: string) => void;
};

export function SystemSidebar({
	selectedCategory,
	onSelectCategory,
}: SystemSidebarProps) {
	return (
		<div className='border-border flex w-60 shrink-0 flex-col self-stretch overflow-hidden rounded-2xl border bg-white shadow-sm'>
			<div className='border-b px-4 py-4'>
				<div className='text-primary text-sm font-semibold uppercase'>Hệ thống</div>
				<div className='text-muted-foreground mt-1 text-sm'>
					Quản trị các khoá cố định dùng trong hệ thống.
				</div>
			</div>
			<nav className='flex flex-col gap-1 p-2'>
				{SYSTEM_CATEGORIES.map((category) => (
					<button
						key={category.id}
						onClick={() => onSelectCategory(category.id)}
						className={cn(
							'flex w-full items-center rounded-lg px-3 py-3 text-left text-[15px] transition-colors',
							selectedCategory === category.id
								? 'bg-primary text-primary-foreground font-medium'
								: 'text-foreground hover:bg-muted',
						)}
					>
						{category.label}
					</button>
				))}
			</nav>
		</div>
	);
}