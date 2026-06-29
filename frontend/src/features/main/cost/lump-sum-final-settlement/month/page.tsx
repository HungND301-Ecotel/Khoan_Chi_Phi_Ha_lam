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
	const [acceptedSavingMonth, setAcceptedSavingMonth] = useState(0);
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
				setAcceptedSavingMonth(monthRes.result.acceptedSavingMonth ?? 0);
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
				setCustomCosts(monthRes.result.customCosts ?? []);
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
				setAcceptedSavingMonth(0);
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
		const customCostRows = customCosts.map((item) => buildCustomCostRow(item));

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
			...customCostRows,
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
				`Giá trị tiết kiệm được cộng vào thu nhập tháng ${selectedMonth}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: savingAddedToIncomeMonth,
				},
			),
			makeZeroRow(
				`Giá trị tiết kiệm được cộng/trừ vào thu nhập luân chuyển tháng ${selectedMonth}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: savingCarryForwardByMonths
						.filter((x) => x.month < Number(selectedMonth))
						.reduce((acc, x) => acc + (x.value ?? 0), 0),
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
				// Giữ lại các temp rows chưa được lưu
				const tempRowsBeforeReload = customCosts.filter(
					(x) => x.id !== row.id && x.id.startsWith('temp-'),
				);

				await reloadCurrentMonth();

				// hiện lên
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
					data={monthDisplayData}
					isLoading={isLoading}
					onAddCustomCost={addCustomCostRow}
					onEditCustomCost={editCustomCost}
					onCancelCustomCost={cancelCustomCost}
					onSaveCustomCost={saveCustomCost}
					onDeleteCustomCost={deleteCustomCost}
					onCustomCostChange={changeCustomCostValue}
					onEditSpecialQuantity={editSpecialQuantity}
					onCancelSpecialQuantity={cancelSpecialQuantity}
					onSaveSpecialQuantity={saveSpecialQuantity}
					onSpecialQuantityChange={changeSpecialQuantityValue}
					onSavingCarryForwardChange={changeSavingCarryForwardValue}
					onSaveSavingCarryForward={saveSavingCarryForwardValue}
					onEditSavingCarryForward={editSavingCarryForwardValue}
					onCancelSavingCarryForward={cancelSavingCarryForwardValue}
				/>
			</CardContent>
		</Card>
	);
}
