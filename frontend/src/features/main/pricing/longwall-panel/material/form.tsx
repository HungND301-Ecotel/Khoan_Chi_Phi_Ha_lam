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
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { InputGroup, InputGroupInput } from '@/components/ui/input-group';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Cuttingthickness } from '@/features/main/catalog/parameter/cuttingthickness/columns';
import type { Longwallparameters } from '@/features/main/catalog/parameter/longwallparameters/columns';
import type { Seamface } from '@/features/main/catalog/parameter/seamface/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { Power } from '@/features/main/catalog/parameter/power/columns';
import type { Technology } from '@/features/main/catalog/parameter/technology/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import {
	LONGWALL_MATERIAL_FORM_DEFAULT,
	longwallMaterialFormSchema,
	type LongwallMaterialFormSchema,
} from '@/features/main/pricing/longwall-panel/material/schema';
import type { LongwallMaterial } from '@/features/main/pricing/longwall-panel/material/columns';
import type {
	LongwallMaterialDetail,
	LongwallMaterialDetailCost,
} from '@/features/main/pricing/longwall-panel/material/type';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { startTransition, useEffect, useMemo, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import { NumericFormat } from 'react-number-format';

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
	cost: LongwallMaterialDetailCost,
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
	costs: LongwallMaterialFormSchema['costs'],
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
	costs: LongwallMaterialFormSchema['costs'],
	assets: MaterialAsset[],
) => {
	const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
	return costs.map((cost) => {
		const unitPrice = assetMap.get(cost.materialId)?.costAmount ?? 0;
		const totalPrice = Number.isNaN(Number(cost.norm))
			? unitPrice
			: (unitPrice * Number(cost.norm)) / 1000;

		return {
			...cost,
			totalPrice,
		};
	});
};

