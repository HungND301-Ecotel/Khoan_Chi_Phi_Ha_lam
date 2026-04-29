import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
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
import type { Department } from '@/features/main/catalog/department/columns';
import type { Product } from '@/features/main/catalog/product/columns';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import type { Production } from './columns';
import {
	type ProductionFormMode,
	type ProductionFormSchema,
	getProductionFormDefault,
	PRODUCTION_GROUP_DEFAULT,
	productionFormSchema,
} from './production-form-schema';

type ProductionOutputDetailProduct = {
	productId: string;
	productionMeters: number;
	actualAshContent?: number;
};

type ProductionOutputDetailProcessGroup = {
	processGroupId: string;
	standardProductionMeters: number;
	products: ProductionOutputDetailProduct[];
};

type ProductionOutputDetail = {
	id: string;
	startMonth: string;
	endMonth?: string;
	departmentId?: string | null;
	acceptanceReportId?: string | null;
	productionMeters: number;
	standardProductionMeters: number;
	processGroups?: ProductionOutputDetailProcessGroup[];
};

type ProductionFormProps = ActionDialogProps<Production> & {
	onSuccess?: () => void;
};
type ProductionGroup = NonNullable<ProductionFormSchema['groups']>[number];
type ProductionGroupProduct = ProductionGroup['products'][number];

function createGroupDefault(): ProductionGroup {
	return {
		...PRODUCTION_GROUP_DEFAULT,
		productIds: [],
		products: [],
	};
}

function isSameStringArray(current: string[] = [], next: string[] = []) {
	if (current.length !== next.length) return false;
	return current.every((value, index) => value === next[index]);
}

function isSameProductionRows(
	current: ProductionGroupProduct[] = [],
	next: ProductionGroupProduct[] = [],
) {
	if (current.length !== next.length) return false;

	return current.every((row, index) => {
		const nextRow = next[index];
		if (!nextRow || row.productId !== nextRow.productId) return false;

		const sameMeters =
			row.productionMeters === nextRow.productionMeters ||
			(Number.isNaN(row.productionMeters) &&
				Number.isNaN(nextRow.productionMeters));

		return sameMeters;
	});
}

function calculateTotals(groups: ProductionGroup[] = []) {
	return {
		productionMeters: groups.reduce(
			(sum, group) =>
				sum +
				(group.products || []).reduce(
					(productSum, product) => productSum + (product.productionMeters || 0),
					0,
				),
			0,
		),
		standardProductionMeters: groups.reduce(
			(sum, group) => sum + (group.standardProductionMeters || 0),
			0,
		),
	};
}

function buildProcessGroupPayload(
	groups: ProductionGroup[] = [],
	akProcessGroupIds: Set<string> = new Set(),
) {
	return groups.map((group) => ({
		processGroupId: group.processGroupId,
		standardProductionMeters: group.standardProductionMeters,
		products: (group.products || []).map((product) => ({
			productId: product.productId,
			productionMeters: product.productionMeters,
			actualAshContent: akProcessGroupIds.has(group.processGroupId)
				? (product.actualAshContent ?? 0)
				: 0,
		})),
	}));
}

