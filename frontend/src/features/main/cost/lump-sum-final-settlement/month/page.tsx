import { FormMonthYear } from '@/components/form-month-year/form-month-year';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { API } from '@/constants/api-enpoint';
import { LUMP_SUM_FINAL_SETTLEMENT_COLUMNS } from '@/features/main/cost/lump-sum-final-settlement/columns';
import { LumpSumDataTable } from '@/features/main/cost/lump-sum-final-settlement/components/datatable';
import { groupByProcessGroup } from '@/features/main/cost/lump-sum-final-settlement/grouping';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementMonthResponse,
	LumpSumFinalSettlementListRequest,
	LumpSumQuarterCustomCost,
	LumpSumQuarterRevenueByMonth,
	LumpSumQuarterTransferredCost,
	ProcessGroup,
	UpsertLumpSumQuarterCustomCostRequest,
	YearFilterForm,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { SavingsRateConfig } from '@/features/main/catalog/savings-rate-config/columns';
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

function resolveSavingsValue(
	acceptedSavingQuarter: number,
	configs: SavingsRateConfig[],
) {
	const matchedConfig = [...configs]
		.filter((x) =>
			isRevenueInRange(acceptedSavingQuarter, x.minRevenue, x.maxRevenue),
		)
		.sort((a, b) => {
			const minDiff = (b.minRevenue ?? Number.NEGATIVE_INFINITY) - (a.minRevenue ?? Number.NEGATIVE_INFINITY);
			if (minDiff !== 0) {
				return minDiff;
			}
			return (a.maxRevenue ?? Number.POSITIVE_INFINITY) - (b.maxRevenue ?? Number.POSITIVE_INFINITY);
		})[0];

	if (!matchedConfig) {
		return 0;
	}

	const rawRate = matchedConfig.maxSavingsRate ?? matchedConfig.minSavingsRate;
	if (rawRate == null) {
		return 0;
	}

	return rawRate > 1 ? rawRate / 100 : rawRate;
}

function isRevenueInRange(
	revenue: number,
	minRevenue?: number,
	maxRevenue?: number,
) {
	const minMatch = minRevenue == null || revenue >= minRevenue;
	const maxMatch = maxRevenue == null || revenue <= maxRevenue;
	return minMatch && maxMatch;
}

