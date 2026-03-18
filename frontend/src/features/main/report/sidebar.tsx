'use client';

import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import SearchIcon from '@mui/icons-material/Search';
import { useMemo, useState } from 'react';
import {
	REPORT_CATEGORIES,
	REPORT_CATEGORY_GROUPS,
	ReportCategory,
} from './types';

interface ReportSidebarProps {
	selectedCategory?: string;
	onSelectCategory: (categoryId: string) => void;
}

export function ReportSidebar({
	selectedCategory,
	onSelectCategory,
}: ReportSidebarProps) {
	const [searchQuery, setSearchQuery] = useState('');

	const filteredCategories = useMemo(() => {
		if (!searchQuery.trim()) return REPORT_CATEGORIES;
		const q = searchQuery.toLowerCase();
		return REPORT_CATEGORIES.filter((c) => c.label.toLowerCase().includes(q));
	}, [searchQuery]);

	const groupedCategories = useMemo(() => {
		const groups = new Map<string, ReportCategory[]>();
		REPORT_CATEGORY_GROUPS.forEach((g) => groups.set(g.id, []));
		groups.set('ungrouped', []);
		filteredCategories.forEach((c) => {
			groups.get(c.group || 'ungrouped')?.push(c);
		});
		return groups;
	}, [filteredCategories]);

	return (
		<div className='border-border flex w-60 shrink-0 flex-col self-stretch overflow-hidden rounded-2xl border bg-white shadow-sm'>
			{/* Search */}
			<div className='px-3 pt-3 pb-2'>
				<div className='relative'>
					<SearchIcon
						style={{ fontSize: 16 }}
						className='text-muted-foreground absolute top-1/2 left-2.5 -translate-y-1/2'
					/>
					<Input
						placeholder='Tìm kiếm danh mục...'
						value={searchQuery}
						onChange={(e) => setSearchQuery(e.target.value)}
						className='focus-visible:ring-primary/30 focus-visible:border-primary h-9 pl-8 text-sm focus-visible:ring-1'
					/>
				</div>
			</div>

			{/* Menu */}
			<div className='flex-1 overflow-y-auto py-1 pb-3'>
				<nav className='flex flex-col gap-0.5 px-2'>
					{REPORT_CATEGORY_GROUPS.map((group) => {
						const categories = groupedCategories.get(group.id) || [];
						if (!categories.length) return null;
						return (
							<div key={group.id} className='mb-1'>
								{/* Group label */}
								<div className='text-primary px-2 pt-4 pb-1.5 text-[13px] font-bold'>
									{group.label}
								</div>
								<div className='flex flex-col gap-1'>
									{categories.map((category) => (
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
								</div>
							</div>
						);
					})}

					{(() => {
						const ungrouped = groupedCategories.get('ungrouped') || [];
						if (!ungrouped.length) return null;
						return (
							<div className='flex flex-col gap-1'>
								{ungrouped.map((category) => (
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
							</div>
						);
					})()}
				</nav>
			</div>
		</div>
	);
}
