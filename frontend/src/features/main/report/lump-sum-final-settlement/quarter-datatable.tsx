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
	LumpSumFinalSettlementMonthResponse,
	LumpSumFinalSettlementQuarterListRequest,
	LumpSumFinalSettlementQuarterResponse,
	LumpSumQuarterCustomCost,
	LumpSumQuarterRevenueByMonth,
	ProcessGroup,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn, formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { useCallback, useEffect, useMemo, useState } from 'react';

const ALL_DEPARTMENT = '__all_department__';
const ALL_PROCESS_GROUP = '__all_process_group__';

const quarterToMonthRange = (quarter: string) => {
	const quarterNumber = Number(quarter);
	const startMonth = (quarterNumber - 1) * 3 + 1;
	return [startMonth, startMonth + 1, startMonth + 2];
};

const toRomanQuarter = (quarter: string | number) => {
	const quarterNumber = Number(quarter);
	if (quarterNumber === 1) return 'I';
	if (quarterNumber === 2) return 'II';
	if (quarterNumber === 3) return 'III';
	if (quarterNumber === 4) return 'IV';
	return String(quarter);
};

const sumRows = (rows: LumpSumQuarterRevenueByMonth[]) =>
	rows.reduce(
		(acc, item) => ({
			materials: acc.materials + (item.materials?.totalAmount ?? 0),
			maintains: acc.maintains + (item.maintains?.totalAmount ?? 0),
			electricities: acc.electricities + (item.electricities?.totalAmount ?? 0),
			total: acc.total + (item.totalAmount ?? 0),
		}),
		{ materials: 0, maintains: 0, electricities: 0, total: 0 },
	);

interface LumpSumFinalSettlementReportTableProps {
	enableSearch?: boolean;
	enablePagination?: boolean;
}

interface ExcelReportHeaderProps {
	quarter: string;
	year: string;
}

interface ExcelReportFooterProps {
	quarter: string;
	year: string;
}

const formatDateString = (date: Date) => {
	return `${date.getDate().toString().padStart(2, '0')} tháng ${(
		date.getMonth() + 1
	)
		.toString()
		.padStart(2, '0')} năm ${date.getFullYear()}`;
};

const ExcelReportHeader = ({ quarter, year }: ExcelReportHeaderProps) => {
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
					Bảng quyết toán
				</p>
				<p className='mt-2 text-base font-bold uppercase md:text-xl'>
					Quý {toRomanQuarter(quarter)} năm {year}
				</p>
			</div>
		</div>
	);
};

const ExcelReportFooter = ({ quarter, year }: ExcelReportFooterProps) => {
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
				Biểu mẫu quý {toRomanQuarter(quarter)}/{year}
			</p>
		</div>
	);
};

