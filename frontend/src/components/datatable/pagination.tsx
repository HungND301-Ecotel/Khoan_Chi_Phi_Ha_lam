import { type Table } from '@tanstack/react-table';
import { ChevronLeft, ChevronRight } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

interface DataTablePaginationProps<TData> {
	table: Table<TData>;
}

export function DataTablePagination<TData>({
	table,
}: DataTablePaginationProps<TData>) {
	const pageSize = table.getState().pagination.pageSize;
	const pageIndex = table.getState().pagination.pageIndex;
	const totalItems = table.getFilteredRowModel().rows.length;

	const startItem = totalItems === 0 ? 0 : pageIndex * pageSize + 1;
	const endItem = Math.min((pageIndex + 1) * pageSize, totalItems);

	return (
		<div className='flex w-full items-center justify-center gap-4 bg-transparent px-4'>
			<span className='text-[14px]'>
				Hiển thị {startItem}-{endItem} trên {totalItems} mục
			</span>
			<Button
				variant='ghost'
				size='icon'
				className='size-8 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
				onClick={() => table.previousPage()}
				disabled={!table.getCanPreviousPage()}
			>
				<ChevronLeft />
			</Button>
			<div className='flex items-center gap-2'>
				{/* displlay page numbers button */}
				{Array.from({ length: table.getPageCount() }, (_, index) => (
					<Button
						key={index}
						variant='ghost'
						size='icon'
						className={cn(
							'h-8 w-8 rounded-sm bg-white shadow-none hover:bg-[#e3e4e7] hover:shadow-none',
							index === table.getState().pagination.pageIndex &&
								'border-primary text-primary border font-semibold',
						)}
						onClick={() => table.setPageIndex(index)}
					>
						{index + 1}
					</Button>
				))}
			</div>

			<Button
				variant='ghost'
				size='icon'
				className='size-8 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
				onClick={() => table.nextPage()}
				disabled={!table.getCanNextPage()}
			>
				<ChevronRight />
			</Button>
			<Select
				value={`${table.getState().pagination.pageSize}`}
				onValueChange={(value) => {
					table.setPageSize(Number(value));
				}}
			>
				<SelectTrigger className='hover:border-primary h-8 border shadow-none hover:shadow-none'>
					<SelectValue placeholder={table.getState().pagination.pageSize} />
				</SelectTrigger>
				<SelectContent side='top' className='mb-2'>
					{[10, 20, 50, 100].map((pageSize) => (
						<SelectItem key={pageSize} value={`${pageSize}`}>
							{`${pageSize} / trang`}
						</SelectItem>
					))}
				</SelectContent>
			</Select>
		</div>
	);
}
