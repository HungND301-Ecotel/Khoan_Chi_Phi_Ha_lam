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
import type { Technology } from '@/features/main/catalog/parameter/technology/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import {
	SUPPORT_AND_DRILLING_COMMON_FORM_DEFAULT,
	supportAndDrillingCommonFormSchema,
	type SupportAndDrillingCommonFormSchema,
	type SupportAndDrillingFormSchema,
} from './support-and-drilling-schema';
import type {
	MaterialDetailCost,
	SupportAndDrillingMaterial,
	SupportAndDrillingMaterialDetail,
} from './type';

type MaterialOption = MultiSelectOption & {
	assignmentCodeId: string;
	materialId: string;
};

type MaterialAsset = Asset & {
	materialType?: number;
};

interface PeriodItemData {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	otherMaterialValue?: number;
	costs: SupportAndDrillingFormSchema['costs'];
	selectedAssignments: MultiSelectOption[];
	selectedMaterials: MaterialOption[];
	persistedCosts: MaterialDetailCost[];
}

const buildMaterialSelectionValue = (
	assignmentCodeId: string,
	materialId: string,
) => `${assignmentCodeId}::${materialId}`;

const buildMaterialOption = (
	assignment: ContractCode | undefined,
	asset: MaterialAsset | undefined,
): MaterialOption | null => {
	if (!assignment || !asset) return null;

	return {
		value: buildMaterialSelectionValue(assignment.id, asset.id),
		label: `[${assignment.code}] ${asset.code} - ${asset.name}`,
		assignmentCodeId: assignment.id,
		materialId: asset.id,
	};
};

const buildPersistedMaterialOption = (
	cost: MaterialDetailCost,
	assignment: ContractCode | undefined,
): MaterialOption => {
	const assignmentCode = assignment?.code ?? cost.assignmentCode;
	return {
		value: buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
		label: `[${assignmentCode}] ${cost.materialCode} - ${cost.materialName}`,
		assignmentCodeId: cost.assignmentCodeId,
		materialId: cost.materialId,
	};
};

const getMaterialOptions = (
	assets: MaterialAsset[],
	assignments: ContractCode[],
	selectedAssignmentIds: string[],
) => {
	const assignmentMap = new Map(assignments.map((item) => [item.id, item]));
	const options: MaterialOption[] = [];

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

const syncCostsWithSelections = (
	costs: SupportAndDrillingFormSchema['costs'],
	selectedMaterialOptions: MaterialOption[],
	assets: MaterialAsset[],
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
			return {
				assignmentCodeId: option.assignmentCodeId,
				materialId: option.materialId,
				norm: Number.NaN,
				totalPrice: asset?.costAmount ?? 0,
			};
		});

	return [...existingRows, ...addedRows];
};

const normalizeCostTotals = (
	costs: SupportAndDrillingFormSchema['costs'],
	assets: MaterialAsset[],
) => {
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	return costs.map((cost) => {
		const asset = assetMap.get(cost.materialId);
		if (!asset) {
			return cost;
		}
		const unitPrice = asset.costAmount ?? 0;
		const totalPrice = Number.isNaN(Number(cost.norm))
			? unitPrice
			: unitPrice * Number(cost.norm);

		return {
			...cost,
			totalPrice,
		};
	});
};

