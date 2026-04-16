import { FormCheckBox } from '@/components/form/form-check-box';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { Button } from '@/components/ui/button';
import { DialogFooter } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Spinner } from '@/components/ui/spinner';
import {
	Table,
	TableBody,
	TableCell,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';
import { PlusCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Path, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { MaintainUnitPriceCreateDialog } from './maintain-unit-price-create-dialog';
import { MaterialFormSchema } from './schema';
import {
	AdditionalCost,
	ADDITIONAL_COST_OPTIONS,
	CONTRACT_LIMIT_OPTIONS,
	CONTRACT_LIMIT_SECONDARY_OPTIONS,
	EXPORTED_TYPE_OPTIONS,
	ItemType,
	MaterialType,
	MaterialsIncludedInContractRevenue,
	OTHER_MATERIAL_DETAIL_OPTIONS,
	QuotaBasedMaterial,
	RECEIVED_TYPE_OPTIONS,
} from './types';

// ── Form-level types ──────────────────────────────────────────────────────────

type MaterialsForm = { materials: MaterialFormSchema[] };

/** Extended schema that includes the runtime-only breakdown fields. */
type MaterialRowValues = MaterialFormSchema & {
	receivedTypes?: string[];
	exportedTypes?: string[];
	receivedBreakdown?: Record<string, number | string>;
	exportedBreakdown?: Record<string, number | string>;
	contractLimitSubCategories?: string[];
	contractLimitBreakdown?: Record<string, number | string>;
};

type MaterialsExtendedForm = { materials: MaterialRowValues[] };

type RowPath = Path<MaterialsExtendedForm>;

type ProcessGroupOption = {
	value: string;
	label: string;
	type: number;
};

type ProductionOrderOption = {
	value: string;
	label: string;
};

type EquipmentOption = {
	id: string;
	code: string;
	name: string;
};

const PRODUCTION_ORDER_OPTION_PREFIX = 'production-order:';
const EQUIPMENT_OPTION_PREFIX = 'equipment:';

function parseProductionOrderOptionId(value: string): string {
	return value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)
		? value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length)
		: value;
}

function parseEquipmentOptionId(value: string): string {
	return value.startsWith(EQUIPMENT_OPTION_PREFIX)
		? value.slice(EQUIPMENT_OPTION_PREFIX.length)
		: value;
}

type MaterialImportFormProps = {
	onCancel?: () => void;
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	orderOrEquipmentOptionsByItemId: Record<string, ProductionOrderOption[]>;
	partEquipmentsByPartId: Record<string, EquipmentOption[]>;
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === MaterialType.Material)
		return MaterialsIncludedInContractRevenue.Material;
	if (type === MaterialType.SparePart)
		return MaterialsIncludedInContractRevenue.Maintain;
	return null;
}

function getDefaultAdditionalCostByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) return AdditionalCost.Material;
	if (type === MaterialType.SparePart) return AdditionalCost.Maintain;
	return null;
}

