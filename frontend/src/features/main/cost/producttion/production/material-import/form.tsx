import { FormCheckBox } from '@/components/form/form-check-box';
import { FormComboBox } from '@/components/form/form-combo-box';
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
import { useEffect, useRef } from 'react';
import { useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { MaterialFormSchema } from './schema';
import {
	AdditionalCost,
	ADDITIONAL_COST_OPTIONS,
	CATEGORY_OPTIONS,
	CONTRACT_LIMIT_OPTIONS,
	CONTRACT_LIMIT_SECONDARY_OPTIONS,
	MaterialType,
	MaterialsIncludedInContractRevenue,
	QuotaBasedMaterial,
} from './types';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type FieldName = any;

type MaterialImportFormProps = {
	onCancel?: () => void;
	processGroupOptions: ProcessGroupOption[];
};

type ProcessGroupOption = {
	value: string;
	label: string;
};

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === MaterialType.Material) {
		return MaterialsIncludedInContractRevenue.Material;
	}

	if (type === MaterialType.SparePart) {
		return MaterialsIncludedInContractRevenue.Maintain;
	}

	return null;
}

function getDefaultAdditionalCostByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) {
		return AdditionalCost.Material;
	}

	if (type === MaterialType.SparePart) {
		return AdditionalCost.Maintain;
	}

	return null;
}

