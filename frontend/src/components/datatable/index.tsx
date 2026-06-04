import { ActionDialog } from '@/components/datatable/actions';
import { DataTableEditDialog } from '@/components/datatable/edit';
import { DataTableEmpty } from '@/components/datatable/empty';
import { Filter } from '@/components/datatable/filter';
import { UseDataTable, useDataTable } from '@/components/datatable/hook';
import { DataTableLoading } from '@/components/datatable/loading';
import { DataTablePagination } from '@/components/datatable/pagination';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	DialogClose,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from '@/components/ui/tooltip';
import { Spinner } from '@/components/ui/spinner';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { PaggingRequest } from '@/lib/api';
import { cn } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import ArrowDropDownIcon from '@mui/icons-material/ArrowDropDown';
import ArrowDropUpIcon from '@mui/icons-material/ArrowDropUp';
import CreateIcon from '@mui/icons-material/Create';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import DeleteIcon from '@mui/icons-material/Delete';
import DownloadIcon from '@mui/icons-material/Download';
import EmailIcon from '@mui/icons-material/Email';
import PrintIcon from '@mui/icons-material/Print';
import UploadIcon from '@mui/icons-material/Upload';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { type ColumnDef, flexRender } from '@tanstack/react-table';
import { Fragment, JSX, useEffect, useRef, useState } from 'react';
import { DataTableImport } from './import';

const shadow = cn(
	'hover:shadow-[0px_2px_4px_-1px_rgba(0,0,0,0.2),0px_4px_5px_0px_rgba(0,0,0,0.14),0px_1px_10px_0px_rgba(0,0,0,0.12)] shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]',
);

export type ActionDialogProps<TData> = {
	data: UseDataTable<TData>;
	row?: TData;
};

type DataTableProps<TData> = {
	columns: ColumnDef<TData>[];
	url?: string;
	query?: PaggingRequest;
	items?: TData[];
	transformData?: (rows: TData[]) => TData[];
	getRowId?: (row: TData, index: number) => string;
	filters?: { key: keyof TData; label: string }[];
	onExpand?: (props: ActionDialogProps<TData>) => JSX.Element;
	onCreate?: (props: ActionDialogProps<TData>) => JSX.Element;
	onDuplicate?: (props: ActionDialogProps<TData>) => JSX.Element;
	onUpdate?: (props: ActionDialogProps<TData>) => JSX.Element;
	onRowImport?: (props: ActionDialogProps<TData>) => JSX.Element;
	onRowExport?: (props: ActionDialogProps<TData>) => Promise<void> | void;
	onDelete?: (props: ActionDialogProps<TData>) => Promise<void> | void;
	onExport?: (props: ActionDialogProps<TData>) => Promise<void> | void;
	onImport?: (file: File, data?: UseDataTable<TData>) => Promise<void> | void;
	onSelectedRowsChange?: (rows: TData[]) => void;
	selectAllPageRows?: boolean;
	deleteCountOverride?: number;
	deleteDisabledOverride?: boolean;
	importCrumb?: string;
	hasActions?: boolean;
	showCreateAction?: boolean;
	showDeleteAction?: boolean;
	showFilterAction?: boolean;
	showUtilityActions?: boolean;
	hasPagination?: boolean;
	hasSort?: boolean;
	hasIndex?: boolean;
	compact?: boolean;
};

