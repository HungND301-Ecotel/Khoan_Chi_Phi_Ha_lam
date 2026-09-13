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
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { InputGroup, InputGroupInput } from '@/components/ui/input-group';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
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
	LONGWALL_MATERIAL_COMMON_FORM_DEFAULT,
	longwallMaterialCommonFormSchema,
	type LongwallMaterialCommonFormSchema,
	type LongwallMaterialFormSchema,
} from '@/features/main/pricing/longwall-panel/material/schema';
import {
	getLongwallMaterialDetail,
	type LongwallMaterial,
} from '@/features/main/pricing/longwall-panel/material/columns';
import type {
	LongwallMaterialDetail,
	LongwallMaterialDetailCost,
} from '@/features/main/pricing/longwall-panel/material/type';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { startTransition, useEffect, useMemo, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { NumericFormat } from 'react-number-format';

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
	costs: LongwallMaterialFormSchema['costs'];
	selectedAssignments: MultiSelectOption[];
	selectedMaterials: MaterialOption[];
	persistedCosts: LongwallMaterialDetailCost[];
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
		const asset = assetMap.get(cost.materialId);
		if (!asset) {
			return cost;
		}
		const unitPrice = asset.costAmount ?? 0;
		const totalPrice = Number.isNaN(Number(cost.norm))
			? 0
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
		LongwallMaterialCommonFormSchema,
		unknown,
		LongwallMaterialCommonFormSchema
	>({
		resolver: zodResolver(longwallMaterialCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: LONGWALL_MATERIAL_COMMON_FORM_DEFAULT,
	});

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
		const initialMonth = periods[0]?.startMonth || new Date().toISOString().substring(0, 10);
		Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				date: initialMonth,
			}),
			api.pagging<MaterialAsset>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				date: initialMonth,
			}),
		]).then(([assignmentsRes, assetsRes]) => {
			setAssignments(assignmentsRes.result.data);
			setAssets(assetsRes.result.data);
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
					const res = await api.get<LongwallMaterialDetail>(
						API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(p.id),
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

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		const newPeriod: PeriodItemData = {
			...target,
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			costs: (target.costs || []).map((c) => ({
				...c,
				id: undefined,
				periodId: undefined,
			})),
			selectedAssignments: [...(target.selectedAssignments || [])],
			selectedMaterials: [...(target.selectedMaterials || [])],
			persistedCosts: [],
		};

		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, newPeriod);
			return next;
		});
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

	// Handle interpolation calculation applied to period 0
	useEffect(() => {
		if (!useInterpolation) return;

		if (
			!selectedUpperNormId ||
			!selectedLowerNormId ||
			selectedUpperNormId === selectedLowerNormId
		) {
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

		if (
			interpolationPoint === undefined ||
			upperPoint === undefined ||
			lowerPoint === undefined ||
			upperPoint <= lowerPoint
		) {
			setPeriods((prev) => {
				if (prev.length === 0) return prev;
				const p0 = prev[0];
				const baseCosts = syncCostsWithSelections(
					p0.costs,
					nextMaterials,
					assets,
				);
				const next = [...prev];
				next[0] = {
					...p0,
					selectedAssignments: nextAssignments,
					selectedMaterials: nextMaterials,
					costs: sortCosts(baseCosts, assignments, assets),
				};
				return next;
			});
			return;
		}

		const ratio = (interpolationPoint - lowerPoint) / (upperPoint - lowerPoint);
		const currentP0Costs = syncCostsWithSelections(
			periods[0]?.costs ?? [],
			nextMaterials,
			assets,
		);
		const assetMap = new Map(assets.map((asset) => [asset.id, asset]));
		const interpolatedCosts = currentP0Costs.map((cost) => {
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

		setPeriods((prev) => {
			if (prev.length === 0) return prev;
			const p0 = prev[0];
			const next = [...prev];
			next[0] = {
				...p0,
				selectedAssignments: nextAssignments,
				selectedMaterials: nextMaterials,
				costs: sortCosts(interpolatedCosts, assignments, assets),
			};
			return next;
		});
	}, [
		assets,
		assignments,
		interpolationPoint,
		lowerPoint,
		selectedLowerNormId,
		selectedUpperNormId,
		upperNorms,
		upperPoint,
		useInterpolation,
	]);

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

	const handleSubmit = async (values: LongwallMaterialCommonFormSchema) => {
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

			const validationResult = validatePeriods(periods);
			if (validationResult) {
				popup.error(validationResult.error);
				return;
			}

			if (deletedPeriodIds.length > 0) {
				await api.delete(
					API.PRICING.MATERIAL.LONGWALL_PANEL.DELETES,
					deletedPeriodIds,
				);
			}

			for (const period of periods) {
				const payload = {
					code: values.code,
					processId: values.processId,
					longwallParametersId: values.longwallParametersId,
					cuttingThicknessId: values.cuttingThicknessId,
					seamFaceId: useInterpolation ? null : values.seamFaceId || '',
					technologyId: values.technologyId,
					powerId: isMechanizedLongwall ? values.powerId || null : null,
					hardnessId: isMechanizedLongwall ? null : values.hardnessId || null,
					InterpolationSeamFaceValue:
						useInterpolation && interpolationPoint !== undefined
							? seamFaceInterpolationValue
							: '',
					startMonth: period.startMonth,
					endMonth: period.endMonth,
					otherMaterialValue: period.otherMaterialValue,
					costs: period.costs,
				};

				if (period.id && !isDuplicate) {
					await api.put(API.PRICING.MATERIAL.LONGWALL_PANEL.UPDATE, {
						id: period.id,
						...payload,
					});
				} else {
					await api.post(API.PRICING.MATERIAL.LONGWALL_PANEL.CREATE, payload);
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
			{/* Thông tin chung định mức */}
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
					Tạo định mức bằng phương pháp nội suy
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
							options={upperNormOptions.map((item) => {
								const details = getLongwallMaterialDetail(item);
								const labelText = [item.code, item.processName, details]
									.filter(Boolean)
									.join(' - ');

								return {
									label: labelText,
									value: item.id,
								};
							})}
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
							options={lowerNormOptions.map((item) => {
								const details = getLongwallMaterialDetail(item);
								const labelText = [item.code, item.processName, details]
									.filter(Boolean)
									.join(' - ');

								return {
									label: labelText,
									value: item.id,
								};
							})}
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
							onCopy={() => handleCopyPeriod(index)}
						/>
					))}

					{/* Nút Thêm thời gian ở dưới cùng bên trái */}
					<div className='flex justify-start pt-1'>
						<Button
							type='button'
							variant='ghost'
							size='sm'
							onClick={handleAddPeriod}
							className='flex h-fit w-fit items-center gap-1.5 border-0 bg-transparent p-0 shadow-none hover:bg-transparent'
						>
							<PlusCircleIcon className='size-4 text-primary' strokeWidth={2} />
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
	onCopy: () => void;
}

function PeriodSection({
	totalPeriods,
	period,
	onUpdate,
	onDelete,
	onCopy,
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
			? 0
			: (unitPrice * normNumber) / 1000;

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
				<div className='mb-2 flex items-center gap-2'>
					<Button
						type='button'
						variant='ghost'
						size='icon'
						onClick={onCopy}
						className='h-fit w-fit border-0 bg-transparent p-0 text-neutral-500 shadow-none hover:bg-transparent hover:text-primary'
						title='Sao chép'
					>
						<Copy className='size-5' />
					</Button>
					{totalPeriods > 1 && (
						<Button
							type='button'
							variant='ghost'
							size='icon'
							onClick={onDelete}
							className='h-fit w-fit border-0 bg-transparent p-0 text-red-500 shadow-none hover:bg-transparent hover:text-red-700'
							title='Xóa'
						>
							<XCircleIcon className='size-5 text-red-500' />
						</Button>
					)}
				</div>
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
	costs: LongwallMaterialFormSchema['costs'];
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
				<Label>Tổng tiền (đ/1000 tấn)</Label>
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
											<Label>Đơn giá vật liệu (đ/1000 tấn)</Label>
											<Input
												readOnly
												value={
													isLegacySummaryRow
														? formatNumber(cost.totalPrice || 0)
														: Number.isNaN(Number(cost.norm))
															? '0'
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
									<Label>Đơn giá vật liệu (đ/1000 tấn)</Label>
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
