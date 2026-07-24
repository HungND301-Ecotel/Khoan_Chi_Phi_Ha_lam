import { FormMonthYear } from '@/components/form-month-year/form-month-year';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { API } from '@/constants/api-enpoint';
import { LUMP_SUM_FINAL_SETTLEMENT_COLUMNS } from '@/features/main/cost/lump-sum-final-settlement/columns';
import { LumpSumDataTable } from '@/features/main/cost/lump-sum-final-settlement/components/datatable';
import { groupByProcessGroup } from '@/features/main/cost/lump-sum-final-settlement/grouping';
import type { Department } from '@/features/main/catalog/department/columns';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementListRequest,
	LumpSumFinalSettlementMonthResponse,
	LumpSumFinalSettlementQuarterResponse,
	LumpSumQuarterCustomCost,
	LumpSumQuarterRevenueByMonth,
	LumpSumQuarterTransferredCost,
	ProcessGroup,
	UpdateLumpSumMonthCarryForwardRequest,
	UpdateLumpSumMonthSpecialQuantityRequest,
	UpsertLumpSumQuarterCustomCostRequest,
	YearFilterForm,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import EmailIcon from '@mui/icons-material/Email';
import PrintIcon from '@mui/icons-material/Print';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

const shadow = cn(
	'hover:shadow-[0px_2px_4px_-1px_rgba(0,0,0,0.2),0px_4px_5px_0px_rgba(0,0,0,0.14),0px_1px_10px_0px_rgba(0,0,0,0.12)] shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]',
);

