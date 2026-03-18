import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { LumpSumFinalSettlement } from '@/features/main/cost/lump-sum-final-settlement/types';
import { cn, formatNumber } from '@/lib/utils';
import {
	ColumnDef,
	flexRender,
	getCoreRowModel,
	useReactTable,
} from '@tanstack/react-table';
import { useMemo } from 'react';

interface LumpSumDataTableProps {
	columns: ColumnDef<LumpSumFinalSettlement>[];
	data: LumpSumFinalSettlement[];
	className?: string;
	isLoading?: boolean;
}

export function LumpSumDataTable({
	columns,
	data,
	className,
	isLoading,
}: LumpSumDataTableProps) {
	const table = useReactTable({
		data,
		columns,
		getCoreRowModel: getCoreRowModel(),
	});

	// Calculate totals
	const totals = useMemo(() => {
		return {
			plannedQuantity: data.reduce(
				(sum, item) => sum + (item.plannedQuantity || 0),
				0,
			),
			actualQuantity: data.reduce(
				(sum, item) => sum + (item.actualQuantity || 0),
				0,
			),
			materialsTotal: data.reduce(
				(sum, item) => sum + (item.materials?.totalAmount || 0),
				0,
			),
			maintainsTotal: data.reduce(
				(sum, item) => sum + (item.maintains?.totalAmount || 0),
				0,
			),
			electricitiesTotal: data.reduce(
				(sum, item) => sum + (item.electricities?.totalAmount || 0),
				0,
			),
			totalAmount: data.reduce((sum, item) => sum + (item.totalAmount || 0), 0),
		};
	}, [data]);

	const actualColumnCount = table.getAllLeafColumns().length;

	return (
		<div className={cn('rounded-md border-2 border-gray-300', className)}>
			<Table className='border-collapse'>
				<TableHeader>
					{table.getHeaderGroups().map((headerGroup) => (
						<TableRow
							key={headerGroup.id}
							className='border-b-2 border-gray-300 bg-gray-50'
						>
							{headerGroup.headers.map((header) => {
								const colSpan = header.colSpan;
								const isGroupHeader =
									header.subHeaders && header.subHeaders.length > 0;

								return (
									<TableHead
										key={header.id}
										colSpan={colSpan}
										className={cn(
											'border-r-2 border-gray-300 p-2 text-center font-bold last:border-r-0',
											isGroupHeader && 'bg-gray-100',
										)}
										style={{
											width: header.getSize(),
										}}
									>
										{header.isPlaceholder
											? null
											: flexRender(
													header.column.columnDef.header,
													header.getContext(),
												)}
									</TableHead>
								);
							})}
						</TableRow>
					))}
					{/* Summary Row */}
					<TableRow className='border-b-2 border-gray-400 bg-yellow-50'>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2 text-left font-bold'>
							{formatNumber(totals.plannedQuantity, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
						<TableCell className='border-r border-gray-300 p-2 text-left font-bold'>
							{formatNumber(totals.actualQuantity, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2 text-left font-bold'>
							{formatNumber(totals.materialsTotal, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2 text-left font-bold'>
							{formatNumber(totals.maintainsTotal, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
						<TableCell className='border-r border-gray-300 p-2'></TableCell>
						<TableCell className='border-r border-gray-300 p-2 text-left font-bold'>
							{formatNumber(totals.electricitiesTotal, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
						<TableCell className='border-r-0 p-2 text-left font-bold'>
							{formatNumber(totals.totalAmount, {
								maximumFractionDigits: 0,
							})}
						</TableCell>
					</TableRow>
				</TableHeader>
				<TableBody>
					{isLoading ? (
						<TableRow>
							<TableCell colSpan={actualColumnCount} className='h-30 p-0'>
								<div className='flex h-full w-full items-center justify-center gap-3'>
									<div className='h-6 w-6 animate-spin rounded-full border-4 border-gray-300 border-t-transparent' />
									<div>Đang tải...</div>
								</div>
							</TableCell>
						</TableRow>
					) : table.getRowModel().rows?.length ? (
						table.getRowModel().rows.map((row) => (
							<TableRow
								key={row.id}
								className='border-b border-gray-200 hover:bg-gray-50'
							>
								{row.getVisibleCells().map((cell) => (
									<TableCell
										key={cell.id}
										className='border-r-2 border-gray-200 p-2 text-left last:border-r-0'
										style={{
											width: cell.column.getSize(),
										}}
									>
										{flexRender(cell.column.columnDef.cell, cell.getContext())}
									</TableCell>
								))}
							</TableRow>
						))
					) : (
						<TableRow>
							<TableCell colSpan={actualColumnCount} className='h-30 p-0'>
								<div className='flex h-full w-full items-center justify-center'>
									Chưa có dữ liệu
								</div>
							</TableCell>
						</TableRow>
					)}
				</TableBody>
			</Table>
		</div>
	);
}