export function MainCostLumpSumFinalSettlementMonthPage() {
	const [filteredData, setFilteredData] = useState<LumpSumFinalSettlement[]>(
		[],
	);
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
	const [transferredCostByMonth, setTransferredCostByMonth] =
		useState<LumpSumQuarterTransferredCost | null>(null);
	const [customCosts, setCustomCosts] = useState<LumpSumQuarterCustomCost[]>(
		[],
	);
	const [editingSnapshot, setEditingSnapshot] = useState<
		Record<string, LumpSumQuarterCustomCost>
	>({});
	const [savingsRateConfigs, setSavingsRateConfigs] = useState<
		SavingsRateConfig[]
	>([]);
	const [isLoading, setIsLoading] = useState(false);
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
		},
	});

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
		},
		[editingSnapshot],
	);

	const getCurrentFilter = useCallback(() => {
		const selectedMonth = form.getValues('month') || defaultMonth;
		const selectedYear = form.getValues('year') || defaultYear;
		const selectedProcessGroup = form.getValues('processGroup') || '';
		return {
			month: selectedMonth,
			year: selectedYear,
			processGroupId: selectedProcessGroup,
		};
	}, [defaultMonth, defaultYear, form]);

	const monthDisplayData = useMemo(() => {
		const selectedMonth = form.watch('month') || defaultMonth;
		const selectedYear = form.watch('year') || defaultYear;

		const revenue = {
			materials: revenueByMonth?.materials?.totalAmount ?? 0,
			maintains: revenueByMonth?.maintains?.totalAmount ?? 0,
			electricities: revenueByMonth?.electricities?.totalAmount ?? 0,
			total: revenueByMonth?.totalAmount ?? 0,
		};

		const customCostRows = customCosts.map((item) => buildCustomCostRow(item));
		const customTotals = customCostRows.reduce(
			(acc, row) => ({
				materials: acc.materials + (row.materials?.totalAmount ?? 0),
				maintains: acc.maintains + (row.maintains?.totalAmount ?? 0),
				electricities:
					acc.electricities + (row.electricities?.totalAmount ?? 0),
				total: acc.total + (row.totalAmount ?? 0),
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		const transferred = {
			materials: transferredCostByMonth?.materials?.totalAmount ?? 0,
			maintains: transferredCostByMonth?.maintains?.totalAmount ?? 0,
			electricities: transferredCostByMonth?.electricities?.totalAmount ?? 0,
			total: transferredCostByMonth?.totalAmount ?? 0,
		};

		const cost = {
			materials: transferred.materials + customTotals.materials,
			maintains: transferred.maintains + customTotals.maintains,
			electricities: transferred.electricities + customTotals.electricities,
			total: transferred.total + customTotals.total,
		};

		const saving = {
			materials: revenue.materials - cost.materials,
			maintains: revenue.maintains - cost.maintains,
			electricities: revenue.electricities - cost.electricities,
			total: revenue.total - cost.total,
		};

		const acceptedSavingMonth =
			saving.materials + saving.maintains + saving.electricities;
		const savingsValue = resolveSavingsValue(
			acceptedSavingMonth,
			savingsRateConfigs,
		);
		const savingAddedToIncomeMonth = acceptedSavingMonth * savingsValue;

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
			makeZeroRow(`Doanh thu tháng ${selectedMonth}/${selectedYear}`, {
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
			makeZeroRow(`Chi phí tháng ${selectedMonth}/${selectedYear}`, {
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
			makeZeroRow(`Chi phí kết chuyển T${selectedMonth}/${selectedYear}`, {
				sttLabel: 'II.1',
				isTransferredDefaultRow: true,
				month: Number(selectedMonth),
				materialsTotalAmount: transferred.materials,
				maintainsTotalAmount: transferred.maintains,
				electricitiesTotalAmount: transferred.electricities,
				totalAmount: transferred.total,
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
					materialsTotalAmount: saving.materials,
					maintainsTotalAmount: saving.maintains,
					electricitiesTotalAmount: saving.electricities,
					totalAmount: saving.total,
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
		defaultMonth,
		defaultYear,
		filteredData,
		form,
		quarterSpecialQuantities,
		revenueByMonth,
		savingsRateConfigs,
		transferredCostByMonth,
	]);

	useEffect(() => {
		const fetchProcessGroups = async () => {
			try {
				const response = await api.pagging<ProcessGroup>(
					API.CATALOG.PROCESS.GROUP.LIST,
					{ ignorePagination: true },
				);
				const options = [
					{ value: '', label: 'Tất cả nhóm công đoạn' },
					...(response.result.data ?? []).map((item: ProcessGroup) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				];
				setProcessGroups(options);
			} catch (error) {
				console.error('Error fetching process groups:', error);
			}
		};

		fetchProcessGroups();
	}, []);

	const fetchLumpSumMonth = useCallback(
		async (payload: {
			month: string;
			year: string;
			processGroupId: string;
		}) => {
			setIsLoading(true);
			try {
				const [monthRes, savingsRateConfigRes] = await Promise.all([
					api.post<
						LumpSumFinalSettlementMonthResponse,
						LumpSumFinalSettlementListRequest
					>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload),
					api.pagging<SavingsRateConfig>(API.CATALOG.SAVINGS_RATE_CONFIG.LIST, {
						ignorePagination: true,
					}),
				]);

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
				setRevenueByMonth(monthRes.result.revenue ?? null);
				setTransferredCostByMonth(monthRes.result.transferredCost ?? null);
				setCustomCosts(monthRes.result.customCosts ?? []);
				setSavingsRateConfigs(savingsRateConfigRes.result.data ?? []);
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
				setRevenueByMonth(null);
				setTransferredCostByMonth(null);
				setCustomCosts([]);
				setSavingsRateConfigs([]);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const refreshCustomCosts = useCallback(
		async (payload: {
			month: string;
			year: string;
			processGroupId: string;
		}) => {
			const monthRes = await api.post<
				LumpSumFinalSettlementMonthResponse,
				LumpSumFinalSettlementListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, {
				month: payload.month,
				year: payload.year,
				processGroupId: payload.processGroupId,
			});
			setCustomCosts(monthRes.result.customCosts ?? []);
		},
		[],
	);

	const saveCustomCost = useCallback(
		async (row: LumpSumFinalSettlement) => {
			const target = customCosts.find((x) => x.id === row.id);
			if (!target) {
				return;
			}

			const { month, year, processGroupId } = getCurrentFilter();
			const targetMonth = String(
				target.month ? Number(target.month) : (row.month ?? Number(month)),
			);
			const payload: UpsertLumpSumQuarterCustomCostRequest = {
				id: target.id.startsWith('temp-') ? undefined : target.id,
				month: targetMonth,
				year,
				processGroupId,
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
				await refreshCustomCosts({ month, year, processGroupId });
				setEditingSnapshot((prev) => {
					if (!row.id) {
						return prev;
					}
					const next = { ...prev };
					delete next[row.id];
					return next;
				});
			} catch (error) {
				console.error('Error saving custom cost:', error);
			}
		},
		[customCosts, getCurrentFilter, refreshCustomCosts],
	);

	const deleteCustomCost = useCallback(async (row: LumpSumFinalSettlement) => {
		if (!row.id) {
			return;
		}
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
			setCustomCosts((prev) => prev.filter((x) => x.id !== id));
		} catch (error) {
			console.error('Error deleting custom cost:', error);
		}
	}, []);

	const addCustomCostRow = useCallback(
		(row?: LumpSumFinalSettlement) => {
			const { month, year, processGroupId } = getCurrentFilter();
			const targetMonth = String(row?.month ?? Number(month));
			const tempId = `temp-${Date.now()}`;
			setCustomCosts((prev) => [
				...prev,
				{
					id: tempId,
					month: targetMonth,
					year,
					processGroupId,
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
			if (!row.id || row.id.startsWith('temp-')) {
				return;
			}
			setEditingSnapshot((prev) => {
				const existing = customCosts.find((x) => x.id === row.id);
				if (!existing) {
					return prev;
				}
				return { ...prev, [row.id!]: { ...existing } };
			});
		},
		[customCosts],
	);

	const cancelCustomCost = useCallback((row: LumpSumFinalSettlement) => {
		if (!row.id) {
			return;
		}
		const id = row.id;
		if (id.startsWith('temp-')) {
			setCustomCosts((prev) => prev.filter((x) => x.id !== id));
			return;
		}
		setEditingSnapshot((prev) => {
			const snapshot = prev[id];
			if (!snapshot) {
				return prev;
			}
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
			if (!row.id) {
				return;
			}
			setCustomCosts((prev) =>
				prev.map((x) => {
					if (x.id !== row.id) {
						return x;
					}
					if (field === 'customName') {
						return { ...x, customName: String(value) };
					}
					return { ...x, [field]: Number(value) };
				}),
			);
		},
		[],
	);

	const handleFilter = useCallback(
		(data: YearFilterForm) => {
			if (!data.month || !data.year) return;

			fetchLumpSumMonth({
				month: data.month,
				year: data.year,
				processGroupId: data.processGroup ?? '',
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
			});
		}
	}, [form, handleFilter]);

	return (
		<Card>
			<CardHeader>
				<FormProvider context={form} onSubmit={handleFilter}>
					<div className='flex items-end justify-between gap-4'>
						<div className='grid w-full max-w-3xl flex-1 grid-cols-1 gap-4 md:grid-cols-3'>
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
						</div>
						<div className='flex shrink-0 gap-4'>
							<Button variant={'ghost'} className={shadow}>
								<DownloadIcon fontSize='small' />
								<span className='hidden xl:block'>Xuất file</span>
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
				/>
			</CardContent>
		</Card>
	);
}
