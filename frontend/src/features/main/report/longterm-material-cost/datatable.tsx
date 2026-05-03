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
import {
	LongtermMaterialDetailItem,
	LongTermTrackingResponse,
} from '@/features/main/cost/producttion/production/longterm-material-cost/types';
import { api } from '@/lib/api';
import { cn, formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
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

interface ExcelReportHeaderProps {
	qkh: number;
	qdm: number;
	month: string;
	year: string;
}

interface ExcelStructuredTableProps {
	items: LongtermMaterialDetailItem[];
	largeText?: boolean;
	startIndex?: number;
}

const borderCellClass =
	'border border-black px-0.5 py-1 align-middle leading-tight whitespace-normal break-words';

const formatPeriodLabel = (month: string, year: string) => {
	return `Tháng ${Number(month)} năm ${year}`;
};

const ExcelReportHeader = ({
	qkh,
	qdm,
	month,
	year,
}: ExcelReportHeaderProps) => {
	return (
		<div className='font-["Times_New_Roman",Times,serif]'>
			<div className='flex items-start justify-between gap-10'>
				<div className='space-y-1 text-left font-bold'>
					<p className='text-lg leading-tight md:text-2xl'>
						CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
					</p>
					<p className='border-b border-black pb-1 text-center text-sm leading-tight md:text-xl'>
						CÔNG TRƯỜNG KHAI THÁC 1
					</p>
				</div>
				<div className='space-y-1 text-right text-sm font-bold md:text-base'>
					<p>
						<span className='mr-2'>Qkh:</span>
						{formatNumber(qkh)}
						<span className='ml-2'>Tấn</span>
					</p>
					<p>
						<span className='mr-2'>Qđm:</span>
						{formatNumber(qdm)}
						<span className='ml-2'>Tấn</span>
					</p>
					<p className='pt-1'>Bảng số: 03</p>
				</div>
			</div>

			<div className='mt-6 text-center'>
				<p className='text-lg font-bold uppercase md:text-2xl'>
					Bảng hạch toán chi phí vật tư dài kỳ
				</p>
				<p className='mt-2 text-sm font-bold md:text-base'>
					{formatPeriodLabel(month, year)}
				</p>
			</div>
		</div>
	);
};

const ExcelStructuredTable = ({
	items,
	largeText = true,
	startIndex = 1,
}: ExcelStructuredTableProps) => {
	const textClass = largeText
		? 'text-[11px] md:text-[12px]'
		: 'text-[10px] md:text-[11px]';

	const totalOpeningBalance = items.reduce(
		(sum, item) => sum + (item.pendingValueStartPeriod ?? 0),
		0,
	);
	const totalAmount = items.reduce(
		(sum, item) => sum + (item.totalAmount ?? 0),
		0,
	);
	const totalValueToAccount = items.reduce(
		(sum, item) => sum + (item.totalValueToAccount ?? 0),
		0,
	);
	const totalOriginAmount = items.reduce(
		(sum, item) => sum + (item.originAmount ?? 0),
		0,
	);
	const totalValueByStandard = items.reduce(
		(sum, item) => sum + (item.valueByStandard ?? 0),
		0,
	);
	const totalAccountedValue = items.reduce(
		(sum, item) => sum + (item.accountedValueThisPeriod ?? 0),
		0,
	);
	const totalEndingBalance = items.reduce(
		(sum, item) => sum + (item.pendingValueEndPeriod ?? 0),
		0,
	);

	return (
		<div className='overflow-x-hidden border border-black bg-white font-["Times_New_Roman",Times,serif]'>
			<table
				className={cn(
					'w-full table-fixed border-collapse text-center font-semibold',
					textClass,
				)}
			>
				<thead>
					<tr className='bg-white'>
						<th rowSpan={2} className={borderCellClass}>
							STT
						</th>
						<th rowSpan={2} className={borderCellClass}>
							MÃ PHỤ TÙNG
						</th>
						<th rowSpan={2} className={borderCellClass}>
							TÊN PHỤ TÙNG
						</th>
						<th rowSpan={2} className={borderCellClass}>
							ĐVT
						</th>
						<th rowSpan={2} className={borderCellClass}>
							GIÁ TRỊ CHỜ HẠCH TOÁN ĐẦU KỲ (Đồng)
						</th>
						<th colSpan={3} className={borderCellClass}>
							GIÁ TRỊ PHÁT SINH TRONG KỲ
						</th>
						<th rowSpan={2} className={borderCellClass}>
							TỔNG GIÁ TRỊ CẦN HẠCH TOÁN (Đồng)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							NGUYÊN GIÁ (đồng)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							THỜI GIAN SỬ DỤNG (Ti)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							THỜI GIAN ĐÃ PHÂN BỔ
						</th>
						<th rowSpan={2} className={borderCellClass}>
							THỜI GIAN CÒN LẠI
						</th>
						<th rowSpan={2} className={borderCellClass}>
							GIÁ TRỊ CẦN HẠCH TOÁN THEO ĐỊNH MỨC (Đồng)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							TỶ LỆ PHÂN BỔ
						</th>
						<th rowSpan={2} className={borderCellClass}>
							GIÁ TRỊ DÀI KỲ HẠCH TOÁN KỲ NÀY (Đồng)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							GIÁ TRỊ CUỐI KỲ CHỜ HẠCH TOÁN KỲ SAU (Đồng)
						</th>
						<th rowSpan={2} className={borderCellClass}>
							GHI CHÚ
						</th>
					</tr>
					<tr className='bg-white'>
						<th className={borderCellClass}>SỐ LƯỢNG</th>
						<th className={borderCellClass}>ĐƠN GIÁ</th>
						<th className={borderCellClass}>THÀNH TIỀN</th>
					</tr>
				</thead>
				<tbody className='font-normal'>
					<tr className='bg-[#f7f7f7] font-semibold'>
						<td colSpan={4} className={cn(borderCellClass, 'text-left')}>
							TỔNG CỘNG
						</td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalOpeningBalance)}
						</td>
						<td className={borderCellClass}></td>
						<td className={borderCellClass}></td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalAmount)}
						</td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalValueToAccount)}
						</td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalOriginAmount)}
						</td>
						<td className={borderCellClass}></td>
						<td className={borderCellClass}></td>
						<td className={borderCellClass}></td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalValueByStandard)}
						</td>
						<td className={borderCellClass}></td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalAccountedValue)}
						</td>
						<td className={cn(borderCellClass, 'text-right')}>
							{formatNumber(totalEndingBalance)}
						</td>
						<td className={borderCellClass}></td>
					</tr>

					{items.length === 0 ? (
						<tr>
							<td colSpan={17} className='border border-black py-8 text-center'>
								Không có dữ liệu
							</td>
						</tr>
					) : (
						items.map((item, index) => (
							<tr
								key={item.id || `${item.partCode}-${index}`}
								className='bg-white'
							>
								<td className={borderCellClass}>{startIndex + index}</td>
								<td className={cn(borderCellClass, 'text-left')}>
									{item.partCode}
								</td>
								<td className={cn(borderCellClass, 'text-left')}>
									{item.partName}
								</td>
								<td className={borderCellClass}>{item.unitOfMeasureName}</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.pendingValueStartPeriod ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.issuedQuantity ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.unitPrice ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.totalAmount ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.totalValueToAccount ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.originAmount ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.usageTime ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.allocatedTime ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.remainingTime ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.valueByStandard ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.allocationRatio ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.accountedValueThisPeriod ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-right')}>
									{formatNumber(item.pendingValueEndPeriod ?? 0)}
								</td>
								<td className={cn(borderCellClass, 'text-left')}>
									{item.note || ''}
								</td>
							</tr>
						))
					)}
				</tbody>
			</table>
		</div>
	);
};

