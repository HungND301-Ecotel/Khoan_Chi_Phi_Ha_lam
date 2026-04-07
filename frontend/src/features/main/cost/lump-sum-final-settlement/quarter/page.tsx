import { FormComboBox } from '@/components/form/form-combo-box';
import { FormQuaterYear } from '@/components/form-quater-year/form-quater-year';
import { FormProvider } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { API } from '@/constants/api-enpoint';
import { LUMP_SUM_FINAL_SETTLEMENT_COLUMNS } from '@/features/main/cost/lump-sum-final-settlement/columns';
import { LumpSumDataTable } from '@/features/main/cost/lump-sum-final-settlement/components/datatable';
import { groupByProcessGroup } from '@/features/main/cost/lump-sum-final-settlement/grouping';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementQuarterListRequest,
	LumpSumFinalSettlementQuarterResponse,
	LumpSumQuarterCustomCost,
	LumpSumQuarterCustomCostListRequest,
	LumpSumQuarterRevenueByMonth,
	LumpSumQuarterTransferredCost,
	ProcessGroup,
	QuarterFilterForm,
	UpsertLumpSumQuarterCustomCostRequest,
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
			const minDiff =
				(b.minRevenue ?? Number.NEGATIVE_INFINITY) -
				(a.minRevenue ?? Number.NEGATIVE_INFINITY);
			if (minDiff !== 0) {
				return minDiff;
			}
			return (
				(a.maxRevenue ?? Number.POSITIVE_INFINITY) -
				(b.maxRevenue ?? Number.POSITIVE_INFINITY)
			);
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

