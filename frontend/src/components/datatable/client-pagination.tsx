import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';

type ClientPaginationProps = {
	totalItems: number;
	pageIndex: number;
	pageSize: number;
	onPageIndexChange: (pageIndex: number) => void;
	onPageSizeChange: (pageSize: number) => void;
	className?: string;
};

export function ClientPagination({
	totalItems,
	pageIndex,
	pageSize,
	onPageIndexChange,
	onPageSizeChange,
	className,
}: ClientPaginationProps) {
	const MAX_VISIBLE_PAGES = 10;
	const MIN_VISIBLE_PAGES = 3;
	const rootRef = useRef<HTMLDivElement | null>(null);
	const [containerWidth, setContainerWidth] = useState(0);
	const pageCount = Math.ceil(totalItems / pageSize);
	const safePageCount = Math.max(pageCount, 1);
	const safePageIndex =
		pageCount > 0 ? Math.min(pageIndex, pageCount - 1) : 0;
	const [jumpPage, setJumpPage] = useState(`${safePageIndex + 1}`);

	const startItem = totalItems === 0 ? 0 : safePageIndex * pageSize + 1;
	const endItem = Math.min((safePageIndex + 1) * pageSize, totalItems);

	useEffect(() => {
		setJumpPage(`${safePageIndex + 1}`);
	}, [safePageIndex]);

	useEffect(() => {
		const element = rootRef.current;
		if (!element) return;

		const updateWidth = () => {
			setContainerWidth(element.clientWidth);
		};

		updateWidth();

		const observer = new ResizeObserver(() => {
			updateWidth();
		});

		observer.observe(element);

		return () => {
			observer.disconnect();
		};
	}, []);

	const maxVisiblePages = useMemo(() => {
		if (containerWidth <= 0) {
			return MAX_VISIBLE_PAGES;
		}

		if (containerWidth >= 1360) return 10;
		if (containerWidth >= 1240) return 9;
		if (containerWidth >= 1140) return 8;
		if (containerWidth >= 1060) return 7;
		if (containerWidth >= 980) return 6;
		if (containerWidth >= 920) return 5;
		if (containerWidth >= 860) return 4;
		return MIN_VISIBLE_PAGES;
	}, [containerWidth]);

	const visiblePages = useMemo(() => {
		if (pageCount <= 0) {
			return [];
		}

		const windowStart =
			Math.floor(safePageIndex / maxVisiblePages) * maxVisiblePages;
		const windowEnd = Math.min(windowStart + maxVisiblePages, pageCount);

		return Array.from(
			{ length: windowEnd - windowStart },
			(_, index) => windowStart + index,
		);
	}, [maxVisiblePages, pageCount, safePageIndex]);

	const goToPage = () => {
		const parsed = Number(jumpPage);
		if (!Number.isFinite(parsed)) {
			setJumpPage(`${safePageIndex + 1}`);
			return;
		}

		const targetPage = Math.min(
			Math.max(Math.trunc(parsed), 1),
			safePageCount,
		);
		onPageIndexChange(targetPage - 1);
		setJumpPage(`${targetPage}`);
	};

	return (
		<div
			ref={rootRef}
			className={cn(
				'grid w-full min-w-0 grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-4 bg-transparent px-4',
				className,
			)}
		>
			<span className='min-w-0 truncate text-left text-[14px]'>
				Hiển thị {startItem}-{endItem} trên {totalItems} mục
			</span>
			<div className='mx-auto flex min-w-0 items-center justify-center gap-2'>
				<Button
					type='button'
					variant='ghost'
					size='icon'
					className='size-8 shrink-0 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
					onClick={() => onPageIndexChange(Math.max(safePageIndex - 1, 0))}
					disabled={safePageIndex <= 0 || pageCount === 0}
				>
					<ChevronLeft />
				</Button>
				<div className='flex min-w-0 items-center justify-center gap-2'>
					{visiblePages.map((index) => (
						<Button
							key={index}
							type='button'
							variant='ghost'
							size='icon'
							className={cn(
								'h-8 w-8 shrink-0 rounded-sm bg-white shadow-none hover:bg-[#e3e4e7] hover:shadow-none',
								index === safePageIndex &&
									'border-primary text-primary border font-semibold',
							)}
							onClick={() => onPageIndexChange(index)}
						>
							{index + 1}
						</Button>
					))}
				</div>
				<Button
					type='button'
					variant='ghost'
					size='icon'
					className='size-8 shrink-0 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
					onClick={() =>
						onPageIndexChange(Math.min(safePageIndex + 1, pageCount - 1))
					}
					disabled={pageCount === 0 || safePageIndex >= pageCount - 1}
				>
					<ChevronRight />
				</Button>
			</div>
			<div className='ml-auto flex min-w-0 items-center justify-end gap-3 whitespace-nowrap justify-self-end'>
				<div className='flex items-center gap-2 whitespace-nowrap'>
					<span className='text-[14px]'>Đến trang</span>
					<Input
						type='number'
						min={1}
						max={safePageCount}
						value={jumpPage}
						onChange={(event) => setJumpPage(event.target.value)}
						onBlur={goToPage}
						onKeyDown={(event) => {
							if (event.key === 'Enter') {
								event.preventDefault();
								goToPage();
							}
						}}
						className='h-8 w-20'
					/>
				</div>
				<Select
					value={`${pageSize}`}
					onValueChange={(value) => {
						onPageSizeChange(Number(value));
					}}
				>
					<SelectTrigger className='hover:border-primary h-8 w-28 border shadow-none hover:shadow-none'>
						<SelectValue placeholder={pageSize} />
					</SelectTrigger>
					<SelectContent side='top' className='mb-2'>
						{[10, 20, 50, 100].map((nextPageSize) => (
							<SelectItem key={nextPageSize} value={`${nextPageSize}`}>
								{`${nextPageSize} / trang`}
							</SelectItem>
						))}
					</SelectContent>
				</Select>
			</div>
		</div>
	);
}
