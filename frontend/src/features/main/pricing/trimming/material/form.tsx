import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Insert } from '@/features/main/catalog/parameter/insert/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Step } from '@/features/main/catalog/parameter/step/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import {
	MATERIAL_FORM_DEFAULT,
	materialFormSchema,
	type MaterialFormSchema,
} from '@/features/main/pricing/trimming/material/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import type { Material, MaterialDetail, MaterialDetailCost } from './type';

type MaterialOption = MultiSelectOption & {
	assignmentCodeId: string;
	materialId: string;
};

type MaterialAsset = Asset & {
	materialType?: number;
};

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

const arraysEqual = (left: string[], right: string[]) =>
	left.length === right.length &&
	left.every((value, index) => value === right[index]);

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
	costs: MaterialFormSchema['costs'],
	selectedMaterialOptions: MaterialOption[],
	assets: MaterialAsset[],
) => {
	const selectedKeys = new Set(
		selectedMaterialOptions.map((option) => option.value),
	);
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	const existingRows = costs.filter(
		(cost) =>
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
	costs: MaterialFormSchema['costs'],
	assets: MaterialAsset[],
) => {
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	return costs.map((cost) => {
		const unitPrice = assetMap.get(cost.materialId)?.costAmount ?? 0;
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
	costs: MaterialFormSchema['costs'],
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

const sumMaterialCosts = (costs: MaterialFormSchema['costs']) =>
	costs.reduce(
		(sum, cost) =>
			sum +
			(Number.isNaN(Number(cost.totalPrice)) ? 0 : Number(cost.totalPrice)),
		0,
	);

const groupCostsByAssignment = (
	costs: MaterialFormSchema['costs'],
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

export function MaterialForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Material> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [inserts, setInserts] = useState<Insert[]>([]);
	const [steps, setSteps] = useState<Step[]>([]);
	const [assignments, setAssignments] = useState<ContractCode[]>([]);
	const [assets, setAssets] = useState<MaterialAsset[]>([]);
	const [selectedAssignments, setSelectedAssignments] = useState<
		MultiSelectOption[]
	>([]);
	const [selectedMaterials, setSelectedMaterials] = useState<MaterialOption[]>(
		[],
	);

	const persistedCostsRef = useRef<MaterialDetailCost[]>([]);
	const form = useForm<MaterialFormSchema, unknown, MaterialFormSchema>({
		resolver: zodResolver(materialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	const watchedStartMonth = useWatch({
		control: form.control,
		name: 'startMonth',
		defaultValue: MATERIAL_FORM_DEFAULT.startMonth,
	});
	const watchedCosts = useWatch({
		control: form.control,
		name: 'costs',
		defaultValue: MATERIAL_FORM_DEFAULT.costs,
	});
	const selectedAssignmentIds = selectedAssignments.map((item) => item.value);

	useEffect(() => {
		Promise.all([
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<Insert>(API.CATALOG.PARAMETER.INSERT.LIST),
			api.pagging<Step>(API.CATALOG.PARAMETER.STEP.LIST),
		]).then(
			([processesRes, passportsRes, strengthsRes, insertsRes, stepsRes]) => {
				setProcesses(processesRes.result.data);
				setPassports(passportsRes.result.data);
				setStrengths(strengthsRes.result.data);
				setInserts(insertsRes.result.data);
				setSteps(stepsRes.result.data);
			},
		);
	}, []);

	useEffect(() => {
		const effectiveStartMonth =
			watchedStartMonth || form.getValues('startMonth') || '';
		if (!effectiveStartMonth) return;

		Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				date: effectiveStartMonth,
			}),
			api.pagging<MaterialAsset>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				date: effectiveStartMonth,
			}),
		]).then(([assignmentsRes, assetsRes]) => {
			setAssignments(assignmentsRes.result.data);
			setAssets(assetsRes.result.data);
		});
	}, [form, watchedStartMonth]);

	useEffect(() => {
		if (!row) return;

		form.reset({
			...MATERIAL_FORM_DEFAULT,
			startMonth: row.startMonth.substring(0, 10),
			endMonth: row.endMonth.substring(0, 10),
			code: isDuplicate ? '' : row.code,
			processId: row.processId,
			passportId: row.passportId,
			hardnessId: row.hardnessId,
			insertItemId: row.insertItemId,
			supportStepId: row.supportStepId,
		});

		api
			.get<MaterialDetail>(API.PRICING.MATERIAL.TRIMMING.DETAIL(row.id))
			.then((res) => {
				const detail = res.result;
				persistedCostsRef.current = detail.costs ?? [];
				setSelectedAssignments(
					Array.from(
						new Map(
							(detail.costs ?? [])
								.filter((cost) => !!cost.assignmentCodeId)
								.map((cost) => [
									cost.assignmentCodeId,
									{
										value: cost.assignmentCodeId,
										label: `${cost.assignmentCode} - ${cost.assignmentCodeName}`,
									},
								]),
						).values(),
					),
				);
				setSelectedMaterials(
					(detail.costs ?? [])
						.map((cost) => buildPersistedMaterialOption(cost, undefined))
						.filter((option): option is MaterialOption => !!option),
				);
				const normalizedCosts = (detail.costs ?? []).map((cost) => ({
					assignmentCodeId: cost.assignmentCodeId,
					materialId: cost.materialId,
					norm: cost.norm,
					totalPrice: cost.totalPrice,
				}));
				form.setValue('costs', normalizedCosts);
				form.setValue(
					'otherMaterialValue',
					detail.otherMaterialValue || detail.otherMaterialValue === 0
						? detail.otherMaterialValue
						: undefined,
				);
			});
	}, [form, isDuplicate, row]);

	useEffect(() => {
		if (assignments.length === 0 && assets.length === 0) return;

		const persistedCosts = persistedCostsRef.current;
		const currentCosts = form.getValues('costs') ?? [];
		const persistedCostMap = new Map(
			persistedCosts.map((cost) => [
				buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
				cost,
			]),
		);
		const persistedAssignmentMap = new Map(
			currentCosts.map((cost) => {
				const persistedCost = persistedCostMap.get(
					buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
				);
				return [
					cost.assignmentCodeId,
					{
						value: cost.assignmentCodeId,
						label: `${persistedCost?.assignmentCode ?? ''} - ${persistedCost?.assignmentCodeName ?? ''}`,
					},
				] as const;
			}),
		);
		const persistedAssignmentIds = Array.from(persistedAssignmentMap.keys());
		const selectedAssignmentSet = new Set([
			...selectedAssignmentIds,
			...persistedAssignmentIds,
		]);
		const normalizedAssignments = Array.from(
			new Map(
				[
					...assignments
						.filter((assignment) => selectedAssignmentSet.has(assignment.id))
						.map<MultiSelectOption>((assignment) => ({
							value: assignment.id,
							label: `${assignment.code} - ${assignment.name}`,
						})),
					...Array.from(persistedAssignmentMap.values()).filter(
						(item) =>
							!assignments.some((assignment) => assignment.id === item.value),
					),
				].map((item) => [item.value, item]),
			).values(),
		);

		if (
			!arraysEqual(
				selectedAssignments.map((item) => item.value),
				normalizedAssignments.map((item) => item.value),
			)
		) {
			setSelectedAssignments(normalizedAssignments);
		}

		const currentOptions = getMaterialOptions(
			assets,
			assignments,
			normalizedAssignments.map((item) => item.value),
		);
		const currentOptionMap = new Map(
			currentOptions.map((option) => [option.value, option]),
		);
		const selectedMaterialMap = new Map(
			selectedMaterials.map((option) => [option.value, option]),
		);
		const currentCostKeys = new Set(
			currentCosts.map((cost) =>
				buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
			),
		);
		const persistedSelectedOptions = Array.from(currentCostKeys)
			.map((key) => persistedCostMap.get(key))
			.filter((cost): cost is MaterialDetailCost => !!cost)
			.map((cost) =>
				buildPersistedMaterialOption(
					cost,
					assignments.find(
						(assignment) => assignment.id === cost.assignmentCodeId,
					),
				),
			)
			.filter((option): option is MaterialOption => !!option);

		const mergedSelectedMaterials = [
			...selectedMaterials
				.map(
					(option) =>
						currentOptionMap.get(option.value) ??
						selectedMaterialMap.get(option.value),
				)
				.filter((option): option is MaterialOption => !!option),
			...persistedSelectedOptions.filter(
				(option) =>
					!selectedMaterialMap.has(option.value) &&
					!currentOptionMap.has(option.value),
			),
		];
		const uniqueSelectedMaterials = Array.from(
			new Map(
				mergedSelectedMaterials.map((option) => [option.value, option]),
			).values(),
		);

		if (
			!arraysEqual(
				selectedMaterials.map((item) => item.value),
				uniqueSelectedMaterials.map((item) => item.value),
			)
		) {
			setSelectedMaterials(uniqueSelectedMaterials);
		}
	}, [
		assignments,
		assets,
		row,
		selectedAssignments,
		selectedAssignmentIds,
		selectedMaterials,
		form,
	]);

	useEffect(() => {
		const isHydratingPersistedSelections =
			selectedMaterials.length === 0 &&
			persistedCostsRef.current.length > 0 &&
			watchedCosts.length > 0;
		if (isHydratingPersistedSelections) return;

		const syncedCosts = sortCosts(
			normalizeCostTotals(
				syncCostsWithSelections(watchedCosts, selectedMaterials, assets),
				assets,
			),
			assignments,
			assets,
		);

		const currentKeys = watchedCosts.map(
			(cost) =>
				`${cost.assignmentCodeId}:${cost.materialId}:${cost.norm}:${cost.totalPrice}`,
		);
		const nextKeys = syncedCosts.map(
			(cost) =>
				`${cost.assignmentCodeId}:${cost.materialId}:${cost.norm}:${cost.totalPrice}`,
		);

		if (!arraysEqual(currentKeys, nextKeys)) {
			form.setValue('costs', syncedCosts, {
				shouldValidate: false,
			});
		}
	}, [assets, assignments, form, selectedMaterials, watchedCosts]);

	const currentMaterialOptions = getMaterialOptions(
		assets,
		assignments,
		selectedAssignmentIds,
	);
	const materialOptions = [
		...currentMaterialOptions,
		...selectedMaterials.filter(
			(option) =>
				!currentMaterialOptions.some(
					(currentOption) => currentOption.value === option.value,
				),
		),
	];

	const handleSubmit = async (values: MaterialFormSchema) => {
		try {
			const processedValues = {
				...values,
				costs: normalizeCostTotals(values.costs, assets),
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.MATERIAL.TRIMMING.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(API.PRICING.MATERIAL.TRIMMING.CREATE, processedValues);
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
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức vật liệu'
				placeholder='Nhập mã định mức vật liệu'
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

			<FormComboBox
				control={form.control}
				name='insertItemId'
				label='Chèn'
				placeholder='Chọn chèn'
				options={inserts.map((insert) => ({
					label: insert.value,
					value: insert.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='supportStepId'
				label='Bước chống'
				placeholder='Chọn bước chống'
				options={steps.map((step) => ({
					label: step.value,
					value: step.id,
				}))}
			/>

			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				values={selectedAssignments}
				onValuesChange={(nextValues) => {
					setSelectedAssignments(nextValues);

					const nextAssignmentIds = new Set(
						nextValues.map((value) => value.value),
					);
					setSelectedMaterials((currentValues) =>
						currentValues.filter((value) =>
							nextAssignmentIds.has(value.assignmentCodeId),
						),
					);
				}}
				options={assignments.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			<MultiSelect
				label='Vật tư theo nhóm'
				placeholder='Chọn vật tư theo nhóm'
				values={selectedMaterials}
				onValuesChange={(nextValues) =>
					setSelectedMaterials(nextValues as MaterialOption[])
				}
				options={materialOptions}
			/>

			<GroupedMaterialCosts
				assignments={assignments}
				assets={assets}
				onRemove={(assignmentCodeId, materialId) => {
					setSelectedMaterials((currentValues) =>
						currentValues.filter(
							(value) =>
								value.value !==
								buildMaterialSelectionValue(assignmentCodeId, materialId),
						),
					);
					form.setValue(
						'costs',
						form
							.getValues('costs')
							.filter(
								(cost) =>
									!(
										cost.assignmentCodeId === assignmentCodeId &&
										cost.materialId === materialId
									),
							),
						{
							shouldValidate: false,
						},
					);
				}}
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function GroupedMaterialCosts({
	assignments,
	assets,
	onRemove,
}: {
	assignments: ContractCode[];
	assets: MaterialAsset[];
	onRemove: (assignmentCodeId: string, materialId: string) => void;
}) {
	const { control } = useFormContext<MaterialFormSchema>();
	const costs = useWatch({ control, name: 'costs' }) ?? [];
	const otherMaterialPercent = useWatch({
		control,
		name: 'otherMaterialValue',
	});

	if (costs.length === 0) return null;

	const materialTotal = sumMaterialCosts(costs);
	const otherMaterialCost =
		(materialTotal * (Number(otherMaterialPercent) || 0)) / 100;
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
								return (
									<FormRow
										key={buildMaterialSelectionValue(
											cost.assignmentCodeId,
											cost.materialId,
										)}
									>
										<MaterialCostRow
											index={index}
											assets={assets}
											onRemove={onRemove}
										/>
									</FormRow>
								);
							})}
						</div>
					))}

					{otherMaterialPercent !== undefined ? (
						<div className='flex flex-col gap-4'>
							<FormSeparator
								className='w-full'
								label={`VTK - Vật tư khác - ${formatNumber(otherMaterialCost)} (đ)`}
							/>
							<VtkRow materialTotal={materialTotal} />
						</div>
					) : (
						<OtherMaterialAddButton />
					)}
				</div>
			</div>
		</div>
	);
}

function MaterialCostRow({
	index,
	assets,
	onRemove,
}: {
	index: number;
	assets: MaterialAsset[];
	onRemove: (assignmentCodeId: string, materialId: string) => void;
}) {
	const { control, getValues } = useFormContext<MaterialFormSchema>();
	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const materialId = getValues(`costs.${index}.materialId`);
	const asset = assets.find((item) => item.id === materialId);
	const norm = useWatch({ control, name: `costs.${index}.norm` });
	const totalPrice = useWatch({ control, name: `costs.${index}.totalPrice` });
	const unitPrice = asset?.costAmount ?? 0;
	const isLegacySummaryRow = !materialId;

	return (
		<>
			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Mã vật tư</Label>
				<Input
					readOnly
					value={asset?.code ?? (isLegacySummaryRow ? 'Bản ghi cũ' : '')}
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
				<FormNumber
					control={control}
					name={`costs.${index}.norm`}
					label='Định mức'
					placeholder='Nhập định mức'
				/>
			</div>

			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Đơn giá vật liệu (đ/m)</Label>
				<Input
					readOnly
					value={
						isLegacySummaryRow
							? formatNumber(totalPrice || 0)
							: Number.isNaN(Number(norm))
								? formatNumber(unitPrice)
								: formatNumber(totalPrice || 0)
					}
					className='read-only:bg-transparent'
				/>
			</div>

			<Button
				type='button'
				variant='ghost'
				size='icon'
				className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
				onClick={() => onRemove(assignmentCodeId, materialId)}
			>
				<XCircleIcon className='size-6' />
			</Button>
		</>
	);
}

function OtherMaterialAddButton() {
	const { setValue } = useFormContext<MaterialFormSchema>();

	return (
		<div
			className='flex cursor-pointer items-center gap-2'
			onClick={() =>
				setValue('otherMaterialValue', 1, {
					shouldValidate: false,
				})
			}
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
	);
}

function VtkRow({ materialTotal }: { materialTotal: number }) {
	const { control, setValue } = useFormContext<MaterialFormSchema>();
	const otherMaterialPercent = useWatch({
		control,
		name: 'otherMaterialValue',
	});
	const otherMaterialCost =
		(materialTotal * (Number(otherMaterialPercent) || 0)) / 100;

	return (
		<FormRow>
			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Mã vật tư</Label>
				<Input readOnly value='VTK' className='read-only:bg-transparent' />
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
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex min-w-24 flex-1 flex-col gap-2'>
				<Label>Đơn vị tính</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name='otherMaterialValue'
					label='Định mức (%)'
					placeholder='Nhập % từ 1 đến 100'
				/>
			</div>

			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Đơn giá vật liệu (đ/m)</Label>
				<Input
					readOnly
					value={formatNumber(isNaN(otherMaterialCost) ? 0 : otherMaterialCost)}
					className='read-only:bg-transparent'
				/>
			</div>

			<Button
				type='button'
				variant='ghost'
				size='icon'
				className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
				onClick={() =>
					setValue('otherMaterialValue', undefined, {
						shouldValidate: false,
					})
				}
			>
				<XCircleIcon className='size-6' />
			</Button>
		</FormRow>
	);
}