export function MainCostLumpSumFinalSettlementQuarterPage() {
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
	const [savingsRateConfigs, setSavingsRateConfigs] = useState<
		SavingsRateConfig[]
	>([]);
	const [revenuesByMonth, setRevenuesByMonth] = useState<
		LumpSumQuarterRevenueByMonth[]
	>([]);
	const [transferredCosts, setTransferredCosts] = useState<
		LumpSumQuarterTransferredCost[]
	>([]);
	const [customCosts, setCustomCosts] = useState<LumpSumQuarterCustomCost[]>(
		[],
	);
	const [editingSnapshot, setEditingSnapshot] = useState<
		Record<string, LumpSumQuarterCustomCost>
	>({});
	const [isLoading, setIsLoading] = useState(false);
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
		},
	});
	const watchedQuarter = form.watch('quarter');
	const watchedYear = form.watch('year');

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
		const selectedQuarter = form.getValues('quarter') || defaultQuarter;
		const selectedYear = form.getValues('year') || defaultYear;
		const selectedProcessGroup = form.getValues('processGroup') || '';
		const startMonth = quarterToStartMonth(selectedQuarter);
		return {
			quarter: selectedQuarter,
			month: String(startMonth),
			year: selectedYear,
			processGroupId: selectedProcessGroup,
		};
	}, [defaultQuarter, defaultYear, form]);

	const quarterDisplayData = useMemo(() => {
		const selectedQuarter = watchedQuarter || defaultQuarter;
		const selectedYear = watchedYear || defaultYear;
		const quarterNum = Number(selectedQuarter);
		const quarterRoman = toRomanQuarter(selectedQuarter);
		const startMonth = (quarterNum - 1) * 3 + 1;
		const months = [startMonth, startMonth + 1, startMonth + 2];
		const revenueByMonthMap = new Map(
			revenuesByMonth.map((item) => [item.month, item]),
		);

		const revenueRows = months.map((month) => {
			const value = revenueByMonthMap.get(month);
			return {
				month,
				materials: value?.materials?.totalAmount ?? 0,
				maintains: value?.maintains?.totalAmount ?? 0,
				electricities: value?.electricities?.totalAmount ?? 0,
				total: value?.totalAmount ?? 0,
			};
		});

		const revenueQuarter = revenueRows.reduce(
			(acc, item) => ({
				materials: acc.materials + item.materials,
				maintains: acc.maintains + item.maintains,
				electricities: acc.electricities + item.electricities,
				total: acc.total + item.total,
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		const transferred = {
			byMonth: new Map(
				transferredCosts.map((item) => [
					item.month,
					{
						materials: item.materials?.totalAmount ?? 0,
						maintains: item.maintains?.totalAmount ?? 0,
						electricities: item.electricities?.totalAmount ?? 0,
						total: item.totalAmount ?? 0,
					},
				]),
			),
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
			const nextTotal = {
				materials: currentTotal.materials + (row.materials?.totalAmount ?? 0),
				maintains: currentTotal.maintains + (row.maintains?.totalAmount ?? 0),
				electricities:
					currentTotal.electricities + (row.electricities?.totalAmount ?? 0),
				total: currentTotal.total + (row.totalAmount ?? 0),
			};
			customCostTotalsByMonth.set(month, nextTotal);
		}

		const costRows = months.map((month) => {
			const monthTransferred = transferred.byMonth.get(month) ?? {
				materials: 0,
				maintains: 0,
				electricities: 0,
				total: 0,
			};
			const monthCustom = customCostTotalsByMonth.get(month) ?? {
				materials: 0,
				maintains: 0,
				electricities: 0,
				total: 0,
			};

			return {
				month,
				materials: monthTransferred.materials + monthCustom.materials,
				maintains: monthTransferred.maintains + monthCustom.maintains,
				electricities:
					monthTransferred.electricities + monthCustom.electricities,
				total: monthTransferred.total + monthCustom.total,
			};
		});

		const costQuarter = costRows.reduce(
			(acc, item) => ({
				materials: acc.materials + item.materials,
				maintains: acc.maintains + item.maintains,
				electricities: acc.electricities + item.electricities,
				total: acc.total + item.total,
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);

		const savingRows = months.map((month, idx) => ({
			month,
			materials:
				(revenueRows[idx]?.materials ?? 0) - (costRows[idx]?.materials ?? 0),
			maintains:
				(revenueRows[idx]?.maintains ?? 0) - (costRows[idx]?.maintains ?? 0),
			electricities:
				(revenueRows[idx]?.electricities ?? 0) -
				(costRows[idx]?.electricities ?? 0),
			total: (revenueRows[idx]?.total ?? 0) - (costRows[idx]?.total ?? 0),
		}));

		const savingQuarter = savingRows.reduce(
			(acc, item) => ({
				materials: acc.materials + item.materials,
				maintains: acc.maintains + item.maintains,
				electricities: acc.electricities + item.electricities,
				total: acc.total + item.total,
			}),
			{ materials: 0, maintains: 0, electricities: 0, total: 0 },
		);
		const acceptedSavingQuarter =
			savingQuarter.materials +
			savingQuarter.maintains +
			savingQuarter.electricities;
		const savingsValue = resolveSavingsValue(
			acceptedSavingQuarter,
			savingsRateConfigs,
		);
		const savingAddedToIncomeQuarter = acceptedSavingQuarter * savingsValue;
		const savingAddedToIncomeByMonth = months.map((_, idx) => {
			const acceptedSavingMonth =
				(savingRows[idx]?.materials ?? 0) +
				(savingRows[idx]?.maintains ?? 0) +
				(savingRows[idx]?.electricities ?? 0);
			const savingValueOfMonth = resolveSavingsValue(
				acceptedSavingMonth,
				savingsRateConfigs,
			);
			return acceptedSavingMonth * savingValueOfMonth;
		});
		const firstTwoMonthsSavingAdded =
			(savingAddedToIncomeByMonth[0] ?? 0) +
			(savingAddedToIncomeByMonth[1] ?? 0);
		const lastMonthSavingAdded =
			savingAddedToIncomeQuarter - firstTwoMonthsSavingAdded;

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
					materialsTotalAmount: revenueRows[idx]?.materials ?? 0,
					maintainsTotalAmount: revenueRows[idx]?.maintains ?? 0,
					electricitiesTotalAmount: revenueRows[idx]?.electricities ?? 0,
					totalAmount: revenueRows[idx]?.total ?? 0,
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
			...months.flatMap((month, idx) => {
				const monthTransferred = transferred.byMonth.get(month);
				const monthCost = costRows[idx];
				return [
					makeZeroRow(`Tháng ${month}/${selectedYear}`, {
						sttLabel: `II.${idx + 1}`,
						unitOfMeasureName: 'Đồng',
						materialsTotalAmount: monthCost?.materials ?? 0,
						maintainsTotalAmount: monthCost?.maintains ?? 0,
						electricitiesTotalAmount: monthCost?.electricities ?? 0,
						totalAmount: monthCost?.total ?? 0,
						hidePlanActual: true,
						hideUnitPrice: true,
					}),
					makeZeroRow(`Chi phí kết chuyển T${month}/${selectedYear}`, {
						sttLabel: '-',
						isTransferredDefaultRow: true,
						month,
						unitOfMeasureName: '',
						materialsTotalAmount: monthTransferred?.materials ?? 0,
						maintainsTotalAmount: monthTransferred?.maintains ?? 0,
						electricitiesTotalAmount: monthTransferred?.electricities ?? 0,
						totalAmount: monthTransferred?.total ?? 0,
						hidePlanActual: true,
						hideUnitPrice: true,
					}),
					...(customCostRowsByMonth.get(month) ?? []),
				];
			}),

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
					materialsTotalAmount: savingRows[idx]?.materials ?? 0,
					maintainsTotalAmount: savingRows[idx]?.maintains ?? 0,
					electricitiesTotalAmount: savingRows[idx]?.electricities ?? 0,
					totalAmount: savingRows[idx]?.total ?? 0,
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
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
					mergedValue: acceptedSavingQuarter,
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
					mergedValue: savingAddedToIncomeQuarter,
				},
			),
			...months.map((m, idx) =>
				makeZeroRow(
					`Giá trị tiết kiệm đã cộng vào thu nhập tháng ${m}/${selectedYear}`,
					{
						sttLabel: '*',
						unitOfMeasureName: 'Đồng',
						hidePlanActual: true,
						hideUnitPrice: true,
						isMergedValueRow: true,
						mergedValue:
							idx < 2
								? (savingAddedToIncomeByMonth[idx] ?? 0)
								: lastMonthSavingAdded,
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
		quarterSpecialQuantities,
		savingsRateConfigs,
		revenuesByMonth,
		transferredCosts,
		watchedQuarter,
		watchedYear,
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

	const fetchLumpSumQuarter = useCallback(
		async (payload: {
			quarter: string;
			year: string;
			processGroupId: string;
		}) => {
			setIsLoading(true);
			try {
				const res = await api.post<
					LumpSumFinalSettlementQuarterResponse,
					LumpSumFinalSettlementQuarterListRequest
				>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_LIST, payload);
				const customCostRes = await api.post<
					LumpSumQuarterCustomCost[],
					LumpSumQuarterCustomCostListRequest
				>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_CUSTOM_COST_LIST, {
					quarter: payload.quarter,
					year: payload.year,
					processGroupId: payload.processGroupId,
				});
				const savingsRateConfigRes = await api.pagging<SavingsRateConfig>(
					API.CATALOG.SAVINGS_RATE_CONFIG.LIST,
					{ ignorePagination: true },
				);

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
				setSavingsRateConfigs(savingsRateConfigRes.result.data ?? []);
				setRevenuesByMonth(res.result.revenuesByMonth ?? []);
				const apiTransferredCosts = res.result.transferredCosts;
				if (apiTransferredCosts?.length) {
					setTransferredCosts(apiTransferredCosts);
				} else {
					setTransferredCosts([]);
				}
				setCustomCosts(customCostRes.result ?? res.result.customCosts ?? []);
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
				setSavingsRateConfigs([]);
				setRevenuesByMonth([]);
				setTransferredCosts([]);
				setCustomCosts([]);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const refreshCustomCosts = useCallback(
		async (payload: {
			quarter: string;
			year: string;
			processGroupId: string;
		}) => {
			const customCostRes = await api.post<
				LumpSumQuarterCustomCost[],
				LumpSumQuarterCustomCostListRequest
			>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.QUARTER_CUSTOM_COST_LIST, {
				quarter: payload.quarter,
				year: payload.year,
				processGroupId: payload.processGroupId,
			});

			setCustomCosts(customCostRes.result ?? []);
		},
		[],
	);

	const saveCustomCost = useCallback(
		async (row: LumpSumFinalSettlement) => {
			const target = customCosts.find((x) => x.id === row.id);
			if (!target) {
				return;
			}

			const { quarter, month, year, processGroupId } = getCurrentFilter();
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
				await refreshCustomCosts({ quarter, year, processGroupId });
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
					customName: ``,
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
		(data: QuarterFilterForm) => {
			if (!data.quarter || !data.year) return;

			fetchLumpSumQuarter({
				quarter: data.quarter,
				year: data.year,
				processGroupId: data.processGroup ?? '',
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
			});
		}
	}, [form, handleFilter]);

	return (
		<Card>
			<CardHeader>
				<FormProvider context={form} onSubmit={handleFilter}>
					<div className='flex items-end justify-between gap-4'>
						<div className='grid w-full max-w-3xl flex-1 grid-cols-1 gap-4 md:grid-cols-3'>
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
	const quarterNumber = Number(quarter) || 1;
	return (quarterNumber - 1) * 3 + 1;
}

function toRomanQuarter(quarter: string | number) {
	const quarterNumber = Number(quarter);
	if (quarterNumber === 1) return 'I';
	if (quarterNumber === 2) return 'II';
	if (quarterNumber === 3) return 'III';
	if (quarterNumber === 4) return 'IV';
	return String(quarter);
}