const sortCosts = (
	costs: LongwallMaterialFormSchema['costs'],
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

const sumMaterialCosts = (costs: LongwallMaterialFormSchema['costs']) =>
	costs.reduce(
		(sum, cost) =>
			sum +
			(Number.isNaN(Number(cost.totalPrice)) ? 0 : Number(cost.totalPrice)),
		0,
	);

const groupCostsByAssignment = (
	costs: LongwallMaterialFormSchema['costs'],
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

const buildInterpolationMaps = (costs: LongwallMaterial['costs']) =>
	new Map(
		(costs ?? [])
			.filter((cost) => !!cost.materialId)
			.map((cost) => [
				buildMaterialSelectionValue(cost.assignmentCodeId, cost.materialId!),
				cost,
			]),
	);

const getInterpolationMismatchLabels = (
	upperNorm: LongwallMaterial | undefined,
	lowerNorm: LongwallMaterial | undefined,
	assignments: ContractCode[],
	assets: MaterialAsset[],
) => {
	if (!upperNorm || !lowerNorm) return [];

	const upperMap = buildInterpolationMaps(upperNorm.costs ?? []);
	const lowerMap = buildInterpolationMaps(lowerNorm.costs ?? []);
	const allKeys = new Set([...upperMap.keys(), ...lowerMap.keys()]);
	const assignmentMap = new Map(assignments.map((item) => [item.id, item]));
	const assetMap = new Map(assets.map((item) => [item.id, item]));
	const mismatches: string[] = [];

	allKeys.forEach((key) => {
		if (upperMap.has(key) && lowerMap.has(key)) return;
		const [assignmentCodeId, materialId] = key.split('::');
		const assignment = assignmentMap.get(assignmentCodeId);
		const asset = assetMap.get(materialId);
		if (assignment && asset) {
			mismatches.push(`[${assignment.code}] ${asset.code} - ${asset.name}`);
			return;
		}

		mismatches.push(key);
	});

	return mismatches.sort((left, right) => left.localeCompare(right, 'vi'));
};

export function LongwallMaterialForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<LongwallMaterial> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [longwallParameters, setLongwallParameters] = useState<
		Longwallparameters[]
	>([]);
	const [technologies, setTechnologies] = useState<Technology[]>([]);
	const [powers, setPowers] = useState<Power[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [seamfaces, setSeamfaces] = useState<Seamface[]>([]);
	const [cuttingthicknesses, setCuttingthicknesses] = useState<
		Cuttingthickness[]
	>([]);
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [assignments, setAssignments] = useState<ContractCode[]>([]);
	const [assets, setAssets] = useState<MaterialAsset[]>([]);
	const [selectedAssignments, setSelectedAssignments] = useState<
		MultiSelectOption[]
	>([]);
	const [selectedMaterials, setSelectedMaterials] = useState<MaterialOption[]>(
		[],
	);
	const [useInterpolation, setUseInterpolation] = useState(false);
	const [interpolationPoint, setInterpolationPoint] = useState<
		number | undefined
	>(undefined);
	const [upperNorms, setUpperNorms] = useState<LongwallMaterial[]>([]);
	const [selectedUpperNormId, setSelectedUpperNormId] = useState<string>('');
	const [selectedLowerNormId, setSelectedLowerNormId] = useState<string>('');
	const [upperPoint, setUpperPoint] = useState<number | undefined>(undefined);
	const [lowerPoint, setLowerPoint] = useState<number | undefined>(undefined);
	const [isMechanizedLongwall, setIsMechanizedLongwall] = useState(false);

	const persistedCostsRef = useRef<LongwallMaterialDetailCost[]>([]);
	const interpolationAppliedRef = useRef(false);

	const form = useForm<
		LongwallMaterialFormSchema,
		unknown,
		LongwallMaterialFormSchema
	>({
		resolver: zodResolver(longwallMaterialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...LONGWALL_MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	const watchedStartMonth = useWatch({
		control: form.control,
		name: 'startMonth',
		defaultValue: LONGWALL_MATERIAL_FORM_DEFAULT.startMonth,
	});
	const watchedCosts = useWatch({
		control: form.control,
		name: 'costs',
		defaultValue: LONGWALL_MATERIAL_FORM_DEFAULT.costs,
	});
	const selectedAssignmentIds = selectedAssignments.map((item) => item.value);

	useEffect(() => {
		const loadData = async () => {
			try {
				const [
					techRes,
					powerRes,
					strengthRes,
					longwallRes,
					cuttingRes,
					seamRes,
					processRes,
					upperNormsRes,
				] = await Promise.all([
					api.pagging<Technology>(API.CATALOG.PARAMETER.TECHNOLOGY.LIST),
					api.pagging<Power>(API.CATALOG.PARAMETER.POWER.LIST),
					api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
					api.pagging<Longwallparameters>(
						API.CATALOG.PARAMETER.LONGWALLPARAMETERS.LIST,
					),
					api.pagging<Cuttingthickness>(
						API.CATALOG.PARAMETER.CUTTINGTHICKNESS.LIST,
					),
					api.pagging<Seamface>(API.CATALOG.PARAMETER.SEAMFACE.LIST),
					api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
					api.pagging<LongwallMaterial>(
						API.PRICING.MATERIAL.LONGWALL_PANEL.LIST,
						{
							ignorePagination: true,
						},
					),
				]);

				setTechnologies(techRes.result.data);
				setPowers(powerRes.result.data);
				setStrengths(strengthRes.result.data);
				setLongwallParameters(longwallRes.result.data);
				setCuttingthicknesses(cuttingRes.result.data);
				setSeamfaces(seamRes.result.data);
				setProcesses(processRes.result.data);
				setUpperNorms(upperNormsRes.result.data);
			} catch (error) {
				popup.error(error);
			}
		};

		loadData();
	}, [popup]);

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
			...LONGWALL_MATERIAL_FORM_DEFAULT,
			startMonth: row.startMonth.substring(0, 10),
			endMonth: row.endMonth.substring(0, 10),
			code: isDuplicate ? '' : row.code,
			processId: row.processId,
			longwallParametersId: row.longwallParametersId || '',
			cuttingThicknessId:
				row.cuttingThicknessId || row.cuttingthicknessId || '',
			seamFaceId: row.seamFaceId || '',
			technologyId: row.technologyId || '',
			powerId: row.powerId || '',
			hardnessId: row.hardnessId || '',
		});
		startTransition(() => {
			setIsMechanizedLongwall(
				!!row.powerId || !!row.isLongwallMaterialUnitPriceCGH,
			);
		});

		api
			.get<LongwallMaterialDetail>(
				API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(row.id),
			)
			.then((res) => {
				const detail = res.result;
				persistedCostsRef.current = detail.costs ?? [];
				setSelectedAssignments(
					Array.from(
						new Map(
							(detail.costs ?? []).map((cost) => [
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
				form.reset({
					id: detail.id,
					code: isDuplicate ? '' : detail.code,
					processId: detail.processId,
					longwallParametersId: detail.longwallParameters?.id || '',
					cuttingThicknessId: detail.cuttingThickness?.id || '',
					seamFaceId: detail.seamFaceId || '',
					technologyId: detail.technologyId || '',
					powerId: detail.powerId || '',
					hardnessId: detail.hardnessId || '',
					startMonth: detail.startMonth.substring(0, 10),
					endMonth: detail.endMonth.substring(0, 10),
					costs: (detail.costs ?? []).map((cost) => ({
						assignmentCodeId: cost.assignmentCodeId,
						materialId: cost.materialId,
						norm: cost.norm,
						totalPrice: cost.totalPrice,
					})),
					otherMaterialValue:
						detail.otherMaterialValue || detail.otherMaterialValue === 0
							? detail.otherMaterialValue
							: undefined,
				});
				setIsMechanizedLongwall(
					!!detail.powerId || !!detail.isLongwallMaterialUnitPriceCGH,
				);
			})
			.catch((error) => popup.error(error));
	}, [form, isDuplicate, popup, row]);

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
			.filter((cost): cost is LongwallMaterialDetailCost => !!cost)
			.map((cost) =>
				buildPersistedMaterialOption(
					cost,
					assignments.find(
						(assignment) => assignment.id === cost.assignmentCodeId,
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
		form,
		selectedAssignments,
		selectedAssignmentIds,
		selectedMaterials,
	]);

	useEffect(() => {
		const isHydratingPersistedSelections =
			selectedMaterials.length === 0 &&
			persistedCostsRef.current.length > 0 &&
			watchedCosts.length > 0;
		if (isHydratingPersistedSelections) return;
		if (interpolationAppliedRef.current) {
			interpolationAppliedRef.current = false;
			return;
		}

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

	useEffect(() => {
		if (!useInterpolation) return;

		if (
			!selectedUpperNormId ||
			!selectedLowerNormId ||
			selectedUpperNormId === selectedLowerNormId
		) {
			interpolationAppliedRef.current = true;
			startTransition(() => {
				setSelectedAssignments([]);
				setSelectedMaterials([]);
			});
			form.setValue('costs', []);
			return;
		}

		const upperNorm = upperNorms.find(
			(item) => item.id === selectedUpperNormId,
		);
		const lowerNorm = upperNorms.find(
			(item) => item.id === selectedLowerNormId,
		);
		if (!upperNorm || !lowerNorm) return;

		const upperMap = buildInterpolationMaps(upperNorm.costs ?? []);
		const lowerMap = buildInterpolationMaps(lowerNorm.costs ?? []);
		const sharedKeys = Array.from(upperMap.keys()).filter((key) =>
			lowerMap.has(key),
		);

		const nextMaterials = sharedKeys
			.map((key) => {
				const [assignmentCodeId, materialId] = key.split('::');
				return buildMaterialOption(
					assignments.find((assignment) => assignment.id === assignmentCodeId),
					assets.find((asset) => asset.id === materialId),
				);
			})
			.filter((option): option is MaterialOption => !!option);

		const nextAssignments = Array.from(
			new Map(
				nextMaterials.map((option) => [
					option.assignmentCodeId,
					{
						value: option.assignmentCodeId,
						label:
							assignments.find(
								(assignment) => assignment.id === option.assignmentCodeId,
							)?.code &&
							assignments.find(
								(assignment) => assignment.id === option.assignmentCodeId,
							)?.name
								? `${assignments.find((assignment) => assignment.id === option.assignmentCodeId)?.code} - ${assignments.find((assignment) => assignment.id === option.assignmentCodeId)?.name}`
								: option.assignmentCodeId,
					},
				]),
			).values(),
		);

		const nextCosts = sortCosts(
			nextMaterials.map((option) => ({
				assignmentCodeId: option.assignmentCodeId,
				materialId: option.materialId,
				norm: 0,
				totalPrice: 0,
			})),
			assignments,
			assets,
		);

		interpolationAppliedRef.current = true;
		startTransition(() => {
			setSelectedAssignments(nextAssignments);
			setSelectedMaterials(nextMaterials);
		});
		form.setValue('costs', nextCosts, {
			shouldValidate: false,
		});
	}, [
		assets,
		assignments,
		form,
		selectedLowerNormId,
		selectedUpperNormId,
		upperNorms,
		useInterpolation,
	]);

	useEffect(() => {
		if (!useInterpolation) return;
		if (!selectedUpperNormId || !selectedLowerNormId) return;
		if (upperPoint === undefined || lowerPoint === undefined) return;
		if (interpolationPoint === undefined) return;
		if (upperPoint <= lowerPoint) return;

		const upperNorm = upperNorms.find(
			(item) => item.id === selectedUpperNormId,
		);
		const lowerNorm = upperNorms.find(
			(item) => item.id === selectedLowerNormId,
		);
		if (!upperNorm || !lowerNorm) return;

		const ratio = (interpolationPoint - lowerPoint) / (upperPoint - lowerPoint);
		const upperMap = buildInterpolationMaps(upperNorm.costs ?? []);
		const lowerMap = buildInterpolationMaps(lowerNorm.costs ?? []);
		const assetMap = new Map(assets.map((asset) => [asset.id, asset]));

		const interpolatedCosts = form.getValues('costs').map((cost) => {
			const key = buildMaterialSelectionValue(
				cost.assignmentCodeId,
				cost.materialId,
			);
			const upperCost = upperMap.get(key);
			const lowerCost = lowerMap.get(key);
			if (!upperCost || !lowerCost) {
				return cost;
			}

			const interpolatedNorm =
				Number(lowerCost.norm || 0) +
				ratio * (Number(upperCost.norm || 0) - Number(lowerCost.norm || 0));
			const unitPrice = assetMap.get(cost.materialId)?.costAmount ?? 0;

			return {
				...cost,
				norm: interpolatedNorm,
				totalPrice: (unitPrice * interpolatedNorm) / 1000,
			};
		});

		interpolationAppliedRef.current = true;
		form.setValue('costs', sortCosts(interpolatedCosts, assignments, assets), {
			shouldValidate: false,
		});
	}, [
		assets,
		assignments,
		form,
		interpolationPoint,
		lowerPoint,
		selectedLowerNormId,
		selectedUpperNormId,
		upperNorms,
		upperPoint,
		useInterpolation,
	]);

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

	const upperNormOptions = useMemo(
		() => upperNorms.filter((item) => item.id !== selectedLowerNormId),
		[selectedLowerNormId, upperNorms],
	);
	const lowerNormOptions = useMemo(
		() => upperNorms.filter((item) => item.id !== selectedUpperNormId),
		[selectedUpperNormId, upperNorms],
	);
	const interpolationMismatches = useMemo(
		() =>
			getInterpolationMismatchLabels(
				upperNorms.find((item) => item.id === selectedUpperNormId),
				upperNorms.find((item) => item.id === selectedLowerNormId),
				assignments,
				assets,
			),
		[assets, assignments, selectedLowerNormId, selectedUpperNormId, upperNorms],
	);
	const seamFaceInterpolationValue =
		interpolationPoint !== undefined
			? `M =${interpolationPoint.toString().replace('.', ',')}m`
			: 'M =';

	const handleSubmit = async (values: LongwallMaterialFormSchema) => {
		try {
			if (isMechanizedLongwall && !values.powerId) {
				form.setError('powerId', {
					type: 'manual',
					message: 'Công suất không được để trống',
				});
				return;
			}

			if (!isMechanizedLongwall && !values.hardnessId) {
				form.setError('hardnessId', {
					type: 'manual',
					message: 'Độ kiên cố than đá không được để trống',
				});
				return;
			}

			if (!useInterpolation && !values.seamFaceId) {
				form.setError('seamFaceId', {
					type: 'manual',
					message: 'Mặt vỉa không được để trống',
				});
				return;
			}

			const processedValues = {
				...values,
				seamFaceId: useInterpolation ? null : values.seamFaceId || '',
				powerId: isMechanizedLongwall ? values.powerId || null : null,
				hardnessId: isMechanizedLongwall ? null : values.hardnessId || null,
				costs: normalizeCostTotals(values.costs, assets),
				InterpolationSeamFaceValue:
					useInterpolation && interpolationPoint !== undefined
						? seamFaceInterpolationValue
						: '',
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.MATERIAL.LONGWALL_PANEL.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(
					API.PRICING.MATERIAL.LONGWALL_PANEL.CREATE,
					processedValues,
				);
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
				name='technologyId'
				label='Công nghệ khai thác'
				placeholder='Chọn công nghệ khai thác'
				options={technologies.map((technology) => ({
					label: technology.value,
					value: technology.id,
				}))}
			/>

			<div className='flex items-center gap-2'>
				<Checkbox
					id='is-mechanized-longwall'
					checked={isMechanizedLongwall}
					onCheckedChange={(checked) => {
						const isChecked = !!checked;
						setIsMechanizedLongwall(isChecked);
						if (isChecked) {
							form.clearErrors('powerId');
							form.setValue('hardnessId', '');
							return;
						}
						form.clearErrors('hardnessId');
						form.setValue('powerId', '');
					}}
				/>
				<label
					htmlFor='is-mechanized-longwall'
					className='cursor-pointer text-sm leading-none font-medium'
				>
					Lò chợ cơ giới hóa (CGH)
				</label>
			</div>

			{isMechanizedLongwall ? (
				<FormComboBox
					control={form.control}
					name='powerId'
					label='Công suất'
					placeholder='Chọn công suất'
					options={powers.map((power) => ({
						label: power.value,
						value: power.id,
					}))}
				/>
			) : (
				<FormComboBox
					control={form.control}
					name='hardnessId'
					label='Độ kiên cố than đá (f)'
					placeholder='Chọn độ kiên cố than đá (f)'
					options={strengths.map((strength) => ({
						label: strength.value,
						value: strength.id,
					}))}
				/>
			)}

			<FormComboBox
				control={form.control}
				name='longwallParametersId'
				label='Thông số lò chợ'
				placeholder='Chọn thông số lò chợ'
				options={longwallParameters.map((item) => ({
					label: `${item.llc}; ${item.lkc}; ${item.mk}`,
					value: item.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='cuttingThicknessId'
				label='Chiều dày lớp khấu (m)'
				placeholder='Chọn chiều dày lớp khấu (m)'
				options={cuttingthicknesses.map((item) => ({
					label: item.value,
					value: item.id,
				}))}
			/>

			{useInterpolation ? (
				<div className='flex flex-col gap-2'>
					<Label>Mặt vỉa (m)</Label>
					<Input
						disabled
						readOnly
						value={seamFaceInterpolationValue}
						className='read-only:bg-muted disabled:cursor-default disabled:opacity-100'
					/>
				</div>
			) : (
				<FormComboBox
					control={form.control}
					name='seamFaceId'
					label='Mặt vỉa (m)'
					placeholder='Chọn mặt vỉa (m)'
					options={seamfaces.map((seamface) => ({
						label: seamface.value,
						value: seamface.id,
					}))}
				/>
			)}

			<div className='flex items-center gap-2'>
				<Checkbox
					id='use-interpolation'
					checked={useInterpolation}
					onCheckedChange={(checked) => {
						const isChecked = !!checked;
						setUseInterpolation(isChecked);
						if (isChecked) {
							form.clearErrors('seamFaceId');
							return;
						}

						setSelectedUpperNormId('');
						setSelectedLowerNormId('');
						setUpperPoint(undefined);
						setLowerPoint(undefined);
						setInterpolationPoint(undefined);
					}}
				/>
				<label
					htmlFor='use-interpolation'
					className='cursor-pointer text-sm leading-none font-medium'
				>
					Tạo định mức bảng phương pháp nội suy
				</label>
			</div>

			{useInterpolation && (
				<div className='flex flex-col gap-4 rounded-md border p-4'>
					<div className='flex flex-col gap-2'>
						<Label>Điểm nội suy</Label>
						<InputGroup>
							<NumericFormat
								decimalSeparator=','
								thousandSeparator='.'
								placeholder='Nhập điểm nội suy'
								value={interpolationPoint ?? ''}
								onValueChange={(value) =>
									setInterpolationPoint(value.floatValue)
								}
								customInput={InputGroupInput}
								type='text'
								inputMode='decimal'
							/>
						</InputGroup>
					</div>

					<div className='grid grid-cols-2 gap-4'>
						<FormComboBox
							label='Định mức cận trên'
							placeholder='Chọn định mức cận trên'
							value={selectedUpperNormId}
							onValueChange={setSelectedUpperNormId}
							options={upperNormOptions.map((item) => ({
								label: item.code,
								value: item.id,
							}))}
						/>

						<div className='flex flex-col gap-2'>
							<Label>Điểm cận trên</Label>
							<InputGroup>
								<NumericFormat
									decimalSeparator=','
									thousandSeparator='.'
									placeholder='Nhập điểm cận trên'
									value={upperPoint ?? ''}
									onValueChange={(value) => setUpperPoint(value.floatValue)}
									customInput={InputGroupInput}
									type='text'
									inputMode='decimal'
								/>
							</InputGroup>
						</div>
					</div>

					<div className='grid grid-cols-2 gap-4'>
						<FormComboBox
							label='Định mức cận dưới'
							placeholder='Chọn định mức cận dưới'
							value={selectedLowerNormId}
							onValueChange={setSelectedLowerNormId}
							options={lowerNormOptions.map((item) => ({
								label: item.code,
								value: item.id,
							}))}
						/>

						<div className='flex flex-col gap-2'>
							<Label>Điểm cận dưới</Label>
							<InputGroup>
								<NumericFormat
									decimalSeparator=','
									thousandSeparator='.'
									placeholder='Nhập điểm cận dưới'
									value={lowerPoint ?? ''}
									onValueChange={(value) => setLowerPoint(value.floatValue)}
									customInput={InputGroupInput}
									type='text'
									inputMode='decimal'
								/>
							</InputGroup>
						</div>
					</div>

					{selectedUpperNormId &&
						selectedLowerNormId &&
						selectedUpperNormId === selectedLowerNormId && (
							<p className='text-destructive text-xs'>
								Định mức cận trên và cận dưới không được trùng nhau
							</p>
						)}

					{upperPoint !== undefined &&
						lowerPoint !== undefined &&
						upperPoint <= lowerPoint && (
							<p className='text-destructive text-xs'>
								Điểm cận trên phải lớn hơn điểm cận dưới
							</p>
						)}

					{interpolationMismatches.length > 0 && (
						<div className='bg-destructive/10 border-destructive/30 rounded-md border p-3'>
							<p className='text-destructive text-xs font-medium'>
								2 định mức có cặp Nhóm vật tư, tài sản và Vật tư tài sản không
								khớp:
							</p>
							<ul className='text-destructive mt-1 list-disc pl-4 text-xs'>
								{interpolationMismatches.map((label) => (
									<li key={label}>{label}</li>
								))}
							</ul>
						</div>
					)}
				</div>
			)}

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
	const { control } = useFormContext<LongwallMaterialFormSchema>();
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
				<Label>Tổng tiền (đ/tấn)</Label>
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
	const { control, getValues } = useFormContext<LongwallMaterialFormSchema>();
	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const materialId = getValues(`costs.${index}.materialId`);
	const asset = assets.find((item) => item.id === materialId);
	const totalPrice = useWatch({ control, name: `costs.${index}.totalPrice` });

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

			<div className='flex min-w-28 flex-1 flex-col gap-2'>
				<Label>Đơn giá (đ)</Label>
				<Input
					readOnly
					value={formatNumber(asset?.costAmount ?? 0)}
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
				<Label>Đơn giá vật liệu (đ/1000 tấn)</Label>
				<Input
					readOnly
					value={formatNumber(totalPrice || 0)}
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
	const { setValue } = useFormContext<LongwallMaterialFormSchema>();

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
	const { control, setValue } = useFormContext<LongwallMaterialFormSchema>();
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
				<Label>Đơn giá vật liệu (đ/1000 tấn)</Label>
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
