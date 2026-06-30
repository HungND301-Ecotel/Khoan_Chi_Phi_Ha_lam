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
import type { Department } from '@/features/main/catalog/department/columns';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementListRequest,
	LumpSumFinalSettlementMonthResponse,
	LumpSumQuarterCustomCost,
	LumpSumQuarterRevenueByMonth,
	LumpSumQuarterTransferredCost,
	ProcessGroup,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn, formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { useCallback, useEffect, useMemo, useState } from 'react';

const ALL_DEPARTMENT = '__all_department__';
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
	const [selectedDepartment, setSelectedDepartment] = useState(ALL_DEPARTMENT);
	const [departmentOptions, setDepartmentOptions] = useState<
		{ value: string; label: string }[]
	>([{ value: ALL_DEPARTMENT, label: 'Tất cả đơn vị' }]);
	const [selectedProcessGroup, setSelectedProcessGroup] =
		useState(ALL_PROCESS_GROUP);
	const [processGroupOptions, setProcessGroupOptions] = useState<
		{ value: string; label: string }[]
	>([{ value: ALL_PROCESS_GROUP, label: 'Tất cả nhóm công đoạn' }]);
	const [rows, setRows] = useState<LumpSumFinalSettlement[]>([]);
	const [quarterSpecialQuantities, setQuarterSpecialQuantities] = useState<{
		coalExcavationActualQuantity: number;
		coalCrosscutActualQuantity: number;
		meterExcavationActualQuantity: number;
		meterCrosscutActualQuantity: number;
	}>({
		coalExcavationActualQuantity: 0,
		coalCrosscutActualQuantity: 0,
		meterExcavationActualQuantity: 0,
		meterCrosscutActualQuantity: 0,
	});
	const [revenueByMonth, setRevenueByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [costByMonth, setCostByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [savingByMonth, setSavingByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [transferredCostByMonth, setTransferredCostByMonth] =
		useState<LumpSumQuarterTransferredCost | null>(null);
	const [customCosts, setCustomCosts] = useState<LumpSumQuarterCustomCost[]>(
		[],
	);
	const [acceptedSavingMonth, setAcceptedSavingMonth] = useState(0);
	const [quyetToanSavingsLimit, setQuyetToanSavingsLimit] = useState(0);
	const [savingAddedToIncomeMonth, setSavingAddedToIncomeMonth] = useState(0);
	const [savingCarryForwardByMonths, setSavingCarryForwardByMonths] = useState<
		{ month: number; value: number }[]
	>([]);
	const [savingCarryForwardToNextMonths, setSavingCarryForwardToNextMonths] =
		useState(0);
	const [isLoading, setIsLoading] = useState(false);
	const [isLoadingDepartments, setIsLoadingDepartments] = useState(false);
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
		const fetchDepartments = async () => {
			setIsLoadingDepartments(true);
			try {
				const response = await api.pagging<Department>(
					API.CATALOG.DEPARTMENT.LIST,
					{ ignorePagination: true },
				);

				const options = [
					{ value: ALL_DEPARTMENT, label: 'Tất cả đơn vị' },
					...(response.result.data ?? []).map((item) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				];

				setDepartmentOptions(options);
			} catch (err) {
				console.error('Failed to fetch departments:', err);
			} finally {
				setIsLoadingDepartments(false);
			}
		};

		fetchDepartments();
	}, []);

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
				departmentId:
					selectedDepartment === ALL_DEPARTMENT ? '' : selectedDepartment,
			};

			const response = await api.post<
				LumpSumFinalSettlement[] | LumpSumFinalSettlementMonthResponse,
				LumpSumFinalSettlementListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload);

			const monthData = Array.isArray(response.result) ? null : response.result;
			const items =
				monthData?.items ??
				(Array.isArray(response.result) ? response.result : []);
			setRows(groupByProcessGroup(items));
			setQuarterSpecialQuantities({
				coalExcavationActualQuantity:
					monthData?.coalExcavationActualQuantity ?? 0,
				coalCrosscutActualQuantity: monthData?.coalCrosscutActualQuantity ?? 0,
				meterExcavationActualQuantity:
					monthData?.meterExcavationActualQuantity ?? 0,
				meterCrosscutActualQuantity:
					monthData?.meterCrosscutActualQuantity ?? 0,
			});
			setRevenueByMonth(monthData?.revenue ?? null);
			setCostByMonth(monthData?.cost ?? null);
			setSavingByMonth(monthData?.saving ?? null);
			setTransferredCostByMonth(monthData?.transferredCost ?? null);
			setAcceptedSavingMonth(monthData?.acceptedSavingMonth ?? 0);
			setQuyetToanSavingsLimit(monthData?.quyetToanSavingsLimit ?? 0);
			setSavingAddedToIncomeMonth(monthData?.savingAddedToIncomeMonth ?? 0);
			setSavingCarryForwardByMonths(
				monthData?.savingCarryForwardByMonths ?? [],
			);
			setSavingCarryForwardToNextMonths(
				monthData?.savingCarryForwardToNextMonths ?? 0,
			);
			setCustomCosts(monthData?.customCosts ?? []);
		} catch (err) {
			setRows([]);
			setQuarterSpecialQuantities({
				coalExcavationActualQuantity: 0,
				coalCrosscutActualQuantity: 0,
				meterExcavationActualQuantity: 0,
				meterCrosscutActualQuantity: 0,
			});
			setRevenueByMonth(null);
			setCostByMonth(null);
			setSavingByMonth(null);
			setTransferredCostByMonth(null);
			setAcceptedSavingMonth(0);
			setQuyetToanSavingsLimit(0);
			setSavingAddedToIncomeMonth(0);
			setSavingCarryForwardByMonths([]);
			setSavingCarryForwardToNextMonths(0);
			setCustomCosts([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu quyết toán khoán theo tháng',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year, selectedDepartment, selectedProcessGroup]);

	useEffect(() => {
		fetchLumpSumFinalSettlementMonth();
	}, [fetchLumpSumFinalSettlementMonth]);

	const reportRows = useMemo(() => {
		const currentMonthNum = Number(month);
		const currentYearNum = Number(year);
		const prevMonthNum = currentMonthNum === 1 ? 12 : currentMonthNum - 1;
		const prevYearNum =
			currentMonthNum === 1 ? currentYearNum - 1 : currentYearNum;

		const revenue = {
			materials: revenueByMonth?.materials?.totalAmount ?? 0,
			maintains: revenueByMonth?.maintains?.totalAmount ?? 0,
			electricities: revenueByMonth?.electricities?.totalAmount ?? 0,
			total: revenueByMonth?.totalAmount ?? 0,
		};
		const cost = {
			materials: costByMonth?.materials?.totalAmount ?? 0,
			maintains: costByMonth?.maintains?.totalAmount ?? 0,
			electricities: costByMonth?.electricities?.totalAmount ?? 0,
			total: costByMonth?.totalAmount ?? 0,
		};
		const saving = {
			materials: savingByMonth?.materials?.totalAmount ?? 0,
			maintains: savingByMonth?.maintains?.totalAmount ?? 0,
			electricities: savingByMonth?.electricities?.totalAmount ?? 0,
			total: savingByMonth?.totalAmount ?? 0,
		};

		const buildCustomCostRow = (
			item: LumpSumQuarterCustomCost,
		): LumpSumFinalSettlement => {
			const quantity = item.actualQuantity || 0;
			const materialUnit = item.materialUnitPrice || 0;
			const maintainUnit = item.maintainUnitPrice || 0;
			const electricityUnit = item.electricityUnitPrice || 0;
			const materialTotal = quantity * materialUnit;
			const maintainTotal = quantity * maintainUnit;
			const electricityTotal = quantity * electricityUnit;

			return {
				id: item.id,
				sttLabel: '-',
				month: item.month ? Number(item.month) : undefined,
				productName: item.customName || '',
				unitOfMeasureName: 'Đồng',
				plannedQuantity: undefined,
				actualQuantity: quantity,
				materials: {
					unitPrice: materialUnit,
					totalAmount: materialTotal,
				},
				maintains: {
					unitPrice: maintainUnit,
					totalAmount: maintainTotal,
				},
				electricities: {
					unitPrice: electricityUnit,
					totalAmount: electricityTotal,
				},
				totalAmount: materialTotal + maintainTotal + electricityTotal,
			};
		};

		const customCostRows = customCosts.map((item) => buildCustomCostRow(item));
		const transferred = {
			materials: transferredCostByMonth?.materials?.totalAmount ?? 0,
			maintains: transferredCostByMonth?.maintains?.totalAmount ?? 0,
			electricities: transferredCostByMonth?.electricities?.totalAmount ?? 0,
			total: transferredCostByMonth?.totalAmount ?? 0,
		};

		const makeZeroRow = (
			productName: string,
			options?: {
				sttLabel?: string;
				isBold?: boolean;
				month?: number;
				unitOfMeasureName?: string;
				materialsTotalAmount?: number;
				maintainsTotalAmount?: number;
				electricitiesTotalAmount?: number;
				totalAmount?: number;
				hidePlanActual?: boolean;
				hideUnitPrice?: boolean;
				isMergedValueRow?: boolean;
				mergedValue?: number;
				isTransferredDefaultRow?: boolean;
			},
		): LumpSumFinalSettlement => ({
			sttLabel: options?.sttLabel,
			isBold: options?.isBold,
			excludeFromSummary: true,
			isMergedValueRow: options?.isMergedValueRow,
			isTransferredDefaultRow: options?.isTransferredDefaultRow,
			month: options?.month,
			mergedValue: options?.mergedValue ?? options?.totalAmount ?? 0,
			productName,
			unitOfMeasureName: options?.unitOfMeasureName ?? '',
			plannedQuantity: options?.hidePlanActual ? undefined : 0,
			actualQuantity: options?.hidePlanActual ? undefined : 0,
			materials: {
				unitPrice: options?.hideUnitPrice ? undefined : 0,
				totalAmount: options?.materialsTotalAmount ?? 0,
			},
			maintains: {
				unitPrice: options?.hideUnitPrice ? undefined : 0,
				totalAmount: options?.maintainsTotalAmount ?? 0,
			},
			electricities: {
				unitPrice: options?.hideUnitPrice ? undefined : 0,
				totalAmount: options?.electricitiesTotalAmount ?? 0,
			},
			totalAmount: options?.totalAmount ?? 0,
		});

		const defaultRows: LumpSumFinalSettlement[] = [
			makeZeroRow(`Doanh thu tháng ${month}/${year}`, {
				sttLabel: 'I',
				isBold: true,
				unitOfMeasureName: 'Đồng',
				materialsTotalAmount: revenue.materials,
				maintainsTotalAmount: revenue.maintains,
				electricitiesTotalAmount: revenue.electricities,
				totalAmount: revenue.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			makeZeroRow(`Chi phí tháng ${month}/${year}`, {
				sttLabel: 'II',
				isBold: true,
				unitOfMeasureName: 'Đồng',
				materialsTotalAmount: cost.materials,
				maintainsTotalAmount: cost.maintains,
				electricitiesTotalAmount: cost.electricities,
				totalAmount: cost.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			makeZeroRow(`Chi phí kết chuyển T${month}/${year}`, {
				sttLabel: 'II.1',
				isTransferredDefaultRow: true,
				month: Number(month),
				materialsTotalAmount: transferred.materials,
				maintainsTotalAmount: transferred.maintains,
				electricitiesTotalAmount: transferred.electricities,
				totalAmount: transferred.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...customCostRows,
			makeZeroRow(`Giá trị tiết kiệm(+)/bội chi(-) tháng ${month}/${year}`, {
				sttLabel: 'III',
				isBold: true,
				unitOfMeasureName: 'Đồng',
				materialsTotalAmount: saving.materials,
				maintainsTotalAmount: saving.maintains,
				electricitiesTotalAmount: saving.electricities,
				totalAmount: saving.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			makeZeroRow(
				`Mức tiết kiệm theo quy định thanh quyết toán tháng ${month}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: quyetToanSavingsLimit,
				},
			),
			makeZeroRow(
				`Tổng giá trị tiết kiệm được chấp nhận tháng ${month}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: acceptedSavingMonth,
				},
			),
			makeZeroRow(
				`Giá trị tiết kiệm luân chuyển từ tháng ${prevMonthNum}/${prevYearNum} cộng/trừ vào thu nhập tháng ${month}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue:
						savingCarryForwardByMonths.find((x) => x.month === prevMonthNum)
							?.value ?? 0,
				},
			),
			makeZeroRow(
				'Giá trị tiết kiệm được cộng/trừ vào thu nhập luân chuyển sang các tháng tiếp theo',
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: savingCarryForwardToNextMonths,
				},
			),
		];

		const specialRows: LumpSumFinalSettlement[] = [
			{
				sttLabel: '1',
				productName: 'Than đào lò',
				unitOfMeasureName: 'Tấn',
				isBold: true,
				plannedQuantity: undefined,
				actualQuantity: quarterSpecialQuantities.coalExcavationActualQuantity,
				excludeFromSummary: true,
			},
			{
				sttLabel: '2',
				productName: 'Than xén lò',
				unitOfMeasureName: 'Tấn',
				isBold: true,
				plannedQuantity: undefined,
				actualQuantity: quarterSpecialQuantities.coalCrosscutActualQuantity,
				excludeFromSummary: true,
			},
			{
				sttLabel: '3',
				productName: 'Mét lò đào',
				unitOfMeasureName: 'm',
				isBold: true,
				plannedQuantity: undefined,
				actualQuantity: quarterSpecialQuantities.meterExcavationActualQuantity,
				excludeFromSummary: true,
			},
			{
				sttLabel: '4',
				productName: 'Mét xén lò',
				unitOfMeasureName: 'm',
				isBold: true,
				plannedQuantity: undefined,
				actualQuantity: quarterSpecialQuantities.meterCrosscutActualQuantity,
				excludeFromSummary: true,
			},
		];

		return [...specialRows, ...rows, ...defaultRows];
	}, [
		acceptedSavingMonth,
		costByMonth,
		customCosts,
		month,
		quarterSpecialQuantities,
		revenueByMonth,
		rows,
		savingAddedToIncomeMonth,
		savingByMonth,
		transferredCostByMonth,
		quyetToanSavingsLimit,
		savingCarryForwardByMonths,
		savingCarryForwardToNextMonths,
		year,
	]);

	const filteredRows = useMemo(() => {
		if (!enableSearch || !searchQuery.trim()) {
			return reportRows;
		}

		const query = searchQuery.toLowerCase();

		return reportRows.filter((row) => {
			const keywords = [row.productCode, row.productName, row.unitOfMeasureName]
				.filter(Boolean)
				.join(' ')
				.toLowerCase();

			return keywords.includes(query);
		});
	}, [enableSearch, reportRows, searchQuery]);

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
					departmentId:
						selectedDepartment === ALL_DEPARTMENT ? '' : selectedDepartment,
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
		return filteredRows
			.filter((item) => !item.isProcessGroupRow && !item.excludeFromSummary)
			.reduce((sum, item) => sum + (item.totalAmount ?? 0), 0);
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

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Đơn vị</p>
						<Select
							value={selectedDepartment}
							onValueChange={setSelectedDepartment}
							disabled={isLoadingDepartments}
						>
							<SelectTrigger className='w-[320px] bg-white'>
								<SelectValue placeholder='Chọn đơn vị' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{departmentOptions.map((option) => (
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
							<span>Tải xuống</span>
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
								Tổng giá trị bảng: {formatNumber(totalReportValue)} Đồng
							</div>

							<ExcelReportFooter month={month} year={year} />
						</div>
					</div>
				</div>
			)}
		</div>
	);
}