export function DataTable<TData>({
	columns,
	url,
	query,
	items,
	transformData,
	getRowId,
	filters,
	onExpand,
	onCreate,
	onDuplicate,
	onUpdate,
	onRowImport,
	onRowExport,
	onDelete,
	onExport,
	onImport,
	onSelectedRowsChange,
	selectAllPageRows,
	deleteCountOverride,
	deleteDisabledOverride,
	importCrumb,
	hasActions = true,
	showCreateAction = true,
	showDeleteAction = true,
	showFilterAction = true,
	showUtilityActions = true,
	hasPagination = true,
	hasIndex = true,
	hasSort = true,
	compact = false,
}: DataTableProps<TData>) {
	const [exportLoading, setExportLoading] = useState(false);
	const datatable = useDataTable<TData>(
		columns,
		!!onExpand,
		hasPagination,
		hasSort,
		url,
		query,
		items,
		transformData,
		getRowId,
	);
	const { table, loading } = datatable;

	const selected = table
		.getFilteredSelectedRowModel()
		.rows.map((row) => row.original);
	const selectedRowIdsKey = table
		.getFilteredSelectedRowModel()
		.rows.map((row) => row.id)
		.join('|');
	const lastSelectedRowIdsKeyRef = useRef('');
	const selectedCount = deleteCountOverride ?? selected.length;
	const deleteDisabled = deleteDisabledOverride ?? !selected.length;
	const rowSelection = table.getState().rowSelection;

	useEffect(() => {
		if (!onSelectedRowsChange) return;
		if (lastSelectedRowIdsKeyRef.current === selectedRowIdsKey) return;
		lastSelectedRowIdsKeyRef.current = selectedRowIdsKey;
		onSelectedRowsChange(selected);
	}, [onSelectedRowsChange, rowSelection, selectedRowIdsKey, selected]);

	useEffect(() => {
		if (selectAllPageRows === undefined) return;
		if (
			selectAllPageRows &&
			table.getIsAllPageRowsSelected() &&
			!table.getIsSomePageRowsSelected()
		) {
			return;
		}
		if (
			!selectAllPageRows &&
			!table.getIsAllPageRowsSelected() &&
			!table.getIsSomePageRowsSelected()
		) {
			return;
		}
		table.toggleAllPageRowsSelected(selectAllPageRows);
	}, [selectAllPageRows, table, datatable.refreshVersion]);

	let columnCount = table.getAllColumns().length;

	if (onExpand) ++columnCount;
	if (onRowImport) ++columnCount;
	if (onRowExport) ++columnCount;
	if (onUpdate) ++columnCount;
	if (onDuplicate) ++columnCount;
	if (onDelete) ++columnCount;
	if (hasIndex) ++columnCount;

	// Check if any column has size defined
	const hasColumnSize = columns.some((col) => col.size !== undefined);
	const indexColumnStyle = hasColumnSize ? { width: '64px' } : undefined;
	const shouldRenderActionBar =
		hasActions &&
		(showCreateAction ||
			showDeleteAction ||
			showFilterAction ||
			showUtilityActions);

	return (
		<div className='flex flex-col gap-4'>
			{shouldRenderActionBar && (
				<div className='flex items-center justify-between gap-4 md:gap-8'>
					<div className='flex gap-4'>
						{showCreateAction && (
							<DialogProvider>
								<DataTableEditDialog
									type='Tạo mới'
									trigger={
										<Button
											variant={'warning'}
											className={cn(
												shadow,
												!onCreate && 'cursor-not-allowed opacity-40',
											)}
											disabled={!onCreate}
										>
											<span className='hidden lg:block'>Tạo mới</span>
											<AddIcon fontSize='small' />
										</Button>
									}
									children={onCreate?.({ data: datatable })}
								/>
							</DialogProvider>
						)}
						{showDeleteAction && (
							<DialogProvider>
								<ActionDialog
									className='min-h-auto sm:max-w-md'
									trigger={
										<Button
											variant={'destructive'}
											disabled={deleteDisabled}
											className={shadow}
										>
											<span className='hidden lg:block'>
												Xoá ({selectedCount})
											</span>
											<DeleteIcon fontSize='small' />
										</Button>
									}
								>
									<DialogHeader>
										<DialogTitle className='text-center uppercase'>
											Xác nhận xóa
										</DialogTitle>
										<DialogDescription className='text-center'>
											Bạn có chắc chắn muốn xóa {selectedCount} mục không?
										</DialogDescription>
									</DialogHeader>
									<DialogFooter className='flex w-full items-center sm:justify-center'>
										<DialogClose asChild>
											<Button variant={'secondary'} className='w-24'>
												Huỷ
											</Button>
										</DialogClose>
										<DialogClose asChild>
											<Button
												variant={'destructive'}
												onClick={() => onDelete?.({ data: datatable })}
												className='w-24'
											>
												Xoá
											</Button>
										</DialogClose>
									</DialogFooter>
								</ActionDialog>
							</DialogProvider>
						)}
					</div>

					{showFilterAction && filters && filters.length > 0 && (
						<Filter table={table} filters={filters} />
					)}

					{showUtilityActions && (
						<div className='flex gap-4'>
							{onImport && (
								<DialogProvider>
									<DataTableEditDialog
										type='Tải lên'
										trigger={
											<Button
												variant={'ghost'}
												className={cn(shadow, 'min-w-24')}
											>
												<UploadIcon fontSize='small' />
												<span className='hidden xl:block'>Tải lên</span>
											</Button>
										}
										children={
											<DataTableImport data={datatable} onImport={onImport} />
										}
									/>
								</DialogProvider>
							)}
							<Button
								variant={'ghost'}
								className={cn(shadow, 'min-w-24')}
								disabled={exportLoading}
								onClick={async () => {
									setExportLoading(true);
									try {
										await onExport?.({ data: datatable });
									} finally {
										setExportLoading(false);
									}
								}}
							>
								{exportLoading ? (
									<Spinner />
								) : (
									<>
										<DownloadIcon fontSize='small' />
										<span className='hidden xl:block'>Xuất file</span>
									</>
								)}
							</Button>
							<Button variant={'ghost'} className={shadow}>
								<PrintIcon fontSize='small' />
								<span className='hidden xl:block'>In</span>
							</Button>
							<Button variant={'ghost'} className={shadow}>
								<EmailIcon fontSize='small' />
								<span className='hidden xl:block'>Gửi</span>
							</Button>
						</div>
					)}
				</div>
			)}

			<div className='overflow-hidden rounded-t-md border shadow'>
				<Table style={hasColumnSize ? { tableLayout: 'fixed' } : undefined}>
					<TableHeader
						className={cn('bg-[#fafafa]', !compact && 'h-14 text-base')}
					>
						{table.getHeaderGroups().map((headerGroup, headerGroupIndex) => {
							const isFirstHeaderGroup = headerGroupIndex === 0;
							const totalHeaderGroups = table.getHeaderGroups().length;

							return (
								<TableRow key={headerGroup.id}>
									{isFirstHeaderGroup && onDelete && (
										<TableHead className='px-2' rowSpan={totalHeaderGroups}>
											<Checkbox
												checked={
													table.getIsAllPageRowsSelected() ||
													(table.getIsSomePageRowsSelected() && 'indeterminate')
												}
												onCheckedChange={(value) =>
													table.toggleAllPageRowsSelected(!!value)
												}
												className='[&_.lucide-check]:text-white'
											/>
										</TableHead>
									)}

									{isFirstHeaderGroup && hasIndex && (
										<TableHead
											className='p-0'
											rowSpan={totalHeaderGroups}
											style={indexColumnStyle}
										>
											<div className='inline-flex h-6 w-full cursor-pointer flex-nowrap items-center justify-between gap-2 px-4'>
												{!onDelete && 'STT'}
											</div>
										</TableHead>
									)}

									{headerGroup.headers.map((header) => {
										const sorted = header.column.getIsSorted();

										const { sorting } = table.getState();
										const columnId = header.column.id;
										const isSorted = sorting.some(
											(item) => item.id === columnId,
										);

										let tooltipContent = 'Nhấn để sắp xếp Tăng dần';
										if (sorted === 'asc') {
											tooltipContent = 'Nhấn để sắp xếp Giảm dần';
										} else if (sorted === 'desc') {
											tooltipContent = 'Nhấn để HỦY sắp xếp';
										}

										// Check if this is a group header (has sub-headers/children columns)
										const isGroupHeader =
											header.subHeaders && header.subHeaders.length > 0;
										// Non-group headers in the first row should span all header rows
										const shouldSpanRows =
											!isGroupHeader &&
											!header.isPlaceholder &&
											isFirstHeaderGroup &&
											totalHeaderGroups > 1;

										return (
											<Tooltip key={header.id}>
												<TooltipTrigger asChild>
													<TableHead
														style={
															hasColumnSize && header.getSize() !== 150
																? { width: `${header.getSize()}px` }
																: undefined
														}
														className={cn(
															'p-0 hover:bg-[#f0f0f0]',
															isSorted && 'bg-[#f0f0f0]',
															header.isPlaceholder && 'pointer-events-none',
														)}
														colSpan={header.colSpan}
														rowSpan={shouldSpanRows ? totalHeaderGroups : 1}
													>
														{header.isPlaceholder ? null : (
															<div
																className={cn(
																	'inline-flex h-fit w-full cursor-pointer flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold',
																	isSorted && 'text-primary',
																	isGroupHeader && 'justify-center',
																)}
																onClick={
																	hasSort && !isGroupHeader
																		? header.column.getToggleSortingHandler()
																		: undefined
																}
															>
																{flexRender(
																	header.column.columnDef.header,
																	header.getContext(),
																)}
																{hasSort && !isGroupHeader && (
																	<div className='text flex flex-col items-center justify-center -space-y-3'>
																		<ArrowDropUpIcon
																			fontSize='small'
																			className={cn(
																				'text-black',
																				sorted === 'asc' && 'text-primary',
																			)}
																		/>
																		<ArrowDropDownIcon
																			fontSize='small'
																			className={cn(
																				'text-black',
																				sorted === 'desc' && 'text-primary',
																			)}
																		/>
																	</div>
																)}
															</div>
														)}
													</TableHead>
												</TooltipTrigger>
												{hasSort && !isGroupHeader && !header.isPlaceholder && (
													<TooltipContent>{tooltipContent}</TooltipContent>
												)}
											</Tooltip>
										);
									})}
									{isFirstHeaderGroup && onRowImport && (
										<TableHead
											className='px-4 text-center'
											rowSpan={totalHeaderGroups}
										>
											Nhập
										</TableHead>
									)}
									{isFirstHeaderGroup && onRowExport && (
										<TableHead
											className='px-4 text-center'
											rowSpan={totalHeaderGroups}
										>
											Xuất
										</TableHead>
									)}
									{isFirstHeaderGroup && onExpand && (
										<TableHead
											className='px-4 text-center'
											rowSpan={totalHeaderGroups}
										>
											Xem
										</TableHead>
									)}
									{isFirstHeaderGroup && onDuplicate && (
										<TableHead
											className='px-4 text-center'
											rowSpan={totalHeaderGroups}
										>
											Copy
										</TableHead>
									)}
									{isFirstHeaderGroup && onUpdate && (
										<TableHead
											className='px-4 text-center'
											rowSpan={totalHeaderGroups}
										>
											Sửa
										</TableHead>
									)}
								</TableRow>
							);
						})}
					</TableHeader>

					<TableBody className={cn(compact ? '' : 'text-base')}>
						{loading && !table.getRowModel().rows?.length ? (
							<DataTableLoading table={table} columns={columnCount} />
						) : table.getRowModel().rows?.length ? (
							table.getRowModel().rows.map((row, index) => (
								<Fragment key={row.id}>
									<TableRow
										data-state={row.getIsSelected() && 'selected'}
										className={cn(!compact && 'h-18')}
									>
										{onDelete && (
											<TableCell className='w-4 py-0'>
												<Checkbox
													checked={row.getIsSelected()}
													onCheckedChange={(value) =>
														row.toggleSelected(!!value)
													}
													aria-label='Select row'
													className='me-2 [&_.lucide-check]:text-white'
												/>
											</TableCell>
										)}

										{hasIndex && (
											<TableCell
												className='w-10 px-4 py-0 whitespace-nowrap'
												style={indexColumnStyle}
											>
												{index + 1}
											</TableCell>
										)}

										{row.getVisibleCells().map((cell) => {
											const columnId = cell.column.id;
											const { sorting } = table.getState();
											const isSorted = sorting.some(
												(item) => item.id === columnId,
											);

											return (
												<TableCell
													key={cell.id}
													style={
														hasColumnSize && cell.column.getSize() !== 150
															? { width: `${cell.column.getSize()}px` }
															: undefined
													}
													className={cn(
														'h-12 px-4 py-2',
														isSorted && 'bg-[#fafafa]',
													)}
												>
													{flexRender(
														cell.column.columnDef.cell,
														cell.getContext(),
													)}
												</TableCell>
											);
										})}

										{onRowImport && (
											<TableCell className='w-10 px-4 py-0'>
												<DialogProvider>
													<DataTableEditDialog
														type='Nhập dữ liệu'
														crumb={importCrumb}
														trigger={
															<Button
																variant={'ghost'}
																size={'icon-lg'}
																className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
															>
																<UploadIcon fontSize='medium' />
															</Button>
														}
														children={onRowImport?.({
															data: datatable,
															row: row.original,
														})}
													/>
												</DialogProvider>
											</TableCell>
										)}

										{onRowExport && (
											<TableCell className='w-10 px-4 py-0'>
												<Button
													variant={'ghost'}
													size={'icon-lg'}
													className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
													onClick={() =>
														onRowExport?.({
															data: datatable,
															row: row.original,
														})
													}
												>
													<DownloadIcon fontSize='medium' />
												</Button>
											</TableCell>
										)}

										{onExpand && (
											<TableCell className='w-10 px-4 py-0'>
												<Button
													variant={'ghost'}
													size={'icon-lg'}
													className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
													onClick={row.getToggleExpandedHandler()}
													disabled={!row.getCanExpand()}
												>
													{row.getIsExpanded() ? (
														<VisibilityOffIcon fontSize='medium' />
													) : (
														<VisibilityIcon fontSize='medium' />
													)}
												</Button>
											</TableCell>
										)}

										{onDuplicate && (
											<TableCell className='w-10 px-4 py-0'>
												<DialogProvider>
													<DataTableEditDialog
														type='Tạo mới'
														trigger={
															<Button
																variant={'ghost'}
																size={'icon-lg'}
																className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
															>
																<ContentCopyIcon fontSize='medium' />
															</Button>
														}
														children={onDuplicate?.({
															data: datatable,
															row: row.original,
														})}
													/>
												</DialogProvider>
											</TableCell>
										)}

										{onUpdate && (
											<TableCell className='w-10 px-4 py-0'>
												<DialogProvider>
													<DataTableEditDialog
														type='Chỉnh sửa'
														trigger={
															<Button
																variant={'ghost'}
																size={'icon-lg'}
																className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
															>
																<CreateIcon fontSize='medium' />
															</Button>
														}
														children={onUpdate?.({
															data: datatable,
															row: row.original,
														})}
													/>
												</DialogProvider>
											</TableCell>
										)}
									</TableRow>

									{row.getIsExpanded() && (
										<TableRow
											key={row.id + index}
											className='bg-white hover:bg-white'
										>
											<TableCell colSpan={columnCount} className='max-w-0 py-4'>
												<div className='w-full'>
													{onExpand?.({ data: datatable, row: row.original })}
												</div>
											</TableCell>
										</TableRow>
									)}
								</Fragment>
							))
						) : (
							<DataTableEmpty table={table} span={columnCount} />
						)}
					</TableBody>
				</Table>
			</div>

			{hasPagination && <DataTablePagination table={table} />}
		</div>
	);
}
