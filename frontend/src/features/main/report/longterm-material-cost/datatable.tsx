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
import { Production } from '@/features/main/cost/producttion/production/columns';
import { FixedColumnDataTable } from '@/features/main/cost/producttion/production/longterm-material-cost/datatable';
import {
	LongtermMaterialDetailItem,
	LongTermTrackingResponse,
} from '@/features/main/cost/producttion/production/longterm-material-cost/types';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';

const UNGROUPED_PROCESS_GROUP = '__ungrouped';

const isSameMonthAndYear = (
	dateValue: string | undefined,
	targetYear: string,
	targetMonth: string,
) => {
	if (!dateValue) return false;

	const rawDate = dateValue.split('T')[0] ?? dateValue;
	const [yearPart, monthPart] = rawDate.split('-');

	if (yearPart && monthPart) {
		return yearPart === targetYear && monthPart === targetMonth;
	}

	const parsedDate = new Date(dateValue);
	if (Number.isNaN(parsedDate.getTime())) {
		return false;
	}

	return (
		String(parsedDate.getFullYear()) === targetYear &&
		String(parsedDate.getMonth() + 1).padStart(2, '0') === targetMonth
	);
};

interface LongtermMaterialCostDataTableProps {
	enableSearch?: boolean;
	enablePagination?: boolean;
	pageSize?: number;
	largeText?: boolean;
}