const ExcelReportFooter = ({
	month,
	year,
}: {
	month: string;
	year: string;
}) => {
	return (
		<div className='mt-10 font-["Times_New_Roman",Times,serif] text-[13px] md:text-[14px]'>
			<div className='mb-3 flex justify-end'>
				<p className='pr-8 text-right text-[15px] font-semibold italic'>
					{/* Hà Lầm, ngày 18 tháng 7 năm 2024 */}
					{`Hà lầm, tháng ${Number(month)} năm ${year}`}
				</p>
			</div>

			<div className='grid grid-cols-2 gap-12 font-semibold'>
				<div className='grid grid-cols-2 text-center'>
					<p>NGƯỜI LẬP</p>
					<div>
						<p>ĐẠI DIỆN BÊN NHẬN KHOÁN</p>
						<p className='mt-2'>QUẢN ĐỐC</p>
					</div>
				</div>

				<div className='grid grid-cols-1 text-center'>
					<div>
						<p>ĐẠI DIỆN BÊN GIAO KHOÁN</p>
						<p className='mt-2'>PHÒNG KẾ HOẠCH</p>
					</div>
				</div>
			</div>

			<div className='mt-16 grid grid-cols-2 gap-12 font-semibold'>
				<div className='grid grid-cols-2 text-center'></div>
				<div></div>
			</div>
		</div>
	);
};

export function LongtermMaterialCostDataTable({
	enableSearch = true,
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

	const fetchLongtermMaterialCost = useCallback(async () => {
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
		const processGroupId =
			selectedProcessGroup !== 'all' &&
			selectedProcessGroup !== UNGROUPED_PROCESS_GROUP
				? selectedProcessGroup
				: undefined;
		setIsExporting(true);
		try {
			const exportFileName = `bang-hach-toan-chi-phi-vat-tu-dai-ky-thang-${month}-nam-${year}.xlsx`;
			await api.export(
				`${API.PRODUCTION.ACCEPTANCE_REPORT.EXPORT_LONG_TERM_MATERIAL_COST(activeAcceptanceReportId)}?${new URLSearchParams(
					Object.entries({
						month,
						year,
						...(processGroupId ? { processGroupId } : {}),
					}) as [string, string][],
				).toString()}`,
				{
					fileName: exportFileName,
					forceFileName: true,
				},
			);
		} catch (err) {
			console.error('Failed to export long-term material cost:', err);
		} finally {
			setIsExporting(false);
		}
	};

	const totalQkh = filteredItems[0]?.actualOutput ?? 0;

	const totalQdm = filteredItems[0]?.standardOutput ?? 0;

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
					<div className='rounded-md border bg-[#e6e6e6] p-3 md:p-4'>
						<div className='mx-auto w-full overflow-auto'>
							<div className='mx-auto h-[210mm] overflow-auto bg-white p-3 shadow-[0_8px_30px_rgba(0,0,0,0.14)] md:p-5'>
								<ExcelReportHeader
									qkh={totalQkh}
									qdm={totalQdm}
									month={month}
									year={year}
								/>

								<div className='mt-10'>
									<ExcelStructuredTable
										items={filteredItems}
										largeText={largeText}
										startIndex={1}
									/>
								</div>

								<ExcelReportFooter month={month} year={year} />
							</div>
						</div>
					</div>
				</>
			)}
		</div>
	);
}
