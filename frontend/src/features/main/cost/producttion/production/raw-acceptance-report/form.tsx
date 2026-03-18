/* eslint-disable react-hooks/incompatible-library */
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormNumber } from '@/components/form/form-number';
import { usePopup } from '@/components/popup';
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
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import {
	RAW_ACCEPTANCE_REPORT_FORM_DEFAULT,
	rawAcceptanceReportFormSchema,
	RawAcceptanceReportFormSchema,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/schema';
import { AcceptanceReportDetail } from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import {
	ADDITIONAL_COST_OPTIONS,
	CATEGORY_OPTIONS,
	CONTRACT_LIMIT_OPTIONS,
	CONTRACT_LIMIT_SECONDARY_OPTIONS,
	MaterialsIncludedInContractRevenue,
	AdditionalCost,
	QuotaBasedMaterial,
	Asset,
	MaterialType,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import { formatNumber, cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import {
	FormProvider,
	useFieldArray,
	useForm,
	useFormContext,
	useWatch,
} from 'react-hook-form';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type FieldName = any;

type ProcessGroupOption = {
	value: string;
	label: string;
};

type ProductionOutputScopeResponse = {
	processGroups?: {
		processGroupId: string;
	}[];
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

export function RawAcceptanceReportForm({
	id,
	output,
	callback,
}: ProductCostFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [loading, setLoading] = useState(false);
	const [acceptanceReportId, setAcceptanceReportId] = useState<string>('');
	const [filePath, setFilePath] = useState<string>('');
	const [processGroupOptions, setProcessGroupOptions] = useState<
		ProcessGroupOption[]
	>([]);

	const form = useForm<RawAcceptanceReportFormSchema>({
		resolver: zodResolver(rawAcceptanceReportFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...RAW_ACCEPTANCE_REPORT_FORM_DEFAULT,
			productionId: id || '',
		},
	});

	useEffect(() => {
		if (!id) {
			setProcessGroupOptions([]);
			return;
		}

		let isMounted = true;

		const fetchScopedProcessGroups = async () => {
			try {
				const [outputRes, processGroupRes] = await Promise.all([
					api.get<ProductionOutputScopeResponse>(
						API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(id),
					),
					api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
						ignorePagination: true,
					}),
				]);

				if (!isMounted) return;

				const allowedIds = new Set(
					(outputRes.result.processGroups || []).map(
						(group) => group.processGroupId,
					),
				);

				const options = processGroupRes.result.data
					.filter((group) => allowedIds.has(group.id))
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((group) => ({
						value: group.id,
						label: `${group.code} - ${group.name}`,
					}));

				setProcessGroupOptions(options);
			} catch (err) {
				if (!isMounted) return;
				setProcessGroupOptions([]);
				console.error('Failed to fetch scoped process groups:', err);
			}
		};

		fetchScopedProcessGroups();

		return () => {
			isMounted = false;
		};
	}, [id]);

	useEffect(() => {
		if (!id || !output?.acceptanceReportId) return;

		// Load existing data from API
		const fetchAcceptanceReport = async () => {
			setLoading(true);
			try {
				const response = await api.get<AcceptanceReportDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(
						output.acceptanceReportId!,
					),
				);

				if (response.result) {
					// Save id and filePath for update
					setAcceptanceReportId(response.result.id);
					setFilePath(response.result.filePath);

					const items = response.result.items.map((item) => {
						// Map enum values to form field values
						const showCategoryDropdown =
							item.materialsIncludedInContractRevenue !==
							MaterialsIncludedInContractRevenue.None;
						const showAdditionalCostDropdown =
							item.additionalCost !== AdditionalCost.None;
						const showContractLimitDropdown =
							item.quotaBasedMaterial !== QuotaBasedMaterial.None;
						const showAssetDropdown = item.asset !== Asset.None;

						// Determine material code and name based on type
						// type = 1: Material, type = 2: Spare Part
						const materialCode =
							item.type === 1 ? item.materialCode : item.partCode;
						const materialName =
							item.type === 1 ? item.materialName : item.partName;

						return {
							id: item.id || '',
							materialCode: materialCode || '',
							materialName: materialName || '',
							unit: item.unitOfMeasureName || '',
							plannedUnitPrice: item.planCost || 0,
							actualUnitPrice: item.actualCost || 0,
							receivedQuantity: item.issuedQuantity || 0,
							exportedQuantity: item.shippedQuantity || 0,
							showCategoryDropdown,
							showAdditionalCostDropdown,
							showContractLimitDropdown,
							showAssetDropdown,
							category: showCategoryDropdown
								? item.materialsIncludedInContractRevenue
								: null,
							categoryProcessGroup: showCategoryDropdown
								? item.processGroupId || null
								: null,
							additionalCostCategory: showAdditionalCostDropdown
								? item.additionalCost
								: null,
							contractLimitCategory: showContractLimitDropdown
								? item.quotaBasedMaterial
								: null,
							contractLimitSubCategory: showContractLimitDropdown
								? item.quotaBasedMaterialType
								: null,
							categoryQuantity: showCategoryDropdown
								? item.materialsIncludedInContractRevenueQuantity || null
								: null,
							additionalCostQuantity: showAdditionalCostDropdown
								? item.additionalCostQuantity || null
								: null,
							contractLimitQuantity: showContractLimitDropdown
								? item.quotaBasedMaterialQuantity || null
								: null,
							assetQuantity: showAssetDropdown
								? item.assetMaterialQuantity || null
								: null,
							type: item.type,
						};
					});

					form.reset({
						productionId: id || '',
						items,
					});
				}
			} catch (err) {
				console.error('Failed to fetch acceptance report:', err);
				error(
					err instanceof Error
						? err.message
						: 'Lỗi tải dữ liệu biên bản nghiệm thu',
				);
			} finally {
				setLoading(false);
			}
		};

		fetchAcceptanceReport();
	}, [id, output?.acceptanceReportId]);

	const handleSubmit = async (values: RawAcceptanceReportFormSchema) => {
		try {
			if (!acceptanceReportId || !filePath) {
				error('Thiếu thông tin cần thiết');
				return;
			}

			// Transform form data to API request
			const requestData = {
				id: acceptanceReportId,
				filePath,
				items: values.items.map((item) => {
					// Map category to enum
					let materialsIncludedInContractRevenue: number =
						MaterialsIncludedInContractRevenue.None;
					if (item.showCategoryDropdown && item.category) {
						materialsIncludedInContractRevenue = item.category;
					}

					const processGroupId =
						item.showCategoryDropdown && item.category
							? item.categoryProcessGroup || null
							: null;

					// Map additional cost to enum
					let additionalCost: number = AdditionalCost.None;
					if (item.showAdditionalCostDropdown && item.additionalCostCategory) {
						additionalCost = item.additionalCostCategory;
					}

					// Map quota based material to enum
					let quotaBasedMaterial: number = QuotaBasedMaterial.None;
					let quotaBasedMaterialType: number = 1;
					if (item.showContractLimitDropdown && item.contractLimitCategory) {
						quotaBasedMaterial = item.contractLimitCategory;
						// Set quotaBasedMaterialType from contractLimitSubCategory
						if (item.contractLimitSubCategory) {
							quotaBasedMaterialType = item.contractLimitSubCategory;
						}
					}

					// Map asset to enum
					const asset: number = item.showAssetDropdown
						? Asset.True
						: Asset.None;

					return {
						id: item.id || '',
						issuedQuantity: item.receivedQuantity || 0,
						shippedQuantity: item.exportedQuantity || 0,
						materialsIncludedInContractRevenue,
						processGroupId,
						materialsIncludedInContractRevenueQuantity:
							item.categoryQuantity || 0,
						additionalCost,
						additionalCostQuantity: item.additionalCostQuantity || 0,
						quotaBasedMaterial,
						quotaBasedMaterialType,
						quotaBasedMaterialQuantity: item.contractLimitQuantity || 0,
						asset,
						assetMaterialQuantity: item.assetQuantity || 0,
					};
				}),
			};

			// Call API to update acceptance report
			await api.put(API.PRODUCTION.ACCEPTANCE_REPORT.UPDATE, requestData);

			success(
				`${breadcrumb} đã được ${id ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider {...form}>
			<form
				onSubmit={form.handleSubmit(handleSubmit)}
				className='flex h-full flex-col gap-6'
			>
				{loading ? (
					<div className='flex h-full items-center justify-center'>
						<Spinner />
					</div>
				) : (
					<div className='min-h-0 flex-1 overflow-x-auto overflow-y-auto'>
						<div className='rounded-lg border shadow-sm'>
							<Table className='w-full'>
								<TableHeader className='bg-linear-to-r from-slate-50 to-slate-100'>
									<TableRow className='bg-linear-to-r from-slate-50 to-slate-100'>
										<TableCell className='sticky left-0 z-20 w-[3%] min-w-12 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											STT
										</TableCell>
										<TableCell className='sticky left-12 z-20 w-[8%] min-w-28 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
											Mã vật tư
										</TableCell>
										<TableCell className='w-[10%] min-w-32 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
											Tên vật tư
										</TableCell>
										<TableCell className='w-[8%] min-w-24 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											DVT
										</TableCell>
										<TableCell className='w-[12%] min-w-32 border-b-2 border-slate-200 px-4 py-4 text-right text-sm font-semibold text-slate-700'>
											Đơn giá kế hoạch (đ)
										</TableCell>
										<TableCell className='w-[12%] min-w-32 border-b-2 border-slate-200 px-4 py-4 text-right text-sm font-semibold text-slate-700'>
											Đơn giá thực tế (đ)
										</TableCell>
										<TableCell className='w-[10%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Số lượng lĩnh
										</TableCell>
										<TableCell className='w-[10%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Số lượng xuất
										</TableCell>
										<TableCell className='w-[15%] min-w-40 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Vật tư tính vào doanh thu khoán
										</TableCell>
										<TableCell className='w-[15%] min-w-40 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Bổ sung chi phí
										</TableCell>
										<TableCell className='w-[15%] min-w-40 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Vật tư theo hạn mức
										</TableCell>
										<TableCell className='w-[12%] min-w-32 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Tài sản
										</TableCell>
									</TableRow>
								</TableHeader>
								<TableBody>
									<RawAcceptanceReportRows
										processGroupOptions={processGroupOptions}
									/>
								</TableBody>
							</Table>
						</div>
					</div>
				)}

				<DialogFooter className='bg-muted sticky bottom-0 z-20 mt-auto w-full px-10 py-4'>
					<Button
						type='button'
						variant='outline'
						className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
						onClick={() => {
							setOpen(false);
						}}
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
			</form>
		</FormProvider>
	);
}

function RawAcceptanceReportRows({
	processGroupOptions,
}: {
	processGroupOptions: ProcessGroupOption[];
}) {
	const form = useFormContext<{
		items: RawAcceptanceReportFormSchema['items'];
	}>();
	const { fields } = useFieldArray({
		control: form.control,
		name: 'items',
	});

	return (
		<>
			{fields.map((field, index) => (
				<RawAcceptanceReportRow
					key={field.id}
					index={index}
					processGroupOptions={processGroupOptions}
				/>
			))}
		</>
	);
}

function resetCellFields(
	form: ReturnType<
		typeof useFormContext<{ items: RawAcceptanceReportFormSchema['items'] }>
	>,
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

function RawAcceptanceReportRow({
	index,
	processGroupOptions,
}: {
	index: number;
	processGroupOptions: ProcessGroupOption[];
}) {
	const form = useFormContext<{
		items: RawAcceptanceReportFormSchema['items'];
	}>();
	const basename = `items.${index}` as const;

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
	const additionalCostCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.additionalCostCategory` as FieldName,
	});
	const contractLimitCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitCategory` as FieldName,
	});
	const contractLimitSubCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitSubCategory` as FieldName,
	});
	const exportedQuantityWatch = useWatch({
		control: form.control,
		name: `${basename}.exportedQuantity` as FieldName,
	});
	const materialTypeValue = useWatch({
		control: form.control,
		name: `${basename}.type` as FieldName,
	});

	const defaultCategoryByType =
		getDefaultCategoryByMaterialType(materialTypeValue);
	const defaultAdditionalCostByType =
		getDefaultAdditionalCostByMaterialType(materialTypeValue);

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

	// Determine if secondary combobox is needed for contract limit
	const needsSecondComboBox =
		contractLimitCategoryValue === QuotaBasedMaterial.MineSupport ||
		contractLimitCategoryValue === QuotaBasedMaterial.SupportAccessories;

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

			if (!additionalCostCategoryValue && defaultAdditionalCostByType != null) {
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
		additionalCostCategoryValue,
		defaultCategoryByType,
		defaultAdditionalCostByType,
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
			showAdditionalCostDropdown && additionalCostCategoryValue,
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

		const exportedQty = Number(exportedQuantityWatch) || 0;

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
			additionalCostCategory: additionalCostCategoryValue,
			contractLimitCategory: contractLimitCategoryValue,
			showCategoryDropdown: showCategoryDropdown,
			showAdditionalCostDropdown: showAdditionalCostDropdown,
			showAssetDropdown: showAssetDropdown,
		};
	}, [
		categoryValue,
		categoryProcessGroupValue,
		additionalCostCategoryValue,
		contractLimitCategoryValue,
		exportedQuantityWatch,
		form,
		basename,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
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

	// Reset contractLimitSubCategory when contractLimitCategory changes
	const prevContractLimitCategoryRef = useRef(contractLimitCategoryValue);
	useEffect(() => {
		if (prevContractLimitCategoryRef.current !== contractLimitCategoryValue) {
			form.setValue(`${basename}.contractLimitSubCategory` as FieldName, null);
			prevContractLimitCategoryRef.current = contractLimitCategoryValue;
		}
	}, [contractLimitCategoryValue, form, basename]);

	const materialCode = form.watch(`${basename}.materialCode` as FieldName);
	const materialName = form.watch(`${basename}.materialName` as FieldName);
	const unit = form.watch(`${basename}.unit` as FieldName);
	const plannedUnitPrice = form.watch(
		`${basename}.plannedUnitPrice` as FieldName,
	);
	const actualUnitPrice = form.watch(
		`${basename}.actualUnitPrice` as FieldName,
	);
	const receivedQuantity = form.watch(
		`${basename}.receivedQuantity` as FieldName,
	);
	const watchedExportedQuantity = form.watch(
		`${basename}.exportedQuantity` as FieldName,
	);
	const categoryQuantity = form.watch(
		`${basename}.categoryQuantity` as FieldName,
	);
	const additionalCostQuantity = form.watch(
		`${basename}.additionalCostQuantity` as FieldName,
	);
	const contractLimitQuantity = form.watch(
		`${basename}.contractLimitQuantity` as FieldName,
	);
	const assetQuantity = form.watch(`${basename}.assetQuantity` as FieldName);

	// Calculate total and validation status
	const calculateTotal = () => {
		let total = 0;
		const hasCategoryActive =
			showCategoryDropdown && categoryValue && categoryProcessGroupValue;
		const hasAdditionalCostActive =
			showAdditionalCostDropdown && additionalCostCategoryValue;
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
	const exportedQty = Number(watchedExportedQuantity) || 0;
	const hasActiveColumns =
		(showCategoryDropdown && categoryValue && categoryProcessGroupValue) ||
		(showAdditionalCostDropdown && additionalCostCategoryValue) ||
		(showContractLimitDropdown && contractLimitCategoryValue) ||
		showAssetDropdown;
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
			<TableCell className='sticky left-0 z-20 w-[3%] min-w-12 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
				{index + 1}
			</TableCell>
			<TableCell className='sticky left-12 z-20 w-[8%] min-w-28 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
				<div className='flex flex-col gap-1'>
					<Input
						readOnly
						value={materialCode || ''}
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
			<TableCell className='w-[10%] min-w-32 border-b border-slate-200 px-4 py-4'>
				<Input
					readOnly
					value={materialName || ''}
					className='border-slate-300 bg-slate-100 text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[8%] min-w-24 border-b border-slate-200 px-4 py-4 text-center'>
				<Input
					readOnly
					value={unit || ''}
					className='border-slate-300 bg-slate-100 text-center text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[12%] min-w-32 border-b border-slate-200 px-4 py-4 text-right'>
				<Input
					readOnly
					value={formatNumber(plannedUnitPrice || 0, {
						maximumFractionDigits: 0,
					})}
					className='border-slate-300 bg-slate-100 text-right text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[12%] min-w-32 border-b border-slate-200 px-4 py-4 text-right'>
				<Input
					readOnly
					value={formatNumber(actualUnitPrice || 0, {
						maximumFractionDigits: 0,
					})}
					className='border-slate-300 bg-slate-100 text-right text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[10%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
				<Input
					readOnly
					value={receivedQuantity || ''}
					className='border-slate-300 bg-slate-100 text-center text-slate-500'
				/>
			</TableCell>
			<TableCell className='w-[10%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
				<Input
					readOnly
					value={watchedExportedQuantity || ''}
					className='border-slate-300 bg-slate-100 text-center text-slate-500'
				/>
			</TableCell>

			{/* Vật tư tính vào doanh thu khoán */}
			<TableCell className='w-[15%] min-w-40 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showCategoryDropdown` as FieldName}
						/>
					</div>
					{showCategoryDropdown && (
						<>
							<div className='w-full'>
								<FormComboBox
									control={form.control}
									name={`${basename}.category` as FieldName}
									options={CATEGORY_OPTIONS}
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
									{hasActiveColumns && !isValidTotal && (
										<p className='mt-1 text-xs text-red-600'>
											Tổng: {totalQuantity} / {exportedQty}
										</p>
									)}
								</div>
							)}
						</>
					)}
				</div>
			</TableCell>

			{/* Bổ sung chi phí */}
			<TableCell className='w-[15%] min-w-40 border-b border-slate-200 px-4 py-4'>
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
									options={ADDITIONAL_COST_OPTIONS}
									placeholder='Chọn danh mục'
								/>
							</div>
							{additionalCostCategoryValue && (
								<div className='w-full'>
									<label className='mb-1.5 block text-xs font-medium text-slate-600'>
										Số lượng vật tư
									</label>
									<FormNumber
										control={form.control}
										name={`${basename}.additionalCostQuantity` as FieldName}
										placeholder='Nhập số lượng'
									/>
									{hasActiveColumns && !isValidTotal && (
										<p className='mt-1 text-xs text-red-600'>
											Tổng: {totalQuantity} / {exportedQty}
										</p>
									)}
								</div>
							)}
						</>
					)}
				</div>
			</TableCell>

			{/* Vật tư theo hạn mức */}
			<TableCell className='w-[15%] min-w-40 border-b border-slate-200 px-4 py-4'>
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
										placeholder='Chọn loại (Lĩnh mới/Tái sử dụng)'
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
										{hasActiveColumns && !isValidTotal && (
											<p className='mt-1 text-xs text-red-600'>
												Tổng: {totalQuantity} / {exportedQty}
											</p>
										)}
									</div>
								)}
						</>
					)}
				</div>
			</TableCell>

			{/* Tài sản */}
			<TableCell className='w-[12%] min-w-32 border-b border-slate-200 px-4 py-4'>
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
								Số lượng tài sản
							</label>
							<FormNumber
								control={form.control}
								name={`${basename}.assetQuantity` as FieldName}
								placeholder='Nhập số lượng'
							/>
							{hasActiveColumns && !isValidTotal && (
								<p className='mt-1 text-xs text-red-600'>
									Tổng: {totalQuantity} / {exportedQty}
								</p>
							)}
						</div>
					)}
				</div>
			</TableCell>
		</TableRow>
	);
}