export function LongtermMaterialCostDataTable({
	enableSearch = true,
	enablePagination = true,
	pageSize = 10,
	largeText = true,
}: LongtermMaterialCostDataTableProps) {
	const now = new Date();
	const currentYear = now.getFullYear();
	const [month, setMonth] = useState(
		String(now.getMonth() + 1).padStart(2, '0'),
	);
	const [year, setYear] = useState(String(currentYear));
	const [selectedProcessGroup, setSelectedProcessGroup] = useState('all');
	const [items, setItems] = useState<LongtermMaterialDetailItem[]>([]);
	const [activeAcceptanceReportId, setActiveAcceptanceReportId] = useState<
		string | null
	>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [searchQuery, setSearchQuery] = useState('');
	const [currentPage, setCurrentPage] = useState(1);
	const [currentPageSize, setCurrentPageSize] = useState(pageSize);

	const monthOptions = useMemo(
		() =>
			Array.from({ length: 12 }, (_, index) => {
				const value = String(index + 1).padStart(2, '0');
				return {
					value,
					label: `Tháng ${value}`,
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

	const processGroupOptions = useMemo(() => {
		const groups = new Map<string, string>();

		items.forEach((item) => {
			const value = item.processGroupId || UNGROUPED_PROCESS_GROUP;
			const label = item.processGroupCode
				? `${item.processGroupCode} - ${item.processGroupName || ''}`
				: item.processGroupName || 'Không có nhóm công đoạn';

			if (!groups.has(value)) {
				groups.set(value, label.trim());
			}
		});

		const dynamicGroups = Array.from(groups.entries())
			.map(([value, label]) => ({ value, label }))
			.sort((a, b) => a.label.localeCompare(b.label, 'vi'));

		return [{ value: 'all', label: 'Tất cả nhóm công đoạn' }, ...dynamicGroups];
	}, [items]);

	useEffect(() => {
		if (
			!processGroupOptions.some(
				(option) => option.value === selectedProcessGroup,
			)
		) {
			setSelectedProcessGroup('all');
		}
	}, [processGroupOptions, selectedProcessGroup]);

	const processGroupFilteredItems = useMemo(() => {
		if (selectedProcessGroup === 'all') {
			return items;
		}

		if (selectedProcessGroup === UNGROUPED_PROCESS_GROUP) {
			return items.filter((item) => !item.processGroupId);
		}

		return items.filter((item) => item.processGroupId === selectedProcessGroup);
	}, [items, selectedProcessGroup]);

	const filteredItems = useMemo(() => {
		if (!enableSearch || !searchQuery.trim()) {
			return processGroupFilteredItems;
		}

		const query = searchQuery.toLowerCase();

		return processGroupFilteredItems.filter((item) => {
			const keywords = [
				item.partCode,
				item.partName,
				item.unitOfMeasureName,
				item.processGroupCode,
				item.processGroupName,
			]
				.filter(Boolean)
				.join(' ')
				.toLowerCase();

			return keywords.includes(query);
		});
	}, [enableSearch, processGroupFilteredItems, searchQuery]);

	const totalPages = useMemo(() => {
		if (!enablePagination) return 1;
		return Math.max(1, Math.ceil(filteredItems.length / currentPageSize));
	}, [enablePagination, filteredItems.length, currentPageSize]);

	const page = useMemo(() => {
		if (!enablePagination) return 1;
		return Math.min(currentPage, totalPages);
	}, [enablePagination, currentPage, totalPages]);

	const pagedItems = useMemo(() => {
		if (!enablePagination) return filteredItems;
		const startIndex = (page - 1) * currentPageSize;
		return filteredItems.slice(startIndex, startIndex + currentPageSize);
	}, [enablePagination, filteredItems, page, currentPageSize]);

	const startItem = useMemo(() => {
		if (filteredItems.length === 0) return 0;
		if (!enablePagination) return 1;
		return (page - 1) * currentPageSize + 1;
	}, [enablePagination, filteredItems.length, page, currentPageSize]);

	const endItem = useMemo(() => {
		if (filteredItems.length === 0) return 0;
		if (!enablePagination) return filteredItems.length;
		return Math.min(page * currentPageSize, filteredItems.length);
	}, [enablePagination, filteredItems.length, page, currentPageSize]);

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

	const fetchLongtermMaterialCost = useCallback(async () => {
		setIsLoading(true);
		setError(null);
		setActiveAcceptanceReportId(null);
		setCurrentPage(1);

		try {
			const outputResponse = await api.pagging<Production>(
				API.PRODUCTION.PRODUCTION_OUTPUT.LIST,
				{
					ignorePagination: true,
				},
			);

			const allOutputs = outputResponse.result.data ?? [];
			const outputsByPeriod = allOutputs.filter((output) =>
				isSameMonthAndYear(output.startMonth, year, month),
			);

			const outputsWithReport = outputsByPeriod
				.filter((output) => !!output.acceptanceReportId)
				.sort(
					(a, b) =>
						new Date(b.startMonth).getTime() - new Date(a.startMonth).getTime(),
				);

			const reportIds = Array.from(
				new Set(outputsWithReport.map((output) => output.acceptanceReportId)),
			).filter((reportId): reportId is string => Boolean(reportId));

			if (reportIds.length === 0) {
				setItems([]);
				return;
			}

			setActiveAcceptanceReportId(reportIds[0]);

			const detailResponses = await Promise.all(
				reportIds.map((reportId) =>
					api.get<LongTermTrackingResponse>(
						API.PRODUCTION.ACCEPTANCE_REPORT.LONG_TERM_TRACKING_LIST(reportId),
					),
				),
			);

			const mergedItems = detailResponses.flatMap((response) => {
				const detail = response.result;
				if (!detail) {
					return [] as LongtermMaterialDetailItem[];
				}

				if (detail.processGroups && detail.processGroups.length > 0) {
					return detail.processGroups.flatMap((group) =>
						(group.items || []).map((item) => ({
							...item,
							processGroupId: item.processGroupId || group.processGroupId,
							processGroupCode: item.processGroupCode || group.processGroupCode,
							processGroupName: item.processGroupName || group.processGroupName,
						})),
					);
				}

				return detail.items || [];
			});

			setItems(mergedItems);
		} catch (err) {
			setItems([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu hạch toán chi phí dài kỳ',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year]);

	useEffect(() => {
		fetchLongtermMaterialCost();
	}, [fetchLongtermMaterialCost]);

	const handleExport = async () => {
		if (!activeAcceptanceReportId) return;

		setIsExporting(true);
		try {
			await api.export(
				API.PRODUCTION.ACCEPTANCE_REPORT.DOWNLOAD(activeAcceptanceReportId),
			);
		} catch (err) {
			console.error('Failed to export long-term material cost:', err);
		} finally {
			setIsExporting(false);
		}
	};

	const showPagination = enablePagination && filteredItems.length > 0;

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Tháng</p>
						<Select value={month} onValueChange={setMonth}>
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
						<Select value={year} onValueChange={setYear}>
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
						>
							<SelectTrigger className='w-[260px] bg-white'>
								<SelectValue placeholder='Chọn nhóm công đoạn' />
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
									placeholder='Tìm theo mã, tên phụ tùng, nhóm công đoạn...'
									className='h-10 w-[340px] bg-white pl-8 text-base'
								/>
							</div>
						</div>
					)}
				</div>

				<Button
					variant='outline'
					size='sm'
					disabled={!activeAcceptanceReportId || isLoading || isExporting}
					onClick={handleExport}
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
			) : isLoading ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<>
					<FixedColumnDataTable items={pagedItems} compact={!largeText} />

					{showPagination && (
						<div className='absolute top-full right-0 left-0 mt-3 flex w-full items-center justify-center gap-4 bg-transparent px-4'>
							<span className='text-[14px]'>
								Hiển thị {startItem}-{endItem} trên {filteredItems.length} mục
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
								<SelectTrigger className='hover:border-primary h-8 min-w-28 border shadow-none hover:shadow-none'>
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
