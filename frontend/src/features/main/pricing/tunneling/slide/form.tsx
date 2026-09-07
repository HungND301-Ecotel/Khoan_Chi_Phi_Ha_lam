import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import type { Slide } from '@/features/main/pricing/tunneling/slide/columns';
import {
	SLIDE_COMMON_FORM_DEFAULT,
	slideCommonFormSchema,
	type SlideCommonFormSchema,
	type SlideFormSchema,
} from '@/features/main/pricing/tunneling/slide/schema';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';

type SlideMaterialOption = MultiSelectOption & {
	assignmentCodeId: string;
	materialId: string;
};

type PersistedSlideMaterialCost = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
	unitPrice: number;
	amount: number;
};

export type SlideDetail = {
	id: string;
	code: string;
	name: string;
	startMonth: string;
	endMonth: string;
	materialCost: Array<{
		assignmentCodeId: string;
		assignmentCode: string;
		assignmentCodeName?: string;
		costs: Array<{
			materialId: string;
			materialCode: string;
			materialName: string;
			unitOfMeasureName: string;
			cost: number;
			amount: number;
		}>;
	}>;
};

interface PeriodItemData {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	costs: SlideFormSchema['costs'];
	selectedAssignments: MultiSelectOption[];
	selectedMaterials: SlideMaterialOption[];
	persistedCosts: PersistedSlideMaterialCost[];
}

const buildMaterialSelectionValue = (
	assignmentCodeId: string,
	materialId: string,
) => `${assignmentCodeId}::${materialId}`;

const buildMaterialOption = (
	assignment: ContractCode | undefined,
	asset: Asset | undefined,
): SlideMaterialOption | null => {
	if (!assignment || !asset) return null;

	return {
		value: buildMaterialSelectionValue(assignment.id, asset.id),
		label: `[${assignment.code}] ${asset.code} - ${asset.name}`,
		assignmentCodeId: assignment.id,
		materialId: asset.id,
	};
};

const getMaterialOptions = (
	assets: Asset[],
	assignments: ContractCode[],
	selectedAssignmentIds: string[],
) => {
	const assignmentMap = new Map(assignments.map((item) => [item.id, item]));
	const options: SlideMaterialOption[] = [];

	selectedAssignmentIds.forEach((assignmentCodeId) => {
		const assignment = assignmentMap.get(assignmentCodeId);
		assets
			.filter((asset) => asset.assignmentCodeIds.includes(assignmentCodeId))
			.sort((left, right) =>
				`${left.code} ${left.name}`.localeCompare(
					`${right.code} ${right.name}`,
					'vi',
				),
			)
			.forEach((asset) => {
				const option = buildMaterialOption(assignment, asset);
				if (option) {
					options.push(option);
				}
			});
	});

	return options;
};

const deriveNormFromAmount = (amount: number, unitPrice: number) => {
	if (unitPrice > 0) {
		return amount / unitPrice;
	}

	return amount === 0 ? 0 : amount;
};

const syncCostsWithSelections = (
	costs: SlideFormSchema['costs'],
	selectedMaterialOptions: SlideMaterialOption[],
	assets: Asset[],
) => {
	const selectedKeys = new Set(
		selectedMaterialOptions.map((option) => option.value),
	);
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	const existingRows = costs.filter((cost) =>
		selectedKeys.has(
			buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
		),
	);
	const existingKeys = new Set(
		existingRows.map((cost) =>
			buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
		),
	);

	const addedRows = selectedMaterialOptions
		.filter((option) => !existingKeys.has(option.value))
		.map((option) => {
			const asset = assetMap.get(option.materialId);
			const unitPrice = asset?.costAmount ?? 0;
			return {
				assignmentCodeId: option.assignmentCodeId,
				materialId: option.materialId,
				norm: Number.NaN,
				amount: unitPrice,
			};
		});

	return [...existingRows, ...addedRows];
};

const normalizeCostAmounts = (
	costs: SlideFormSchema['costs'],
	assets: Asset[],
) => {
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	return costs.map((cost) => {
		const asset = assetMap.get(cost.materialId);
		if (asset) {
			const unitPrice = asset.costAmount ?? 0;
			const amount = Number.isNaN(Number(cost.norm))
				? unitPrice
				: unitPrice * Number(cost.norm);

			return {
				...cost,
				amount,
			};
		}
		return cost;
	});
};

