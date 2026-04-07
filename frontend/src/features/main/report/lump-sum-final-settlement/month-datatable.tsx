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
import { groupByProcessGroup } from '@/features/main/cost/lump-sum-final-settlement/grouping';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementListRequest,
	LumpSumFinalSettlementMonthResponse,
	ProcessGroup,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn, formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { useCallback, useEffect, useMemo, useState } from 'react';

const ALL_PROCESS_GROUP = '__all_process_group__';

interface LumpSumFinalSettlementMonthReportTableProps {
	enableSearch?: boolean;
	enablePagination?: boolean;
}

interface ExcelReportHeaderProps {
	month: string;
	year: string;
}

interface ExcelReportFooterProps {
	month: string;
	year: string;
}

const formatDateString = (date: Date) => {
	return `${date.getDate().toString().padStart(2, '0')} tháng ${(
		date.getMonth() + 1
	)
		.toString()
		.padStart(2, '0')} năm ${date.getFullYear()}`;
};

const ExcelReportHeader = ({ month, year }: ExcelReportHeaderProps) => {
	return (
		<div className='font-["Times_New_Roman",Times,serif]'>
			<div className='flex items-start justify-between gap-10'>
				<div className='space-y-1 text-left font-bold'>
					<p className='text-base leading-tight md:text-lg'>
						CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
					</p>
					<p className='border-b border-black pb-1 text-center text-sm leading-tight md:text-base'>
						CÔNG TRƯỜNG KHAI THÁC 1
					</p>
				</div>
				<div className='space-y-1 pt-1 text-right text-sm font-bold md:text-base'>
					<p>ĐVT: Đồng</p>
					<p>Bảng số: 05</p>
				</div>
			</div>

			<div className='mt-4 text-center'>
				<p className='text-lg font-bold uppercase md:text-2xl'>
					Bảng thanh toán
				</p>
				<p className='mt-2 text-base font-bold uppercase md:text-xl'>
					Tháng {month} năm {year}
				</p>
			</div>
		</div>
	);
};

const ExcelReportFooter = ({ month, year }: ExcelReportFooterProps) => {
	const signDate = formatDateString(new Date());

	return (
		<div className='mt-10 font-["Times_New_Roman",Times,serif] text-[14px] md:text-[16px]'>
			<div className='mb-3 flex justify-end'>
				<p className='pr-8 text-right font-semibold italic'>
					Hà Lầm, ngày {signDate}
				</p>
			</div>

			<div className='grid grid-cols-2 gap-12 font-semibold'>
				<div className='grid grid-cols-3 text-center'>
					<p className='col-span-3'>ĐẠI DIỆN BÊN NHẬN KHOÁN</p>
					<p className='mt-2'>NGƯỜI LẬP</p>
					<p className='mt-2'>QUẢN ĐỐC</p>
					<p className='mt-2'>PHÒNG KTTC</p>
				</div>

				<div className='grid grid-cols-1 text-center'>
					<p>ĐẠI DIỆN BÊN GIAO KHOÁN</p>
					<div className='mt-2 grid grid-cols-2 gap-4'>
						<p>PHÒNG KH</p>
						<div>
							<p>KT.GIÁM ĐỐC</p>
							<p className='mt-2'>PHÓ GIÁM ĐỐC</p>
						</div>
					</div>
				</div>
			</div>

			<p className='mt-4 text-xs italic'>
				Biểu mẫu tháng {month}/{year}
			</p>
		</div>
	);
};

