'use client';

import { Button } from '@/components/ui/button';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import { useEffect, useMemo, useState } from 'react';

type EquipmentLookup = {
	id: string;
	code: string;
	name: string;
};

type SctxRevenueByMonthApi = {
	month: number;
	unitPrice: number;
	plannedOutput: number;
	actualOutput: number;
	initialRevenue: number;
	adjustedRevenue: number;
};

type SctxRevenueByYearApi = {
	year: number;
	months: SctxRevenueByMonthApi[];
};

type SctxRevenueByEquipmentApiResponse = {
	equipmentId: string;
	years: SctxRevenueByYearApi[];
};

interface SctxRevenueRow {
	month: number;
	unitPrice: number;
	plannedQuantity: number;
	actualQuantity: number;
	baseRevenue: number;
	adjustedRevenue: number;
}

const borderCellClass =
	'border border-black px-2 py-1 align-middle leading-tight whitespace-normal break-words';

const yearOptions = Array.from({ length: 11 }, (_, index) => {
	const year = new Date().getFullYear() - 5 + index;
	return {
		value: String(year),
		label: String(year),
	};
});

const buildRowsFromApi = (months: SctxRevenueByMonthApi[]): SctxRevenueRow[] => {
	const monthMap = new Map(months.map((item) => [item.month, item]));

	return Array.from({ length: 12 }, (_, index) => {
		const month = index + 1;
		const item = monthMap.get(month);

		return {
			month,
			unitPrice: item?.unitPrice ?? 0,
			plannedQuantity: item?.plannedOutput ?? 0,
			actualQuantity: item?.actualOutput ?? 0,
			baseRevenue: item?.initialRevenue ?? 0,
			adjustedRevenue: item?.adjustedRevenue ?? 0,
		};
	});
};