export function MaterialImportForm({
	onCancel,
	processGroupOptions,
}: MaterialImportFormProps) {
	const form = useFormContext<{ materials: MaterialFormSchema[] }>();
	const { fields, remove } = useFieldArray({
		control: form.control,
		name: 'materials',
	});

	// Debug: log errors when they change
	useEffect(() => {
		if (Object.keys(form.formState.errors).length > 0) {
			console.log('Form validation errors:', form.formState.errors);
		}
	}, [form.formState.errors]);

	return (
		<div className='flex h-full flex-col gap-6'>
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
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Số lượng lĩnh
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
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
							{fields.map((field, index) => (
								<MaterialImportRow
									key={field.id}
									index={index}
									processGroupOptions={processGroupOptions}
									onRemove={() => remove(index)}
								/>
							))}
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

function resetCellFields(
	form: ReturnType<typeof useFormContext<{ materials: MaterialFormSchema[] }>>,
	basename: string,
	fields: {
		checkbox: string;
		dropdown?: string;
		dropdownSecondary?: string;
		quantity?: string;
	}[],
) {
	for (const field of fields) {
		form.setValue(`${basename}.${field.checkbox}` as FieldName, false);
		if (field.dropdown) {
			form.setValue(`${basename}.${field.dropdown}` as FieldName, null);
		}
		if (field.dropdownSecondary) {
			form.setValue(
				`${basename}.${field.dropdownSecondary}` as FieldName,
				null,
			);
		}
		if (field.quantity) {
			form.setValue(`${basename}.${field.quantity}` as FieldName, null);
		}
	}
}

function MaterialImportRow({
	index,
	processGroupOptions,
}: {
	index: number;
	processGroupOptions: ProcessGroupOption[];
	onRemove: () => void;
}) {
	const form = useFormContext<{ materials: MaterialFormSchema[] }>();
	const basename = `materials.${index}` as const;

	const showCategoryDropdown = useWatch({
		control: form.control,
		name: `${basename}.showCategoryDropdown` as FieldName,
	});
	const showAdditionalCostDropdown = useWatch({
		control: form.control,
		name: `${basename}.showAdditionalCostDropdown` as FieldName,
	});
	const showContractLimitDropdown = useWatch({
		control: form.control,
		name: `${basename}.showContractLimitDropdown` as FieldName,
	});
	const showAssetDropdown = useWatch({
		control: form.control,
		name: `${basename}.showAssetDropdown` as FieldName,
	});
	const categoryValue = useWatch({
		control: form.control,
		name: `${basename}.category` as FieldName,
	});
	const categoryProcessGroupValue = useWatch({
		control: form.control,
		name: `${basename}.categoryProcessGroup` as FieldName,
	});
	const additionalCostCategory = useWatch({
		control: form.control,
		name: `${basename}.additionalCostCategory` as FieldName,
	});
	const contractLimitCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitCategory` as FieldName,
	});
	const quantityExported = useWatch({
		control: form.control,
		name: `${basename}.quantityExported` as FieldName,
	});
	const categoryQuantity = useWatch({
		control: form.control,
		name: `${basename}.categoryQuantity` as FieldName,
	});
	const additionalCostQuantity = useWatch({
		control: form.control,
		name: `${basename}.additionalCostQuantity` as FieldName,
	});
	const contractLimitQuantity = useWatch({
		control: form.control,
		name: `${basename}.contractLimitQuantity` as FieldName,
	});
	const contractLimitSubCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitSubCategory` as FieldName,
	});
	const assetQuantity = useWatch({
		control: form.control,
		name: `${basename}.assetQuantity` as FieldName,
	});
	const materialTypeValue = useWatch({
		control: form.control,
		name: `${basename}.type` as FieldName,
	});

	const defaultCategoryByType =
		getDefaultCategoryByMaterialType(materialTypeValue);
	const defaultAdditionalCostByType =
		getDefaultAdditionalCostByMaterialType(materialTypeValue);

	const categoryOptionsByType =
		defaultCategoryByType == null
			? CATEGORY_OPTIONS
			: CATEGORY_OPTIONS.filter(
					(option) => option.value === defaultCategoryByType,
				);

	const additionalCostOptionsByType =
		defaultAdditionalCostByType == null
			? ADDITIONAL_COST_OPTIONS
			: ADDITIONAL_COST_OPTIONS.filter(
					(option) => option.value === defaultAdditionalCostByType,
				);

	// Determine if secondary combobox is needed for contract limit
	const needsSecondComboBox =
		contractLimitCategoryValue === QuotaBasedMaterial.MineSupport ||
		contractLimitCategoryValue === QuotaBasedMaterial.SupportAccessories;

	// Dùng ref để track giá trị trước, tránh loop vô hạn
	const prevState = useRef({
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showContractLimitDropdown: false,
		showAssetDropdown: false,
	});

	const prevDropdownState = useRef({
		category: null as number | null | undefined,
		categoryProcessGroup: null as string | null | undefined,
		additionalCostCategory: null as number | null | undefined,
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

		// Thêm: track vừa TẮT
		const justDisabledCategory =
			prev.showCategoryDropdown && !showCategoryDropdown;
		const justDisabledAdditional =
			prev.showAdditionalCostDropdown && !showAdditionalCostDropdown;
		const justDisabledContractLimit =
			prev.showContractLimitDropdown && !showContractLimitDropdown;

		if (justEnabledContractLimit) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'category',
					dropdownSecondary: 'categoryProcessGroup',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
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
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					quantity: 'additionalCostQuantity',
				},
				{
					checkbox: 'showContractLimitDropdown',
					dropdown: 'contractLimitCategory',
					quantity: 'contractLimitQuantity',
				},
			]);
		} else if (justEnabledCategory) {
			if (prev.showContractLimitDropdown) {
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			}
			if (prev.showAssetDropdown) {
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			}

			if (!categoryValue && defaultCategoryByType != null) {
				form.setValue(
					`${basename}.category` as FieldName,
					defaultCategoryByType,
				);
			}

			if (!categoryProcessGroupValue && processGroupOptions.length === 1) {
				form.setValue(
					`${basename}.categoryProcessGroup` as FieldName,
					processGroupOptions[0].value,
				);
			}
		} else if (justEnabledAdditional) {
			if (prev.showContractLimitDropdown) {
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			}
			if (prev.showAssetDropdown) {
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			}

			if (!additionalCostCategory && defaultAdditionalCostByType != null) {
				form.setValue(
					`${basename}.additionalCostCategory` as FieldName,
					defaultAdditionalCostByType,
				);
			}
		}

		// Reset giá trị khi uncheck
		if (justDisabledCategory) {
			form.setValue(`${basename}.category` as FieldName, null);
			form.setValue(`${basename}.categoryProcessGroup` as FieldName, null);
			form.setValue(`${basename}.categoryQuantity` as FieldName, null);
		}
		if (justDisabledAdditional) {
			form.setValue(`${basename}.additionalCostCategory` as FieldName, null);
			form.setValue(`${basename}.additionalCostQuantity` as FieldName, null);
		}
		if (justDisabledContractLimit) {
			form.setValue(`${basename}.contractLimitCategory` as FieldName, null);
			form.setValue(`${basename}.contractLimitQuantity` as FieldName, null);
			form.setValue(`${basename}.contractLimitSubCategory` as FieldName, null);
		}
		const justDisabledAsset = prev.showAssetDropdown && !showAssetDropdown;
		if (justDisabledAsset) {
			form.setValue(`${basename}.assetQuantity` as FieldName, null);
		}

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
		categoryProcessGroupValue,
		additionalCostCategory,
		defaultCategoryByType,
		defaultAdditionalCostByType,
		processGroupOptions,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showCategoryDropdown || !categoryValue) return;
		if (processGroupOptions.length !== 1) return;
		if (categoryProcessGroupValue) return;

		form.setValue(
			`${basename}.categoryProcessGroup` as FieldName,
			processGroupOptions[0].value,
		);
	}, [
		showCategoryDropdown,
		categoryValue,
		categoryProcessGroupValue,
		processGroupOptions,
		form,
		basename,
	]);

	// Auto-calculate quantity values when dropdowns are selected
	useEffect(() => {
		const prev = prevDropdownState.current;

		const hasCategoryActiveNow = Boolean(
			showCategoryDropdown && categoryValue && categoryProcessGroupValue,
		);
		const hasAdditionalCostActiveNow = Boolean(
			showAdditionalCostDropdown && additionalCostCategory,
		);
		const hasCategoryActiveBefore = Boolean(
			prev.showCategoryDropdown && prev.category && prev.categoryProcessGroup,
		);
		const hasAdditionalCostActiveBefore = Boolean(
			prev.showAdditionalCostDropdown && prev.additionalCostCategory,
		);
		const categoryJustReady = !hasCategoryActiveBefore && hasCategoryActiveNow;
		const additionalCostJustSelected =
			!hasAdditionalCostActiveBefore && hasAdditionalCostActiveNow;
		// Check if contractLimit dropdown just got a value
		const contractLimitJustSelected =
			!prev.contractLimitCategory && contractLimitCategoryValue;
		// Check if asset checkbox just got enabled
		const assetJustEnabled = !prev.showAssetDropdown && showAssetDropdown;

		const exportedQty = Number(quantityExported) || 0;

		if (assetJustEnabled && exportedQty > 0) {
			// Asset gets full exported quantity
			form.setValue(`${basename}.assetQuantity` as FieldName, exportedQty);
		} else if (contractLimitJustSelected && exportedQty > 0) {
			// Contract limit gets full exported quantity
			form.setValue(
				`${basename}.contractLimitQuantity` as FieldName,
				exportedQty,
			);
		} else if (categoryJustReady || additionalCostJustSelected) {
			if (hasCategoryActiveNow && hasAdditionalCostActiveNow) {
				// Both category and additional cost are selected - split in half
				const halfQty = exportedQty / 2;
				form.setValue(`${basename}.categoryQuantity` as FieldName, halfQty);
				form.setValue(
					`${basename}.additionalCostQuantity` as FieldName,
					halfQty,
				);
			} else if (hasCategoryActiveNow && categoryJustReady) {
				// Only category is selected - full quantity
				form.setValue(`${basename}.categoryQuantity` as FieldName, exportedQty);
			} else if (hasAdditionalCostActiveNow && additionalCostJustSelected) {
				// Only additional cost is selected - full quantity
				form.setValue(
					`${basename}.additionalCostQuantity` as FieldName,
					exportedQty,
				);
			}
		}

		prevDropdownState.current = {
			category: categoryValue,
			categoryProcessGroup: categoryProcessGroupValue,
			additionalCostCategory: additionalCostCategory,
			contractLimitCategory: contractLimitCategoryValue,
			showCategoryDropdown: showCategoryDropdown,
			showAdditionalCostDropdown: showAdditionalCostDropdown,
			showAssetDropdown: showAssetDropdown,
		};
	}, [
		categoryValue,
		categoryProcessGroupValue,
		additionalCostCategory,
		contractLimitCategoryValue,
		quantityExported,
		form,
		basename,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
	]);

	// Reset contractLimitSubCategory when contractLimitCategory changes
	const prevContractLimitCategoryRef = useRef(contractLimitCategoryValue);
	useEffect(() => {
		if (prevContractLimitCategoryRef.current !== contractLimitCategoryValue) {
			form.setValue(`${basename}.contractLimitSubCategory` as FieldName, null);
			prevContractLimitCategoryRef.current = contractLimitCategoryValue;
		}
	}, [contractLimitCategoryValue, form, basename]);

	// Calculate total and validation status
	const calculateTotal = () => {
		let total = 0;
		const hasCategoryActive =
			showCategoryDropdown && categoryValue && categoryProcessGroupValue;
		const hasAdditionalCostActive =
			showAdditionalCostDropdown && additionalCostCategory;
		const hasContractLimitActive =
			showContractLimitDropdown &&
			contractLimitCategoryValue &&
			(!needsSecondComboBox || contractLimitSubCategoryValue);
		const hasAssetActive = showAssetDropdown;

		if (hasCategoryActive && categoryQuantity != null) {
			total += Number(categoryQuantity);
		}
		if (hasAdditionalCostActive && additionalCostQuantity != null) {
			total += Number(additionalCostQuantity);
		}
		if (hasContractLimitActive && contractLimitQuantity != null) {
			total += Number(contractLimitQuantity);
		}
		if (hasAssetActive && assetQuantity != null) {
			total += Number(assetQuantity);
		}

		return total;
	};

	const totalQuantity = calculateTotal();
	const exportedQty = Number(quantityExported) || 0;
	const isValidTotal = Math.abs(totalQuantity - exportedQty) < 0.01;

	return (
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
			<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
				{index + 1}
			</TableCell>
			<TableCell className='sticky left-16 z-20 w-[10%] min-w-32 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
				<div className='flex flex-col gap-1'>
					<Input
						readOnly
						value={
							(form.watch(`${basename}.materialCode` as FieldName) as string) ||
							''
						}
						className='border-slate-300 bg-slate-100 font-medium text-slate-500'
					/>
					{materialTypeValue === MaterialType.Material && (
						<span className='rounded bg-blue-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-blue-700'>
							Vật liệu
						</span>
					)}
					{materialTypeValue === MaterialType.SparePart && (
						<span className='rounded bg-amber-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-amber-700'>
							Phụ tùng
						</span>
					)}
				</div>
			</TableCell>
			<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
				<Input
					readOnly
					value={
						(form.watch(
							`${basename}.unitOfMeasureName` as FieldName,
						) as string) || ''
					}
					className='border-slate-300 bg-slate-100 text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
				<Input
					readOnly
					value={
						(form.watch(
							`${basename}.quantityReceived` as FieldName,
						) as number) || 0
					}
					className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
				/>
			</TableCell>
			<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
				<Input
					readOnly
					value={
						(form.watch(
							`${basename}.quantityExported` as FieldName,
						) as number) || 0
					}
					className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
				/>
			</TableCell>
			{/* Vật tư tính vào doanh thu khoán */}
			<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showCategoryDropdown` as FieldName}
						/>
					</div>
					{!showCategoryDropdown &&
						!showAdditionalCostDropdown &&
						!showContractLimitDropdown &&
						!showAssetDropdown &&
						form.formState.errors?.materials?.[index]?.showCategoryDropdown && (
							<p className='text-xs text-red-600'>
								{
									form.formState.errors.materials[index]?.showCategoryDropdown
										?.message
								}
							</p>
						)}
					{showCategoryDropdown && (
						<>
							<div className='w-full'>
								<FormComboBox
									control={form.control}
									name={`${basename}.category` as FieldName}
									options={categoryOptionsByType}
									placeholder='Chọn danh mục'
								/>
							</div>
							{categoryValue && (
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.categoryProcessGroup` as FieldName}
										options={processGroupOptions}
										placeholder='Chọn nhóm công đoạn'
									/>
								</div>
							)}
							{categoryValue && categoryProcessGroupValue && (
								<div className='w-full'>
									<label className='mb-1.5 block text-xs font-medium text-slate-600'>
										Số lượng vật tư
									</label>
									<FormNumber
										control={form.control}
										name={`${basename}.categoryQuantity` as FieldName}
										placeholder='Nhập số lượng'
									/>
									<div
										className={cn(
											'mt-1 text-xs',
											isValidTotal ? 'text-green-600' : 'text-red-600',
										)}
									>
										<div>
											Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
										</div>
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
							name={`${basename}.showAdditionalCostDropdown` as FieldName}
						/>
					</div>
					{showAdditionalCostDropdown && (
						<>
							<div className='w-full'>
								<FormComboBox
									control={form.control}
									name={`${basename}.additionalCostCategory` as FieldName}
									options={additionalCostOptionsByType}
									placeholder='Chọn danh mục'
								/>
							</div>
							{additionalCostCategory && (
								<div className='w-full'>
									<label className='mb-1.5 block text-xs font-medium text-slate-600'>
										Số lượng vật tư
									</label>
									<FormNumber
										control={form.control}
										name={`${basename}.additionalCostQuantity` as FieldName}
										placeholder='Nhập số lượng'
									/>
									<div
										className={cn(
											'mt-1 text-xs',
											isValidTotal ? 'text-green-600' : 'text-red-600',
										)}
									>
										<div>
											Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
										</div>
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
							name={`${basename}.showContractLimitDropdown` as FieldName}
						/>
					</div>
					{showContractLimitDropdown && (
						<>
							<div className='w-full'>
								<FormComboBox
									control={form.control}
									name={`${basename}.contractLimitCategory` as FieldName}
									options={CONTRACT_LIMIT_OPTIONS}
									placeholder='Chọn danh mục'
								/>
							</div>
							{contractLimitCategoryValue && needsSecondComboBox && (
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.contractLimitSubCategory` as FieldName}
										options={CONTRACT_LIMIT_SECONDARY_OPTIONS}
										placeholder='Chọn danh mục phụ'
									/>
								</div>
							)}
							{contractLimitCategoryValue &&
								(!needsSecondComboBox || contractLimitSubCategoryValue) && (
									<div className='w-full'>
										<label className='mb-1.5 block text-xs font-medium text-slate-600'>
											Số lượng vật tư
										</label>
										<FormNumber
											control={form.control}
											name={`${basename}.contractLimitQuantity` as FieldName}
											placeholder='Nhập số lượng'
										/>
										<div
											className={cn(
												'mt-1 text-xs',
												isValidTotal ? 'text-green-600' : 'text-red-600',
											)}
										>
											<div>
												Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
											</div>
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
							name={`${basename}.showAssetDropdown` as FieldName}
						/>
					</div>
					{showAssetDropdown && (
						<div className='w-full'>
							<label className='mb-1.5 block text-xs font-medium text-slate-600'>
								Số lượng
							</label>
							<FormNumber
								control={form.control}
								name={`${basename}.assetQuantity` as FieldName}
								placeholder='Nhập số lượng'
							/>
							<div
								className={cn(
									'mt-1 text-xs',
									isValidTotal ? 'text-green-600' : 'text-red-600',
								)}
							>
								<div>
									Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
								</div>
							</div>
						</div>
					)}
				</div>
			</TableCell>
		</TableRow>
	);
}