const sortCosts = (
	costs: SupportAndDrillingFormSchema['costs'],
	assignments: ContractCode[],
	assets: MaterialAsset[],
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

const sumMaterialCosts = (costs: SupportAndDrillingFormSchema['costs']) =>
	costs.reduce(
		(sum, cost) =>
			sum +
			(Number.isNaN(Number(cost.totalPrice)) ? 0 : Number(cost.totalPrice)),
		0,
	);

const groupCostsByAssignment = (
	costs: SupportAndDrillingFormSchema['costs'],
	assignments: ContractCode[],
) => {
	const grouped = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentLabel: string;
			indices: number[];
			totalPrice: number;
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
		const rowTotal = Number.isNaN(Number(cost.totalPrice))
			? 0
			: Number(cost.totalPrice);

		if (!current) {
			grouped.set(cost.assignmentCodeId, {
				assignmentCodeId: cost.assignmentCodeId,
				assignmentLabel,
				indices: [index],
				totalPrice: rowTotal,
			});
			return;
		}

		current.indices.push(index);
		current.totalPrice += rowTotal;
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

export function SupportAndDrillingForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<SupportAndDrillingMaterial> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [technologies, setTechnologies] = useState<Technology[]>([]);
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

	const form = useForm<
		SupportAndDrillingCommonFormSchema,
		unknown,
		SupportAndDrillingCommonFormSchema
	>({
		resolver: zodResolver(supportAndDrillingCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: SUPPORT_AND_DRILLING_COMMON_FORM_DEFAULT,
	});

	useEffect(() => {
		Promise.all([
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
			api.pagging<Technology>(API.CATALOG.PARAMETER.TECHNOLOGY.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
		]).then(([processesRes, technologiesRes, passportsRes, strengthsRes]) => {
			setProcesses(processesRes.result.data);
			setTechnologies(technologiesRes.result.data);
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
			processId: row.processId,
			technologyId: row.technologyId ?? '',
			passportId: row.passportId,
			hardnessId: row.hardnessId,
		});

		const rawPeriods =
			row.periods && row.periods.length > 0
				? row.periods
				: row.id
					? [
							{
								id: row.id,
								startMonth: row.startMonth,
								endMonth: row.endMonth,
								totalPrice: row.totalPrice ?? 0,
							},
						]
					: [];

		if (rawPeriods.length === 0) {
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
						endMonth: (p.endMonth ?? new Date().toISOString()).substring(0, 10),
						costs: [],
						selectedAssignments: [],
						selectedMaterials: [],
						persistedCosts: [],
					};
				}
				try {
					const res = await api.get<SupportAndDrillingMaterialDetail>(
						API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DETAIL(p.id),
					);
					const detail = res.result;
					const persistedCosts = detail.costs ?? [];
					const selectedAssignments = Array.from(
						new Map(
							persistedCosts
								.filter((cost) => !!cost.assignmentCodeId)
								.map((cost) => [
									cost.assignmentCodeId,
									{
										value: cost.assignmentCodeId,
										label: `${cost.assignmentCode} - ${cost.assignmentCodeName}`,
									},
								]),
						).values(),
					);
					const selectedMaterials = persistedCosts
						.map((cost) => buildPersistedMaterialOption(cost, undefined))
						.filter((option): option is MaterialOption => !!option);
					const normalizedCosts = persistedCosts.map((cost) => ({
						assignmentCodeId: cost.assignmentCodeId,
						materialId: cost.materialId,
						norm: cost.norm,
						totalPrice: cost.totalPrice,
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
						otherMaterialValue:
							detail.otherMaterialValue || detail.otherMaterialValue === 0
								? detail.otherMaterialValue
								: undefined,
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
			otherMaterialValue: undefined,
			costs: [],
			selectedAssignments: [],
			selectedMaterials: [],
			persistedCosts: [],
		};

		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleDeletePeriod = (indexToDelete: number) => {
		if (periods.length <= 1) {
			popup.error('Mã đơn giá cần có ít nhất một khoảng thời gian.');
			return;
		}

		const periodToDelete = periods[indexToDelete];
		if (periodToDelete.id) {
			setDeletedPeriodIds((prev) => [...prev, periodToDelete.id!]);
		}

		setPeriods((prev) => prev.filter((_, idx) => idx !== indexToDelete));
	};

	const handleSubmit = async (values: SupportAndDrillingCommonFormSchema) => {
		try {
			const validationResult = validatePeriods(periods);
			if (validationResult) {
				popup.error(validationResult.error);
				return;
			}

			if (deletedPeriodIds.length > 0) {
				await api.delete(
					API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DELETES,
					deletedPeriodIds,
				);
			}

			for (const period of periods) {
				const payload = {
					code: values.code,
					processId: values.processId,
					technologyId: values.technologyId,
					passportId: values.passportId,
					hardnessId: values.hardnessId,
					startMonth: period.startMonth,
					endMonth: period.endMonth,
					otherMaterialValue: period.otherMaterialValue,
					costs: period.costs,
				};

				if (period.id && !isDuplicate) {
					await api.put(API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.UPDATE, {
						id: period.id,
						...payload,
					});
				} else {
					await api.post(
						API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.CREATE,
						payload,
					);
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
			{/* Thông tin chung đơn giá */}
			<FormInput
				control={form.control}
				name='code'
				label='Mã đơn giá'
				placeholder='Nhập mã đơn giá'
			/>

			<FormComboBox
				control={form.control}
				name='processId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={processes.map((process) => ({
					label: process.name,
					value: process.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='technologyId'
				label='Công nghệ'
				placeholder='Chọn công nghệ'
				options={technologies.map((technology) => ({
					label: technology.value,
					value: technology.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='passportId'
				label='Hộ chiếu'
				placeholder='Chọn hộ chiếu'
				options={passports.map((passport) => ({
					label: `H/c ${passport.name}; ${passport.sd}; ${passport.sc}`,
					value: passport.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố than đá'
				placeholder='Chọn độ kiên cố than đá'
				options={strengths.map((strength) => ({
					label: strength.value,
					value: strength.id,
				}))}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			{/* Danh sách các form khoảng thời gian xếp dọc */}
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
	const [assets, setAssets] = useState<MaterialAsset[]>([]);
	const isFirstLoadRef = useRef(true);

	useEffect(() => {
		if (!period.startMonth) return;
		Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				date: period.startMonth,
			}),
			api.pagging<MaterialAsset>(API.CATALOG.ASSET.LIST, {
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
			normalizeCostTotals(
				syncCostsWithSelections(period.costs, period.selectedMaterials, assets),
				assets,
			),
			assignments,
			assets,
		);

		const currentKeys = period.costs
			.map((c) => `${c.assignmentCodeId}:${c.materialId}:${c.norm}:${c.totalPrice}`)
			.join('|');
		const nextKeys = syncedCosts
			.map((c) => `${c.assignmentCodeId}:${c.materialId}:${c.norm}:${c.totalPrice}`)
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
			normalizeCostTotals(
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
		const nextMaterials = nextValues as MaterialOption[];
		const syncedCosts = sortCosts(
			normalizeCostTotals(
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
		const totalPrice = Number.isNaN(normNumber)
			? unitPrice
			: unitPrice * normNumber;

		const nextCosts = [...period.costs];
		nextCosts[costIndex] = {
			...targetCost,
			norm: normNumber,
			totalPrice,
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

	const handleOtherMaterialChange = (newVal: number | undefined) => {
		onUpdate({
			...period,
			otherMaterialValue: newVal,
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
				otherMaterialValue={period.otherMaterialValue}
				assignments={assignments}
				assets={assets}
				onNormChange={handleNormChange}
				onRemoveCost={handleRemoveCost}
				onOtherMaterialChange={handleOtherMaterialChange}
			/>
		</div>
	);
}

function GroupedPeriodCosts({
	costs,
	otherMaterialValue,
	assignments,
	assets,
	onNormChange,
	onRemoveCost,
	onOtherMaterialChange,
}: {
	costs: SupportAndDrillingFormSchema['costs'];
	otherMaterialValue?: number;
	assignments: ContractCode[];
	assets: MaterialAsset[];
	onNormChange: (index: number, norm: number | undefined) => void;
	onRemoveCost: (assignmentCodeId: string, materialId: string) => void;
	onOtherMaterialChange: (val: number | undefined) => void;
}) {
	if (costs.length === 0) return null;

	const materialTotal = sumMaterialCosts(costs);
	const otherMaterialCost =
		(materialTotal * (Number(otherMaterialValue) || 0)) / 100;
	const totalPrice =
		materialTotal + (Number.isNaN(otherMaterialCost) ? 0 : otherMaterialCost);
	const groupedCosts = groupCostsByAssignment(costs, assignments);

	return (
		<div className='flex flex-col gap-4'>
			<FormSeparator />

			<div className='flex flex-col gap-2'>
				<Label>Tổng tiền (đ/m)</Label>
				<Input
					readOnly
					value={formatNumber(totalPrice)}
					className='font-semibold read-only:bg-transparent'
				/>
			</div>

			<div className='scrollbar-sm max-h-100 overflow-auto'>
				<div className='flex flex-col gap-4'>
					{groupedCosts.map((group) => (
						<div key={group.assignmentCodeId} className='flex flex-col gap-4'>
							<FormSeparator
								className='w-full'
								label={`${group.assignmentLabel} - ${formatNumber(group.totalPrice)} (đ)`}
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
											<Label>Đơn giá vật liệu (đ/m)</Label>
											<Input
												readOnly
												value={
													isLegacySummaryRow
														? formatNumber(cost.totalPrice || 0)
														: Number.isNaN(Number(cost.norm))
															? formatNumber(unitPrice)
															: formatNumber(cost.totalPrice || 0)
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

					{otherMaterialValue !== undefined ? (
						<div className='flex flex-col gap-4'>
							<FormSeparator
								className='w-full'
								label={`VTK - Vật tư khác - ${formatNumber(otherMaterialCost)} (đ)`}
							/>
							<FormRow>
								<div className='flex min-w-28 flex-1 flex-col gap-2'>
									<Label>Mã vật tư</Label>
									<Input
										readOnly
										value='VTK'
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-36 flex-1 flex-col gap-2'>
									<Label>Tên vật tư</Label>
									<Input
										readOnly
										value='Vật tư khác'
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-28 flex-1 flex-col gap-2'>
									<Label>Đơn giá (đ)</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-24 flex-1 flex-col gap-2'>
									<Label>Đơn vị tính</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-28 flex-1 flex-col gap-2'>
									<Label>Định mức (%)</Label>
									<FormNumberInput
										value={otherMaterialValue}
										onValueChange={onOtherMaterialChange}
										placeholder='Nhập % từ 1 đến 100'
									/>
								</div>

								<div className='flex min-w-28 flex-1 flex-col gap-2'>
									<Label>Đơn giá vật liệu (đ/m)</Label>
									<Input
										readOnly
										value={formatNumber(
											isNaN(otherMaterialCost) ? 0 : otherMaterialCost,
										)}
										className='read-only:bg-transparent'
									/>
								</div>

								<Button
									type='button'
									variant='ghost'
									size='icon'
									className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
									onClick={() => onOtherMaterialChange(undefined)}
								>
									<XCircleIcon className='size-6' />
								</Button>
							</FormRow>
						</div>
					) : (
						<div
							className='flex cursor-pointer items-center gap-2'
							onClick={() => onOtherMaterialChange(1)}
						>
							<Button
								type='button'
								variant='ghost'
								size='icon'
								className='bg-transparent text-cyan-600 hover:text-cyan-700'
								title='Thêm vật tư khác'
							>
								<PlusCircleIcon className='size-6' />
							</Button>
							<span className='text-sm text-black'>Thêm vật tư khác</span>
						</div>
					)}
				</div>
			</div>
		</div>
	);
}