export function LumpSumFinalSettlementMonthReportTable({
	enableSearch = true,
}: LumpSumFinalSettlementMonthReportTableProps) {
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

	const monthOptions = useMemo(
		() =>
			Array.from({ length: 12 }, (_, index) => {
				const value = String(index + 1);
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

	const fetchLumpSumFinalSettlementMonth = useCallback(async () => {
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
				LumpSumFinalSettlement[] | LumpSumFinalSettlementMonthResponse,
				LumpSumFinalSettlementListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload);
			const items = Array.isArray(response.result)
				? response.result
				: (response.result.items ?? []);
			setRows(groupByProcessGroup(items));
		} catch (err) {
			setRows([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu quyết toán khoán theo tháng',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year, selectedProcessGroup]);

	useEffect(() => {
		fetchLumpSumFinalSettlementMonth();
	}, [fetchLumpSumFinalSettlementMonth]);

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

	const handleExport = async () => {
		if (filteredRows.length === 0 || isExporting) {
			return;
		}

		setIsExporting(true);

		try {
			await api.export(API.COST.LUMP_SUM_FINAL_SETTLEMENT.MONTH_EXPORT, {
				query: {
					month,
					year,
					processGroupId:
						selectedProcessGroup === ALL_PROCESS_GROUP
							? ''
							: selectedProcessGroup,
					search: searchQuery.trim(),
				},
			});
			/* const headers = [
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
				row.sttLabel ?? index + 1,
				row.productCode ?? '',
				row.productName ?? '',
				row.unitOfMeasureName ?? '',
				row.plannedQuantity ?? '',
				row.actualQuantity ?? '',
				row.materials?.unitPrice ?? '',
				row.materials?.totalAmount ?? '',
				row.maintains?.unitPrice ?? '',
				row.maintains?.totalAmount ?? '',
				row.electricities?.unitPrice ?? '',
				row.electricities?.totalAmount ?? '',
				row.totalAmount ?? '',
			]);

			const csvText = [headers, ...records]
				.map((record) => record.map(escapeCsvCell).join(','))
				.join('\n');

			const blob = new Blob([`\uFEFF${csvText}`], {
				type: 'text/csv;charset=utf-8;',
			});
			const downloadUrl = window.URL.createObjectURL(blob);
			const link = document.createElement('a');

			link.href = downloadUrl;
			link.download = `bao-cao-quyet-toan-thang-${month}-${year}.csv`;
			document.body.appendChild(link);
			link.click();
			window.URL.revokeObjectURL(downloadUrl);
			document.body.removeChild(link); */
		} catch (err) {
			console.error('Failed to export lump-sum month report:', err);
		} finally {
			setIsExporting(false);
		}
	};

	const totalReportValue = useMemo(() => {
		return filteredRows.reduce((sum, item) => sum + (item.totalAmount ?? 0), 0);
	}, [filteredRows]);

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Tháng</p>
						<Select value={month} onValueChange={setMonth}>
							<SelectTrigger className='w-[120px] bg-white'>
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
							onValueChange={setSelectedProcessGroup}
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
			) : isLoading ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<div className='rounded-md border bg-[#e6e6e6] p-3 md:p-4'>
					<div className='mx-auto w-full overflow-x-auto'>
						<div className='mx-auto min-h-[210mm] min-w-[1680px] bg-white p-3 shadow-[0_8px_30px_rgba(0,0,0,0.14)] md:p-5'>
							<ExcelReportHeader month={month} year={year} />

							<div className='mt-6 rounded-t-md shadow'>
								<LumpSumDataTable
									columns={LUMP_SUM_FINAL_SETTLEMENT_COLUMNS}
									data={filteredRows}
									isLoading={isLoading}
									className={cn(
										'border-border rounded-none border shadow-none',
										'[&_table]:font-["Times_New_Roman",Times,serif]',
										'[&_thead_tr]:bg-white',
										'[&_thead_th]:bg-white',
										'[&_tbody_button]:hidden',
										'**:data-[slot=table-container]:overflow-visible',
									)}
								/>
							</div>

							<div className='mt-2 text-right font-["Times_New_Roman",Times,serif] text-sm italic'>
								Tổng giá trị bảng:{' '}
								{formatNumber(totalReportValue, { maximumFractionDigits: 0 })}{' '}
								Đồng
							</div>

							<ExcelReportFooter month={month} year={year} />
						</div>
					</div>
				</div>
			)}
		</div>
	);
}