export function LumpSumFinalSettlementReportTable({
	enableSearch = true,
}: LumpSumFinalSettlementReportTableProps) {
	const now = new Date();
	const currentYear = now.getFullYear();

	const [quarter, setQuarter] = useState(
		String(Math.floor(now.getMonth() / 3) + 1),
	);
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
	const [revenuesByMonth, setRevenuesByMonth] = useState<
		LumpSumQuarterRevenueByMonth[]
	>([]);
	const [costsByMonth, setCostsByMonth] = useState<
		LumpSumQuarterRevenueByMonth[]
	>([]);
	const [savingsByMonth, setSavingsByMonth] = useState<
		LumpSumQuarterRevenueByMonth[]
	>([]);
	const [monthBreakdowns, setMonthBreakdowns] = useState<
		LumpSumFinalSettlementMonthResponse[]
	>([]);
	const [customCosts, setCustomCosts] = useState<LumpSumQuarterCustomCost[]>(
		[],
	);
	const [acceptedSavingQuarter, setAcceptedSavingQuarter] = useState(0);
	const [savingAddedToIncomeQuarter, setSavingAddedToIncomeQuarter] =
		useState(0);
	const [isLoading, setIsLoading] = useState(false);
	const [isLoadingDepartments, setIsLoadingDepartments] = useState(false);
	const [isLoadingProcessGroups, setIsLoadingProcessGroups] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [searchQuery, setSearchQuery] = useState('');

	const quarterOptions = useMemo(
		() =>
			Array.from({ length: 4 }, (_, index) => {
				const value = String(index + 1);
				return {
					value,
					label: `Quý ${value}`,
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

	const fetchLumpSumFinalSettlementQuarter = useCallback(async () => {
		setIsLoading(true);
		setError(null);

		try {
			const payload: LumpSumFinalSettlementQuarterListRequest = {
				quarter,
				year,
				processGroupId:
					selectedProcessGroup === ALL_PROCESS_GROUP
						? ''
						: selectedProcessGroup,
				departmentId:
					selectedDepartment === ALL_DEPARTMENT ? '' : selectedDepartment,
			};

			const response = await api.post<
				LumpSumFinalSettlementQuarterResponse,
				LumpSumFinalSettlementQuarterListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_LIST, payload);

			setRows(groupByProcessGroup(response.result.items ?? [], 5));
			setQuarterSpecialQuantities({
				coalExcavationActualQuantity:
					response.result.coalExcavationActualQuantity ?? 0,
				coalCrosscutActualQuantity:
					response.result.coalCrosscutActualQuantity ?? 0,
				meterExcavationActualQuantity:
					response.result.meterExcavationActualQuantity ?? 0,
				meterCrosscutActualQuantity:
					response.result.meterCrosscutActualQuantity ?? 0,
			});
			setRevenuesByMonth(response.result.revenuesByMonth ?? []);
			setCostsByMonth(response.result.costsByMonth ?? []);
			setSavingsByMonth(response.result.savingsByMonth ?? []);
			setMonthBreakdowns(response.result.monthBreakdowns ?? []);
			setAcceptedSavingQuarter(response.result.acceptedSavingQuarter ?? 0);
			setSavingAddedToIncomeQuarter(
				response.result.savingAddedToIncomeQuarter ?? 0,
			);
			setCustomCosts(response.result.customCosts ?? []);
		} catch (err) {
			setRows([]);
			setQuarterSpecialQuantities({
				coalExcavationActualQuantity: 0,
				coalCrosscutActualQuantity: 0,
				meterExcavationActualQuantity: 0,
				meterCrosscutActualQuantity: 0,
			});
			setRevenuesByMonth([]);
			setCostsByMonth([]);
			setSavingsByMonth([]);
			setMonthBreakdowns([]);
			setAcceptedSavingQuarter(0);
			setSavingAddedToIncomeQuarter(0);
			setCustomCosts([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu quyết toán khoán theo quý',
			);
		} finally {
			setIsLoading(false);
		}
	}, [quarter, year, selectedDepartment, selectedProcessGroup]);

	useEffect(() => {
		fetchLumpSumFinalSettlementQuarter();
	}, [fetchLumpSumFinalSettlementQuarter]);

	const reportRows = useMemo(() => {
		const quarterRoman = toRomanQuarter(quarter);
		const months = quarterToMonthRange(quarter);
		const revenueByMonthMap = new Map(
			revenuesByMonth.map((item) => [item.month, item]),
		);
		const costByMonthMap = new Map(
			costsByMonth.map((item) => [item.month, item]),
		);
		const savingByMonthMap = new Map(
			savingsByMonth.map((item) => [item.month, item]),
		);
		const monthBreakdownByMonth = new Map(
			monthBreakdowns.map((item) => [item.revenue?.month ?? 0, item]),
		);

		const revenueQuarter = sumRows(revenuesByMonth);
		const costQuarter = sumRows(costsByMonth);
		const savingQuarter = sumRows(savingsByMonth);

		// Tính tổng quyetToanSavingsLimit của 3 tháng
		const quyetToanSavingsLimitQuarter = monthBreakdowns.reduce(
			(acc, month) => acc + (month.quyetToanSavingsLimit ?? 0),
			0,
		);

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

		const customCostRowsByMonth = new Map<number, LumpSumFinalSettlement[]>();
		const customCostTotalsByMonth = new Map<
			number,
			{
				materials: number;
				maintains: number;
				electricities: number;
				total: number;
			}
		>();
		for (const item of customCosts) {
			const month = Number(item.month ?? 0);
			if (!month) {
				continue;
			}
			const row = buildCustomCostRow(item);
			const rows = customCostRowsByMonth.get(month) ?? [];
			rows.push(row);
			customCostRowsByMonth.set(month, rows);

			const currentTotal = customCostTotalsByMonth.get(month) ?? {
				materials: 0,
				maintains: 0,
				electricities: 0,
				total: 0,
			};
			customCostTotalsByMonth.set(month, {
				materials: currentTotal.materials + (row.materials?.totalAmount ?? 0),
				maintains: currentTotal.maintains + (row.maintains?.totalAmount ?? 0),
				electricities:
					currentTotal.electricities + (row.electricities?.totalAmount ?? 0),
				total: currentTotal.total + (row.totalAmount ?? 0),
			});
		}

		const makeZeroRow = (
			productName: string,
			options?: {
				sttLabel?: string;
				isBold?: boolean;
				month?: number;
				isTransferredDefaultRow?: boolean;
				unitOfMeasureName?: string;
				materialsTotalAmount?: number;
				maintainsTotalAmount?: number;
				electricitiesTotalAmount?: number;
				totalAmount?: number;
				hidePlanActual?: boolean;
				hideUnitPrice?: boolean;
				isMergedValueRow?: boolean;
				mergedValue?: number;
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
			makeZeroRow(`Doanh thu quý ${quarterRoman}/${year}`, {
				sttLabel: 'I',
				isBold: true,
				materialsTotalAmount: revenueQuarter.materials,
				maintainsTotalAmount: revenueQuarter.maintains,
				electricitiesTotalAmount: revenueQuarter.electricities,
				totalAmount: revenueQuarter.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.map((monthNumber, idx) =>
				makeZeroRow(`Tháng ${monthNumber}/${year}`, {
					sttLabel: `I.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						revenueByMonthMap.get(monthNumber)?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						revenueByMonthMap.get(monthNumber)?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						revenueByMonthMap.get(monthNumber)?.electricities?.totalAmount ?? 0,
					totalAmount: revenueByMonthMap.get(monthNumber)?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),
			makeZeroRow(`Chi phí quý ${quarterRoman}/${year}`, {
				sttLabel: 'II',
				isBold: true,
				materialsTotalAmount: costQuarter.materials,
				maintainsTotalAmount: costQuarter.maintains,
				electricitiesTotalAmount: costQuarter.electricities,
				totalAmount: costQuarter.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.flatMap((monthNumber, idx) => {
				const monthTransferred =
					monthBreakdownByMonth.get(monthNumber)?.transferredCost;
				return [
					makeZeroRow(`Tháng ${monthNumber}/${year}`, {
						sttLabel: `II.${idx + 1}`,
						unitOfMeasureName: 'Đồng',
						materialsTotalAmount:
							costByMonthMap.get(monthNumber)?.materials?.totalAmount ?? 0,
						maintainsTotalAmount:
							costByMonthMap.get(monthNumber)?.maintains?.totalAmount ?? 0,
						electricitiesTotalAmount:
							costByMonthMap.get(monthNumber)?.electricities?.totalAmount ?? 0,
						totalAmount: costByMonthMap.get(monthNumber)?.totalAmount ?? 0,
						hidePlanActual: true,
						hideUnitPrice: true,
					}),
					makeZeroRow(`Chi phí kết chuyển T${monthNumber}/${year}`, {
						sttLabel: '-',
						isTransferredDefaultRow: true,
						month: monthNumber,
						unitOfMeasureName: '',
						materialsTotalAmount: monthTransferred?.materials?.totalAmount ?? 0,
						maintainsTotalAmount: monthTransferred?.maintains?.totalAmount ?? 0,
						electricitiesTotalAmount:
							monthTransferred?.electricities?.totalAmount ?? 0,
						totalAmount: monthTransferred?.totalAmount ?? 0,
						hidePlanActual: true,
						hideUnitPrice: true,
					}),
					...(customCostRowsByMonth.get(monthNumber) ?? []),
				];
			}),
			makeZeroRow(`Giá trị tiết kiệm, bội chi quý ${quarterRoman}/${year}`, {
				sttLabel: 'III',
				isBold: true,
				materialsTotalAmount: savingQuarter.materials,
				maintainsTotalAmount: savingQuarter.maintains,
				electricitiesTotalAmount: savingQuarter.electricities,
				totalAmount: savingQuarter.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.map((monthNumber, idx) =>
				makeZeroRow(`Tháng ${monthNumber}/${year}`, {
					sttLabel: `III.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						savingByMonthMap.get(monthNumber)?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						savingByMonthMap.get(monthNumber)?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						savingByMonthMap.get(monthNumber)?.electricities?.totalAmount ?? 0,
					totalAmount: savingByMonthMap.get(monthNumber)?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),
			makeZeroRow(
				`Mức tiết kiệm theo quy định thanh quyết toán quý ${quarterRoman}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: quyetToanSavingsLimitQuarter,
				},
			),
			makeZeroRow(
				`Tổng giá trị tiết kiệm được chấp nhận quý ${quarterRoman}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: acceptedSavingQuarter,
				},
			),
			makeZeroRow(
				`Giá trị tiết kiệm được cộng vào thu nhập quý ${quarterRoman}/${year}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: savingAddedToIncomeQuarter,
				},
			),
			...months.map((monthNumber) =>
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${monthNumber}/${year}`,
					{
						sttLabel: '*',
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue:
							monthBreakdownByMonth.get(monthNumber)
								?.savingAddedToIncomeMonth ?? 0,
					},
				),
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
		acceptedSavingQuarter,
		costsByMonth,
		customCosts,
		monthBreakdowns,
		quarter,
		quarterSpecialQuantities,
		revenuesByMonth,
		rows,
		savingAddedToIncomeQuarter,
		savingsByMonth,
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
			await api.export(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_EXPORT, {
				query: {
					quarter,
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
		} catch (err) {
			console.error('Failed to export lump-sum quarter report:', err);
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
						<p className='text-sm font-medium'>Quý</p>
						<Select
							value={quarter}
							onValueChange={(value) => {
								setQuarter(value);
							}}
						>
							<SelectTrigger className='w-[120px] bg-white'>
								<SelectValue placeholder='Chọn quý' />
							</SelectTrigger>
							<SelectContent>
								{quarterOptions.map((option) => (
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

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Đơn vị</p>
						<Select
							value={selectedDepartment}
							onValueChange={(value) => {
								setSelectedDepartment(value);
							}}
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
							<ExcelReportHeader quarter={quarter} year={year} />

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

							<ExcelReportFooter quarter={quarter} year={year} />
						</div>
					</div>
				</div>
			)}
		</div>
	);
}
