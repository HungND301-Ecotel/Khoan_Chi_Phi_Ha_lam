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
	LumpSumQuarterRevenueByMonth,
	LumpSumQuarterTransferredCost,
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
	const [revenuesByMonth, setRevenuesByMonth] = useState<
		LumpSumQuarterRevenueByMonth[]
	>([]);
	const [transferredCost, setTransferredCost] =
		useState<LumpSumQuarterTransferredCost | null>(null);
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
		return {
			quarter: selectedQuarter,
			year: selectedYear,
			processGroupId: selectedProcessGroup,
		};
	}, [defaultQuarter, defaultYear, form]);

	const quarterDisplayData = useMemo(() => {
		const selectedQuarter = watchedQuarter || defaultQuarter;
		const selectedYear = watchedYear || defaultYear;
		const quarterNum = Number(selectedQuarter);
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
			month: transferredCost?.month ?? quarterEndMonth(quarterNum),
			materials: transferredCost?.materials?.totalAmount ?? 0,
			maintains: transferredCost?.maintains?.totalAmount ?? 0,
			electricities: transferredCost?.electricities?.totalAmount ?? 0,
			total: transferredCost?.totalAmount ?? 0,
		};
		const customCostRows = customCosts.map((item) => buildCustomCostRow(item));

		const makeZeroRow = (
			productName: string,
			options?: {
				sttLabel?: string;
				isBold?: boolean;
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
			makeZeroRow(`Doanh thu quý ${selectedQuarter}/${selectedYear}`, {
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
			makeZeroRow(`Chi phí quý ${selectedQuarter}/${selectedYear}`, {
				sttLabel: 'II',
				isBold: true,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...months.map((m, idx) =>
				makeZeroRow(`Tháng ${m}/${selectedYear}`, {
					sttLabel: `II.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),
			makeZeroRow(`Chi phí kết chuyển T${transferred.month}/${selectedYear}`, {
				sttLabel: '-',
				isTransferredDefaultRow: true,
				unitOfMeasureName: '',
				materialsTotalAmount: transferred.materials,
				maintainsTotalAmount: transferred.maintains,
				electricitiesTotalAmount: transferred.electricities,
				totalAmount: transferred.total,
				hidePlanActual: true,
				hideUnitPrice: true,
			}),
			...customCostRows,

			makeZeroRow(
				`Giá trị tiết kiệm, bội chi quý ${selectedQuarter}/${selectedYear}`,
				{
					sttLabel: 'III',
					isBold: true,
					hidePlanActual: true,
					hideUnitPrice: true,
				},
			),
			...months.map((m, idx) =>
				makeZeroRow(`Tháng ${m}/${selectedYear}`, {
					sttLabel: `III.${idx + 1}`,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
				}),
			),

			makeZeroRow(
				`Tổng giá trị tiết kiệm được chấp nhận quý ${selectedQuarter}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: 0,
				},
			),
			makeZeroRow(
				`Giá trị tiết kiệm được cộng vào thu nhập quý ${selectedQuarter}/${selectedYear}`,
				{
					sttLabel: '*',
					isBold: true,
					unitOfMeasureName: 'Đồng',
					hidePlanActual: true,
					hideUnitPrice: true,
					isMergedValueRow: true,
					mergedValue: 0,
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
						mergedValue: 0,
					},
				),
			),
		];

		return [...filteredData, ...defaultRows];
	}, [
		buildCustomCostRow,
		customCosts,
		defaultQuarter,
		defaultYear,
		filteredData,
		revenuesByMonth,
		transferredCost,
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

				setFilteredData(groupByProcessGroup(res.result.items ?? []));
				setRevenuesByMonth(res.result.revenuesByMonth ?? []);
				setTransferredCost(res.result.transferredCost ?? null);
				setCustomCosts(res.result.customCosts ?? []);
				setEditingSnapshot({});
			} catch (error) {
				console.error('Error fetching lump sum quarter list:', error);
				setFilteredData([]);
				setRevenuesByMonth([]);
				setTransferredCost(null);
				setCustomCosts([]);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const saveCustomCost = useCallback(
		async (row: LumpSumFinalSettlement) => {
			const target = customCosts.find((x) => x.id === row.id);
			if (!target) {
				return;
			}

			const { quarter, year, processGroupId } = getCurrentFilter();
			const payload: UpsertLumpSumQuarterCustomCostRequest = {
				id: target.id.startsWith('temp-') ? undefined : target.id,
				quarter,
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

				await fetchLumpSumQuarter({ quarter, year, processGroupId });
			} catch (error) {
				console.error('Error saving custom cost:', error);
			}
		},
		[customCosts, fetchLumpSumQuarter, getCurrentFilter],
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

	const addCustomCostRow = useCallback(() => {
		const { quarter, year, processGroupId } = getCurrentFilter();
		const tempId = `temp-${Date.now()}`;
		setCustomCosts((prev) => [
			...prev,
			{
				id: tempId,
				quarter,
				year,
				processGroupId,
				customName: ``,
				actualQuantity: 0,
				materialUnitPrice: 0,
				maintainUnitPrice: 0,
				electricityUnitPrice: 0,
			},
		]);
	}, [getCurrentFilter]);

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
								label='Quý và Năm'
								placeholder='Chọn quý và năm'
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

function quarterEndMonth(quarter: number) {
	return (quarter - 1) * 3 + 3;
}