export function SctxRevenueReportDataTable() {
	const [fromYear, setFromYear] = useState(String(new Date().getFullYear() - 1));
	const [toYear, setToYear] = useState(String(new Date().getFullYear()));
	const [equipments, setEquipments] = useState<EquipmentLookup[]>([]);
	const [selectedEquipmentId, setSelectedEquipmentId] = useState('');
	const [yearData, setYearData] = useState<SctxRevenueByYearApi[]>([]);
	const [isLoadingEquipments, setIsLoadingEquipments] = useState(false);
	const [isLoadingRows, setIsLoadingRows] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);

	const handleFromYearChange = (value: string) => {
		setFromYear(value);
		if (Number(value) > Number(toYear)) {
			setToYear(value);
		}
	};

	const handleToYearChange = (value: string) => {
		setToYear(value);
		if (Number(value) < Number(fromYear)) {
			setFromYear(value);
		}
	};

	useEffect(() => {
		let cancelled = false;

		const fetchEquipments = async () => {
			setIsLoadingEquipments(true);

			try {
				const response = await api.pagging<EquipmentLookup>(
					API.CATALOG.EQUIPMENT.LIST,
					{ ignorePagination: true },
				);

				if (cancelled) {
					return;
				}

				const options = (response.result.data ?? []).sort((a, b) =>
					a.code.localeCompare(b.code, 'vi'),
				);

				setEquipments(options);
				setSelectedEquipmentId((prev) => {
					if (prev && options.some((item) => item.id === prev)) {
						return prev;
					}
					return options[0]?.id ?? '';
				});
			} catch (err) {
				if (cancelled) {
					return;
				}

				setEquipments([]);
				setSelectedEquipmentId('');
				setRows([]);
				setError(
					err instanceof Error
						? err.message
						: 'Không thể tải danh sách thiết bị',
				);
			} finally {
				if (!cancelled) {
					setIsLoadingEquipments(false);
				}
			}
		};

		fetchEquipments();

		return () => {
			cancelled = true;
		};
	}, []);

	useEffect(() => {
		if (!selectedEquipmentId) {
			setYearData([]);
			return;
		}

		let cancelled = false;

		const fetchRevenueByEquipment = async () => {
			setIsLoadingRows(true);
			setError(null);

			try {
				const response = await api.post<
					SctxRevenueByEquipmentApiResponse,
					{ fromYear: number; toYear: number; equipmentId: string }
				>(
					API.PRODUCTION.ACCEPTANCE_REPORT.SCTX_REVENUE_BY_EQUIPMENT,
					{
						fromYear: Number(fromYear),
						toYear: Number(toYear),
						equipmentId: selectedEquipmentId,
					},
				);

				if (cancelled) {
					return;
				}

				setYearData(response.result.years ?? []);
			} catch (err) {
				if (cancelled) {
					return;
				}

				setYearData([]);
				setError(
					err instanceof Error
						? err.message
						: 'Không thể tải dữ liệu báo cáo doanh thu SCTX',
				);
			} finally {
				if (!cancelled) {
					setIsLoadingRows(false);
				}
			}
		};

		fetchRevenueByEquipment();

		return () => {
			cancelled = true;
		};
	}, [selectedEquipmentId, fromYear, toYear]);

	const yearRows = useMemo(() => {
		const sorted = [...yearData].sort((a, b) => a.year - b.year);

		return sorted.map((item) => ({
			year: item.year,
			rows: buildRowsFromApi(item.months ?? []),
		}));
	}, [yearData]);

	const yearTotals = useMemo(() => {
		return yearRows.map((group) => ({
			year: group.year,
			totals: group.rows.reduce(
				(acc, row) => ({
					plannedQuantity: acc.plannedQuantity + row.plannedQuantity,
					actualQuantity: acc.actualQuantity + row.actualQuantity,
					baseRevenue: acc.baseRevenue + row.baseRevenue,
					adjustedRevenue: acc.adjustedRevenue + row.adjustedRevenue,
				}),
				{
					plannedQuantity: 0,
					actualQuantity: 0,
					baseRevenue: 0,
					adjustedRevenue: 0,
				},
			),
		}));
	}, [yearRows]);

	const selectedEquipment = useMemo(
		() => equipments.find((item) => item.id === selectedEquipmentId),
		[equipments, selectedEquipmentId],
	);

	const displayEquipmentName = selectedEquipment?.name || 'Không xác định';
	const displayEquipmentCode = selectedEquipment?.code || 'thiet-bi';
	const displayYearRange =
		fromYear === toYear
			? `Năm ${fromYear}`
			: `Giai đoạn ${fromYear} - ${toYear}`;

	const handleExport = async () => {
		if (isExporting) {
			return;
		}

		setIsExporting(true);

		try {
			const XLSX = await import('xlsx');

			const excelRows = yearRows.flatMap((group) => {
				const totals = yearTotals.find(
					(item) => item.year === group.year,
				)?.totals;

				const rows = group.rows.map((row) => ({
					'Năm': group.year,
					'Thời gian': `Tháng ${row.month}`,
					'Đơn giá (đ/t)': row.unitPrice,
					'Sản lượng ban đầu (t)': row.plannedQuantity,
					'Sản lượng thực tế (t)': row.actualQuantity,
					'Doanh thu SCTX ban đầu (đ)': row.baseRevenue,
					'Doanh thu SCTX điều chỉnh (đ)': row.adjustedRevenue,
				}));

				rows.push({
					'Năm': group.year,
					'Thời gian': 'Tổng cộng',
					'Đơn giá (đ/t)': 0,
					'Sản lượng ban đầu (t)': totals?.plannedQuantity ?? 0,
					'Sản lượng thực tế (t)': totals?.actualQuantity ?? 0,
					'Doanh thu SCTX ban đầu (đ)': totals?.baseRevenue ?? 0,
					'Doanh thu SCTX điều chỉnh (đ)': totals?.adjustedRevenue ?? 0,
				});

				return rows;
			});

			const worksheet = XLSX.utils.json_to_sheet(excelRows);
			worksheet['!cols'] = [
				{ wch: 10 },
				{ wch: 16 },
				{ wch: 18 },
				{ wch: 24 },
				{ wch: 22 },
				{ wch: 30 },
				{ wch: 32 },
			];

			const workbook = XLSX.utils.book_new();
			XLSX.utils.book_append_sheet(workbook, worksheet, 'Doanh thu SCTX');

			const normalizedCode = displayEquipmentCode.replace(
				/\s+/g,
				'-',
			);
			XLSX.writeFile(
				workbook,
				`bao-cao-doanh-thu-sctx-${normalizedCode}-${fromYear}-${toYear}.xlsx`,
			);
		} finally {
			setIsExporting(false);
		}
	};

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Từ năm</p>
						<Select value={fromYear} onValueChange={handleFromYearChange}>
							<SelectTrigger className='w-[120px] bg-white'>
								<SelectValue placeholder='Chọn năm' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{yearOptions.map((item) => (
									<SelectItem key={item.value} value={item.value}>
										{item.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Đến năm</p>
						<Select value={toYear} onValueChange={handleToYearChange}>
							<SelectTrigger className='w-[120px] bg-white'>
								<SelectValue placeholder='Chọn năm' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{yearOptions.map((item) => (
									<SelectItem key={item.value} value={item.value}>
										{item.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Thiết bị</p>
						<Select
							value={selectedEquipmentId}
							onValueChange={setSelectedEquipmentId}
							disabled={isLoadingEquipments || equipments.length === 0}
						>
							<SelectTrigger className='w-[300px] bg-white'>
								<SelectValue
									placeholder={
										isLoadingEquipments
											? 'Đang tải thiết bị...'
											: 'Chọn thiết bị'
									}
								/>
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{equipments.map((item) => (
									<SelectItem key={item.id} value={item.id}>
										{item.code} - {item.name}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>
				</div>

				<Button
					variant='outline'
					size='sm'
					onClick={handleExport}
					disabled={
						isExporting ||
						isLoadingRows ||
						yearRows.length === 0
					}
					className='h-10 gap-1.5'
				>
					{isExporting ? (
						<Spinner />
					) : (
						<>
							<DownloadIcon style={{ fontSize: 18 }} />
							<span>Xuất Excel</span>
						</>
					)}
				</Button>
			</div>

			<div className='rounded-md border bg-[#e6e6e6] p-3 md:p-4'>
				{error ? (
					<p className='mb-3 text-sm text-red-600'>{error}</p>
				) : null}
				<div className='mx-auto w-full overflow-x-auto'>
					<div className='mx-auto max-h-[70vh] min-h-[210mm] min-w-[1320px] overflow-y-auto bg-white p-3 shadow-[0_8px_30px_rgba(0,0,0,0.14)] md:p-5'>
						<div className='font-["Times_New_Roman",Times,serif]'>
							<div className='flex items-start justify-between gap-8'>
								<div className='space-y-1 text-left font-bold'>
									<p className='text-base leading-tight md:text-lg'>
										CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
									</p>
									<p className='border-b border-black pb-1 text-center text-sm leading-tight md:text-base'>
										CÔNG TRƯỜNG KHAI THÁC 1
									</p>
								</div>
							</div>

							<div className='mt-4 text-center'>
								<p className='text-lg font-bold uppercase md:text-2xl'>
									Bảng theo dõi doanh thu SCTX thiết bị
								</p>
								<p className='mt-2 text-base font-bold md:text-xl'>
									{displayYearRange}
								</p>
							</div>

							<div className='mt-5 space-y-1 text-base md:text-lg'>
								<p>
									<span className='font-bold'>Thiết bị:</span>{' '}
									{displayEquipmentName}
								</p>
							</div>

							{isLoadingRows ? (
								<div className='flex items-center justify-center py-6'>
									<Spinner />
								</div>
							) : (
								yearRows.map((group) => {
									const totals = yearTotals.find(
										(item) => item.year === group.year,
									)?.totals;

									return (
										<div key={group.year} className='mt-5'>
											<p className='mb-2 text-base font-bold md:text-lg'>
												Năm {group.year}
											</p>
											<table className='w-full min-w-[1200px] table-fixed border-collapse text-center text-sm md:text-base'>
												<thead>
													<tr className='font-bold'>
														<th className={borderCellClass}>Thời gian</th>
														<th className={borderCellClass}>Đơn giá (đ/t)</th>
														<th className={borderCellClass}>Sản lượng ban đầu (t)</th>
														<th className={borderCellClass}>Sản lượng thực tế (t)</th>
														<th className={borderCellClass}>Doanh thu SCTX ban đầu (đ)</th>
														<th className={borderCellClass}>
															Doanh thu SCTX điều chỉnh (đ)
														</th>
													</tr>
												</thead>
												<tbody>
													{group.rows.map((row) => (
														<tr key={`${group.year}-${row.month}`}>
															<td className={borderCellClass}>Tháng {row.month}</td>
															<td className={`${borderCellClass} text-right`}>
																{formatNumber(row.unitPrice, {
																	maximumFractionDigits: 0,
																})}
															</td>
															<td className={`${borderCellClass} text-right`}>
																{formatNumber(row.plannedQuantity, {
																	maximumFractionDigits: 0,
																})}
															</td>
															<td className={`${borderCellClass} text-right`}>
																{formatNumber(row.actualQuantity, {
																	maximumFractionDigits: 0,
																})}
															</td>
															<td className={`${borderCellClass} text-right`}>
																{formatNumber(row.baseRevenue, {
																	maximumFractionDigits: 0,
																})}
															</td>
															<td className={`${borderCellClass} text-right`}>
																{formatNumber(row.adjustedRevenue, {
																	maximumFractionDigits: 0,
																})}
															</td>
														</tr>
													))}
													<tr className='bg-[#f7f7f7] font-bold'>
														<td className={borderCellClass}>Tổng cộng</td>
														<td className={borderCellClass}></td>
														<td className={`${borderCellClass} text-right`}>
															{formatNumber(totals?.plannedQuantity ?? 0, {
																maximumFractionDigits: 0,
															})}
														</td>
														<td className={`${borderCellClass} text-right`}>
															{formatNumber(totals?.actualQuantity ?? 0, {
																maximumFractionDigits: 0,
															})}
														</td>
														<td className={`${borderCellClass} text-right`}>
															{formatNumber(totals?.baseRevenue ?? 0, {
																maximumFractionDigits: 0,
															})}
														</td>
														<td className={`${borderCellClass} text-right`}>
															{formatNumber(totals?.adjustedRevenue ?? 0, {
																maximumFractionDigits: 0,
															})}
														</td>
													</tr>
												</tbody>
											</table>
										</div>
									);
								})
							)}
						</div>
					</div>
				</div>
			</div>
		</div>
	);
}
