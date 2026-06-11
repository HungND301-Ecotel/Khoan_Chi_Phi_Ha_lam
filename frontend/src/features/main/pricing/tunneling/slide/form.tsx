import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
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
import { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Slide } from '@/features/main/pricing/tunneling/slide/columns';
import {
	SLIDE_FORM_DEFAULT,
	slideFormSchema,
	SlideFormSchema,
} from '@/features/main/pricing/tunneling/slide/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';

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
		assignmentCodeName: string;
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

const buildMaterialSelectionValue = (
	assignmentCodeId: string,
	materialId: string,
) => `${assignmentCodeId}::${materialId}`;

const buildMaterialOption = (
	contract: ContractCode | undefined,
	asset: Asset | undefined,
): SlideMaterialOption | null => {
	if (!contract || !asset) return null;

	return {
		value: buildMaterialSelectionValue(contract.id, asset.id),
		label: `[${contract.code}] ${asset.code} - ${asset.name}`,
		assignmentCodeId: contract.id,
		materialId: asset.id,
	};
};

const buildPersistedMaterialOption = (
	cost: PersistedSlideMaterialCost,
	contract: ContractCode | undefined,
): SlideMaterialOption => {
	const assignmentCode = contract?.code ?? cost.assignmentCode;

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
	assets: Asset[],
	contracts: ContractCode[],
	selectedContractIds: string[],
) => {
	const contractMap = new Map(contracts.map((item) => [item.id, item]));
	const options: SlideMaterialOption[] = [];

	selectedContractIds.forEach((assignmentCodeId) => {
		const contract = contractMap.get(assignmentCodeId);
		assets
			.filter((asset) => asset.assignmentCodeIds.includes(assignmentCodeId))
			.sort((left, right) =>
				`${left.code} ${left.name}`.localeCompare(
					`${right.code} ${right.name}`,
					'vi',
				),
			)
			.forEach((asset) => {
				const option = buildMaterialOption(contract, asset);
				if (option) {
					options.push(option);
				}
			});
	});

	return options;
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
		.map((option) => ({
			assignmentCodeId: option.assignmentCodeId,
			materialId: option.materialId,
			norm: Number.NaN,
			amount: assetMap.get(option.materialId)?.costAmount ?? 0,
		}));

	return [...existingRows, ...addedRows];
};

const deriveNormFromAmount = (amount: number, unitPrice: number) => {
	if (unitPrice > 0) {
		return amount / unitPrice;
	}

	return amount === 0 ? 0 : amount;
};

const normalizeCostAmounts = (
	costs: SlideFormSchema['costs'],
	assets: Asset[],
) => {
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	return costs.map((cost) => {
		const unitPrice = assetMap.get(cost.materialId)?.costAmount ?? 0;
		const amount = Number.isNaN(Number(cost.norm))
			? unitPrice
			: unitPrice * Number(cost.norm);

		return {
			...cost,
			amount,
		};
	});
};

const sortCosts = (
	costs: SlideFormSchema['costs'],
	contracts: ContractCode[],
	assets: Asset[],
) => {
	const contractOrder = new Map(
		contracts.map((contract, index) => [contract.id, index]),
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
		const contractCompare =
			(contractOrder.get(left.assignmentCodeId) ?? Number.MAX_SAFE_INTEGER) -
			(contractOrder.get(right.assignmentCodeId) ?? Number.MAX_SAFE_INTEGER);
		if (contractCompare !== 0) return contractCompare;

		return (
			(assetOrder.get(left.materialId) ?? Number.MAX_SAFE_INTEGER) -
			(assetOrder.get(right.materialId) ?? Number.MAX_SAFE_INTEGER)
		);
	});
};