export function ProductionForm({ data, row, onSuccess }: ProductionFormProps) {
	const isEdit = !!row;
	const mode: ProductionFormMode = isEdit ? 'edit' : 'create';
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);
	const [products, setProducts] = useState<Product[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [akProcessGroupIds, setAkProcessGroupIds] = useState<Set<string>>(
		new Set(),
	);

	const form = useForm<ProductionFormSchema>({
		resolver: zodResolver(productionFormSchema),
		mode: 'onSubmit',
		defaultValues: getProductionFormDefault(mode),
	});

	const {
		fields: groupFields,
		append,
		remove,
	} = useFieldArray({
		control: form.control,
		name: 'groups',
	});

	const watchedGroups = useWatch({
		control: form.control,
		name: 'groups',
		defaultValue: [] as ProductionGroup[],
	}) as ProductionGroup[];

	useEffect(() => {
		form.reset(getProductionFormDefault(mode));
	}, [mode, form]);

	useEffect(() => {
		if (!isEdit || !row) return;

		api
			.get<ProductionOutputDetail>(
				API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(row.id),
			)
			.then((res) => {
				const {
					startMonth,
					departmentId,
					processGroups,
					productionMeters,
					standardProductionMeters,
				} = res.result;

				const mappedGroups: ProductionGroup[] = (processGroups || []).map(
					(group) => {
						const mappedProducts = (group.products || []).map((product) => ({
							productId: product.productId,
							productionMeters: product.productionMeters,
							actualAshContent: product.actualAshContent ?? 0,
						}));

						return {
							processGroupId: group.processGroupId,
							standardProductionMeters: group.standardProductionMeters,
							productIds: mappedProducts.map((product) => product.productId),
							products: mappedProducts,
						};
					},
				);

				form.reset({
					mode: 'edit',
					startMonth: startMonth.substring(0, 10),
					departmentId: departmentId ?? '',
					productionMeters,
					standardProductionMeters,
					groups:
						mappedGroups.length > 0 ? mappedGroups : [createGroupDefault()],
				});
			});
	}, [isEdit, row, form]);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Product>(API.CATALOG.PRODUCT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<{ processGroupId: string }>(
				API.CATALOG.AK_FACTOR_CONFIG.LIST,
				{
					ignorePagination: true,
				},
			),
		]);

		promises.then(
			([departmentRes, processGroupRes, productRes, akFactorConfigRes]) => {
				setDepartments(
					[...departmentRes.result.data].sort((a, b) =>
						a.code.localeCompare(b.code),
					),
				);
				setProcessGroups(
					[...processGroupRes.result.data].sort((a, b) =>
						a.code.localeCompare(b.code),
					),
				);
				setProducts(
					[...productRes.result.data].sort((a, b) =>
						a.code.localeCompare(b.code),
					),
				);
				setAkProcessGroupIds(
					new Set(
						(akFactorConfigRes.result.data || [])
							.map((item) => item.processGroupId)
							.filter((id) => !!id),
					),
				);
			},
		);
	}, []);

	useEffect(() => {
		if (products.length === 0) {
			return;
		}

		watchedGroups.forEach((group, groupIndex) => {
			const validProductIds = products
				.filter((product) => product.processGroupId === group.processGroupId)
				.map((product) => product.id);

			const nextProductIds = (group.productIds || []).filter(
				(productId: string) => validProductIds.includes(productId),
			);

			const currentRows = group.products || [];
			const nextRows = nextProductIds.map((productId: string) => {
				const existing = currentRows.find(
					(row: ProductionGroupProduct) => row.productId === productId,
				);
				return {
					productId,
					productionMeters: existing?.productionMeters ?? 0,
					actualAshContent: existing?.actualAshContent ?? 0,
				};
			});

			if (!isSameStringArray(group.productIds || [], nextProductIds)) {
				form.setValue(`groups.${groupIndex}.productIds`, nextProductIds, {
					shouldValidate: true,
					shouldDirty: true,
				});
			}

			if (!isSameProductionRows(currentRows, nextRows)) {
				form.setValue(`groups.${groupIndex}.products`, nextRows, {
					shouldValidate: true,
					shouldDirty: true,
				});
			}
		});
	}, [watchedGroups, products, form]);

	const handleProductsChange = (
		groupIndex: number,
		values: MultiSelectOption[],
	) => {
		const selectedProductIds = values.map((value) => value.value);
		const currentRows = form.getValues(`groups.${groupIndex}.products`) || [];
		const nextRows = selectedProductIds.map((productId: string) => {
			const existing = currentRows.find(
				(row: ProductionGroupProduct) => row.productId === productId,
			);
			return {
				productId,
				productionMeters: existing?.productionMeters ?? 0,
				actualAshContent: existing?.actualAshContent ?? 0,
			};
		});

		form.setValue(`groups.${groupIndex}.productIds`, selectedProductIds, {
			shouldValidate: true,
			shouldDirty: true,
		});

		form.setValue(`groups.${groupIndex}.products`, nextRows, {
			shouldValidate: true,
			shouldDirty: true,
		});
	};

	const handleSubmit = async (values: ProductionFormSchema) => {
		try {
			const groups = values.groups || [];
			const { productionMeters, standardProductionMeters } =
				calculateTotals(groups);
			const processGroups = buildProcessGroupPayload(groups, akProcessGroupIds);

			if (isEdit && row) {
				await api.put(API.PRODUCTION.PRODUCTION_OUTPUT.UPDATE, {
					id: row.id,
					startMonth: values.startMonth,
					endMonth: values.startMonth,
					departmentId: values.departmentId,
					acceptanceReportId: row.acceptanceReportId || null,
					productionMeters,
					standardProductionMeters,
					processGroups,
				});
			} else {
				await api.post(API.PRODUCTION.PRODUCTION_OUTPUT.CREATE, {
					startMonth: values.startMonth,
					endMonth: values.startMonth,
					departmentId: values.departmentId,
					productionMeters,
					standardProductionMeters,
					processGroups,
				});
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${isEdit ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
			onSuccess?.();
		} catch (error) {
			popup.error(error);
		}
	};

	const totals = calculateTotals(watchedGroups);

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormRow>
				<FormComboBox
					control={form.control}
					name='departmentId'
					label='Đơn vị'
					placeholder='Chọn đơn vị'
					options={departments.map((department) => ({
						label: `${department.code} - ${department.name}`,
						value: department.id,
					}))}
				/>
			</FormRow>
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Tổng sản lượng thực tế</Label>
					<Input readOnly value={formatNumber(totals.productionMeters)} />
				</div>
				<div className='flex-1 space-y-2'>
					<Label>Tổng sản lượng định mức</Label>
					<Input
						readOnly
						value={formatNumber(totals.standardProductionMeters)}
					/>
				</div>
			</FormRow>

			<div className='flex flex-col gap-4'>
				{groupFields.map((field, groupIndex) => {
					const group = watchedGroups[groupIndex] || createGroupDefault();
					const isAkApplicableForGroup =
						!!group.processGroupId &&
						akProcessGroupIds.has(group.processGroupId);
					const availableProducts = products.filter(
						(product) => product.processGroupId === group.processGroupId,
					);

					const productOptions = availableProducts.map((product) => ({
						value: product.id,
						label: `${product.code}`,
					}));

					const selectedProducts = productOptions.filter((option) =>
						(group.productIds || []).includes(option.value),
					);

					const groupProducts = group.products || [];
					const totalProductionMeters = groupProducts.reduce(
						(sum: number, product: ProductionGroupProduct) => {
							if (Number.isNaN(product.productionMeters)) return sum;
							return sum + (product.productionMeters || 0);
						},
						0,
					);

					const productErrors =
						form.formState.errors.groups?.[groupIndex]?.productIds?.message ||
						form.formState.errors.groups?.[groupIndex]?.products?.message;

					return (
						<div
							key={field.id}
							className='flex flex-col gap-4 rounded-sm border border-[#999999] p-4'
						>
							<div className='flex items-center justify-end'>
								<Button
									type='button'
									variant='ghost'
									size='sm'
									className='text-error hover:text-error-muted bg-transparent'
									onClick={() => remove(groupIndex)}
									disabled={groupFields.length === 1}
								>
									<XCircleIcon className='size-4' />
									<span>Xóa</span>
								</Button>
							</div>

							<FormRow>
								<FormComboBox
									control={form.control}
									name={`groups.${groupIndex}.processGroupId`}
									label='Nhóm công đoạn sản xuất'
									placeholder='Chọn nhóm công đoạn sản xuất'
									options={processGroups.map((processGroup) => ({
										label: `${processGroup.code}`,
										value: processGroup.id,
									}))}
								/>

								<div className='flex-1 space-y-2'>
									<Label>Sản lượng thực tế</Label>
									<Input readOnly value={formatNumber(totalProductionMeters)} />
								</div>

								<div className='flex-1'>
									<FormNumber
										control={form.control}
										name={`groups.${groupIndex}.standardProductionMeters`}
										label='Sản lượng định mức (Qđm)'
										placeholder='Nhập sản lượng định mức'
									/>
								</div>
							</FormRow>

							<MultiSelect
								label='Danh sách sản phẩm'
								placeholder='Chọn sản phẩm'
								values={selectedProducts}
								onValuesChange={(values) =>
									handleProductsChange(groupIndex, values)
								}
								options={productOptions}
							/>

							{typeof productErrors === 'string' && (
								<p className='text-destructive text-sm'>{productErrors}</p>
							)}

							{groupProducts.length > 0 && (
								<div className='flex flex-col gap-3'>
									{groupProducts.map(
										(product: ProductionGroupProduct, productIndex: number) => {
											const selectedProduct = products.find(
												(item) => item.id === product.productId,
											);

											return (
												<FormRow key={`${product.productId}-${groupIndex}`}>
													<div className='flex-1 space-y-2'>
														<Label>Mã sản phẩm</Label>
														<Input
															readOnly
															value={selectedProduct?.code || product.productId}
															placeholder='Chọn sản phẩm'
														/>
													</div>

													<div className='flex-1'>
														<FormNumber
															control={form.control}
															name={`groups.${groupIndex}.products.${productIndex}.productionMeters`}
															label='Sản lượng thực tế'
															placeholder='Nhập sản lượng thực tế'
														/>
													</div>

													{isAkApplicableForGroup && (
														<div className='flex-1'>
															<FormNumber
																control={form.control}
																name={`groups.${groupIndex}.products.${productIndex}.actualAshContent`}
																label='Ak thực hiện (%)'
																placeholder='Nhập Ak thực hiện'
															/>
														</div>
													)}
												</FormRow>
											);
										},
									)}
								</div>
							)}
						</div>
					);
				})}

				<Button
					type='button'
					variant='ghost'
					size='sm'
					className='h-fit w-fit bg-transparent px-0'
					onClick={() => append(createGroupDefault())}
				>
					<PlusCircleIcon className='text-primary size-4' />
					<span>Thêm nhóm công đoạn sản xuất</span>
				</Button>
			</div>

			<FormSeparator />

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