function getMaterialBadge(
	type?: number | null,
	itemType?: number | null,
): {
	label: string;
	className: string;
} {
	if (
		type === MaterialType.Material &&
		itemType === ItemType.SafetyAndWelfare
	) {
		return {
			label:
				'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
			className:
				'rounded bg-emerald-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-emerald-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.Resource) {
		return {
			label: 'Tài sản',
			className:
				'rounded bg-violet-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-violet-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.QuotaMaterials) {
		return {
			label: 'Vật tư theo hạn mức',
			className:
				'rounded bg-cyan-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-cyan-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.InContract) {
		return {
			label: 'Vật tư, tài sản trong khoán',
			className:
				'rounded bg-blue-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-blue-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.OutContract) {
		return {
			label: 'Vật tư, tài sản ngoài khoán',
			className:
				'rounded bg-slate-200 px-1.5 py-0.5 text-center text-[10px] font-medium text-slate-700',
		};
	}

	if (type === MaterialType.SparePart && itemType === ItemType.InContract) {
		return {
			label: 'Phụ tùng theo thiết bị',
			className:
				'rounded bg-amber-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-amber-700',
		};
	}

	if (type === MaterialType.SparePart && itemType === ItemType.OutContract) {
		return {
			label: 'Phụ tùng khác',
			className:
				'rounded bg-orange-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-orange-700',
		};
	}

	if (type === MaterialType.Material) {
		return {
			label: 'Vật liệu',
			className:
				'rounded bg-blue-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-blue-700',
		};
	}

	if (type === MaterialType.SparePart) {
		return {
			label: 'Phụ tùng',
			className:
				'rounded bg-amber-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-amber-700',
		};
	}

	return {
		label: 'Vật tư',
		className:
			'rounded bg-slate-200 px-1.5 py-0.5 text-center text-[10px] font-medium text-slate-700',
	};
}

type MaterialBadgeFilterOption = {
	key: string;
	label: string;
	className: string;
};

function getMaterialBadgeFilterKey(
	type?: number | null,
	itemType?: number | null,
): string {
	if (
		type === MaterialType.Material &&
		itemType === ItemType.SafetyAndWelfare
	) {
		return 'material-safety-and-welfare';
	}

	if (type === MaterialType.Material && itemType === ItemType.Resource) {
		return 'material-asset';
	}

	if (type === MaterialType.Material && itemType === ItemType.QuotaMaterials) {
		return 'material-quota';
	}

	if (type === MaterialType.Material && itemType === ItemType.InContract) {
		return 'material-in-contract';
	}

	if (type === MaterialType.Material && itemType === ItemType.OutContract) {
		return 'material-out-contract';
	}

	if (type === MaterialType.SparePart && itemType === ItemType.InContract) {
		return 'spare-part-in-contract';
	}

	if (type === MaterialType.SparePart && itemType === ItemType.OutContract) {
		return 'spare-part-out-contract';
	}

	if (type === MaterialType.Material) {
		return 'material-default';
	}

	if (type === MaterialType.SparePart) {
		return 'spare-part-default';
	}

	return 'material-unknown';
}

function getMaterialBadgeFilterOption(
	type?: number | null,
	itemType?: number | null,
): MaterialBadgeFilterOption {
	const badge = getMaterialBadge(type, itemType);
	return {
		key: getMaterialBadgeFilterKey(type, itemType),
		label: badge.label,
		className: badge.className,
	};
}

const CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS =
	CONTRACT_LIMIT_SECONDARY_OPTIONS.map((option) => ({
		value: String(option.value),
		label: option.label,
	}));
const DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE =
	CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS[0]?.value ?? '';
const DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE =
	CONTRACT_LIMIT_OPTIONS[0]?.value ?? null;
const DEFAULT_OTHER_MATERIAL_DETAIL_VALUE =
	OTHER_MATERIAL_DETAIL_OPTIONS[0]?.value ?? null;

// ── Main form ─────────────────────────────────────────────────────────────────

export function MaterialImportForm({
	onCancel,
	processGroupOptions,
	productionOrderOptions,
	orderOrEquipmentOptionsByItemId,
	partEquipmentsByPartId,
}: MaterialImportFormProps) {
	const form = useFormContext<MaterialsForm>();
	const { fields, remove } = useFieldArray({
		control: form.control,
		name: 'materials',
	});
	const watchedMaterials = useWatch({
		control: form.control,
		name: 'materials',
	});
	const [selectedMaterialBadgeKeys, setSelectedMaterialBadgeKeys] = useState<
		string[]
	>([]);
	const materialBadgeFilters = useMemo(() => {
		const map = new Map<string, MaterialBadgeFilterOption>();
		for (const item of watchedMaterials || []) {
			const option = getMaterialBadgeFilterOption(item?.type, item?.itemType);
			if (!map.has(option.key)) {
				map.set(option.key, option);
			}
		}
		return Array.from(map.values());
	}, [watchedMaterials]);
	const visibleMaterialIndexes = useMemo(() => {
		if (selectedMaterialBadgeKeys.length === 0) {
			return fields.map((_, index) => index);
		}

		return fields
			.map((_, index) => index)
			.filter((index) => {
				const item = watchedMaterials?.[index];
				const key = getMaterialBadgeFilterKey(item?.type, item?.itemType);
				return selectedMaterialBadgeKeys.includes(key);
			});
	}, [fields, watchedMaterials, selectedMaterialBadgeKeys]);

	useEffect(() => {
		if (materialBadgeFilters.length === 0) {
			setSelectedMaterialBadgeKeys([]);
			return;
		}

		const availableKeys = new Set(
			materialBadgeFilters.map((option) => option.key),
		);
		setSelectedMaterialBadgeKeys((prev) => {
			const next = prev.filter((key) => availableKeys.has(key));
			if (next.length === 0) {
				return Array.from(availableKeys);
			}
			return next;
		});
	}, [materialBadgeFilters]);

	useEffect(() => {
		if (Object.keys(form.formState.errors).length > 0) {
			console.log('Form validation errors:', form.formState.errors);
		}
	}, [form.formState.errors]);

	return (
		<div className='flex h-full flex-col gap-6'>
			{materialBadgeFilters.length > 0 && (
				<div className='rounded-lg border border-slate-200 bg-slate-50 p-3'>
					<div className='flex flex-wrap gap-3'>
						{materialBadgeFilters.map((option) => {
							const checked = selectedMaterialBadgeKeys.includes(option.key);
							return (
								<label
									key={option.key}
									className='inline-flex cursor-pointer items-center gap-2'
								>
									<input
										type='checkbox'
										className='h-4 w-4 rounded border-slate-300'
										checked={checked}
										onChange={(event) => {
											setSelectedMaterialBadgeKeys((prev) => {
												if (event.target.checked) {
													return prev.includes(option.key)
														? prev
														: [...prev, option.key];
												}
												return prev.filter((key) => key !== option.key);
											});
										}}
									/>
									<span className={option.className}>{option.label}</span>
								</label>
							);
						})}
					</div>
				</div>
			)}
			<div className='min-h-0 flex-1 overflow-x-auto overflow-y-auto'>
				<div className='rounded-lg border shadow-sm'>
					<Table className='w-full'>
						<TableHeader className='bg-linear-to-r from-slate-50 to-slate-100'>
							<TableRow className='bg-linear-to-r from-slate-50 to-slate-100'>
								<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									STT
								</TableCell>
								<TableCell className='sticky left-16 z-20 w-[8%] min-w-32 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Mã vật tư
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Đơn vị tính
								</TableCell>
								<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Số lượng lĩnh
								</TableCell>
								<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Số lượng xuất
								</TableCell>
								<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Vật tư tính vào doanh thu khoán
								</TableCell>
								<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Bổ sung chi phí
								</TableCell>
								<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Vật tư theo hạn mức
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Tài sản
								</TableCell>
							</TableRow>
						</TableHeader>
						<TableBody>
							{visibleMaterialIndexes.map((index, displayIndex) => (
								<MaterialImportRow
									key={fields[index].id}
									index={index}
									displayIndex={displayIndex + 1}
									processGroupOptions={processGroupOptions}
									productionOrderOptions={productionOrderOptions}
									orderOrEquipmentOptionsByItemId={
										orderOrEquipmentOptionsByItemId
									}
									partEquipmentsByPartId={partEquipmentsByPartId}
									onRemove={() => remove(index)}
								/>
							))}
							{visibleMaterialIndexes.length === 0 && (
								<TableRow>
									<TableCell
										colSpan={9}
										className='py-6 text-center text-sm text-slate-500'
									>
										Không có vật tư phù hợp với bộ lọc đã chọn.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				</div>
			</div>

			<DialogFooter className='bg-muted sticky bottom-0 z-20 mt-auto w-full px-10 py-4'>
				<Button
					type='button'
					variant='outline'
					className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
					onClick={() => onCancel?.()}
				>
					Huỷ
				</Button>
				<Button
					type='submit'
					variant='default'
					className='h-8 w-24 shadow-none hover:shadow-none'
					disabled={form.formState.isSubmitting}
				>
					{form.formState.isSubmitting ? <Spinner /> : 'Lưu'}
				</Button>
			</DialogFooter>
		</div>
	);
}

// ── resetCellFields ───────────────────────────────────────────────────────────

type ResetFieldSpec = {
	checkbox: string;
	dropdown?: string;
	dropdownSecondary?: string;
	dropdownTertiary?: string;
	dropdownQuaternary?: string;
	quantity?: string;
};

function resetCellFields(
	form: ReturnType<typeof useFormContext<MaterialsExtendedForm>>,
	basename: `materials.${number}`,
	fields: ResetFieldSpec[],
) {
	for (const field of fields) {
		form.setValue(`${basename}.${field.checkbox}` as RowPath, false as never);
		if (field.dropdown)
			form.setValue(`${basename}.${field.dropdown}` as RowPath, null as never);
		if (field.dropdownSecondary)
			form.setValue(
				`${basename}.${field.dropdownSecondary}` as RowPath,
				null as never,
			);
		if (field.dropdownTertiary)
			form.setValue(
				`${basename}.${field.dropdownTertiary}` as RowPath,
				null as never,
			);
		if (field.dropdownQuaternary)
			form.setValue(
				`${basename}.${field.dropdownQuaternary}` as RowPath,
				null as never,
			);
		if (field.quantity)
			form.setValue(`${basename}.${field.quantity}` as RowPath, null as never);
	}
}

// ── QuantityBreakdownInputs ───────────────────────────────────────────────────

type QuantityBreakdownInputsProps = {
	selectedKeys: string[];
	allOptions: { value: string; label: string }[];
	values: Record<string, number | string>;
	onChange: (key: string, val: number | string) => void;
	isValid: boolean;
	equalWidth?: boolean;
};

function QuantityBreakdownInputs({
	selectedKeys,
	allOptions,
	values,
	onChange,
	isValid,
	equalWidth = false,
}: QuantityBreakdownInputsProps) {
	return (
		<>
			{selectedKeys.map((key) => {
				const opt = allOptions.find((o) => o.value === key);
				return (
					<div
						key={key}
						className={cn(
							'flex flex-col gap-0.5',
							equalWidth ? 'min-w-0 flex-1' : 'w-24 shrink-0',
						)}
					>
						<label
							className='truncate text-[10px] leading-tight font-medium text-slate-500'
							title={opt?.label}
						>
							{opt?.label}
						</label>
						<Input
							type='number'
							min={0}
							step='any'
							value={values[key] ?? ''}
							onChange={(e) =>
								onChange(
									key,
									e.target.value === '' ? '' : Number(e.target.value),
								)
							}
							placeholder='0'
							className={cn(
								'text-center',
								!isValid && 'border-red-400 focus-visible:ring-red-300',
							)}
						/>
					</div>
				);
			})}
		</>
	);
}

// ── MaterialImportRow ─────────────────────────────────────────────────────────

function MaterialImportRow({
	index,
	displayIndex,
	processGroupOptions,
	productionOrderOptions,
	orderOrEquipmentOptionsByItemId,
	partEquipmentsByPartId,
}: {
	index: number;
	displayIndex?: number;
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	orderOrEquipmentOptionsByItemId: Record<string, ProductionOrderOption[]>;
	partEquipmentsByPartId: Record<string, EquipmentOption[]>;
	onRemove: () => void;
}) {
	const form = useFormContext<MaterialsExtendedForm>();
	const basename = `materials.${index}` as const;
	const [isCreateMaintainDialogOpen, setIsCreateMaintainDialogOpen] =
		useState(false);

	// ── Watch helpers (typed via RowPath) ────────────────────────────────────
	const w = <K extends keyof MaterialRowValues>(key: K) =>
		// useWatch requires a registered path; casting is the narrowest escape here
		// because RHF's generic doesn't accept template-literal paths directly.
		// eslint-disable-next-line react-hooks/rules-of-hooks
		useWatch({
			control: form.control,
			name: `${basename}.${key}` as RowPath,
		}) as MaterialRowValues[K];

	const showCategoryDropdown = w('showCategoryDropdown');
	const showAdditionalCostDropdown = w('showAdditionalCostDropdown');
	const showContractLimitDropdown = w('showContractLimitDropdown');
	const showAssetDropdown = w('showAssetDropdown');
	const categoryValue = w('category');
	const categoryProcessGroupValue = w('categoryProcessGroup');
	const categoryProductionOrderId = w('categoryProductionOrderId');
	const categoryEquipmentId = w('categoryEquipmentId');
	const additionalCostCategory = w('additionalCostCategory');
	const additionalCostProductionOrderId = w('additionalCostProductionOrderId');
	const otherMaterialDetailValue = w('otherMaterialDetail');
	const contractLimitCategoryValue = w('contractLimitCategory');
	const quantityExported = w('quantityExported');
	const quantityReceived = w('quantityReceived');
	const categoryQuantity = w('categoryQuantity');
	const additionalCostQuantity = w('additionalCostQuantity');
	const contractLimitQuantity = w('contractLimitQuantity');
	const contractLimitSubCategoriesValue = w('contractLimitSubCategories') as
		| string[]
		| undefined;
	const contractLimitBreakdown = w('contractLimitBreakdown') as
		| Record<string, number | string>
		| undefined;
	const assetQuantity = w('assetQuantity');
	const materialTypeValue = w('type');
	const itemTypeValue = w('itemType');
	const materialOrPartId = w('materialOrPartId');
	const partTypeValue = w('partType');
	const hasMaintainUnitPriceEquipment = w('hasMaintainUnitPriceEquipment');
	const receivedTypes = w('receivedTypes') as string[] | undefined;
	const exportedTypes = w('exportedTypes') as string[] | undefined;
	const receivedBreakdown = w('receivedBreakdown') as
		| Record<string, number | string>
		| undefined;
	const exportedBreakdown = w('exportedBreakdown') as
		| Record<string, number | string>
		| undefined;

	// ── Derived values ───────────────────────────────────────────────────────
	const defaultCategoryByType =
		getDefaultCategoryByMaterialType(materialTypeValue);
	const defaultAdditionalCostByType =
		getDefaultAdditionalCostByMaterialType(materialTypeValue);
	const isSafetyAndWelfareMaterial =
		materialTypeValue === MaterialType.Material &&
		itemTypeValue === ItemType.SafetyAndWelfare;
	const isSparePartByEquipment =
		materialTypeValue === MaterialType.SparePart &&
		itemTypeValue === ItemType.InContract;

	const resolvedCategoryValue = categoryValue ?? defaultCategoryByType;
	const orderOrEquipmentOptions =
		(materialOrPartId
			? orderOrEquipmentOptionsByItemId[materialOrPartId]
			: undefined) ?? productionOrderOptions;
	const equipmentOptions = orderOrEquipmentOptions.filter((option) =>
		option.value.startsWith(EQUIPMENT_OPTION_PREFIX),
	);
	const productionOrderOnlyOptions = orderOrEquipmentOptions.filter((option) =>
		option.value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX),
	);
	const categoryEquipmentOptions = equipmentOptions.map((option) => ({
		value: parseEquipmentOptionId(option.value),
		label: option.label,
	}));
	const categoryProductionOrderOptions = productionOrderOnlyOptions.map(
		(option) => ({
			value: parseProductionOrderOptionId(option.value),
			label: option.label,
		}),
	);
	const additionalCostOrderOrEquipmentOptions = isSparePartByEquipment
		? productionOrderOnlyOptions
		: orderOrEquipmentOptions;

	const additionalCostOptionsByType =
		defaultAdditionalCostByType == null
			? ADDITIONAL_COST_OPTIONS
			: ADDITIONAL_COST_OPTIONS.filter(
					(o) =>
						o.value === defaultAdditionalCostByType ||
						o.value === AdditionalCost.OtherMaterial,
				);

	const needsSecondComboBox =
		contractLimitCategoryValue === QuotaBasedMaterial.MineSupport ||
		contractLimitCategoryValue === QuotaBasedMaterial.SupportAccessories;
	const categoryNeedsProductionOrder =
		resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain;
	const categoryNeedsEquipment =
		categoryNeedsProductionOrder && isSparePartByEquipment;
	const additionalCostNeedsProductionOrder =
		additionalCostCategory === AdditionalCost.Material ||
		additionalCostCategory === AdditionalCost.Maintain;
	const additionalCostNeedsOtherMaterialDetail =
		additionalCostCategory === AdditionalCost.OtherMaterial;
	const contractLimitSelectedKeys =
		contractLimitSubCategoriesValue &&
		contractLimitSubCategoriesValue.length > 0
			? contractLimitSubCategoriesValue
			: [];
	const contractLimitSelectedTypes = contractLimitSelectedKeys.map((type) =>
		Number(type),
	);
	const contractLimitBreakdownTotal = contractLimitSelectedKeys.reduce(
		(acc, key) => acc + (Number(contractLimitBreakdown?.[key]) || 0),
		0,
	);

	// ── Helper to set a field value ──────────────────────────────────────────
	const set = <K extends keyof MaterialRowValues>(
		key: K,
		value: MaterialRowValues[K],
	) => form.setValue(`${basename}.${key}` as RowPath, value as never);

	// ── Initialize MultiSelect defaults on mount ─────────────────────────────
	useEffect(() => {
		if (!receivedTypes || receivedTypes.length === 0) {
			set('receivedTypes', [RECEIVED_TYPE_OPTIONS[0].value]);
		}
		if (!exportedTypes || exportedTypes.length === 0) {
			set('exportedTypes', [EXPORTED_TYPE_OPTIONS[0].value]);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	// ── Sync breakdown: redistribute total equally when selection changes ────
	useEffect(() => {
		if (!receivedTypes) return;
		const current = receivedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...receivedTypes].sort().join(',')
		)
			return;
		const count = receivedTypes.length;
		const divided = count > 0 ? (Number(quantityReceived) || 0) / count : 0;
		const next: Record<string, number | string> = {};
		for (const key of receivedTypes) next[key] = divided;
		set('receivedBreakdown', next);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [receivedTypes, receivedBreakdown, quantityReceived]);

	useEffect(() => {
		if (!exportedTypes) return;
		const current = exportedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...exportedTypes].sort().join(',')
		)
			return;
		const count = exportedTypes.length;
		const divided = count > 0 ? (Number(quantityExported) || 0) / count : 0;
		const next: Record<string, number | string> = {};
		for (const key of exportedTypes) next[key] = divided;
		set('exportedBreakdown', next);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [exportedTypes, exportedBreakdown, quantityExported]);

	// ── Checkbox toggle effects ──────────────────────────────────────────────
	const prevState = useRef({
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showContractLimitDropdown: false,
		showAssetDropdown: false,
	});

	const prevDropdownState = useRef({
		category: null as number | null | undefined,
		categoryProcessGroup: null as string | null | undefined,
		categoryProductionOrderId: null as string | null | undefined,
		categoryEquipmentId: null as string | null | undefined,
		additionalCostCategory: null as number | null | undefined,
		additionalCostProductionOrderId: null as string | null | undefined,
		otherMaterialDetail: null as number | null | undefined,
		contractLimitCategory: null as number | null | undefined,
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showAssetDropdown: false,
	});

	useEffect(() => {
		const prev = prevState.current;

		const justEnabledCategory =
			!prev.showCategoryDropdown && showCategoryDropdown;
		const justEnabledAdditional =
			!prev.showAdditionalCostDropdown && showAdditionalCostDropdown;
		const justEnabledContractLimit =
			!prev.showContractLimitDropdown && showContractLimitDropdown;
		const justEnabledAsset = !prev.showAssetDropdown && showAssetDropdown;
		const justDisabledCategory =
			prev.showCategoryDropdown && !showCategoryDropdown;
		const justDisabledAdditional =
			prev.showAdditionalCostDropdown && !showAdditionalCostDropdown;
		const justDisabledContractLimit =
			prev.showContractLimitDropdown && !showContractLimitDropdown;
		const justDisabledAsset = prev.showAssetDropdown && !showAssetDropdown;

		if (justEnabledContractLimit) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'category',
					dropdownSecondary: 'categoryProcessGroup',
					dropdownTertiary: 'categoryProductionOrderId',
					dropdownQuaternary: 'categoryEquipmentId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostProductionOrderId',
					dropdownTertiary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
			]);
		} else if (justEnabledAsset) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'category',
					dropdownSecondary: 'categoryProcessGroup',
					dropdownTertiary: 'categoryProductionOrderId',
					dropdownQuaternary: 'categoryEquipmentId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostProductionOrderId',
					dropdownTertiary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{
					checkbox: 'showContractLimitDropdown',
					dropdown: 'contractLimitCategory',
					quantity: 'contractLimitQuantity',
				},
			]);
		} else if (justEnabledCategory) {
			if (prev.showContractLimitDropdown)
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			if (prev.showAssetDropdown)
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			if (categoryValue == null && defaultCategoryByType != null)
				set('category', defaultCategoryByType);
			if (!categoryProcessGroupValue && processGroupOptions.length === 1)
				set('categoryProcessGroup', processGroupOptions[0].value);
			if (
				resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain &&
				categoryProductionOrderId == null &&
				categoryProductionOrderOptions.length > 0
			)
				set(
					'categoryProductionOrderId',
					categoryProductionOrderOptions[0].value,
				);
			if (
				resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain &&
				categoryNeedsEquipment &&
				categoryEquipmentId == null &&
				categoryEquipmentOptions.length > 0
			) {
				set('categoryEquipmentId', categoryEquipmentOptions[0].value);
			}
		} else if (justEnabledAdditional) {
			if (prev.showContractLimitDropdown)
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			if (prev.showAssetDropdown)
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			if (isSafetyAndWelfareMaterial) {
				if (additionalCostCategory !== AdditionalCost.OtherMaterial) {
					set('additionalCostCategory', AdditionalCost.OtherMaterial);
				}
			} else if (!additionalCostCategory && defaultAdditionalCostByType != null)
				set('additionalCostCategory', defaultAdditionalCostByType);
			if (
				additionalCostCategory === AdditionalCost.Material ||
				additionalCostCategory === AdditionalCost.Maintain
			) {
				const hasValidSelection =
					additionalCostProductionOrderId != null &&
					additionalCostOrderOrEquipmentOptions.some(
						(option) => option.value === additionalCostProductionOrderId,
					);
				if (!hasValidSelection) {
					set(
						'additionalCostProductionOrderId',
						additionalCostOrderOrEquipmentOptions[0]?.value ?? null,
					);
				}
			}
			if (
				additionalCostCategory === AdditionalCost.OtherMaterial &&
				otherMaterialDetailValue == null &&
				DEFAULT_OTHER_MATERIAL_DETAIL_VALUE != null
			) {
				set('otherMaterialDetail', DEFAULT_OTHER_MATERIAL_DETAIL_VALUE);
			}
		}

		if (justDisabledCategory) {
			set('category', null);
			set('categoryProcessGroup', null);
			set('categoryProductionOrderId', null);
			set('categoryEquipmentId', null);
			set('categoryQuantity', null);
		}
		if (justDisabledAdditional) {
			set('additionalCostCategory', null);
			set('additionalCostProductionOrderId', null);
			set('otherMaterialDetail', null);
			set('additionalCostQuantity', null);
		}
		if (justDisabledContractLimit) {
			set('contractLimitCategory', null);
			set('contractLimitQuantity', null);
			set('contractLimitSubCategory', null);
			set('contractLimitSubCategories', []);
			set('contractLimitBreakdown', {});
		}
		if (justDisabledAsset) set('assetQuantity', null);

		prevState.current = {
			showCategoryDropdown,
			showAdditionalCostDropdown,
			showContractLimitDropdown,
			showAssetDropdown,
		};
	}, [
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
		categoryValue,
		resolvedCategoryValue,
		categoryProcessGroupValue,
		categoryProductionOrderId,
		categoryEquipmentId,
		additionalCostCategory,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		defaultCategoryByType,
		defaultAdditionalCostByType,
		isSafetyAndWelfareMaterial,
		processGroupOptions,
		categoryProductionOrderOptions,
		categoryEquipmentOptions,
		categoryNeedsEquipment,
		additionalCostOrderOrEquipmentOptions,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showAdditionalCostDropdown || !isSafetyAndWelfareMaterial) return;
		if (additionalCostCategory === AdditionalCost.OtherMaterial) return;
		set('additionalCostCategory', AdditionalCost.OtherMaterial);
	}, [
		showAdditionalCostDropdown,
		isSafetyAndWelfareMaterial,
		additionalCostCategory,
	]);

	useEffect(() => {
		if (!showCategoryDropdown || !resolvedCategoryValue) return;
		if (processGroupOptions.length !== 1 || categoryProcessGroupValue) return;
		set('categoryProcessGroup', processGroupOptions[0].value);
	}, [
		showCategoryDropdown,
		resolvedCategoryValue,
		categoryProcessGroupValue,
		processGroupOptions,
	]);

	useEffect(() => {
		if (!showCategoryDropdown) return;
		if (categoryValue == null && defaultCategoryByType != null) {
			set('category', defaultCategoryByType);
			return;
		}
		if (resolvedCategoryValue !== MaterialsIncludedInContractRevenue.Maintain) {
			if (categoryProductionOrderId != null)
				set('categoryProductionOrderId', null);
			if (categoryEquipmentId != null) set('categoryEquipmentId', null);
			return;
		}
		if (
			categoryProductionOrderId == null &&
			categoryProductionOrderOptions.length > 0
		) {
			set('categoryProductionOrderId', categoryProductionOrderOptions[0].value);
		}
		if (categoryNeedsEquipment) {
			if (categoryEquipmentId == null && categoryEquipmentOptions.length > 0) {
				set('categoryEquipmentId', categoryEquipmentOptions[0].value);
			}
			return;
		}
		if (categoryEquipmentId != null) set('categoryEquipmentId', null);
	}, [
		showCategoryDropdown,
		categoryValue,
		defaultCategoryByType,
		resolvedCategoryValue,
		categoryProductionOrderId,
		categoryEquipmentId,
		categoryProductionOrderOptions,
		categoryEquipmentOptions,
		categoryNeedsEquipment,
	]);

	useEffect(() => {
		if (!showAdditionalCostDropdown) return;
		const requiresProductionOrder =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain;
		const requiresOtherMaterialDetail =
			additionalCostCategory === AdditionalCost.OtherMaterial;
		if (!requiresProductionOrder) {
			if (additionalCostProductionOrderId != null)
				set('additionalCostProductionOrderId', null);
		} else {
			const hasValidSelection =
				additionalCostProductionOrderId != null &&
				additionalCostOrderOrEquipmentOptions.some(
					(option) => option.value === additionalCostProductionOrderId,
				);
			if (!hasValidSelection) {
				set(
					'additionalCostProductionOrderId',
					additionalCostOrderOrEquipmentOptions[0]?.value ?? null,
				);
			}
		}

		if (!requiresOtherMaterialDetail) {
			if (otherMaterialDetailValue != null) set('otherMaterialDetail', null);
			return;
		}

		if (
			otherMaterialDetailValue == null &&
			DEFAULT_OTHER_MATERIAL_DETAIL_VALUE != null
		) {
			set('otherMaterialDetail', DEFAULT_OTHER_MATERIAL_DETAIL_VALUE);
		}
	}, [
		showAdditionalCostDropdown,
		additionalCostCategory,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		additionalCostOrderOrEquipmentOptions,
	]);

	useEffect(() => {
		const prev = prevDropdownState.current;
		const categoryRequiresProductionOrder =
			resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain;
		const categoryRequiresEquipment =
			categoryRequiresProductionOrder && isSparePartByEquipment;
		const additionalRequiresProductionOrder =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain;
		const additionalRequiresOtherDetail =
			additionalCostCategory === AdditionalCost.OtherMaterial;
		const hasCategoryActiveNow = Boolean(
			showCategoryDropdown &&
			resolvedCategoryValue &&
			categoryProcessGroupValue &&
			(!categoryRequiresProductionOrder || categoryProductionOrderId != null) &&
			(!categoryRequiresEquipment || categoryEquipmentId != null),
		);
		const hasAdditionalCostActiveNow = Boolean(
			showAdditionalCostDropdown &&
			additionalCostCategory &&
			(!additionalRequiresProductionOrder ||
				additionalCostProductionOrderId != null) &&
			(!additionalRequiresOtherDetail || otherMaterialDetailValue != null),
		);
		const hasCategoryActiveBefore = Boolean(
			prev.showCategoryDropdown &&
			prev.category &&
			prev.categoryProcessGroup &&
			(prev.category !== MaterialsIncludedInContractRevenue.Maintain ||
				(prev.categoryProductionOrderId != null &&
					(!isSparePartByEquipment || prev.categoryEquipmentId != null))),
		);
		const hasAdditionalCostActiveBefore = Boolean(
			prev.showAdditionalCostDropdown &&
			prev.additionalCostCategory &&
			(prev.additionalCostCategory !== AdditionalCost.Material &&
			prev.additionalCostCategory !== AdditionalCost.Maintain
				? true
				: prev.additionalCostProductionOrderId != null) &&
			(prev.additionalCostCategory !== AdditionalCost.OtherMaterial ||
				prev.otherMaterialDetail != null),
		);
		const categoryJustReady = !hasCategoryActiveBefore && hasCategoryActiveNow;
		const additionalCostJustSelected =
			!hasAdditionalCostActiveBefore && hasAdditionalCostActiveNow;
		const contractLimitJustSelected =
			!prev.contractLimitCategory && contractLimitCategoryValue;
		const assetJustEnabled = !prev.showAssetDropdown && showAssetDropdown;
		const exportedQtyNow = Number(quantityExported) || 0;

		if (assetJustEnabled && exportedQtyNow > 0) {
			set('assetQuantity', exportedQtyNow);
		} else if (contractLimitJustSelected && exportedQtyNow > 0) {
			set('contractLimitQuantity', exportedQtyNow);
		} else if (categoryJustReady || additionalCostJustSelected) {
			if (hasCategoryActiveNow && hasAdditionalCostActiveNow) {
				set('categoryQuantity', exportedQtyNow / 2);
				set('additionalCostQuantity', exportedQtyNow / 2);
			} else if (hasCategoryActiveNow && categoryJustReady) {
				set('categoryQuantity', exportedQtyNow);
			} else if (hasAdditionalCostActiveNow && additionalCostJustSelected) {
				set('additionalCostQuantity', exportedQtyNow);
			}
		}

		prevDropdownState.current = {
			category: categoryValue,
			categoryProcessGroup: categoryProcessGroupValue,
			categoryProductionOrderId,
			categoryEquipmentId,
			additionalCostCategory,
			additionalCostProductionOrderId,
			otherMaterialDetail: otherMaterialDetailValue,
			contractLimitCategory: contractLimitCategoryValue,
			showCategoryDropdown,
			showAdditionalCostDropdown,
			showAssetDropdown,
		};
	}, [
		categoryValue,
		resolvedCategoryValue,
		categoryProcessGroupValue,
		categoryProductionOrderId,
		categoryEquipmentId,
		additionalCostCategory,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		contractLimitCategoryValue,
		quantityExported,
		isSparePartByEquipment,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
	]);

	const prevContractLimitCategoryRef = useRef(contractLimitCategoryValue);
	const prevContractLimitSelectionSignatureRef = useRef('');
	useEffect(() => {
		if (prevContractLimitCategoryRef.current !== contractLimitCategoryValue) {
			set('contractLimitSubCategory', null);
			set('contractLimitSubCategories', []);
			set('contractLimitBreakdown', {});
			set('contractLimitQuantity', null);
			prevContractLimitSelectionSignatureRef.current = '';
			prevContractLimitCategoryRef.current = contractLimitCategoryValue;
		}
	}, [contractLimitCategoryValue]);

	useEffect(() => {
		if (!showContractLimitDropdown) return;
		if (contractLimitCategoryValue != null) return;
		if (DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE == null) return;
		set('contractLimitCategory', DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE);
	}, [showContractLimitDropdown, contractLimitCategoryValue]);

	useEffect(() => {
		if (!showContractLimitDropdown || !contractLimitCategoryValue) return;
		if (needsSecondComboBox) return;

		const exportedQty = Number(quantityExported) || 0;
		if (Math.abs((Number(contractLimitQuantity) || 0) - exportedQty) < 0.0001) {
			return;
		}

		set('contractLimitQuantity', exportedQty);
	}, [
		showContractLimitDropdown,
		contractLimitCategoryValue,
		needsSecondComboBox,
		quantityExported,
		contractLimitQuantity,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		const selectedKeys =
			contractLimitSubCategoriesValue &&
			contractLimitSubCategoriesValue.length > 0
				? contractLimitSubCategoriesValue
				: [];

		if (selectedKeys.length === 0) {
			if (!DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE) return;
			set('contractLimitSubCategories', [
				DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE,
			]);
			return;
		}

		const selectedSignature = [...selectedKeys].sort().join(',');
		if (prevContractLimitSelectionSignatureRef.current === selectedSignature) {
			return;
		}

		const exportedQty = Number(quantityExported) || 0;
		const divided = exportedQty / selectedKeys.length;
		const nextBreakdown: Record<string, number | string> = {};
		for (const key of selectedKeys) {
			nextBreakdown[key] = divided;
		}
		set('contractLimitBreakdown', nextBreakdown);
		prevContractLimitSelectionSignatureRef.current = selectedSignature;
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitSubCategoriesValue,
		quantityExported,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		if (
			Math.abs(
				(Number(contractLimitQuantity) || 0) - contractLimitBreakdownTotal,
			) < 0.0001
		) {
			return;
		}
		set('contractLimitQuantity', contractLimitBreakdownTotal);
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitQuantity,
		contractLimitBreakdownTotal,
	]);

	// ── Total validation ─────────────────────────────────────────────────────
	const totalQuantity = (() => {
		let total = 0;
		if (
			showCategoryDropdown &&
			resolvedCategoryValue &&
			categoryProcessGroupValue &&
			(!categoryNeedsProductionOrder || categoryProductionOrderId != null) &&
			(!categoryNeedsEquipment || categoryEquipmentId != null) &&
			categoryQuantity != null
		)
			total += Number(categoryQuantity);
		if (
			showAdditionalCostDropdown &&
			additionalCostCategory &&
			(!additionalCostNeedsProductionOrder ||
				additionalCostProductionOrderId != null) &&
			(!additionalCostNeedsOtherMaterialDetail ||
				otherMaterialDetailValue != null) &&
			additionalCostQuantity != null
		)
			total += Number(additionalCostQuantity);
		if (
			showContractLimitDropdown &&
			contractLimitCategoryValue &&
			(!needsSecondComboBox || contractLimitSelectedTypes.length > 0) &&
			contractLimitQuantity != null
		)
			total += Number(contractLimitQuantity);
		if (showAssetDropdown && assetQuantity != null)
			total += Number(assetQuantity);
		return total;
	})();

	const exportedQty = Number(quantityExported) || 0;
	const receivedQty = Number(quantityReceived) || 0;
	const isValidTotal = Math.abs(totalQuantity - exportedQty) < 0.01;
	const isContractLimitBreakdownValid =
		!needsSecondComboBox ||
		Math.abs(contractLimitBreakdownTotal - exportedQty) < 0.01;

	// ── Breakdown change handlers ────────────────────────────────────────────
	const handleReceivedBreakdownChange = (key: string, val: number | string) =>
		set('receivedBreakdown', { ...(receivedBreakdown ?? {}), [key]: val });

	const handleExportedBreakdownChange = (key: string, val: number | string) =>
		set('exportedBreakdown', { ...(exportedBreakdown ?? {}), [key]: val });
	const handleContractLimitBreakdownChange = (
		key: string,
		val: number | string,
	) =>
		set('contractLimitBreakdown', {
			...(contractLimitBreakdown ?? {}),
			[key]: val,
		});

	const activeReceivedKeys = receivedTypes ?? [RECEIVED_TYPE_OPTIONS[0].value];
	const activeExportedKeys = exportedTypes ?? [EXPORTED_TYPE_OPTIONS[0].value];
	const showReceivedBreakdown = activeReceivedKeys.length > 1;
	const showExportedBreakdown = activeExportedKeys.length > 1;
	const materialBadge = getMaterialBadge(materialTypeValue, itemTypeValue);
	const needsMaintainUnitPriceCreation =
		materialTypeValue === MaterialType.SparePart &&
		Number(partTypeValue ?? 1) === 1 &&
		!hasMaintainUnitPriceEquipment;
	const partEquipments = materialOrPartId
		? (partEquipmentsByPartId[materialOrPartId] ?? [])
		: [];

	// ── Render ───────────────────────────────────────────────────────────────
	return (
		<>
			<TableRow
				className={cn(
					'transition-colors',
					materialTypeValue === MaterialType.Material
						? 'bg-blue-50/40 hover:bg-blue-50/70'
						: materialTypeValue === MaterialType.SparePart
							? 'bg-amber-50/40 hover:bg-amber-50/70'
							: 'hover:bg-slate-50/50',
				)}
			>
				{/* STT */}
				<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
					{displayIndex ?? index + 1}
				</TableCell>

				{/* Mã vật tư */}
				<TableCell className='sticky left-16 z-20 w-[10%] min-w-32 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
					<div className='flex flex-col gap-1'>
						<div className='flex items-center gap-2'>
							<Input
								readOnly
								value={
									(form.watch(
										`${basename}.materialCode` as RowPath,
									) as string) || ''
								}
								className={cn(
									'font-medium',
									needsMaintainUnitPriceCreation
										? 'border-red-500 bg-red-50 text-red-700'
										: 'border-slate-300 bg-slate-100 text-slate-500',
								)}
							/>
							{needsMaintainUnitPriceCreation && (
								<Button
									type='button'
									variant='outline'
									size='icon'
									className='size-9 border-red-300 text-red-600 hover:bg-red-50 hover:text-red-700'
									onClick={() => setIsCreateMaintainDialogOpen(true)}
									title='Tạo mới đơn giá và định mức SCTX'
								>
									<PlusCircleIcon className='size-4' />
								</Button>
							)}
						</div>
						{needsMaintainUnitPriceCreation && (
							<span className='text-[11px] font-medium text-red-600'>
								Chưa có đơn giá và định mức SCTX cho phụ tùng này
							</span>
						)}
						<span className={materialBadge.className}>
							{materialBadge.label}
						</span>
					</div>
				</TableCell>

				{/* Đơn vị tính */}
				<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
					<Input
						readOnly
						value={
							(form.watch(
								`${basename}.unitOfMeasureName` as RowPath,
							) as string) || ''
						}
						className='border-slate-300 bg-slate-100 text-slate-500'
					/>
				</TableCell>

				{/* Số lượng lĩnh */}
				<TableCell className='border-b border-slate-200 px-4 py-4'>
					{(() => {
						const subTotal = activeReceivedKeys.reduce(
							(acc, key) => acc + (Number((receivedBreakdown ?? {})[key]) || 0),
							0,
						);
						const isReceivedValid =
							!showReceivedBreakdown || Math.abs(subTotal - receivedQty) < 0.01;
						return (
							<div className='flex flex-col gap-2'>
								<div className='flex items-end gap-2'>
									{/* Total — always leftmost */}
									<div
										className={cn(
											'flex shrink-0 flex-col gap-0.5',
											showReceivedBreakdown ? 'w-24' : 'w-full',
										)}
									>
										<label className='text-[10px] font-medium text-slate-500'>
											Tổng
										</label>
										<Input
											readOnly
											value={receivedQty}
											className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
										/>
									</div>
									{/* Sub-inputs — only when >1 selected */}
									{showReceivedBreakdown && (
										<QuantityBreakdownInputs
											selectedKeys={activeReceivedKeys}
											allOptions={RECEIVED_TYPE_OPTIONS}
											values={receivedBreakdown ?? {}}
											onChange={handleReceivedBreakdownChange}
											isValid={isReceivedValid}
										/>
									)}
								</div>
								<FormMultiSelect
									control={form.control}
									name={`${basename}.receivedTypes` as RowPath}
									options={RECEIVED_TYPE_OPTIONS}
									placeholder='Chọn loại lĩnh'
								/>
								{showReceivedBreakdown && !isReceivedValid && (
									<p className='text-[11px] font-medium text-red-500'>
										Tổng các mục ({subTotal}) phải bằng số lượng lĩnh (
										{receivedQty})
									</p>
								)}
							</div>
						);
					})()}
				</TableCell>

				{/* Số lượng xuất */}
				<TableCell className='border-b border-slate-200 px-4 py-4'>
					{(() => {
						const subTotal = activeExportedKeys.reduce(
							(acc, key) => acc + (Number((exportedBreakdown ?? {})[key]) || 0),
							0,
						);
						const isExportedValid =
							!showExportedBreakdown || Math.abs(subTotal - exportedQty) < 0.01;
						return (
							<div className='flex flex-col gap-2'>
								<div className='flex items-end gap-2'>
									{/* Total — always leftmost */}
									<div
										className={cn(
											'flex shrink-0 flex-col gap-0.5',
											showExportedBreakdown ? 'w-24' : 'w-full',
										)}
									>
										<label className='text-[10px] font-medium text-slate-500'>
											Tổng
										</label>
										<Input
											readOnly
											value={exportedQty}
											className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
										/>
									</div>
									{/* Sub-inputs — only when >1 selected */}
									{showExportedBreakdown && (
										<QuantityBreakdownInputs
											selectedKeys={activeExportedKeys}
											allOptions={EXPORTED_TYPE_OPTIONS}
											values={exportedBreakdown ?? {}}
											onChange={handleExportedBreakdownChange}
											isValid={isExportedValid}
										/>
									)}
								</div>
								<FormMultiSelect
									control={form.control}
									name={`${basename}.exportedTypes` as RowPath}
									options={EXPORTED_TYPE_OPTIONS}
									placeholder='Chọn loại xuất'
								/>
								{showExportedBreakdown && !isExportedValid && (
									<p className='text-[11px] font-medium text-red-500'>
										Tổng các mục ({subTotal}) phải bằng số lượng xuất (
										{exportedQty})
									</p>
								)}
							</div>
						);
					})()}
				</TableCell>

				{/* Vật tư tính vào doanh thu khoán */}
				<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
					<div className='flex flex-col items-center gap-3'>
						<div className='flex justify-center *:w-auto!'>
							<FormCheckBox
								control={form.control}
								name={`${basename}.showCategoryDropdown` as RowPath}
							/>
						</div>
						{!showCategoryDropdown &&
							!showAdditionalCostDropdown &&
							!showContractLimitDropdown &&
							!showAssetDropdown &&
							form.formState.errors?.materials?.[index]
								?.showCategoryDropdown && (
								<p className='text-xs text-red-600'>
									{
										form.formState.errors.materials[index]?.showCategoryDropdown
											?.message
									}
								</p>
							)}
						{showCategoryDropdown && (
							<>
								{resolvedCategoryValue && (
									<div className='w-full'>
										<FormComboBox
											control={form.control}
											name={`${basename}.categoryProcessGroup` as RowPath}
											options={processGroupOptions}
											placeholder='Chọn nhóm công đoạn'
										/>
									</div>
								)}
								{resolvedCategoryValue ===
									MaterialsIncludedInContractRevenue.Maintain && (
									<>
										<div className='w-full'>
											<FormComboBox
												control={form.control}
												name={
													`${basename}.categoryProductionOrderId` as RowPath
												}
												options={categoryProductionOrderOptions}
												placeholder='Chọn quyết định, lệnh sản xuất'
											/>
										</div>
										{categoryNeedsEquipment && (
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={`${basename}.categoryEquipmentId` as RowPath}
													options={categoryEquipmentOptions}
													placeholder='Chọn thiết bị'
												/>
											</div>
										)}
									</>
								)}
								{resolvedCategoryValue &&
									categoryProcessGroupValue &&
									(!categoryNeedsProductionOrder ||
										categoryProductionOrderId != null) &&
									(!categoryNeedsEquipment || categoryEquipmentId != null) && (
										<div className='w-full'>
											<label className='mb-1.5 block text-xs font-medium text-slate-600'>
												Số lượng vật tư
											</label>
											<FormNumber
												control={form.control}
												name={`${basename}.categoryQuantity` as RowPath}
												placeholder='Nhập số lượng'
											/>
											<div
												className={cn(
													'mt-1 text-xs',
													isValidTotal ? 'text-green-600' : 'text-red-600',
												)}
											>
												Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
											</div>
										</div>
									)}
							</>
						)}
					</div>
				</TableCell>

				{/* Bổ sung chi phí */}
				<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
					<div className='flex flex-col items-center gap-3'>
						<div className='flex justify-center *:w-auto!'>
							<FormCheckBox
								control={form.control}
								name={`${basename}.showAdditionalCostDropdown` as RowPath}
							/>
						</div>
						{showAdditionalCostDropdown && (
							<>
								{!isSafetyAndWelfareMaterial && (
									<div className='w-full'>
										<FormComboBox
											control={form.control}
											name={`${basename}.additionalCostCategory` as RowPath}
											options={additionalCostOptionsByType}
											placeholder='Chọn danh mục'
										/>
									</div>
								)}
								{additionalCostCategory && (
									<>
										{additionalCostNeedsProductionOrder && (
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={
														`${basename}.additionalCostProductionOrderId` as RowPath
													}
													options={additionalCostOrderOrEquipmentOptions}
													placeholder='Chọn quyết định, lệnh sản xuất'
												/>
											</div>
										)}
										{additionalCostNeedsOtherMaterialDetail && (
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={`${basename}.otherMaterialDetail` as RowPath}
													options={OTHER_MATERIAL_DETAIL_OPTIONS}
													placeholder='Chọn loại vật tư'
												/>
											</div>
										)}
									</>
								)}
								{additionalCostCategory &&
									(!additionalCostNeedsProductionOrder ||
										additionalCostProductionOrderId != null) &&
									(!additionalCostNeedsOtherMaterialDetail ||
										otherMaterialDetailValue != null) && (
										<div className='w-full'>
											<label className='mb-1.5 block text-xs font-medium text-slate-600'>
												Số lượng vật tư
											</label>
											<FormNumber
												control={form.control}
												name={`${basename}.additionalCostQuantity` as RowPath}
												placeholder='Nhập số lượng'
											/>
											<div
												className={cn(
													'mt-1 text-xs',
													isValidTotal ? 'text-green-600' : 'text-red-600',
												)}
											>
												Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
											</div>
										</div>
									)}
							</>
						)}
					</div>
				</TableCell>

				{/* Vật tư theo hạn mức */}
				<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
					<div className='flex flex-col items-center gap-3'>
						<div className='flex justify-center *:w-auto!'>
							<FormCheckBox
								control={form.control}
								name={`${basename}.showContractLimitDropdown` as RowPath}
							/>
						</div>
						{showContractLimitDropdown && (
							<>
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.contractLimitCategory` as RowPath}
										options={CONTRACT_LIMIT_OPTIONS}
										placeholder='Chọn danh mục'
									/>
								</div>
								{contractLimitCategoryValue && needsSecondComboBox && (
									<>
										<div className='w-full'>
											<FormMultiSelect
												control={form.control}
												name={
													`${basename}.contractLimitSubCategories` as RowPath
												}
												options={CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS}
												placeholder='Chọn loại (Lĩnh mới/Tái sử dụng)'
											/>
										</div>
										{contractLimitSelectedKeys.length > 0 && (
											<div className='w-full space-y-2'>
												<div className='flex w-full items-end gap-2'>
													<QuantityBreakdownInputs
														selectedKeys={contractLimitSelectedKeys}
														allOptions={CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS}
														values={contractLimitBreakdown ?? {}}
														onChange={handleContractLimitBreakdownChange}
														isValid={isContractLimitBreakdownValid}
														equalWidth
													/>
												</div>
												{!isContractLimitBreakdownValid && (
													<p className='text-xs text-red-600'>
														Tổng lĩnh mới + lĩnh tái sử dụng phải bằng số lượng
														xuất ({contractLimitBreakdownTotal} / {exportedQty})
													</p>
												)}
											</div>
										)}
									</>
								)}
								{contractLimitCategoryValue &&
									(!needsSecondComboBox ||
										contractLimitSelectedTypes.length > 0) && (
										<div className='w-full'>
											{!needsSecondComboBox && (
												<>
													<label className='mb-1.5 block text-xs font-medium text-slate-600'>
														Số lượng vật tư
													</label>
													<FormNumber
														control={form.control}
														name={
															`${basename}.contractLimitQuantity` as RowPath
														}
														placeholder='Nhập số lượng'
													/>
												</>
											)}
											<div
												className={cn(
													'mt-1 text-xs',
													isValidTotal ? 'text-green-600' : 'text-red-600',
												)}
											>
												Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
											</div>
										</div>
									)}
							</>
						)}
					</div>
				</TableCell>

				{/* Tài sản */}
				<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
					<div className='flex flex-col items-center gap-3'>
						<div className='flex justify-center *:w-auto!'>
							<FormCheckBox
								control={form.control}
								name={`${basename}.showAssetDropdown` as RowPath}
							/>
						</div>
						{showAssetDropdown && (
							<div className='w-full'>
								<label className='mb-1.5 block text-xs font-medium text-slate-600'>
									Số lượng
								</label>
								<FormNumber
									control={form.control}
									name={`${basename}.assetQuantity` as RowPath}
									placeholder='Nhập số lượng'
								/>
								<div
									className={cn(
										'mt-1 text-xs',
										isValidTotal ? 'text-green-600' : 'text-red-600',
									)}
								>
									Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
								</div>
							</div>
						)}
					</div>
				</TableCell>
			</TableRow>
			{needsMaintainUnitPriceCreation && materialOrPartId && (
				<MaintainUnitPriceCreateDialog
					open={isCreateMaintainDialogOpen}
					onOpenChange={setIsCreateMaintainDialogOpen}
					partCode={
						(form.watch(`${basename}.materialCode` as RowPath) as string) || ''
					}
					processGroupOptions={processGroupOptions}
					equipmentOptions={partEquipments}
					onCreated={() => {
						set('hasMaintainUnitPriceEquipment', true);
					}}
				/>
			)}
		</>
	);
}