function groupCostsByAssignmentCode(
	costs: SlideFormSchema['costs'],
	contracts: ContractCode[],
): Array<{
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	indices: number[];
	totalAmount: number;
}> {
	const grouped = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentCode: string;
			assignmentCodeName: string;
			indices: number[];
		}
	>();

	costs.forEach((cost, index) => {
		const assignmentCodeId = cost.assignmentCodeId;
		const contract = contracts.find((c) => c.id === assignmentCodeId);

		if (!grouped.has(assignmentCodeId)) {
			grouped.set(assignmentCodeId, {
				assignmentCodeId,
				assignmentCode: contract?.code || '',
				assignmentCodeName: contract?.name || '',
				indices: [],
			});
		}

		grouped.get(assignmentCodeId)?.indices.push(index);
	});

	const groups = Array.from(grouped.values()).sort((a, b) =>
		a.assignmentCode.localeCompare(b.assignmentCode),
	);

	// Calculate total amount for each group
	return groups.map((group) => {
		let totalAmount = 0;

		group.indices.forEach((index) => {
			const cost = costs[index];
			if (!cost) return;

			const amount = cost.amount || 0;
			if (!isNaN(amount)) {
				totalAmount += amount;
			}
		});

		return {
			...group,
			totalAmount,
		};
	});
}

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
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [assets, setAssets] = useState<Asset[]>([]);
	const [selectedContracts, setSelectedContracts] = useState<
		MultiSelectOption[]
	>([]);
	const [selectedMaterials, setSelectedMaterials] = useState<
		SlideMaterialOption[]
	>([]);
	const persistedCostsRef = useRef<PersistedSlideMaterialCost[]>([]);

	const form = useForm<SlideFormSchema>({
		resolver: zodResolver(slideFormSchema),
		mode: 'onSubmit',
		defaultValues: SLIDE_FORM_DEFAULT,
	});

	const watchedStartMonth = useWatch({
		control: form.control,
		name: 'startMonth',
		defaultValue: SLIDE_FORM_DEFAULT.startMonth,
	});
	const watchedCosts = useWatch({
		control: form.control,
		name: 'costs',
		defaultValue: SLIDE_FORM_DEFAULT.costs,
	});
	const selectedContractIds = selectedContracts.map((item) => item.value);

	useEffect(() => {
		Promise.all([
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
		]).then(([processesRes, passportsRes, strengthsRes]) => {
			setGroups(processesRes.result.data);
			setPassports(passportsRes.result.data);
			setStrengths(strengthsRes.result.data);
		});
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
			api.pagging<Asset>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				date: effectiveStartMonth,
			}),
		]).then(([contractsRes, assetsRes]) => {
			setContracts(contractsRes.result.data);
			setAssets(assetsRes.result.data);
		});
	}, [form, watchedStartMonth]);

	useEffect(() => {
		if (!row) return;

		form.reset({
			...SLIDE_FORM_DEFAULT,
			startMonth: row.startMonth.substring(0, 10),
			endMonth: row.endMonth.substring(0, 10),
			code: isDuplicate ? '' : row.code,
			processGroupId: row.processGroupId,
			passportId: row.passportId,
			hardnessId: row.hardnessId,
		});

		api.get<SlideDetail>(API.PRICING.SLIDE.DETAIL(row.id)).then((res) => {
			const persistedCosts = res.result.materialCost.flatMap((group) =>
				group.costs.map((cost) => ({
					assignmentCodeId: group.assignmentCodeId,
					assignmentCode: group.assignmentCode,
					assignmentCodeName: group.assignmentCodeName ?? '',
					materialId: cost.materialId,
					materialCode: cost.materialCode,
					materialName: cost.materialName,
					unitPrice: cost.cost,
					amount: cost.amount,
				})),
			);

			persistedCostsRef.current = persistedCosts;
			setSelectedContracts(
				Array.from(
					new Map(
						persistedCosts.map((cost) => [
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
				persistedCosts.map((cost) =>
					buildPersistedMaterialOption(cost, undefined),
				),
			);
			form.setValue(
				'costs',
				persistedCosts.map((cost) => ({
					assignmentCodeId: cost.assignmentCodeId,
					materialId: cost.materialId,
					norm: deriveNormFromAmount(cost.amount, cost.unitPrice),
					amount: cost.amount,
				})),
			);
		});
	}, [form, isDuplicate, row]);

	useEffect(() => {
		if (contracts.length === 0 && assets.length === 0) return;

		const persistedCosts = persistedCostsRef.current;
		const currentCosts = form.getValues('costs') ?? [];
		const persistedCostMap = new Map(
			persistedCosts.map((cost) => [
				buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId),
				cost,
			]),
		);
		const persistedContractMap = new Map(
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
		const persistedContractIds = Array.from(persistedContractMap.keys());
		const selectedContractSet = new Set([
			...selectedContractIds,
			...persistedContractIds,
		]);
		const normalizedContracts = Array.from(
			new Map(
				[
					...contracts
						.filter((contract) => selectedContractSet.has(contract.id))
						.map<MultiSelectOption>((contract) => ({
							value: contract.id,
							label: `${contract.code} - ${contract.name}`,
						})),
					...Array.from(persistedContractMap.values()).filter(
						(item) => !contracts.some((contract) => contract.id === item.value),
					),
				].map((item) => [item.value, item]),
			).values(),
		);

		if (
			!arraysEqual(
				selectedContracts.map((item) => item.value),
				normalizedContracts.map((item) => item.value),
			)
		) {
			setSelectedContracts(normalizedContracts);
		}

		const currentOptions = getMaterialOptions(
			assets,
			contracts,
			normalizedContracts.map((item) => item.value),
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
			.filter((cost): cost is PersistedSlideMaterialCost => !!cost)
			.map((cost) =>
				buildPersistedMaterialOption(
					cost,
					contracts.find(
						(contract) => contract.id === cost.assignmentCodeId,
					),
				),
			);

		const mergedSelectedMaterials = [
			...selectedMaterials
				.map(
					(option) =>
						currentOptionMap.get(option.value) ??
						selectedMaterialMap.get(option.value),
				)
				.filter((option): option is SlideMaterialOption => !!option),
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
		assets,
		contracts,
		form,
		selectedContractIds,
		selectedContracts,
		selectedMaterials,
	]);

	useEffect(() => {
		const isHydratingPersistedSelections =
			selectedMaterials.length === 0 &&
			persistedCostsRef.current.length > 0 &&
			watchedCosts.length > 0;
		if (isHydratingPersistedSelections) return;

		const syncedCosts = sortCosts(
			normalizeCostAmounts(
				syncCostsWithSelections(watchedCosts, selectedMaterials, assets),
				assets,
			),
			contracts,
			assets,
		);
		const currentKeys = watchedCosts.map(
			(cost) =>
				`${cost.assignmentCodeId}:${cost.materialId}:${cost.norm}:${cost.amount}`,
		);
		const nextKeys = syncedCosts.map(
			(cost) =>
				`${cost.assignmentCodeId}:${cost.materialId}:${cost.norm}:${cost.amount}`,
		);

		if (!arraysEqual(currentKeys, nextKeys)) {
			form.setValue('costs', syncedCosts, {
				shouldValidate: false,
			});
		}
	}, [assets, contracts, form, selectedMaterials, watchedCosts]);

	const currentMaterialOptions = getMaterialOptions(
		assets,
		contracts,
		selectedContractIds,
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

	const handleSubmit = async (values: SlideFormSchema) => {
		try {
			const processedValues = {
				...values,
				costs: normalizeCostAmounts(values.costs, assets).map((cost) => ({
					assignmentCodeId: cost.assignmentCodeId,
					materialId: cost.materialId,
					amount: cost.amount,
				})),
			};
			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.SLIDE.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(API.PRICING.SLIDE.CREATE, processedValues);
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
				label='Mã định mức máng trượt'
				placeholder='Nhập mã định mức máng trượt'
			/>

			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={groups.map((process) => ({
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

			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				values={selectedContracts}
				onValuesChange={(nextValues) => {
					setSelectedContracts(nextValues);

					const nextContractIds = new Set(
						nextValues.map((value) => value.value),
					);
					setSelectedMaterials((currentValues) =>
						currentValues.filter((value) =>
							nextContractIds.has(value.assignmentCodeId),
						),
					);
				}}
				options={contracts.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			<MultiSelect
				label='Vật tư theo nhóm'
				placeholder='Chọn vật tư theo nhóm'
				values={selectedMaterials}
				onValuesChange={(nextValues) =>
					setSelectedMaterials(nextValues as SlideMaterialOption[])
				}
				options={materialOptions}
			/>

			{watchedCosts.length > 0 && (
				<GroupedMaterialCosts
					contracts={contracts}
					assets={assets}
					onRemove={(assignmentCodeId, materialId) => {
						setSelectedMaterials((currentValues) =>
							currentValues.filter(
								(value) =>
									value.value !==
									buildMaterialSelectionValue(
										assignmentCodeId,
										materialId,
									),
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
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function GroupedMaterialCosts({
	contracts,
	assets,
	onRemove,
}: {
	contracts: ContractCode[];
	assets: Asset[];
	onRemove: (assignmentCodeId: string, materialId: string) => void;
}) {
	const { control } = useFormContext<SlideFormSchema>();
	const costs =
		useWatch({
			control,
			name: 'costs',
		}) || [];

	const groups = groupCostsByAssignmentCode(costs, contracts);

	if (groups.length === 0) return null;

	return (
		<div className='scrollbar-sm max-h-100 overflow-auto'>
			<div className='flex flex-col gap-4'>
			{groups.map((group) => (
				<div key={group.assignmentCodeId} className='flex flex-col gap-4'>
					<FormSeparator
						label={`${group.assignmentCode}${group.assignmentCodeName ? ` - ${group.assignmentCodeName}` : ''} - ${formatNumber(group.totalAmount)} (đ)`}
					/>
					{group.indices.map((index) => (
						<FormRow
							key={buildMaterialSelectionValue(
								costs[index]?.assignmentCodeId ?? '',
								costs[index]?.materialId ?? '',
							)}
						>
							<PricingMaterialCosts
								index={index}
								assets={assets}
								onRemove={onRemove}
							/>
						</FormRow>
					))}
				</div>
			))}
			</div>
		</div>
	);
}

function PricingMaterialCosts({
	index,
	assets,
	onRemove,
}: {
	index: number;
	assets: Asset[];
	onRemove: (assignmentCodeId: string, materialId: string) => void;
}) {
	const { control, getValues } = useFormContext<SlideFormSchema>();

	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const materialId = getValues(`costs.${index}.materialId`);
	const asset = assets.find((a) => a.id === materialId);
	const norm = useWatch({ control, name: `costs.${index}.norm` });
	const amount = useWatch({ control, name: `costs.${index}.amount` });
	const unitPrice = asset?.costAmount ?? 0;

	return (
		<>
			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Mã vật tư</Label>
				<Input
					readOnly
					value={asset?.code ?? ''}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-36 flex-1 flex-col gap-2'>
				<Label>Tên vật tư</Label>
				<Input
					readOnly
					value={asset?.name ?? ''}
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

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn giá (đ)</Label>
				<Input
					readOnly
					value={formatNumber(unitPrice)}
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

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn giá máng trượt (đ/m)</Label>
				<Input
					readOnly
					value={
						Number.isNaN(Number(norm))
							? formatNumber(unitPrice)
							: formatNumber(amount || 0)
					}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='hidden'>
				<FormNumber
					control={control}
					name={`costs.${index}.amount`}
					label='Đơn giá máng trượt (đ/m)'
					placeholder='Nhập đơn giá máng trượt (đ/m)'
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
