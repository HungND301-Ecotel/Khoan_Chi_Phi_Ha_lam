'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { LUMP_SUM_FINAL_SETTLEMENT_COLUMNS } from '@/features/main/cost/lump-sum-final-settlement/columns';
import { LumpSumDataTable } from '@/features/main/cost/lump-sum-final-settlement/components/datatable';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementListRequest,
	ProcessGroup,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';

const ALL_PROCESS_GROUP = '__all_process_group__';

const escapeCsvCell = (value: string | number | null | undefined) => {
	const cell = value == null ? '' : String(value);
	if (cell.includes(',') || cell.includes('"') || cell.includes('\n')) {
		return `"${cell.replace(/"/g, '""')}"`;
	}

	return cell;
};

interface LumpSumFinalSettlementReportTableProps {
	enableSearch?: boolean;
	enablePagination?: boolean;
	pageSize?: number;
}

export function LumpSumFinalSettlementReportTable({
	enableSearch = true,
	enablePagination = true,
	pageSize = 10,
}: LumpSumFinalSettlementReportTableProps) {
	const now = new Date();
	const currentYear = now.getFullYear();

	const [month, setMonth] = useState(String(now.getMonth() + 1));
	const [year, setYear] = useState(String(currentYear));
	const [selectedProcessGroup, setSelectedProcessGroup] =
		useState(ALL_PROCESS_GROUP);
	const [processGroupOptions, setProcessGroupOptions] = useState<
		{ value: string; label: string }[]
	>([{ value: ALL_PROCESS_GROUP, label: 'Tất cả nhóm công đoạn' }]);
	const [rows, setRows] = useState<LumpSumFinalSettlement[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [isLoadingProcessGroups, setIsLoadingProcessGroups] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [searchQuery, setSearchQuery] = useState('');
	const [currentPage, setCurrentPage] = useState(1);
	const [currentPageSize, setCurrentPageSize] = useState(pageSize);

	const monthOptions = useMemo(
		() =>
			Array.from({ length: 12 }, (_, index) => {
				const value = String(index + 1);
				return {
					value,
					label: `Tháng ${value.padStart(2, '0')}`,
				};
			}),
		[],
	);

	const yearOptions = useMemo(() => {
		return Array.from({ length: 101 }, (_, index) => {
			const optionYear = String(currentYear - index);
			return {
				value: optionYear,
				label: optionYear,
			};
		});
	}, [currentYear]);

	useEffect(() => {
		setCurrentPageSize(pageSize);
	}, [pageSize]);

	useEffect(() => {
		const fetchProcessGroups = async () => {
			setIsLoadingProcessGroups(true);
			try {
				const response = await api.pagging<ProcessGroup>(
					API.CATALOG.PROCESS.GROUP.LIST,
					{ ignorePagination: true },
				);

				const options = [
					{ value: ALL_PROCESS_GROUP, label: 'Tất cả nhóm công đoạn' },
					...(response.result.data ?? []).map((item) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				];

				setProcessGroupOptions(options);
			} catch (err) {
				console.error('Failed to fetch process groups:', err);
			} finally {
				setIsLoadingProcessGroups(false);
			}
		};

		fetchProcessGroups();
	}, []);

	const fetchLumpSumFinalSettlement = useCallback(async () => {
		setIsLoading(true);
		setError(null);

		try {
			const payload: LumpSumFinalSettlementListRequest = {
				month,
				year,
				processGroupId:
					selectedProcessGroup === ALL_PROCESS_GROUP
						? ''
						: selectedProcessGroup,
			};

			const response = await api.post<
				LumpSumFinalSettlement[],
				LumpSumFinalSettlementListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload);

			setRows(response.result ?? []);
		} catch (err) {
			setRows([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu bảng quyết toán',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year, selectedProcessGroup]);

	useEffect(() => {
		fetchLumpSumFinalSettlement();
	}, [fetchLumpSumFinalSettlement]);

	const filteredRows = useMemo(() => {
		if (!enableSearch || !searchQuery.trim()) {
			return rows;
		}

		const query = searchQuery.toLowerCase();

		return rows.filter((row) => {
			const keywords = [row.productCode, row.productName, row.unitOfMeasureName]
				.filter(Boolean)
				.join(' ')
				.toLowerCase();

			return keywords.includes(query);
		});
	}, [enableSearch, rows, searchQuery]);

	const totalPages = useMemo(() => {
		if (!enablePagination) return 1;
		return Math.max(1, Math.ceil(filteredRows.length / currentPageSize));
	}, [enablePagination, filteredRows.length, currentPageSize]);

	const page = useMemo(() => {
		if (!enablePagination) return 1;
		return Math.min(currentPage, totalPages);
	}, [enablePagination, currentPage, totalPages]);

	const pagedRows = useMemo(() => {
		if (!enablePagination) return filteredRows;
		const startIndex = (page - 1) * currentPageSize;
		return filteredRows.slice(startIndex, startIndex + currentPageSize);
	}, [enablePagination, filteredRows, page, currentPageSize]);

	const startItem = useMemo(() => {
		if (filteredRows.length === 0) return 0;
		if (!enablePagination) return 1;
		return (page - 1) * currentPageSize + 1;
	}, [enablePagination, filteredRows.length, page, currentPageSize]);

	const endItem = useMemo(() => {
		if (filteredRows.length === 0) return 0;
		if (!enablePagination) return filteredRows.length;
		return Math.min(page * currentPageSize, filteredRows.length);
	}, [enablePagination, filteredRows.length, page, currentPageSize]);

	const pageNumbers = useMemo(() => {
		if (!enablePagination) return [] as number[];
		if (totalPages <= 5)
			return Array.from({ length: totalPages }, (_, index) => index + 1);
		if (page <= 3) return [1, 2, 3, 4, 5];
		if (page >= totalPages - 2) {
			return [
				totalPages - 4,
				totalPages - 3,
				totalPages - 2,
				totalPages - 1,
				totalPages,
			];
		}

		return [page - 2, page - 1, page, page + 1, page + 2];
	}, [enablePagination, page, totalPages]);

	const handleExport = async () => {
		if (filteredRows.length === 0 || isExporting) {
			return;
		}

		setIsExporting(true);

		try {
			const headers = [
				'STT',
				'Mã sản phẩm',
				'Sản phẩm',
				'ĐVT',
				'KH',
				'TH',
				'Vật liệu - Đơn giá',
				'Vật liệu - Thành tiền',
				'SCTX - Đơn giá',
				'SCTX - Thành tiền',
				'Điện năng - Đơn giá',
				'Điện năng - Thành tiền',
				'Tổng thành tiền',
			];

			const records = filteredRows.map((row, index) => [
				index + 1,
				row.productCode ?? '',
				row.productName ?? '',
				row.unitOfMeasureName ?? '',
				row.plannedQuantity ?? 0,
				row.actualQuantity ?? 0,
				row.materials?.unitPrice ?? 0,
				row.materials?.totalAmount ?? 0,
				row.maintains?.unitPrice ?? 0,
				row.maintains?.totalAmount ?? 0,
				row.electricities?.unitPrice ?? 0,
				row.electricities?.totalAmount ?? 0,
				row.totalAmount ?? 0,
			]);

			const csvText = [headers, ...records]
				.map((record) => record.map(escapeCsvCell).join(','))
				.join('\n');

			const blob = new Blob([`\uFEFF${csvText}`], {
				type: 'text/csv;charset=utf-8;',
			});
			const downloadUrl = window.URL.createObjectURL(blob);
			const link = document.createElement('a');
			const monthLabel = month.padStart(2, '0');

			link.href = downloadUrl;
			link.download = `bang-quyet-toan-${monthLabel}-${year}.csv`;
			document.body.appendChild(link);
			link.click();
			window.URL.revokeObjectURL(downloadUrl);
			document.body.removeChild(link);
		} catch (err) {
			console.error('Failed to export lump-sum final settlement:', err);
		} finally {
			setIsExporting(false);
		}
	};

	const showPagination = enablePagination && filteredRows.length > 0;

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Tháng</p>
						<Select
							value={month}
							onValueChange={(value) => {
								setMonth(value);
								setCurrentPage(1);
							}}
						>
							<SelectTrigger className='w-[150px] bg-white'>
								<SelectValue placeholder='Chọn tháng' />
							</SelectTrigger>
							<SelectContent>
								{monthOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Năm</p>
						<Select
							value={year}
							onValueChange={(value) => {
								setYear(value);
								setCurrentPage(1);
							}}
						>
							<SelectTrigger className='w-[120px] bg-white'>
								<SelectValue placeholder='Chọn năm' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{yearOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Nhóm công đoạn</p>
						<Select
							value={selectedProcessGroup}
							onValueChange={(value) => {
								setSelectedProcessGroup(value);
								setCurrentPage(1);
							}}
							disabled={isLoadingProcessGroups}
						>
							<SelectTrigger className='w-[320px] bg-white'>
								<SelectValue placeholder='Chọn nhóm công đoạn sản xuất' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{processGroupOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					{enableSearch && (
						<div className='space-y-1'>
							<p className='text-sm font-medium'>Tìm kiếm</p>
							<div className='relative'>
								<SearchIcon
									style={{ fontSize: 18 }}
									className='text-muted-foreground absolute top-1/2 left-2.5 -translate-y-1/2'
								/>
								<Input
									value={searchQuery}
									onChange={(event) => {
										setSearchQuery(event.target.value);
										setCurrentPage(1);
									}}
									placeholder='Tìm theo mã, tên sản phẩm...'
									className='h-10 w-[320px] bg-white pl-8 text-base'
								/>
							</div>
						</div>
					)}
				</div>

				<Button
					variant='outline'
					size='sm'
					onClick={handleExport}
					disabled={filteredRows.length === 0 || isLoading || isExporting}
					className='h-10 gap-1.5'
				>
					{isExporting ? (
						<Spinner />
					) : (
						<>
							<DownloadIcon style={{ fontSize: 18 }} />
							<span>Xuất file</span>
						</>
					)}
				</Button>
			</div>

			{error ? (
				<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
					<div className='text-muted-foreground text-center'>
						<p className='text-lg font-medium'>Lỗi tải dữ liệu</p>
						<p className='text-sm'>{error}</p>
					</div>
				</div>
			) : isLoading && rows.length === 0 ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<>
					<div className='min-h-0 overflow-auto rounded-t-md shadow'>
						<LumpSumDataTable
							columns={LUMP_SUM_FINAL_SETTLEMENT_COLUMNS}
							data={pagedRows}
							isLoading={isLoading}
							className='border-border rounded-t-md border shadow-none [&_table]:text-sm'
						/>
					</div>

					{showPagination && (
						<div className='flex w-full items-center justify-center gap-4 px-4 pt-1 pb-6'>
							<span className='text-[14px]'>
								Hiển thị {startItem}-{endItem} trên {filteredRows.length} mục
							</span>

							<Button
								variant='ghost'
								size='icon'
								className='size-8 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
								onClick={() =>
									setCurrentPage((value) => Math.max(1, value - 1))
								}
								disabled={page === 1}
							>
								<ChevronLeft />
							</Button>

							<div className='flex items-center gap-2'>
								{pageNumbers.map((pageNumber) => (
									<Button
										key={pageNumber}
										variant='ghost'
										size='icon'
										className={cn(
											'h-8 w-8 rounded-sm bg-white shadow-none hover:bg-[#e3e4e7] hover:shadow-none',
											pageNumber === page &&
												'border-primary text-primary border font-semibold',
										)}
										onClick={() => setCurrentPage(pageNumber)}
									>
										{pageNumber}
									</Button>
								))}
							</div>

							<Button
								variant='ghost'
								size='icon'
								className='size-8 bg-transparent shadow-none hover:bg-[#e3e4e7] hover:shadow-none disabled:text-[#b5b5b7]'
								onClick={() =>
									setCurrentPage((value) => Math.min(totalPages, value + 1))
								}
								disabled={page === totalPages}
							>
								<ChevronRight />
							</Button>

							<Select
								value={`${currentPageSize}`}
								onValueChange={(value) => {
									setCurrentPageSize(Number(value));
									setCurrentPage(1);
								}}
							>
								<SelectTrigger className='hover:border-primary h-8 min-w-28 border bg-white shadow-none hover:shadow-none'>
									<SelectValue placeholder={currentPageSize} />
								</SelectTrigger>
								<SelectContent side='top' className='mb-2'>
									{[10, 20, 50, 100].map((size) => (
										<SelectItem key={size} value={`${size}`}>
											{`${size} / trang`}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
					)}
				</>
			)}
		</div>
	);
}