export function MainCostLumpSumFinalSettlementMonthPage() {
	const { hasPermission } = usePermission();
	const [filteredData, setFilteredData] = useState<LumpSumFinalSettlement[]>(
		[],
	);
	const [quarterSpecialQuantities, setQuarterSpecialQuantities] = useState({
		coalExcavationActualQuantity: 0,
		coalCrosscutActualQuantity: 0,
		meterExcavationActualQuantity: 0,
		meterCrosscutActualQuantity: 0,
	});
	const [specialQuantityEditingField, setSpecialQuantityEditingField] =
		useState<
			'coalExcavationActualQuantity' | 'coalCrosscutActualQuantity' | null
		>(null);
	const [specialQuantityDraft, setSpecialQuantityDraft] = useState({
		coalExcavationActualQuantity: 0,
		coalCrosscutActualQuantity: 0,
	});
	const [revenueByMonth, setRevenueByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [costByMonth, setCostByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [savingByMonth, setSavingByMonth] =
		useState<LumpSumQuarterRevenueByMonth | null>(null);
	const [transferredCostByMonth, setTransferredCostByMonth] =
		useState<LumpSumQuarterTransferredCost | null>(null);
	const [quarterBreakdown, setQuarterBreakdown] =
		useState<LumpSumFinalSettlementQuarterResponse | null>(null);
	const [acceptedSavingMonth, setAcceptedSavingMonth] = useState(0);
	const [quyetToanSavingsLimit, setQuyetToanSavingsLimit] = useState(0);
	const [savingAddedToIncomeMonth, setSavingAddedToIncomeMonth] = useState(0);
	const [savingCarryForwardByMonths, setSavingCarryForwardByMonths] = useState<
		{ month: number; value: number }[]
	>([]);
	const [savingCarryForwardToNextMonths, setSavingCarryForwardToNextMonths] =
		useState(0);
	const [
		savingCarryForwardToNextMonthsDraft,
		setSavingCarryForwardToNextMonthsDraft,
	] = useState(0);
	const [savingCarryForwardEditing, setSavingCarryForwardEditing] =
		useState(false);
	const [customCosts, setCustomCosts] = useState<LumpSumQuarterCustomCost[]>(
		[],
	);
	const [editingSnapshot, setEditingSnapshot] = useState<
		Record<string, LumpSumQuarterCustomCost>
	>({});
	const [isLoading, setIsLoading] = useState(false);
	const [departments, setDepartments] = useState<
		{ value: string; label: string }[]
	>([{ value: '', label: 'Tất cả đơn vị' }]);
	const [processGroups, setProcessGroups] = useState<
		{ value: string; label: string }[]
	>([{ value: '', label: 'Tất cả nhóm công đoạn' }]);

	const now = new Date();
	const defaultMonth = String(now.getMonth() + 1);
	const defaultYear = String(now.getFullYear());

	const form = useForm<YearFilterForm>({
		defaultValues: {
			month: defaultMonth,
			year: defaultYear,
			processGroup: '',
			department: '',
		},
	});

	const getCurrentFilter = useCallback(() => {
		return {
			month: form.getValues('month') || defaultMonth,
			year: form.getValues('year') || defaultYear,
			processGroupId: form.getValues('processGroup') || null,
			departmentId: form.getValues('department') || null,
		};
	}, [defaultMonth, defaultYear, form]);

	const fetchLumpSumMonth = useCallback(
		async (payload: {
			month: string;
			year: string;
			processGroupId?: string | null;
			departmentId?: string | null;
		}) => {
			setIsLoading(true);
			try {
				const monthRes = await api.post<
					LumpSumFinalSettlementMonthResponse,
					LumpSumFinalSettlementListRequest
				>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload);
				setFilteredData(groupByProcessGroup(monthRes.result.items ?? [], 5));
				setQuarterSpecialQuantities({
					coalExcavationActualQuantity:
						monthRes.result.coalExcavationActualQuantity ?? 0,
					coalCrosscutActualQuantity:
						monthRes.result.coalCrosscutActualQuantity ?? 0,
					meterExcavationActualQuantity:
						monthRes.result.meterExcavationActualQuantity ?? 0,
					meterCrosscutActualQuantity:
						monthRes.result.meterCrosscutActualQuantity ?? 0,
				});
				setSpecialQuantityDraft({
					coalExcavationActualQuantity:
						monthRes.result.coalExcavationActualQuantity ?? 0,
					coalCrosscutActualQuantity:
						monthRes.result.coalCrosscutActualQuantity ?? 0,
				});
				setSpecialQuantityEditingField(null);
				setRevenueByMonth(monthRes.result.revenue ?? null);
				setCostByMonth(monthRes.result.cost ?? null);
				setSavingByMonth(monthRes.result.saving ?? null);
				setTransferredCostByMonth(monthRes.result.transferredCost ?? null);
				setQuarterBreakdown(monthRes.result.quarterBreakdown ?? null);
				setAcceptedSavingMonth(monthRes.result.acceptedSavingMonth ?? 0);
				setQuyetToanSavingsLimit(monthRes.result.quyetToanSavingsLimit ?? 0);
				setSavingAddedToIncomeMonth(
					monthRes.result.savingAddedToIncomeMonth ?? 0,
				);
				setSavingCarryForwardByMonths(
					monthRes.result.savingCarryForwardByMonths ?? [],
				);
				setSavingCarryForwardToNextMonths(
					monthRes.result.savingCarryForwardToNextMonths ?? 0,
				);
				setSavingCarryForwardToNextMonthsDraft(
					monthRes.result.savingCarryForwardToNextMonths ?? 0,
				);
				setSavingCarryForwardEditing(false);
				const selectedMonthNum = Number(payload.month);
				const monthCustomCosts = monthRes.result.customCosts ?? [];
				const quarterCustomCosts = (
					monthRes.result.quarterBreakdown?.customCosts ?? []
				).filter((item) => Number(item.month) !== selectedMonthNum);
				setCustomCosts([...monthCustomCosts, ...quarterCustomCosts]);
				setEditingSnapshot({});
			} catch (error) {
				console.error('Error fetching lump sum month list:', error);
				setFilteredData([]);
				setQuarterSpecialQuantities({
					coalExcavationActualQuantity: 0,
					coalCrosscutActualQuantity: 0,
					meterExcavationActualQuantity: 0,
					meterCrosscutActualQuantity: 0,
				});
				setSpecialQuantityDraft({
					coalExcavationActualQuantity: 0,
					coalCrosscutActualQuantity: 0,
				});
				setSpecialQuantityEditingField(null);
				setRevenueByMonth(null);
				setCostByMonth(null);
				setSavingByMonth(null);
				setTransferredCostByMonth(null);
				setQuarterBreakdown(null);
				setAcceptedSavingMonth(0);
				setQuyetToanSavingsLimit(0);
				setSavingAddedToIncomeMonth(0);
				setSavingCarryForwardByMonths([]);
				setSavingCarryForwardToNextMonths(0);
				setSavingCarryForwardToNextMonthsDraft(0);
				setSavingCarryForwardEditing(false);
				setCustomCosts([]);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const reloadCurrentMonth = useCallback(async () => {
		await fetchLumpSumMonth(getCurrentFilter());
	}, [fetchLumpSumMonth, getCurrentFilter]);

	const buildCustomCostRow = useCallback(
		(item: LumpSumQuarterCustomCost): LumpSumFinalSettlement => {
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
				isCustomCostRow: true,
				isEditing: item.id.startsWith('temp-') || !!editingSnapshot[item.id],
				month: item.month ? Number(item.month) : undefined,
				productName: item.customName || '',
				unitOfMeasureName: 'Đồng',
				plannedQuantity: undefined,
				actualQuantity: quantity,
				materials: { unitPrice: materialUnit, totalAmount: materialTotal },
				maintains: { unitPrice: maintainUnit, totalAmount: maintainTotal },
				electricities: {
					unitPrice: electricityUnit,
					totalAmount: electricityTotal,
				},
				totalAmount: materialTotal + maintainTotal + electricityTotal,
			};
		},
		[editingSnapshot],
	);

	const monthDisplayData = useMemo(() => {
		const selectedMonth = form.watch('month') || defaultMonth;
		const selectedYear = form.watch('year') || defaultYear;
		const customCostRowsByMonth = new Map<number, LumpSumFinalSettlement[]>();
		for (const item of customCosts) {
			const itemMonth = item.month ? Number(item.month) : undefined;
			if (!itemMonth) continue;
			const list = customCostRowsByMonth.get(itemMonth) ?? [];
			list.push(buildCustomCostRow(item));
			customCostRowsByMonth.set(itemMonth, list);
		}

		const currentMonthNum = Number(selectedMonth);
		const currentYearNum = Number(selectedYear);
		const prevMonthNum = currentMonthNum === 1 ? 12 : currentMonthNum - 1;
		const prevYearNum =
			currentMonthNum === 1 ? currentYearNum - 1 : currentYearNum;

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
				isSavingCarryForwardInputRow?: boolean;
				isEditing?: boolean;
				mergedValue?: number;
				isTransferredDefaultRow?: boolean;
			},
		): LumpSumFinalSettlement => ({
			sttLabel: options?.sttLabel,
			isBold: options?.isBold,
			excludeFromSummary: true,
			isMergedValueRow: options?.isMergedValueRow,
			isSavingCarryForwardInputRow: options?.isSavingCarryForwardInputRow,
			isEditing: options?.isEditing,
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
			makeZeroRow(`Doanh thu tháng ${selectedMonth}/${selectedYear}`, {
				sttLabel: 'I',
				isBold: true,
				unitOfMeasureName: 'Đồng',
				materialsTotalAmount: revenueByMonth?.materials?.totalAmount ?? 0,
				maintainsTotalAmount: revenueByMonth?.maintains?.totalAmount ?? 0,
				electricitiesTotalAmount:
					revenueByMonth?.electricities?.totalAmount ?? 0,
				totalAmount: revenueByMonth?.totalAmount ?? 0,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			makeZeroRow(`Chi phí tháng ${selectedMonth}/${selectedYear}`, {
				sttLabel: 'II',
				isBold: true,
				unitOfMeasureName: 'Đồng',
				materialsTotalAmount: costByMonth?.materials?.totalAmount ?? 0,
				maintainsTotalAmount: costByMonth?.maintains?.totalAmount ?? 0,
				electricitiesTotalAmount: costByMonth?.electricities?.totalAmount ?? 0,
				totalAmount: costByMonth?.totalAmount ?? 0,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			makeZeroRow(`Chi phí kết chuyển T${selectedMonth}/${selectedYear}`, {
				sttLabel: 'II.1',
				isTransferredDefaultRow: true,
				month: Number(selectedMonth),
				materialsTotalAmount:
					transferredCostByMonth?.materials?.totalAmount ?? 0,
				maintainsTotalAmount:
					transferredCostByMonth?.maintains?.totalAmount ?? 0,
				electricitiesTotalAmount:
					transferredCostByMonth?.electricities?.totalAmount ?? 0,
				totalAmount: transferredCostByMonth?.totalAmount ?? 0,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...(customCostRowsByMonth.get(currentMonthNum) ?? []),
			makeZeroRow(
				`Giá trị tiết kiệm(+)/bội chi(-) tháng ${selectedMonth}/${selectedYear}`,
				{
					sttLabel: 'III',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: savingByMonth?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: savingByMonth?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						savingByMonth?.electricities?.totalAmount ?? 0,
					totalAmount: savingByMonth?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				},
			),
			makeZeroRow(
				`Mức tiết kiệm theo quy định thanh quyết toán tháng ${selectedMonth}/${selectedYear}`,
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
				`Tổng giá trị tiết kiệm được chấp nhận tháng ${selectedMonth}/${selectedYear}`,
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
				`Giá trị tiết kiệm luân chuyển từ tháng ${prevMonthNum}/${prevYearNum} cộng/trừ vào thu nhập tháng ${selectedMonth}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue:
						currentMonthNum === 1
							? 0
							: savingCarryForwardByMonths
									.filter((x) => x.month <= prevMonthNum)
									.reduce((sum, x) => sum + (x.value ?? 0), 0),
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
					isSavingCarryForwardInputRow: true,
					isEditing: savingCarryForwardEditing,
					mergedValue: savingCarryForwardToNextMonthsDraft,
				},
			),
		];

		if (currentMonthNum % 3 === 0 && quarterBreakdown) {
			const quarterNum = Math.floor((currentMonthNum - 1) / 3) + 1;
			const romanQuarter =
				quarterNum === 1
					? 'I'
					: quarterNum === 2
						? 'II'
						: quarterNum === 3
							? 'III'
							: 'IV';
			const quarterStartMonth = (quarterNum - 1) * 3 + 1;
			const m1 = quarterStartMonth;
			const m2 = quarterStartMonth + 1;
			const m3 = quarterStartMonth + 2;

			const monthBreakdowns = quarterBreakdown.monthBreakdowns ?? [];
			const findMonthBreakdown = (targetMonth: number, fallbackIndex: number) =>
				monthBreakdowns.find(
					(x: LumpSumFinalSettlementMonthResponse) =>
						x?.revenue?.month === targetMonth ||
						x?.cost?.month === targetMonth ||
						x?.saving?.month === targetMonth,
				) ?? monthBreakdowns[fallbackIndex];

			const mb1 = findMonthBreakdown(m1, 0);
			const mb2 = findMonthBreakdown(m2, 1);
			const mb3 = findMonthBreakdown(m3, 2);

			const savingCarryForwardToNextQuarter = savingCarryForwardByMonths.reduce(
				(sum, x) => sum + (x.value ?? 0),
				0,
			);
			const savingAddedToIncomeQuarterAfterCarryForward =
				(quarterBreakdown.savingAddedToIncomeQuarter ?? 0) -
				savingCarryForwardToNextQuarter;
			const savingAddedToIncomeMonth1 = mb1?.savingAddedToIncomeMonth ?? 0;
			const savingAddedToIncomeMonth2 = mb2?.savingAddedToIncomeMonth ?? 0;
			const savingAddedToIncomeMonth3 =
				savingAddedToIncomeQuarterAfterCarryForward -
				savingAddedToIncomeMonth1 -
				savingAddedToIncomeMonth2;

			const quarterCustomCostRows1 = customCostRowsByMonth.get(m1) ?? [];
			const quarterCustomCostRows2 = customCostRowsByMonth.get(m2) ?? [];
			const quarterCustomCostRows3 = customCostRowsByMonth.get(m3) ?? [];

			defaultRows.push(
				makeZeroRow(`Doanh thu quý ${romanQuarter}/${selectedYear}`, {
					sttLabel: 'IV',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						quarterBreakdown.revenueQuarter?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						quarterBreakdown.revenueQuarter?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						quarterBreakdown.revenueQuarter?.electricities?.totalAmount ?? 0,
					totalAmount: quarterBreakdown.revenueQuarter?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m1}/${selectedYear}`, {
					sttLabel: 'IV.1',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb1?.revenue?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb1?.revenue?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb1?.revenue?.electricities?.totalAmount ?? 0,
					totalAmount: mb1?.revenue?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m2}/${selectedYear}`, {
					sttLabel: 'IV.2',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb2?.revenue?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb2?.revenue?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb2?.revenue?.electricities?.totalAmount ?? 0,
					totalAmount: mb2?.revenue?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m3}/${selectedYear}`, {
					sttLabel: 'IV.3',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb3?.revenue?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb3?.revenue?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb3?.revenue?.electricities?.totalAmount ?? 0,
					totalAmount: mb3?.revenue?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Chi phí quý ${romanQuarter}/${selectedYear}`, {
					sttLabel: 'V',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						quarterBreakdown.costQuarter?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						quarterBreakdown.costQuarter?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						quarterBreakdown.costQuarter?.electricities?.totalAmount ?? 0,
					totalAmount: quarterBreakdown.costQuarter?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m1}/${selectedYear}`, {
					sttLabel: 'V.1',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb1?.cost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb1?.cost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount: mb1?.cost?.electricities?.totalAmount ?? 0,
					totalAmount: mb1?.cost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Chi phí kết chuyển T${m1}/${selectedYear}`, {
					sttLabel: '-',
					isTransferredDefaultRow: true,
					month: m1,
					unitOfMeasureName: '',
					materialsTotalAmount:
						mb1?.transferredCost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						mb1?.transferredCost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb1?.transferredCost?.electricities?.totalAmount ?? 0,
					totalAmount: mb1?.transferredCost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				...quarterCustomCostRows1,
				makeZeroRow(`Tháng ${m2}/${selectedYear}`, {
					sttLabel: 'V.2',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb2?.cost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb2?.cost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount: mb2?.cost?.electricities?.totalAmount ?? 0,
					totalAmount: mb2?.cost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Chi phí kết chuyển T${m2}/${selectedYear}`, {
					sttLabel: '-',
					isTransferredDefaultRow: true,
					month: m2,
					unitOfMeasureName: '',
					materialsTotalAmount:
						mb2?.transferredCost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						mb2?.transferredCost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb2?.transferredCost?.electricities?.totalAmount ?? 0,
					totalAmount: mb2?.transferredCost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				...quarterCustomCostRows2,
				makeZeroRow(`Tháng ${m3}/${selectedYear}`, {
					sttLabel: 'V.3',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb3?.cost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb3?.cost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount: mb3?.cost?.electricities?.totalAmount ?? 0,
					totalAmount: mb3?.cost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Chi phí kết chuyển T${m3}/${selectedYear}`, {
					sttLabel: '-',
					isTransferredDefaultRow: true,
					month: m3,
					unitOfMeasureName: '',
					materialsTotalAmount:
						mb3?.transferredCost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						mb3?.transferredCost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb3?.transferredCost?.electricities?.totalAmount ?? 0,
					totalAmount: mb3?.transferredCost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				...quarterCustomCostRows3,
				makeZeroRow(
					`Giá trị tiết kiệm, bội chi quý ${romanQuarter}/${selectedYear}`,
					{
						sttLabel: 'VI',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						materialsTotalAmount:
							quarterBreakdown.savingQuarter?.materials?.totalAmount ?? 0,
						maintainsTotalAmount:
							quarterBreakdown.savingQuarter?.maintains?.totalAmount ?? 0,
						electricitiesTotalAmount:
							quarterBreakdown.savingQuarter?.electricities?.totalAmount ?? 0,
						totalAmount: quarterBreakdown.savingQuarter?.totalAmount ?? 0,
						hidePlanActual: true,
						hideUnitPrice: true,
					},
				),
				makeZeroRow(`Tháng ${m1}/${selectedYear}`, {
					sttLabel: 'VI.1',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb1?.saving?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb1?.saving?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb1?.saving?.electricities?.totalAmount ?? 0,
					totalAmount: mb1?.saving?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m2}/${selectedYear}`, {
					sttLabel: 'VI.2',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb2?.saving?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb2?.saving?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb2?.saving?.electricities?.totalAmount ?? 0,
					totalAmount: mb2?.saving?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Tháng ${m3}/${selectedYear}`, {
					sttLabel: 'VI.3',
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount: mb3?.saving?.materials?.totalAmount ?? 0,
					maintainsTotalAmount: mb3?.saving?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						mb3?.saving?.electricities?.totalAmount ?? 0,
					totalAmount: mb3?.saving?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(
					`Tổng giá trị tiết kiệm được chấp nhận quý ${romanQuarter}/${selectedYear}`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: quarterBreakdown.acceptedSavingQuarter ?? 0,
					},
				),
				makeZeroRow(
					`Giá trị tiết kiệm được cộng vào thu nhập quý ${romanQuarter}/${selectedYear}`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: quarterBreakdown.savingAddedToIncomeQuarter ?? 0,
					},
				),
				makeZeroRow(
					`Luân chuyển giá trị tiết kiệm quý ${romanQuarter}/${selectedYear} sang quý sau`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: savingCarryForwardToNextQuarter,
					},
				),
				makeZeroRow(
					`Giá trị tiết kiệm được cộng vào thu nhập quý ${romanQuarter}/${selectedYear} (Sau luân chuyển)`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: savingAddedToIncomeQuarterAfterCarryForward,
					},
				),
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${m1}/${selectedYear}`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: savingAddedToIncomeMonth1,
					},
				),
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${m2}/${selectedYear}`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: savingAddedToIncomeMonth2,
					},
				),
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${m3}/${selectedYear}`,
					{
						sttLabel: '*',
						isBold: true,
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue: savingAddedToIncomeMonth3,
					},
				),
			);
		}

		const specialRows: LumpSumFinalSettlement[] = [
			{
				sttLabel: '1',
				productName: 'Than đào lò',
				unitOfMeasureName: 'Tấn',
				isBold: true,
				isSpecialQuantityRow: true,
				specialQuantityField: 'coalExcavationActualQuantity',
				isEditing:
					specialQuantityEditingField === 'coalExcavationActualQuantity',
				plannedQuantity: undefined,
				actualQuantity:
					specialQuantityEditingField === 'coalExcavationActualQuantity'
						? specialQuantityDraft.coalExcavationActualQuantity
						: quarterSpecialQuantities.coalExcavationActualQuantity,
				excludeFromSummary: true,
			},
			{
				sttLabel: '2',
				productName: 'Than xén lò',
				unitOfMeasureName: 'Tấn',
				isBold: true,
				isSpecialQuantityRow: true,
				specialQuantityField: 'coalCrosscutActualQuantity',
				isEditing: specialQuantityEditingField === 'coalCrosscutActualQuantity',
				plannedQuantity: undefined,
				actualQuantity:
					specialQuantityEditingField === 'coalCrosscutActualQuantity'
						? specialQuantityDraft.coalCrosscutActualQuantity
						: quarterSpecialQuantities.coalCrosscutActualQuantity,
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

		return [...specialRows, ...filteredData, ...defaultRows];
	}, [
		acceptedSavingMonth,
		buildCustomCostRow,
		costByMonth,
		customCosts,
		defaultMonth,
		defaultYear,
		filteredData,
		form,
		quarterBreakdown,
		quarterSpecialQuantities,
		revenueByMonth,
		savingAddedToIncomeMonth,
		savingCarryForwardByMonths,
		savingCarryForwardEditing,
		savingCarryForwardToNextMonthsDraft,
		savingByMonth,
		specialQuantityDraft,
		specialQuantityEditingField,
		transferredCostByMonth,
		quyetToanSavingsLimit,
	]);

	useEffect(() => {
		const fetchDepartments = async () => {
			try {
				const response = await api.pagging<Department>(
					API.CATALOG.DEPARTMENT.LIST,
					{ ignorePagination: true },
				);
				setDepartments([
					{ value: '', label: 'Tất cả đơn vị' },
					...(response.result.data ?? []).map((item) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				]);
			} catch (error) {
				console.error('Error fetching departments:', error);
			}
		};

		const fetchProcessGroups = async () => {
			try {
				const response = await api.pagging<ProcessGroup>(
					API.CATALOG.PROCESS.GROUP.LIST,
					{ ignorePagination: true },
				);
				setProcessGroups([
					{ value: '', label: 'Tất cả nhóm công đoạn' },
					...(response.result.data ?? []).map((item) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				]);
			} catch (error) {
				console.error('Error fetching process groups:', error);
			}
		};

		fetchDepartments();
		fetchProcessGroups();
	}, []);

	const saveCustomCost = useCallback(
		async (row: LumpSumFinalSettlement) => {
			const target = customCosts.find((x) => x.id === row.id);
			if (!target) {
				return;
			}

			const { month, year, processGroupId } = getCurrentFilter();
			const payload: UpsertLumpSumQuarterCustomCostRequest = {
				id: target.id.startsWith('temp-') ? undefined : target.id,
				month: String(
					target.month ? Number(target.month) : (row.month ?? Number(month)),
				),
				year,
				processGroupId: processGroupId ?? '',
				actualQuantity: target.actualQuantity || 0,
				customName: target.customName || row.productName || '',
				materialUnitPrice: target.materialUnitPrice || 0,
				maintainUnitPrice: target.maintainUnitPrice || 0,
				electricityUnitPrice: target.electricityUnitPrice || 0,
			};

			try {
				if (payload.id) {
					await api.put<boolean, UpsertLumpSumQuarterCustomCostRequest>(
						API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_CUSTOM_COST_UPDATE,
						payload,
					);
				} else {
					await api.post<boolean, UpsertLumpSumQuarterCustomCostRequest>(
						API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_CUSTOM_COST_CREATE,
						payload,
					);
				}
				const tempRowsBeforeReload = customCosts.filter(
					(x) => x.id !== row.id && x.id.startsWith('temp-'),
				);

				await reloadCurrentMonth();

				if (tempRowsBeforeReload.length > 0) {
					setCustomCosts((prev) => [...prev, ...tempRowsBeforeReload]);
				}

				setEditingSnapshot((prev) => {
					if (!row.id) return prev;
					const next = { ...prev };
					delete next[row.id];
					return next;
				});
			} catch (error) {
				console.error('Error saving custom cost:', error);
			}
		},
		[customCosts, getCurrentFilter, reloadCurrentMonth],
	);

	const deleteCustomCost = useCallback(
		async (row: LumpSumFinalSettlement) => {
			if (!row.id) return;
			const id = row.id;
			if (id.startsWith('temp-')) {
				setCustomCosts((prev) => prev.filter((x) => x.id !== id));
				setEditingSnapshot((prev) => {
					const next = { ...prev };
					delete next[id];
					return next;
				});
				return;
			}
			try {
				await api.delete<boolean, undefined>(
					API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_CUSTOM_COST_DELETE(id),
				);
				await reloadCurrentMonth();
			} catch (error) {
				console.error('Error deleting custom cost:', error);
			}
		},
		[reloadCurrentMonth],
	);

	const addCustomCostRow = useCallback(
		(row?: LumpSumFinalSettlement) => {
			const { month, year, processGroupId } = getCurrentFilter();
			const tempId = `temp-${Date.now()}`;
			setCustomCosts((prev) => [
				...prev,
				{
					id: tempId,
					month: String(row?.month ?? Number(month)),
					year,
					processGroupId: processGroupId ?? '',
					customName: '',
					actualQuantity: 0,
					materialUnitPrice: 0,
					maintainUnitPrice: 0,
					electricityUnitPrice: 0,
				},
			]);
		},
		[getCurrentFilter],
	);

	const editCustomCost = useCallback(
		(row: LumpSumFinalSettlement) => {
			if (!row.id || row.id.startsWith('temp-')) return;
			setEditingSnapshot((prev) => {
				const existing = customCosts.find((x) => x.id === row.id);
				if (!existing) return prev;
				return { ...prev, [row.id!]: { ...existing } };
			});
		},
		[customCosts],
	);

	const cancelCustomCost = useCallback((row: LumpSumFinalSettlement) => {
		if (!row.id) return;
		const id = row.id;
		if (id.startsWith('temp-')) {
			setCustomCosts((prev) => prev.filter((x) => x.id !== id));
			return;
		}
		setEditingSnapshot((prev) => {
			const snapshot = prev[id];
			if (!snapshot) return prev;
			setCustomCosts((old) =>
				old.map((x) => (x.id === id ? { ...snapshot } : x)),
			);
			const next = { ...prev };
			delete next[id];
			return next;
		});
	}, []);

	const changeCustomCostValue = useCallback(
		(
			row: LumpSumFinalSettlement,
			field:
				| 'customName'
				| 'actualQuantity'
				| 'materialUnitPrice'
				| 'maintainUnitPrice'
				| 'electricityUnitPrice',
			value: number | string,
		) => {
			if (!row.id) return;
			setCustomCosts((prev) =>
				prev.map((x) => {
					if (x.id !== row.id) return x;
					if (field === 'customName')
						return { ...x, customName: String(value) };
					return { ...x, [field]: Number(value) };
				}),
			);
		},
		[],
	);

	const editSpecialQuantity = useCallback(
		(row: LumpSumFinalSettlement) => {
			if (!row.specialQuantityField) return;
			setSpecialQuantityDraft({
				coalExcavationActualQuantity:
					quarterSpecialQuantities.coalExcavationActualQuantity,
				coalCrosscutActualQuantity:
					quarterSpecialQuantities.coalCrosscutActualQuantity,
			});
			setSpecialQuantityEditingField(row.specialQuantityField);
		},
		[quarterSpecialQuantities],
	);

	const cancelSpecialQuantity = useCallback(() => {
		setSpecialQuantityDraft({
			coalExcavationActualQuantity:
				quarterSpecialQuantities.coalExcavationActualQuantity,
			coalCrosscutActualQuantity:
				quarterSpecialQuantities.coalCrosscutActualQuantity,
		});
		setSpecialQuantityEditingField(null);
	}, [quarterSpecialQuantities]);

	const changeSpecialQuantityValue = useCallback(
		(row: LumpSumFinalSettlement, value: number) => {
			if (!row.specialQuantityField) return;
			if (row.specialQuantityField === 'coalExcavationActualQuantity') {
				setSpecialQuantityDraft((prev) => ({
					...prev,
					coalExcavationActualQuantity: Number(value ?? 0),
				}));
				return;
			}
			setSpecialQuantityDraft((prev) => ({
				...prev,
				coalCrosscutActualQuantity: Number(value ?? 0),
			}));
		},
		[],
	);

	const saveSpecialQuantity = useCallback(
		async (row: LumpSumFinalSettlement) => {
			void row;
			const { month, year, processGroupId } = getCurrentFilter();
			const payload: UpdateLumpSumMonthSpecialQuantityRequest = {
				month,
				year,
				processGroupId: processGroupId ?? '',
				coalExcavationActualQuantity:
					specialQuantityDraft.coalExcavationActualQuantity || 0,
				coalCrosscutActualQuantity:
					specialQuantityDraft.coalCrosscutActualQuantity || 0,
			};
			try {
				await api.put<boolean, UpdateLumpSumMonthSpecialQuantityRequest>(
					API.COST.LUMP_SUM_FINAL_SETTLEMENT.MONTH_SPECIAL_QUANTITY_UPDATE,
					payload,
				);
				await reloadCurrentMonth();
			} catch (error) {
				console.error('Error saving month special quantities:', error);
			}
		},
		[getCurrentFilter, reloadCurrentMonth, specialQuantityDraft],
	);

	const changeSavingCarryForwardValue = useCallback(
		(row: LumpSumFinalSettlement, value: number) => {
			void row;
			setSavingCarryForwardToNextMonthsDraft(Number(value ?? 0));
		},
		[],
	);

	const saveSavingCarryForwardValue = useCallback(
		async (row: LumpSumFinalSettlement) => {
			void row;
			const { month, year, processGroupId } = getCurrentFilter();
			const payload: UpdateLumpSumMonthCarryForwardRequest = {
				month,
				year,
				processGroupId: processGroupId ?? '',
				savingCarryForwardToNextMonths:
					savingCarryForwardToNextMonthsDraft || 0,
			};
			try {
				await api.put<boolean, UpdateLumpSumMonthCarryForwardRequest>(
					API.COST.LUMP_SUM_FINAL_SETTLEMENT.MONTH_CARRY_FORWARD_UPDATE,
					payload,
				);
				setSavingCarryForwardEditing(false);
				await reloadCurrentMonth();
			} catch (error) {
				console.error('Error saving month carry forward value:', error);
			}
		},
		[getCurrentFilter, reloadCurrentMonth, savingCarryForwardToNextMonthsDraft],
	);

	const editSavingCarryForwardValue = useCallback(
		(row: LumpSumFinalSettlement) => {
			void row;
			setSavingCarryForwardEditing(true);
		},
		[],
	);

	const cancelSavingCarryForwardValue = useCallback(
		(row: LumpSumFinalSettlement) => {
			void row;
			setSavingCarryForwardToNextMonthsDraft(savingCarryForwardToNextMonths);
			setSavingCarryForwardEditing(false);
		},
		[savingCarryForwardToNextMonths],
	);

	const handleFilter = useCallback(
		(data: YearFilterForm) => {
			if (!data.month || !data.year) return;
			void fetchLumpSumMonth({
				month: data.month,
				year: data.year,
				processGroupId: data.processGroup || null,
				departmentId: data.department || null,
			});
		},
		[fetchLumpSumMonth],
	);

	useEffect(() => {
		const subscription = form.watch((value) => {
			if (value.month && value.year) {
				handleFilter({
					month: value.month,
					year: value.year,
					processGroup: value.processGroup ?? '',
					department: value.department ?? '',
				});
			}
		});
		return () => subscription.unsubscribe();
	}, [form, handleFilter]);

	useEffect(() => {
		const value = form.getValues();
		if (value.month && value.year) {
			handleFilter({
				month: value.month,
				year: value.year,
				processGroup: value.processGroup ?? '',
				department: value.department ?? '',
			});
		}
	}, [form, handleFilter]);

	return (
		<Card>
			<CardHeader>
				<FormProvider context={form} onSubmit={handleFilter}>
					<div className='flex items-end justify-between gap-4'>
						<div className='grid w-full max-w-4xl flex-1 grid-cols-1 gap-4 md:grid-cols-4'>
							<FormMonthYear
								control={form.control}
								month='month'
								year='year'
								label='Thời gian'
								placeholder='Chọn thời gian'
							/>
							<FormComboBox
								control={form.control}
								name='processGroup'
								label='Nhóm công đoạn sản xuất'
								placeholder='Tất cả nhóm công đoạn'
								options={processGroups}
							/>
							<FormComboBox
								control={form.control}
								name='department'
								label='Đơn vị'
								placeholder='Tất cả đơn vị'
								options={departments}
							/>
						</div>
						<div className='flex shrink-0 gap-4'>
							{hasPermission('report.lumpsumfinalsettlement.export') && (
								<Button variant={'ghost'} className={shadow}>
									<DownloadIcon fontSize='small' />
									<span className='hidden xl:block'>Tải xuống</span>
								</Button>
							)}
							<Button variant={'ghost'} className={shadow}>
								<PrintIcon fontSize='small' />
								<span className='hidden xl:block'>In</span>
							</Button>
							<Button variant={'ghost'} className={shadow}>
								<EmailIcon fontSize='small' />
								<span className='hidden xl:block'>Gửi</span>
							</Button>
						</div>
					</div>
				</FormProvider>
			</CardHeader>
			<CardContent>
				<LumpSumDataTable
					columns={LUMP_SUM_FINAL_SETTLEMENT_COLUMNS}
					data={monthDisplayData}
					isLoading={isLoading}
					onAddCustomCost={
						hasPermission('production.lumpsumfinalsettlement.create')
							? addCustomCostRow
							: undefined
					}
					onEditCustomCost={
						hasPermission('production.lumpsumfinalsettlement.update')
							? editCustomCost
							: undefined
					}
					onCancelCustomCost={cancelCustomCost}
					onSaveCustomCost={
						hasPermission('production.lumpsumfinalsettlement.update')
							? saveCustomCost
							: undefined
					}
					onDeleteCustomCost={
						hasPermission('production.lumpsumfinalsettlement.delete')
							? deleteCustomCost
							: undefined
					}
					onCustomCostChange={changeCustomCostValue}
					onEditSpecialQuantity={
						hasPermission('production.lumpsumfinalsettlement.update')
							? editSpecialQuantity
							: undefined
					}
					onCancelSpecialQuantity={cancelSpecialQuantity}
					onSaveSpecialQuantity={
						hasPermission('production.lumpsumfinalsettlement.update')
							? saveSpecialQuantity
							: undefined
					}
					onSpecialQuantityChange={changeSpecialQuantityValue}
					onSavingCarryForwardChange={changeSavingCarryForwardValue}
					onSaveSavingCarryForward={
						hasPermission('production.lumpsumfinalsettlement.update')
							? saveSavingCarryForwardValue
							: undefined
					}
					onEditSavingCarryForward={
						hasPermission('production.lumpsumfinalsettlement.update')
							? editSavingCarryForwardValue
							: undefined
					}
					onCancelSavingCarryForward={cancelSavingCarryForwardValue}
				/>
			</CardContent>
		</Card>
	);
}