const sortCosts = (
	costs: SlideFormSchema['costs'],
	assignments: ContractCode[],
	assets: Asset[],
) => {
	const assignmentOrder = new Map(
		assignments.map((assignment, index) => [assignment.id, index]),
	);
	const assetOrder = new Map(
		assets
			.slice()
			.sort((left, right) =>
				`${left.code} ${left.name}`.localeCompare(
					`${right.code} ${right.name}`,
					'vi',
				),
			)
			.map((asset, index) => [asset.id, index]),
	);

	return [...costs].sort((left, right) => {
		const assignmentCompare =
			(assignmentOrder.get(left.assignmentCodeId) ?? Number.MAX_SAFE_INTEGER) -
			(assignmentOrder.get(right.assignmentCodeId) ?? Number.MAX_SAFE_INTEGER);
		if (assignmentCompare !== 0) return assignmentCompare;

		return (
			(assetOrder.get(left.materialId) ?? Number.MAX_SAFE_INTEGER) -
			(assetOrder.get(right.materialId) ?? Number.MAX_SAFE_INTEGER)
		);
	});
};

const groupCostsByAssignment = (
	costs: SlideFormSchema['costs'],
	assignments: ContractCode[],
) => {
	const grouped = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentLabel: string;
			indices: number[];
			totalAmount: number;
		}
	>();

	costs.forEach((cost, index) => {
		const assignment = assignments.find(
			(item) => item.id === cost.assignmentCodeId,
		);
		const assignmentLabel = assignment
			? `${assignment.code} - ${assignment.name}`
			: cost.assignmentCodeId;
		const current = grouped.get(cost.assignmentCodeId);
		const rowTotal = Number.isNaN(Number(cost.amount))
			? 0
			: Number(cost.amount);

		if (!current) {
			grouped.set(cost.assignmentCodeId, {
				assignmentCodeId: cost.assignmentCodeId,
				assignmentLabel,
				indices: [index],
				totalAmount: rowTotal,
			});
			return;
		}

		current.indices.push(index);
		current.totalAmount += rowTotal;
	});

	return Array.from(grouped.values());
};

const validatePeriods = (
	allPeriods: PeriodItemData[],
): { error: string } | null => {
	if (allPeriods.length === 0) {
		return { error: 'Cần ít nhất một khoảng thời gian áp dụng.' };
	}

	for (let i = 0; i < allPeriods.length; i++) {
		const p = allPeriods[i];
		if (!p.startMonth) {
			return {
				error: `Khoảng thời gian ${i + 1}: Chưa chọn thời gian bắt đầu.`,
			};
		}
		if (!p.endMonth) {
			return {
				error: `Khoảng thời gian ${i + 1}: Chưa chọn thời gian kết thúc.`,
			};
		}
		const s = new Date(p.startMonth).getTime();
		const e = new Date(p.endMonth).getTime();
		if (s > e) {
			return {
				error: `Khoảng thời gian ${i + 1}: Thời gian bắt đầu không được lớn hơn thời gian kết thúc.`,
			};
		}
		if (!p.costs || p.costs.length === 0) {
			return {
				error: `Khoảng thời gian ${i + 1}: Chưa có vật tư nào trong định mức. Vui lòng chọn nhóm vật tư và vật tư.`,
			};
		}
		for (const c of p.costs) {
			if (
				c.norm === undefined ||
				c.norm === null ||
				Number.isNaN(Number(c.norm))
			) {
				return {
					error: `Khoảng thời gian ${i + 1}: Có vật tư chưa nhập định mức.`,
				};
			}
		}
	}

	for (let i = 0; i < allPeriods.length; i++) {
		for (let j = i + 1; j < allPeriods.length; j++) {
			const p1 = allPeriods[i];
			const p2 = allPeriods[j];
			const s1 = new Date(p1.startMonth).getTime();
			const e1 = new Date(p1.endMonth).getTime();
			const s2 = new Date(p2.startMonth).getTime();
			const e2 = new Date(p2.endMonth).getTime();

			if (s1 <= e2 && s2 <= e1) {
				const label1 = `Khoảng thời gian ${i + 1} (${formatDate(p1.startMonth, 'MM/yyyy')} - ${formatDate(p1.endMonth, 'MM/yyyy')})`;
				const label2 = `Khoảng thời gian ${j + 1} (${formatDate(p2.startMonth, 'MM/yyyy')} - ${formatDate(p2.endMonth, 'MM/yyyy')})`;
				return {
					error: `Thời gian áp dụng của ${label1} và ${label2} bị trùng lặp. Vui lòng kiểm tra lại.`,
				};
			}
		}
	}

	return null;
};

