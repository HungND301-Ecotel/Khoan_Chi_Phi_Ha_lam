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
import { ProductionOutputDto } from '@/features/main/cost/producttion/production/acceptance-report/api-types';
import { transformApiResponseToHierarchical } from '@/features/main/cost/producttion/production/acceptance-report/api-transformer';
import { AcceptanceReportDataTable as AcceptanceReportGrid } from '@/features/main/cost/producttion/production/acceptance-report/datatable';
import { HierarchicalRow } from '@/features/main/cost/producttion/production/acceptance-report/types';
import { flattenHierarchicalData } from '@/features/main/cost/producttion/production/acceptance-report/utils';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { useCallback, useEffect, useMemo, useState } from 'react';

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

interface AcceptanceReportDataTableProps {
	enableSearch?: boolean;
	largeText?: boolean;
}

export function AcceptanceReportDataTable({
	enableSearch = true,
	largeText = true,
}: AcceptanceReportDataTableProps) {
	const now = new Date();
	const currentYear = now.getFullYear();
	const [month, setMonth] = useState(
		String(now.getMonth() + 1).padStart(2, '0'),
	);
	const [year, setYear] = useState(String(currentYear));
	const [rows, setRows] = useState<HierarchicalRow[]>([]);
	const [activeAcceptanceReportId, setActiveAcceptanceReportId] = useState<
		string | null
	>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [searchQuery, setSearchQuery] = useState('');

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

	const filteredRows = useMemo(() => {
		if (!enableSearch || !searchQuery.trim()) {
			return rows;
		}

		const query = searchQuery.toLowerCase();

		return rows.filter((row) => {
			const keywords = [row.label, row.itemCode, row.itemName, row.unit]
				.filter(Boolean)
				.join(' ')
				.toLowerCase();

			return keywords.includes(query);
		});
	}, [enableSearch, rows, searchQuery]);

	const fetchAcceptanceReport = useCallback(async () => {
		setIsLoading(true);
		setError(null);
		setActiveAcceptanceReportId(null);

		try {
			const outputResponse = await api.pagging<Production>(
				API.PRODUCTION.PRODUCTION_OUTPUT.LIST,
				{
					ignorePagination: true,
				},
			);

			const allOutputs = outputResponse.result.data ?? [];
			const outputsByPeriod = allOutputs
				.filter((output) => isSameMonthAndYear(output.startMonth, year, month))
				.filter((output) => !!output.acceptanceReportId)
				.sort(
					(a, b) =>
						new Date(b.startMonth).getTime() - new Date(a.startMonth).getTime(),
				);

			if (outputsByPeriod.length === 0) {
				setRows([]);
				return;
			}

			setActiveAcceptanceReportId(
				outputsByPeriod[0]?.acceptanceReportId ?? null,
			);

			const detailResponses = await Promise.all(
				outputsByPeriod.map((output) =>
					api.get<ProductionOutputDto>(
						API.PRODUCTION.PRODUCTION_OUTPUT.DETAIL(output.id),
					),
				),
			);

			const mergedRows = detailResponses.flatMap((response, index) => {
				const detail = response.result;
				if (!detail) return [] as HierarchicalRow[];

				const hierarchical = transformApiResponseToHierarchical(detail);
				const flattened = flattenHierarchicalData(hierarchical);
				const outputId = outputsByPeriod[index]?.id || `output-${index}`;

				return flattened.map((row) => ({
					...row,
					id: `${outputId}-${row.id}`,
				}));
			});

			setRows(mergedRows);
		} catch (err) {
			setRows([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu bảng nghiệm thu và kết chuyển chi phí',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year]);

	useEffect(() => {
		fetchAcceptanceReport();
	}, [fetchAcceptanceReport]);

	const handleExport = async () => {
		if (!activeAcceptanceReportId) return;

		setIsExporting(true);
		try {
			await api.export(
				API.PRODUCTION.ACCEPTANCE_REPORT.DOWNLOAD(activeAcceptanceReportId),
			);
		} catch (err) {
			console.error('Failed to export acceptance report:', err);
		} finally {
			setIsExporting(false);
		}
	};

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
									}}
									placeholder='Tìm theo mã, tên vật tư...'
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
					<AcceptanceReportGrid
						data={filteredRows}
						className={cn(
							'max-h-[70vh] overflow-y-auto rounded-t-md shadow',
							largeText && '[&_table]:text-sm',
						)}
					/>
				</>
			)}
		</div>
	);
}
