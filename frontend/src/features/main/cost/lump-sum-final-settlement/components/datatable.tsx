import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { FormNumberInput } from '@/components/form/form-number';
import { Input } from '@/components/ui/input';
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
	onAddCustomCost?: (row?: LumpSumFinalSettlement) => void;
	onEditCustomCost?: (row: LumpSumFinalSettlement) => void;
	onCancelCustomCost?: (row: LumpSumFinalSettlement) => void;
	onSaveCustomCost?: (row: LumpSumFinalSettlement) => void;
	onDeleteCustomCost?: (row: LumpSumFinalSettlement) => void;
	onCustomCostChange?: (
		row: LumpSumFinalSettlement,
		field:
			| 'customName'
			| 'actualQuantity'
			| 'materialUnitPrice'
			| 'maintainUnitPrice'
			| 'electricityUnitPrice',
		value: number | string,
	) => void;
}

export function LumpSumDataTable({
	columns,
	data,
	className,
	isLoading,
	onAddCustomCost,
	onEditCustomCost,
	onCancelCustomCost,
	onSaveCustomCost,
	onDeleteCustomCost,
	onCustomCostChange,
}: LumpSumDataTableProps) {
	const renderUnitPrice = (value: number | null | undefined) => {
		if (value == null || value === 0) {
			return '';
		}
		return formatNumber(Math.round(value));
	};

	const table = useReactTable({
		data,
		columns,
		getCoreRowModel: getCoreRowModel(),
	});

	// Calculate totals
	const totals = useMemo(() => {
		const sourceData = data.filter(
			(item) => !item.isProcessGroupRow && !item.excludeFromSummary,
		);

		return {
			plannedQuantity: sourceData.reduce(
				(sum, item) => sum + (item.plannedQuantity || 0),
				0,
			),
			actualQuantity: sourceData.reduce(
				(sum, item) => sum + (item.actualQuantity || 0),
				0,
			),
			materialsTotal: sourceData.reduce(
				(sum, item) => sum + (item.materials?.totalAmount || 0),
				0,
			),
			maintainsTotal: sourceData.reduce(
				(sum, item) => sum + (item.maintains?.totalAmount || 0),
				0,
			),
			electricitiesTotal: sourceData.reduce(
				(sum, item) => sum + (item.electricities?.totalAmount || 0),
				0,
			),
			totalAmount: sourceData.reduce(
				(sum, item) => sum + (item.totalAmount || 0),
				0,
			),
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
						table.getRowModel().rows.map((row) => {
							if (row.original.isCustomCostRow) {
								const r = row.original;
								const isEditing = !!r.isEditing;
								return (
									<TableRow
										key={row.id}
										className='border-b border-gray-200 bg-blue-50/30'
									>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{r.sttLabel || '-'}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											<div className='flex items-center justify-between gap-2'>
												{isEditing ? (
													<Input
														className='h-8 max-w-xs'
														value={r.productName ?? ''}
														onChange={(e) =>
															onCustomCostChange?.(
																r,
																'customName',
																e.target.value,
															)
														}
													/>
												) : (
													<span>{r.productName}</span>
												)}
												<div className='flex items-center gap-1'>
													{isEditing ? (
														<>
															<Button
																variant='default'
																size='sm'
																className='h-8 px-3'
																onClick={() => onSaveCustomCost?.(r)}
															>
																Lưu
															</Button>
															<Button
																variant='outline'
																size='sm'
																className='h-8 px-3'
																onClick={() => onCancelCustomCost?.(r)}
															>
																Hủy
															</Button>
														</>
													) : (
														<>
															<Button
																variant='outline'
																size='sm'
																className='h-8 px-3'
																onClick={() => onEditCustomCost?.(r)}
															>
																Sửa
															</Button>
															<Button
																variant='destructive'
																size='sm'
																className='h-8 px-3'
																onClick={() => onDeleteCustomCost?.(r)}
															>
																Xóa
															</Button>
														</>
													)}
												</div>
											</div>
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-center'>
											{r.unitOfMeasureName}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell className='border-r-2 border-gray-200 p-1 text-left'>
											{isEditing ? (
												<FormNumberInput
													className='h-8'
													value={r.actualQuantity ?? 0}
													onValueChange={(value) =>
														onCustomCostChange?.(
															r,
															'actualQuantity',
															value ?? 0,
														)
													}
												/>
											) : (
												formatNumber(r.actualQuantity ?? 0, {
													maximumFractionDigits: 3,
												})
											)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-1 text-left'>
											{isEditing ? (
												<FormNumberInput
													className='h-8'
													value={r.materials?.unitPrice ?? 0}
													onValueChange={(value) =>
														onCustomCostChange?.(
															r,
															'materialUnitPrice',
															value ?? 0,
														)
													}
												/>
											) : (
												formatNumber(Math.round(r.materials?.unitPrice ?? 0))
											)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(Math.round(r.materials?.totalAmount ?? 0))}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-1 text-left'>
											{isEditing ? (
												<FormNumberInput
													className='h-8'
													value={r.maintains?.unitPrice ?? 0}
													onValueChange={(value) =>
														onCustomCostChange?.(
															r,
															'maintainUnitPrice',
															value ?? 0,
														)
													}
												/>
											) : (
												formatNumber(Math.round(r.maintains?.unitPrice ?? 0))
											)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(Math.round(r.maintains?.totalAmount ?? 0))}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-1 text-left'>
											{isEditing ? (
												<FormNumberInput
													className='h-8'
													value={r.electricities?.unitPrice ?? 0}
													onValueChange={(value) =>
														onCustomCostChange?.(
															r,
															'electricityUnitPrice',
															value ?? 0,
														)
													}
												/>
											) : (
												formatNumber(
													Math.round(r.electricities?.unitPrice ?? 0),
												)
											)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(
												Math.round(r.electricities?.totalAmount ?? 0),
											)}
										</TableCell>
										<TableCell className='p-2 text-left font-medium'>
											{formatNumber(Math.round(r.totalAmount ?? 0))}
										</TableCell>
									</TableRow>
								);
							}

							if (row.original.isMergedValueRow) {
								return (
									<TableRow
										key={row.id}
										className='border-b border-gray-200 hover:bg-gray-50'
									>
										<TableCell
											className={cn(
												'border-r-2 border-gray-200 p-2 text-left',
												row.original.isBold && 'font-bold',
											)}
										>
											{row.original.sttLabel || row.index + 1}
										</TableCell>
										<TableCell
											className={cn(
												'border-r-2 border-gray-200 p-2 text-left',
												row.original.isBold && 'font-bold',
											)}
										>
											{row.original.productName}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{row.original.unitOfMeasureName}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell
											colSpan={5}
											className={cn(
												'border-r-2 border-gray-200 p-2 text-center',
												row.original.isBold && 'font-bold',
											)}
										>
											{formatNumber(row.original.mergedValue ?? 0, {
												maximumFractionDigits: 0,
											})}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell className='p-2 text-left'></TableCell>
									</TableRow>
								);
							}

							if (row.original.isTransferredDefaultRow) {
								const r = row.original;
								return (
									<TableRow
										key={row.id}
										className='border-b border-gray-200 hover:bg-gray-50'
									>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											<div className='flex items-center justify-between gap-2'>
												<Button
													variant='default'
													size='sm'
													className='w-full px-0'
													onClick={() => onAddCustomCost?.(r)}
												>
													+
												</Button>
											</div>
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											<span>{r.productName}</span>
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-center'>
											{r.unitOfMeasureName}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'></TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{renderUnitPrice(r.materials?.unitPrice)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(Math.round(r.materials?.totalAmount ?? 0))}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{renderUnitPrice(r.maintains?.unitPrice)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(Math.round(r.maintains?.totalAmount ?? 0))}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{renderUnitPrice(r.electricities?.unitPrice)}
										</TableCell>
										<TableCell className='border-r-2 border-gray-200 p-2 text-left'>
											{formatNumber(
												Math.round(r.electricities?.totalAmount ?? 0),
											)}
										</TableCell>
										<TableCell className='p-2 text-left'>
											{formatNumber(Math.round(r.totalAmount ?? 0))}
										</TableCell>
									</TableRow>
								);
							}

							return (
								<TableRow
									key={row.id}
									className='border-b border-gray-200 hover:bg-gray-50'
								>
									{row.getVisibleCells().map((cell) => (
										<TableCell
											key={cell.id}
											className={cn(
												'border-r-2 border-gray-200 p-2 text-left last:border-r-0',
												row.original.isBold && 'font-bold',
											)}
											style={{
												width: cell.column.getSize(),
											}}
										>
											{flexRender(
												cell.column.columnDef.cell,
												cell.getContext(),
											)}
										</TableCell>
									))}
								</TableRow>
							);
						})
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