export function SlideForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Slide> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);

	const [periods, setPeriods] = useState<PeriodItemData[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			costs: [],
			selectedAssignments: [],
			selectedMaterials: [],
			persistedCosts: [],
		},
	]);
	const [deletedPeriodIds, setDeletedPeriodIds] = useState<string[]>([]);
	const [isLoadingPeriods, setIsLoadingPeriods] = useState(false);

	const form = useForm<SlideCommonFormSchema, unknown, SlideCommonFormSchema>({
		resolver: zodResolver(slideCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: SLIDE_COMMON_FORM_DEFAULT,
	});

	useEffect(() => {
		Promise.all([
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
		]).then(([groupsRes, passportsRes, strengthsRes]) => {
			setGroups(groupsRes.result.data);
			setPassports(passportsRes.result.data);
			setStrengths(strengthsRes.result.data);
		});
	}, []);

	useEffect(() => {
		if (!row) {
			setPeriods([
				{
					tempId: `period_${Date.now()}`,
					startMonth: new Date().toISOString().substring(0, 10),
					endMonth: new Date().toISOString().substring(0, 10),
					costs: [],
					selectedAssignments: [],
					selectedMaterials: [],
					persistedCosts: [],
				},
			]);
			return;
		}

		form.reset({
			code: isDuplicate ? '' : row.code,
			processGroupId: row.processGroupId,
			passportId: row.passportId,
			hardnessId: row.hardnessId,
		});

		const rawPeriods =
			row.periods && row.periods.length > 0
				? row.periods
				: [
						{
							id: row.id,
							startMonth: row.startMonth,
							endMonth: row.endMonth,
							totalPrice: row.totalPrice ?? 0,
						},
					];

		setIsLoadingPeriods(true);
		Promise.all(
			rawPeriods.map(async (p, idx) => {
				if (!p.id) {
					return {
						tempId: `period_${idx}_${Date.now()}`,
						id: undefined,
						startMonth: (
							p.startMonth ?? new Date().toISOString()
						).substring(0, 10),
						endMonth: (
							p.endMonth ?? new Date().toISOString()
						).substring(0, 10),
						costs: [],
						selectedAssignments: [],
						selectedMaterials: [],
						persistedCosts: [],
					};
				}
				try {
					const res = await api.get<SlideDetail>(
						API.PRICING.SLIDE.DETAIL(p.id),
					);
					const detail = res.result;
					const persistedCosts: PersistedSlideMaterialCost[] = [];
					const materialCosts = detail.materialCost ?? [];

					materialCosts.forEach((group) => {
						(group.costs ?? []).forEach((c) => {
							persistedCosts.push({
								assignmentCodeId: group.assignmentCodeId,
								assignmentCode: group.assignmentCode,
								assignmentCodeName: group.assignmentCodeName ?? '',
								materialId: c.materialId,
								materialCode: c.materialCode,
								materialName: c.materialName,
								unitOfMeasureName: c.unitOfMeasureName,
								unitPrice: c.cost,
								amount: c.amount,
							});
						});
					});

					const selectedAssignments = materialCosts.map((group) => ({
						value: group.assignmentCodeId,
						label: `${group.assignmentCode} - ${group.assignmentCodeName ?? ''}`,
					}));

					const selectedMaterials = persistedCosts.map((cost) => ({
						value: buildMaterialSelectionValue(
							cost.assignmentCodeId,
							cost.materialId,
						),
						label: `[${cost.assignmentCode}] ${cost.materialCode} - ${cost.materialName}`,
						assignmentCodeId: cost.assignmentCodeId,
						materialId: cost.materialId,
					}));

					const normalizedCosts = persistedCosts.map((cost) => ({
						assignmentCodeId: cost.assignmentCodeId,
						materialId: cost.materialId,
						norm: deriveNormFromAmount(cost.amount, cost.unitPrice),
						amount: cost.amount,
					}));

					return {
						tempId: p.id,
						id: isDuplicate ? undefined : p.id,
						startMonth: (
							p.startMonth ?? new Date().toISOString()
						).substring(0, 10),
						endMonth: (
							p.endMonth ?? new Date().toISOString()
						).substring(0, 10),
						costs: normalizedCosts,
						selectedAssignments,
						selectedMaterials,
						persistedCosts,
					};
				} catch {
					return {
						tempId: p.id,
						id: isDuplicate ? undefined : p.id,
						startMonth: (
							p.startMonth ?? new Date().toISOString()
						).substring(0, 10),
						endMonth: (
							p.endMonth ?? new Date().toISOString()
						).substring(0, 10),
						costs: [],
						selectedAssignments: [],
						selectedMaterials: [],
						persistedCosts: [],
					};
				}
			}),
		).then((loadedPeriods) => {
			setIsLoadingPeriods(false);
			setPeriods(loadedPeriods);
		});
	}, [form, isDuplicate, row]);

	const handleUpdatePeriod = (index: number, updated: PeriodItemData) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleAddPeriod = () => {
		let nextStart = new Date().toISOString().substring(0, 10);
		let nextEnd = new Date().toISOString().substring(0, 10);

		if (periods.length > 0) {
			const sortedEndDates = periods
				.map((p) => p.endMonth)
				.filter(Boolean)
				.sort();
			const latestEnd = sortedEndDates[sortedEndDates.length - 1];
			if (latestEnd) {
				const date = new Date(latestEnd);
				const nextStartDate = new Date(
					date.getFullYear(),
					date.getMonth() + 1,
					1,
				);
				const nextEndDate = new Date(
					date.getFullYear() + 1,
					date.getMonth() + 1,
					0,
				);
				nextStart = nextStartDate.toISOString().substring(0, 10);
				nextEnd = nextEndDate.toISOString().substring(0, 10);
			}
		}

		const newPeriod: PeriodItemData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextStart,
			endMonth: nextEnd,
			costs: [],
			selectedAssignments: [],
			selectedMaterials: [],
			persistedCosts: [],
		};

		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleDeletePeriod = (indexToDelete: number) => {
		if (periods.length <= 1) {
			popup.error('Mã định mức cần có ít nhất một khoảng thời gian.');
			return;
		}

		const periodToDelete = periods[indexToDelete];
		if (periodToDelete.id) {
			setDeletedPeriodIds((prev) => [...prev, periodToDelete.id!]);
		}

		setPeriods((prev) => prev.filter((_, idx) => idx !== indexToDelete));
	};

	const handleSubmit = async (values: SlideCommonFormSchema) => {
		try {
			const validationResult = validatePeriods(periods);
			if (validationResult) {
				popup.error(validationResult.error);
				return;
			}

			if (deletedPeriodIds.length > 0) {
				await api.delete(
					API.PRICING.SLIDE.DELETES,
					deletedPeriodIds,
				);
			}

			for (const period of periods) {
				const payload = {
					code: values.code,
					processGroupId: values.processGroupId,
					passportId: values.passportId,
					hardnessId: values.hardnessId,
					startMonth: period.startMonth,
					endMonth: period.endMonth,
					costs: period.costs.map((cost) => ({
						assignmentCodeId: cost.assignmentCodeId,
						materialId: cost.materialId,
						amount: cost.amount,
					})),
				};

				if (period.id && !isDuplicate) {
					await api.put(API.PRICING.SLIDE.UPDATE, {
						id: period.id,
						...payload,
					});
				} else {
					await api.post(API.PRICING.SLIDE.CREATE, payload);
				}
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			{/* Phần 1: Thông tin chung định mức */}
			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức máng trượt'
				placeholder='Nhập mã định mức máng trượt'
			/>

			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={groups.map((group) => ({
					label: group.name,
					value: group.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='passportId'
				label='Hộ chiếu, Sđ, Sc'
				placeholder='Chọn hộ chiếu'
				options={passports.map((passport) => ({
					label: `H/c ${passport.name}; ${passport.sd}; ${passport.sc}`,
					value: passport.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố đá, than (f)'
				placeholder='Chọn Độ kiên cố đá, than (f)'
				options={strengths.map((strength) => ({
					label: strength.value,
					value: strength.id,
				}))}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			{/* Phần 2: Danh sách các form khoảng thời gian xếp dọc */}
			{isLoadingPeriods ? (
				<div className='flex items-center justify-center py-8 text-sm text-neutral-500'>
					<Spinner className='mr-2 size-4' /> Đang tải dữ liệu các khoảng thời
					gian...
				</div>
			) : (
				<div className='flex flex-col gap-6'>
					{periods.map((period, index) => (
						<PeriodSection
							key={period.tempId}
							totalPeriods={periods.length}
							period={period}
							onUpdate={(updated) => handleUpdatePeriod(index, updated)}
							onDelete={() => handleDeletePeriod(index)}
						/>
					))}

					{/* Nút Thêm thời gian ở dưới cùng bên trái */}
					<div className='flex justify-start pt-1'>
						<Button
							type='button'
							variant='ghost'
							size='sm'
							onClick={handleAddPeriod}
							className='h-fit w-fit bg-transparent flex items-center gap-1.5 p-0 hover:bg-transparent'
						>
							<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
							<span className='text-sm text-black'>Thêm thời gian</span>
						</Button>
					</div>
				</div>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

interface PeriodSectionProps {
	totalPeriods: number;
	period: PeriodItemData;
	onUpdate: (updated: PeriodItemData) => void;
	onDelete: () => void;
}

function PeriodSection({
	totalPeriods,
	period,
	onUpdate,
	onDelete,
}: PeriodSectionProps) {
	const [assignments, setAssignments] = useState<ContractCode[]>([]);
	const [assets, setAssets] = useState<Asset[]>([]);
	const isFirstLoadRef = useRef(true);

	useEffect(() => {
		if (!period.startMonth) return;
		Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				date: period.startMonth,
			}),
			api.pagging<Asset>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				date: period.startMonth,
			}),
		]).then(([assignmentsRes, assetsRes]) => {
			setAssignments(assignmentsRes.result.data);
			setAssets(assetsRes.result.data);
		});
	}, [period.startMonth]);

	// Sync costs with assets after catalog loads
	useEffect(() => {
		if (assets.length === 0) return;
		if (isFirstLoadRef.current) {
			isFirstLoadRef.current = false;
			return;
		}

		const syncedCosts = sortCosts(
			normalizeCostAmounts(
				syncCostsWithSelections(period.costs, period.selectedMaterials, assets),
				assets,
			),
			assignments,
			assets,
		);

		const currentKeys = period.costs
			.map((c) => `${c.assignmentCodeId}:${c.materialId}:${c.norm}:${c.amount}`)
			.join('|');
		const nextKeys = syncedCosts
			.map((c) => `${c.assignmentCodeId}:${c.materialId}:${c.norm}:${c.amount}`)
			.join('|');

		if (currentKeys !== nextKeys) {
			onUpdate({
				...period,
				costs: syncedCosts,
			});
		}
	}, [assets, assignments]);

	const selectedAssignmentIds = period.selectedAssignments.map(
		(item) => item.value,
	);
	const currentMaterialOptions = getMaterialOptions(
		assets,
		assignments,
		selectedAssignmentIds,
	);
	const materialOptions = [
		...currentMaterialOptions,
		...period.selectedMaterials.filter(
			(option) =>
				!currentMaterialOptions.some(
					(currentOption) => currentOption.value === option.value,
				),
		),
	];

	const handleAssignmentsChange = (nextValues: MultiSelectOption[]) => {
		const nextAssignmentIds = new Set(nextValues.map((value) => value.value));
		const nextMaterials = period.selectedMaterials.filter((value) =>
			nextAssignmentIds.has(value.assignmentCodeId),
		);
		const syncedCosts = sortCosts(
			normalizeCostAmounts(
				syncCostsWithSelections(period.costs, nextMaterials, assets),
				assets,
			),
			assignments,
			assets,
		);

		onUpdate({
			...period,
			selectedAssignments: nextValues,
			selectedMaterials: nextMaterials,
			costs: syncedCosts,
		});
	};

	const handleMaterialsChange = (nextValues: MultiSelectOption[]) => {
		const nextMaterials = nextValues as SlideMaterialOption[];
		const syncedCosts = sortCosts(
			normalizeCostAmounts(
				syncCostsWithSelections(period.costs, nextMaterials, assets),
				assets,
			),
			assignments,
			assets,
		);

		onUpdate({
			...period,
			selectedMaterials: nextMaterials,
			costs: syncedCosts,
		});
	};

	const handleNormChange = (costIndex: number, newNorm: number | undefined) => {
		const targetCost = period.costs[costIndex];
		if (!targetCost) return;

		const asset = assets.find((a) => a.id === targetCost.materialId);
		const unitPrice = asset?.costAmount ?? 0;
		const normNumber =
			newNorm === undefined ? Number.NaN : Number(newNorm);
		const amount = Number.isNaN(normNumber)
			? unitPrice
			: unitPrice * normNumber;

		const nextCosts = [...period.costs];
		nextCosts[costIndex] = {
			...targetCost,
			norm: normNumber,
			amount,
		};

		onUpdate({
			...period,
			costs: nextCosts,
		});
	};

	const handleRemoveCost = (assignmentCodeId: string, materialId: string) => {
		const nextMaterials = period.selectedMaterials.filter(
			(value) =>
				value.value !==
				buildMaterialSelectionValue(assignmentCodeId, materialId),
		);
		const nextCosts = period.costs.filter(
			(cost) =>
				!(
					cost.assignmentCodeId === assignmentCodeId &&
					cost.materialId === materialId
				),
		);

		onUpdate({
			...period,
			selectedMaterials: nextMaterials,
			costs: nextCosts,
		});
	};

	return (
		<div className='bg-neutral-50/70 border-neutral-300 shadow-xs flex flex-col gap-4 rounded-xl border p-5'>
			{/* Thời gian bắt đầu - Thời gian kết thúc & Nút xóa thời gian */}
			<div className='flex items-end gap-4'>
				<MonthYearInput
					label='Thời gian bắt đầu'
					value={period.startMonth}
					onChange={(val) => onUpdate({ ...period, startMonth: val })}
					className='flex-1'
				/>
				<MonthYearInput
					label='Thời gian kết thúc'
					value={period.endMonth}
					onChange={(val) => onUpdate({ ...period, endMonth: val })}
					className='flex-1'
				/>
				{totalPeriods > 1 && (
					<Button
						type='button'
						variant='ghost'
						size='sm'
						onClick={onDelete}
						className='text-error hover:text-error-muted bg-transparent h-fit w-fit flex items-center gap-1 p-0 hover:bg-transparent mb-2.5'
					>
						<XCircleIcon className='size-4' />
						<span>Xóa thời gian</span>
					</Button>
				)}
			</div>

			{/* Nhóm vật tư */}
			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				values={period.selectedAssignments}
				onValuesChange={handleAssignmentsChange}
				options={assignments.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			{/* Vật tư theo nhóm */}
			<MultiSelect
				label='Vật tư theo nhóm'
				placeholder='Chọn vật tư theo nhóm'
				values={period.selectedMaterials}
				onValuesChange={handleMaterialsChange}
				options={materialOptions}
			/>

			{/* Bảng chi tiết vật tư của khoảng thời gian này */}
			<GroupedPeriodCosts
				costs={period.costs}
				assignments={assignments}
				assets={assets}
				onNormChange={handleNormChange}
				onRemoveCost={handleRemoveCost}
			/>
		</div>
	);
}

function GroupedPeriodCosts({
	costs,
	assignments,
	assets,
	onNormChange,
	onRemoveCost,
}: {
	costs: SlideFormSchema['costs'];
	assignments: ContractCode[];
	assets: Asset[];
	onNormChange: (index: number, norm: number | undefined) => void;
	onRemoveCost: (assignmentCodeId: string, materialId: string) => void;
}) {
	if (costs.length === 0) return null;

	const totalAmount = costs.reduce(
		(sum, c) => sum + (Number.isNaN(Number(c.amount)) ? 0 : Number(c.amount)),
		0,
	);
	const groupedCosts = groupCostsByAssignment(costs, assignments);

	return (
		<div className='flex flex-col gap-4'>
			<FormSeparator />

			<div className='flex flex-col gap-2'>
				<Label>Tổng tiền (đ/m)</Label>
				<Input
					readOnly
					value={formatNumber(totalAmount)}
					className='font-semibold read-only:bg-transparent'
				/>
			</div>

			<div className='scrollbar-sm max-h-100 overflow-auto'>
				<div className='flex flex-col gap-4'>
					{groupedCosts.map((group) => (
						<div key={group.assignmentCodeId} className='flex flex-col gap-4'>
							<FormSeparator
								className='w-full'
								label={`${group.assignmentLabel} - ${formatNumber(group.totalAmount)} (đ)`}
							/>
							{group.indices.map((index) => {
								const cost = costs[index];
								const asset = assets.find(
									(item) => item.id === cost.materialId,
								);
								const unitPrice = asset?.costAmount ?? 0;
								const isLegacySummaryRow = !cost.materialId;

								return (
									<FormRow
										key={buildMaterialSelectionValue(
											cost.assignmentCodeId,
											cost.materialId,
										)}
									>
										<div className='flex min-w-28 flex-1 flex-col gap-2'>
											<Label>Mã vật tư</Label>
											<Input
												readOnly
												value={
													asset?.code ??
													(isLegacySummaryRow ? 'Bản ghi cũ' : '')
												}
												className='read-only:bg-transparent'
											/>
										</div>

										<div className='flex min-w-36 flex-1 flex-col gap-2'>
											<Label>Tên vật tư</Label>
											<Input
												readOnly
												value={
													asset?.name ??
													(isLegacySummaryRow
														? 'Chọn lại vật tư theo nhóm trước khi lưu'
														: '')
												}
												className='read-only:bg-transparent'
											/>
										</div>

										<div className='flex min-w-28 flex-1 flex-col gap-2'>
											<Label>Đơn giá (đ)</Label>
											<Input
												readOnly
												value={formatNumber(unitPrice)}
												className='read-only:bg-transparent'
											/>
										</div>

										<div className='flex min-w-24 flex-1 flex-col gap-2'>
											<Label>Đơn vị tính</Label>
											<Input
												readOnly
												value={asset?.unitOfMeasureName ?? ''}
												className='read-only:bg-transparent'
											/>
										</div>

										<div className='flex min-w-28 flex-1 flex-col gap-2'>
											<Label>Định mức</Label>
											<FormNumberInput
												value={
													Number.isNaN(Number(cost.norm))
														? undefined
														: Number(cost.norm)
												}
												onValueChange={(val) => onNormChange(index, val)}
												placeholder='Nhập định mức'
											/>
										</div>

										<div className='flex min-w-28 flex-1 flex-col gap-2'>
											<Label>Đơn giá máng trượt (đ/m)</Label>
											<Input
												readOnly
												value={
													isLegacySummaryRow
														? formatNumber(cost.amount || 0)
														: Number.isNaN(Number(cost.norm))
															? formatNumber(unitPrice)
															: formatNumber(cost.amount || 0)
												}
												className='read-only:bg-transparent'
											/>
										</div>

										<Button
											type='button'
											variant='ghost'
											size='icon'
											className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
											onClick={() =>
												onRemoveCost(
													cost.assignmentCodeId,
													cost.materialId,
												)
											}
										>
											<XCircleIcon className='size-6' />
										</Button>
									</FormRow>
								);
							})}
						</div>
					))}
				</div>
			</div>
		</div>
	);
}
