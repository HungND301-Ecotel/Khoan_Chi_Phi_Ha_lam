import { FormComboBox } from '@/components/form/form-combo-box';
import { FormQuaterYear } from '@/components/form-quater-year/form-quater-year';
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
	LumpSumFinalSettlementMonthResponse,
	LumpSumFinalSettlementQuarterListRequest,
	LumpSumFinalSettlementQuarterResponse,
	LumpSumQuarterCustomCost,
	ProcessGroup,
	QuarterFilterForm,
	UpsertLumpSumQuarterCustomCostRequest,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import EmailIcon from '@mui/icons-material/Email';
import PrintIcon from '@mui/icons-material/Print';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

const shadow = cn(
	'hover:shadow-[0px_2px_4px_-1px_rgba(0,0,0,0.2),0px_4px_5px_0px_rgba(0,0,0,0.14),0px_1px_10px_0px_rgba(0,0,0,0.12)] shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]',
);

export function MainCostLumpSumFinalSettlementQuarterPage() {
	const [filteredData, setFilteredData] = useState<LumpSumFinalSettlement[]>(
		[],
	);
	const [quarterSpecialQuantities, setQuarterSpecialQuantities] = useState({
		coalExcavationActualQuantity: 0,
		coalCrosscutActualQuantity: 0,
		meterExcavationActualQuantity: 0,
		meterCrosscutActualQuantity: 0,
	});
	const [monthBreakdowns, setMonthBreakdowns] = useState<
		LumpSumFinalSettlementMonthResponse[]
	>([]);
	const [quarterAcceptedSaving, setQuarterAcceptedSaving] = useState(0);
	const [quarterAddedToIncome, setQuarterAddedToIncome] = useState(0);
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
	const defaultYear = String(now.getFullYear());
	const defaultQuarter = String(Math.floor(now.getMonth() / 3) + 1);

	const form = useForm<QuarterFilterForm>({
		defaultValues: {
			quarter: defaultQuarter,
			year: defaultYear,
			processGroup: '',
			department: '',
		},
	});
	const watchedQuarter = form.watch('quarter');
	const watchedYear = form.watch('year');

	const getCurrentFilter = useCallback(() => {
		const selectedQuarter = form.getValues('quarter') || defaultQuarter;
		const selectedYear = form.getValues('year') || defaultYear;
		return {
			quarter: selectedQuarter,
			month: String(quarterToStartMonth(selectedQuarter)),
			year: selectedYear,
			processGroupId: form.getValues('processGroup') || null,
			departmentId: form.getValues('department') || null,
		};
	}, [defaultQuarter, defaultYear, form]);

	const fetchLumpSumQuarter = useCallback(
		async (payload: {
			quarter: string;
			year: string;
			processGroupId?: string | null;
			departmentId?: string | null;
		}) => {
			setIsLoading(true);
			try {
				const res = await api.post<
					LumpSumFinalSettlementQuarterResponse,
					LumpSumFinalSettlementQuarterListRequest
				>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_LIST, payload);
				setFilteredData(groupByProcessGroup(res.result.items ?? [], 5));
				setQuarterSpecialQuantities({
					coalExcavationActualQuantity:
						res.result.coalExcavationActualQuantity ?? 0,
					coalCrosscutActualQuantity:
						res.result.coalCrosscutActualQuantity ?? 0,
					meterExcavationActualQuantity:
						res.result.meterExcavationActualQuantity ?? 0,
					meterCrosscutActualQuantity:
						res.result.meterCrosscutActualQuantity ?? 0,
				});
				setMonthBreakdowns(
					(res.result.monthBreakdowns ?? [])
						.slice()
						.sort((a, b) => (a.revenue?.month ?? 0) - (b.revenue?.month ?? 0)),
				);
				setQuarterAcceptedSaving(res.result.acceptedSavingQuarter ?? 0);
				setQuarterAddedToIncome(res.result.savingAddedToIncomeQuarter ?? 0);
				setCustomCosts(res.result.customCosts ?? []);
				setEditingSnapshot({});
			} catch (error) {
				console.error('Error fetching lump sum quarter list:', error);
				setFilteredData([]);
				setQuarterSpecialQuantities({
					coalExcavationActualQuantity: 0,
					coalCrosscutActualQuantity: 0,
					meterExcavationActualQuantity: 0,
					meterCrosscutActualQuantity: 0,
				});
				setMonthBreakdowns([]);
				setQuarterAcceptedSaving(0);
				setQuarterAddedToIncome(0);
				setCustomCosts([]);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const reloadCurrentQuarter = useCallback(async () => {
		const { quarter, year, processGroupId, departmentId } = getCurrentFilter();
		await fetchLumpSumQuarter({ quarter, year, processGroupId, departmentId });
	}, [fetchLumpSumQuarter, getCurrentFilter]);

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
				productName: item.customName || ``,
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

	const quarterDisplayData = useMemo(() => {
		const selectedQuarter = watchedQuarter || defaultQuarter;
		const selectedYear = watchedYear || defaultYear;
		const quarterRoman = toRomanQuarter(selectedQuarter);
		const months = getMonthsForQuarter(selectedQuarter);
		const monthBreakdownByMonth = new Map<
			number,
			LumpSumFinalSettlementMonthResponse
		>(
			monthBreakdowns.map((x, index) => [
				x.revenue?.month ?? months[index] ?? 0,
				x,
			]),
		);

		const customCostRowsByMonth = new Map<number, LumpSumFinalSettlement[]>();
		for (const item of customCosts) {
			const month = Number(item.month ?? 0);
			if (!month) continue;
			const rows = customCostRowsByMonth.get(month) ?? [];
			rows.push(buildCustomCostRow(item));
			customCostRowsByMonth.set(month, rows);
		}

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

		const revenueQuarter = monthBreakdowns.reduce(
			(acc, month) => ({
				materials: acc.materials + (month.revenue?.materials?.totalAmount ?? 0),
				maintains: acc.maintains + (month.revenue?.maintains?.totalAmount ?? 0),
				electricities:
					acc.electricities + (month.revenue?.electricities?.totalAmount ?? 0),
				total: acc.total + (month.revenue?.totalAmount ?? 0),
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		const costQuarter = monthBreakdowns.reduce(
			(acc, month) => ({
				materials: acc.materials + (month.cost?.materials?.totalAmount ?? 0),
				maintains: acc.maintains + (month.cost?.maintains?.totalAmount ?? 0),
				electricities:
					acc.electricities + (month.cost?.electricities?.totalAmount ?? 0),
				total: acc.total + (month.cost?.totalAmount ?? 0),
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		const savingQuarter = monthBreakdowns.reduce(
			(acc, month) => ({
				materials: acc.materials + (month.saving?.materials?.totalAmount ?? 0),
				maintains: acc.maintains + (month.saving?.maintains?.totalAmount ?? 0),
				electricities:
					acc.electricities + (month.saving?.electricities?.totalAmount ?? 0),
				total: acc.total + (month.saving?.totalAmount ?? 0),
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		// ĐÂY LÀ ĐOẠN TỚ THÊM VÀO: Tính tổng quyetToanSavingsLimit của 3 tháng
		const quyetToanSavingsLimitQuarter = monthBreakdowns.reduce(
			(acc, month) => acc + (month.quyetToanSavingsLimit ?? 0),
			0,
		);

		const defaultRows: LumpSumFinalSettlement[] = [
			makeZeroRow(`Doanh thu quý ${quarterRoman}/${selectedYear}`, {
				sttLabel: 'I',
				isBold: true,
				materialsTotalAmount: revenueQuarter.materials,
				maintainsTotalAmount: revenueQuarter.maintains,
				electricitiesTotalAmount: revenueQuarter.electricities,
				totalAmount: revenueQuarter.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.map((m, idx) =>
				makeZeroRow(`Tháng ${m}/${selectedYear}`, {
					sttLabel: `I.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						monthBreakdownByMonth.get(m)?.revenue?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						monthBreakdownByMonth.get(m)?.revenue?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						monthBreakdownByMonth.get(m)?.revenue?.electricities?.totalAmount ??
						0,
					totalAmount: monthBreakdownByMonth.get(m)?.revenue?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),
			makeZeroRow(`Chi phí quý ${quarterRoman}/${selectedYear}`, {
				sttLabel: 'II',
				isBold: true,
				materialsTotalAmount: costQuarter.materials,
				maintainsTotalAmount: costQuarter.maintains,
				electricitiesTotalAmount: costQuarter.electricities,
				totalAmount: costQuarter.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.flatMap((m, idx) => [
				makeZeroRow(`Tháng ${m}/${selectedYear}`, {
					sttLabel: `II.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						monthBreakdownByMonth.get(m)?.cost?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						monthBreakdownByMonth.get(m)?.cost?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						monthBreakdownByMonth.get(m)?.cost?.electricities?.totalAmount ?? 0,
					totalAmount: monthBreakdownByMonth.get(m)?.cost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				makeZeroRow(`Chi phí kết chuyển T${m}/${selectedYear}`, {
					sttLabel: '-',
					isTransferredDefaultRow: true,
					month: m,
					materialsTotalAmount:
						monthBreakdownByMonth.get(m)?.transferredCost?.materials
							?.totalAmount ?? 0,
					maintainsTotalAmount:
						monthBreakdownByMonth.get(m)?.transferredCost?.maintains
							?.totalAmount ?? 0,
					electricitiesTotalAmount:
						monthBreakdownByMonth.get(m)?.transferredCost?.electricities
							?.totalAmount ?? 0,
					totalAmount:
						monthBreakdownByMonth.get(m)?.transferredCost?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
				...(customCostRowsByMonth.get(m) ?? []),
			]),
			makeZeroRow(
				`Giá trị tiết kiệm, bội chi quý ${quarterRoman}/${selectedYear}`,
				{
					sttLabel: 'III',
					isBold: true,
					materialsTotalAmount: savingQuarter.materials,
					maintainsTotalAmount: savingQuarter.maintains,
					electricitiesTotalAmount: savingQuarter.electricities,
					totalAmount: savingQuarter.total,
					hidePlanActual: true,
					hideUnitPrice: true,
				},
			),
			...months.map((m, idx) =>
				makeZeroRow(`Tháng ${m}/${selectedYear}`, {
					sttLabel: `III.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					materialsTotalAmount:
						monthBreakdownByMonth.get(m)?.saving?.materials?.totalAmount ?? 0,
					maintainsTotalAmount:
						monthBreakdownByMonth.get(m)?.saving?.maintains?.totalAmount ?? 0,
					electricitiesTotalAmount:
						monthBreakdownByMonth.get(m)?.saving?.electricities?.totalAmount ??
						0,
					totalAmount: monthBreakdownByMonth.get(m)?.saving?.totalAmount ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),

			// ĐÂY LÀ ĐOẠN TỚ THÊM VÀO: Row hiển thị mức tiết kiệm thanh quyết toán của Quý
			makeZeroRow(
				`Mức tiết kiệm theo quy định thanh quyết toán quý ${quarterRoman}/${selectedYear}`,
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
				`Tổng giá trị tiết kiệm được chấp nhận quý ${quarterRoman}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: quarterAcceptedSaving,
				},
			),
			makeZeroRow(
				`Giá trị tiết kiệm được cộng vào thu nhập quý ${quarterRoman}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: quarterAddedToIncome,
				},
			),
			...months.map((m) =>
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${m}/${selectedYear}`,
					{
						sttLabel: '*',
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue:
							monthBreakdownByMonth.get(m)?.savingAddedToIncomeMonth ?? 0,
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

		return [...specialRows, ...filteredData, ...defaultRows];
	}, [
		buildCustomCostRow,
		customCosts,
		defaultQuarter,
		defaultYear,
		filteredData,
		monthBreakdowns,
		quarterAcceptedSaving,
		quarterAddedToIncome,
		quarterSpecialQuantities,
		watchedQuarter,
		watchedYear,
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
			if (!target) return;

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

				await reloadCurrentQuarter();

				if (tempRowsBeforeReload.length > 0) {
					setCustomCosts((prev) => [...prev, ...tempRowsBeforeReload]);
				}
			} catch (error) {
				console.error('Error saving custom cost:', error);
			}
		},
		[customCosts, getCurrentFilter, reloadCurrentQuarter],
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
				await reloadCurrentQuarter();
			} catch (error) {
				console.error('Error deleting custom cost:', error);
			}
		},
		[reloadCurrentQuarter],
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

	const handleFilter = useCallback(
		(data: QuarterFilterForm) => {
			if (!data.quarter || !data.year) return;
			void fetchLumpSumQuarter({
				quarter: data.quarter,
				year: data.year,
				processGroupId: data.processGroup || null,
				departmentId: data.department || null,
			});
		},
		[fetchLumpSumQuarter],
	);

	useEffect(() => {
		const subscription = form.watch((value) => {
			if (value.quarter && value.year) {
				handleFilter({
					quarter: value.quarter,
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
		if (value.quarter && value.year) {
			handleFilter({
				quarter: value.quarter,
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
							<FormQuaterYear
								control={form.control}
								quarter='quarter'
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
							<Button variant={'ghost'} className={shadow}>
								<DownloadIcon fontSize='small' />
								<span className='hidden xl:block'>Tải xuống</span>
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
					</div>
				</FormProvider>
			</CardHeader>
			<CardContent>
				<LumpSumDataTable
					columns={LUMP_SUM_FINAL_SETTLEMENT_COLUMNS}
					data={quarterDisplayData}
					isLoading={isLoading}
					onAddCustomCost={addCustomCostRow}
					onEditCustomCost={editCustomCost}
					onCancelCustomCost={cancelCustomCost}
					onSaveCustomCost={saveCustomCost}
					onDeleteCustomCost={deleteCustomCost}
					onCustomCostChange={changeCustomCostValue}
				/>
			</CardContent>
		</Card>
	);
}

function quarterToStartMonth(quarter: string | number) {
	return ((Number(quarter) || 1) - 1) * 3 + 1;
}

function getMonthsForQuarter(quarter: string | number) {
	const startMonth = quarterToStartMonth(quarter);
	return [startMonth, startMonth + 1, startMonth + 2];
}

function toRomanQuarter(quarter: string | number) {
	const quarterNumber = Number(quarter);
	if (quarterNumber === 1) return 'I';
	if (quarterNumber === 2) return 'II';
	if (quarterNumber === 3) return 'III';
	if (quarterNumber === 4) return 'IV';
	return String(quarter);
}
