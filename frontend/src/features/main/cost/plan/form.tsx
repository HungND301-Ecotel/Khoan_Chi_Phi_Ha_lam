import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Department } from '@/features/main/catalog/department/columns';
import type { Product } from '@/features/main/catalog/product/columns';
import type { Unit } from '@/features/main/catalog/unit/columns';
import type { DepartmentPlanGroup } from '@/features/main/cost/plan/columns';
import {
	DEPARTMENT_PLAN_FORM_DEFAULT,
	type DepartmentPlanFormSchema,
	departmentPlanFormSchema,
} from '@/features/main/cost/plan/schema';
import {
	type DepartmentPlannedDetail,
	mapDepartmentPlannedDetail,
} from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import {
	type UseFormReturn,
	useFieldArray,
	useForm,
	useWatch,
} from 'react-hook-form';

type PlanFormProps = ActionDialogProps<DepartmentPlanGroup> & {
	onSuccess?: () => void;
};

type MonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	canRemove: boolean;
	onRemoveMonth: () => void;
	products: Product[];
	units: Unit[];
	akProcessGroupIds: Set<string>;
	onSyncProductUnit: (
		productId: string,
		unitOfMeasureId: string,
		origin: { monthIndex: number; itemIndex: number },
	) => void;
};

function MonthSection({
	form,
	monthIndex,
	canRemove,
	onRemoveMonth,
	products,
	units,
	akProcessGroupIds,
	onSyncProductUnit,
}: MonthSectionProps) {
	const monthPath = `months.${monthIndex}` as const;
	const watchedMonth = useWatch({
		control: form.control,
		name: monthPath,
	}) as DepartmentPlanFormSchema['months'][number];
	const {
		fields: itemFields,
		append,
		remove,
	} = useFieldArray({
		control: form.control,
		name: `months.${monthIndex}.items`,
	});

	const getProduct = (productId?: string) =>
		products.find((product) => product.id === productId);

	const handleProductChange = (itemIndex: number, productId: string) => {
		form.setValue(
			`months.${monthIndex}.items.${itemIndex}.productId`,
			productId,
			{
				shouldDirty: true,
				shouldValidate: true,
			},
		);

		const syncedUnit = form
			.getValues('months')
			.flatMap((month) => month.items)
			.find(
				(item) => item.productId === productId && item.unitOfMeasureId,
			)?.unitOfMeasureId;

		if (syncedUnit) {
			form.setValue(
				`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
				syncedUnit,
				{
					shouldDirty: true,
					shouldValidate: true,
				},
			);
		}
	};

	const handleUnitChange = (itemIndex: number, unitOfMeasureId: string) => {
		const productId = form.getValues(
			`months.${monthIndex}.items.${itemIndex}.productId`,
		);
		form.setValue(
			`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
			unitOfMeasureId,
			{
				shouldDirty: true,
				shouldValidate: true,
			},
		);

		if (productId) {
			onSyncProductUnit(productId, unitOfMeasureId, { monthIndex, itemIndex });
		}
	};

	return (
		<div className='flex flex-col gap-4 rounded-sm border border-[#999999] p-4'>
			<div className='flex items-center justify-between gap-4'>
				<FormMonthYear
					control={form.control}
					name={`months.${monthIndex}.month`}
					label='Thời gian'
					className='flex-1'
				/>
				<Button
					type='button'
					variant='ghost'
					size='sm'
					className='text-error hover:text-error-muted mt-7 bg-transparent'
					onClick={onRemoveMonth}
					disabled={!canRemove}
				>
					<XCircleIcon className='size-4' />
					<span>Xóa tháng</span>
				</Button>
			</div>

			{typeof form.formState.errors.months?.[monthIndex]?.month?.message ===
				'string' && (
				<FieldError
					errors={[form.formState.errors.months?.[monthIndex]?.month]}
				/>
			)}

			<div className='flex flex-col gap-4'>
				{itemFields.map((field, itemIndex) => {
					const currentItem = watchedMonth?.items?.[itemIndex];
					const product = getProduct(currentItem?.productId);
					const isAkApplicable =
						!!product?.processGroupId &&
						akProcessGroupIds.has(product.processGroupId);

					return (
						<div
							key={field.id}
							className='flex flex-col gap-3 rounded-sm border border-dashed border-[#BDBDBD] p-3'
						>
							<div className='flex gap-4 [&>div>label]:flex [&>div>label]:min-h-10 [&>div>label]:items-end [&>div>label]:leading-5'>
								<FormComboBox
									label='Mã sản phẩm'
									placeholder='Chọn mã sản phẩm'
									value={currentItem?.productId || ''}
									onValueChange={(value) =>
										handleProductChange(itemIndex, value)
									}
									options={products.map((productOption) => ({
										label: `${productOption.code} - ${productOption.name}`,
										value: productOption.id,
									}))}
								/>
								<FormComboBox
									label='Đơn vị tính'
									placeholder='Chọn đơn vị tính'
									value={currentItem?.unitOfMeasureId || ''}
									onValueChange={(value) => handleUnitChange(itemIndex, value)}
									options={units.map((unit) => ({
										label: unit.name,
										value: unit.id,
									}))}
								/>
								<div className='flex flex-1 flex-col gap-2'>
									<Label>Sản lượng kế hoạch ban đầu</Label>
									<FormNumberInput
										value={currentItem?.productionMeters}
										onValueChange={(value) =>
											form.setValue(
												`months.${monthIndex}.items.${itemIndex}.productionMeters`,
												value ?? Number.NaN,
												{
													shouldDirty: true,
													shouldValidate: true,
												},
											)
										}
										placeholder='Nhập sản lượng kế hoạch ban đầu'
									/>
								</div>
								{isAkApplicable && (
									<div className='flex flex-1 flex-col gap-2'>
										<Label>Ak kế hoạch (%)</Label>
										<FormNumberInput
											value={currentItem?.planAshContent}
											onValueChange={(value) =>
												form.setValue(
													`months.${monthIndex}.items.${itemIndex}.planAshContent`,
													value ?? 0,
													{
														shouldDirty: true,
														shouldValidate: true,
													},
												)
											}
											placeholder='Nhập Ak kế hoạch'
										/>
									</div>
								)}
								<Button
									type='button'
									variant='ghost'
									size='icon'
									className='text-error hover:text-error-muted mt-7 bg-transparent'
									onClick={() => remove(itemIndex)}
									disabled={itemFields.length === 1}
								>
									<XCircleIcon className='size-6' />
								</Button>
							</div>

							<div className='grid grid-cols-1 gap-2 md:grid-cols-3'>
								{typeof form.formState.errors.months?.[monthIndex]?.items?.[
									itemIndex
								]?.productId?.message === 'string' && (
									<FieldError
										errors={[
											form.formState.errors.months?.[monthIndex]?.items?.[
												itemIndex
											]?.productId,
										]}
									/>
								)}
								{typeof form.formState.errors.months?.[monthIndex]?.items?.[
									itemIndex
								]?.unitOfMeasureId?.message === 'string' && (
									<FieldError
										errors={[
											form.formState.errors.months?.[monthIndex]?.items?.[
												itemIndex
											]?.unitOfMeasureId,
										]}
									/>
								)}
								{typeof form.formState.errors.months?.[monthIndex]?.items?.[
									itemIndex
								]?.productionMeters?.message === 'string' && (
									<FieldError
										errors={[
											form.formState.errors.months?.[monthIndex]?.items?.[
												itemIndex
											]?.productionMeters,
										]}
									/>
								)}
							</div>
						</div>
					);
				})}
			</div>

			{typeof form.formState.errors.months?.[monthIndex]?.items?.message ===
				'string' && (
				<FieldError
					errors={[form.formState.errors.months?.[monthIndex]?.items]}
				/>
			)}

			<Button
				type='button'
				variant='ghost'
				size='sm'
				className='h-fit w-fit bg-transparent'
				onClick={() =>
					append({
						productId: '',
						unitOfMeasureId: '',
						productionMeters: Number.NaN,
						planAshContent: 0,
					})
				}
			>
				<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
				<span>Thêm sản phẩm</span>
			</Button>
		</div>
	);
}

export function PlanForm({ data, row, onSuccess }: PlanFormProps) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [products, setProducts] = useState<Product[]>([]);
	const [units, setUnits] = useState<Unit[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [akProcessGroupIds, setAkProcessGroupIds] = useState<Set<string>>(
		new Set(),
	);
	const isEdit = !!row;

	const form = useForm<DepartmentPlanFormSchema>({
		resolver: zodResolver(departmentPlanFormSchema),
		mode: 'onSubmit',
		defaultValues: DEPARTMENT_PLAN_FORM_DEFAULT,
	});

	const {
		fields: monthFields,
		append,
		remove,
	} = useFieldArray({
		control: form.control,
		name: 'months',
	});

	const watchedMonths = useWatch({
		control: form.control,
		name: 'months',
		defaultValue: DEPARTMENT_PLAN_FORM_DEFAULT.months,
	}) as DepartmentPlanFormSchema['months'];

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Product>(API.CATALOG.PRODUCT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Unit>(API.CATALOG.UNIT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
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
			async ([productsRes, unitsRes, departmentsRes, akConfigs]) => {
				setProducts(productsRes.result.data ?? []);
				setUnits(unitsRes.result.data ?? []);
				setDepartments(departmentsRes.result.data ?? []);
				setAkProcessGroupIds(
					new Set(
						(akConfigs.result.data || [])
							.map((item) => item.processGroupId)
							.filter((id) => !!id),
					),
				);

				if (!row) return;

				const detail = await api.get<DepartmentPlannedDetail>(
					API.COST.PRODUCT.DETAIL_PLANNED_BY_DEPARTMENT(row.id),
				);
				const mappedDetail = mapDepartmentPlannedDetail(detail.result);

				form.reset({
					departmentId: mappedDetail.departmentId,
					months: mappedDetail.months.map((month) => ({
						month: month.month.substring(0, 10),
						items: month.items.map((item) => ({
							productUnitPriceId: item.productUnitPriceId,
							outputId: item.outputId,
							productId: item.productId,
							unitOfMeasureId: item.unitOfMeasureId,
							productionMeters: item.productionMeters,
							planAshContent: item.planAshContent ?? 0,
						})),
					})),
				});
			},
		);
	}, [form, row]);

	useEffect(() => {
		if (!row) {
			form.reset(DEPARTMENT_PLAN_FORM_DEFAULT);
		}
	}, [form, row]);

	const productMap = useMemo(
		() => new Map(products.map((product) => [product.id, product])),
		[products],
	);

	const syncProductUnit = (
		productId: string,
		unitOfMeasureId: string,
		origin?: { monthIndex: number; itemIndex: number },
	) => {
		form.getValues('months').forEach((month, monthIndex) => {
			month.items.forEach((item, itemIndex) => {
				if (item.productId !== productId) return;
				if (
					origin &&
					origin.monthIndex === monthIndex &&
					origin.itemIndex === itemIndex
				) {
					return;
				}
				if (item.unitOfMeasureId === unitOfMeasureId) return;

				form.setValue(
					`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
					unitOfMeasureId,
					{
						shouldDirty: true,
						shouldValidate: true,
					},
				);
			});
		});
	};

	useEffect(() => {
		const firstUnitByProduct = new Map<string, string>();

		watchedMonths.forEach((month, monthIndex) => {
			month.items.forEach((item, itemIndex) => {
				if (!item.productId || !item.unitOfMeasureId) return;
				const existing = firstUnitByProduct.get(item.productId);
				if (!existing) {
					firstUnitByProduct.set(item.productId, item.unitOfMeasureId);
					return;
				}
				if (existing !== item.unitOfMeasureId) {
					form.setValue(
						`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
						existing,
						{
							shouldDirty: true,
							shouldValidate: true,
						},
					);
				}
			});
		});
	}, [form, watchedMonths]);

	const handleSubmit = async (values: DepartmentPlanFormSchema) => {
		try {
			const payload = {
				departmentId: values.departmentId,
				months: values.months.map((month) => ({
					month: month.month,
					items: month.items.map((item) => {
						const product = productMap.get(item.productId);
						const isAkApplicable =
							!!product?.processGroupId &&
							akProcessGroupIds.has(product.processGroupId);

						return {
							productUnitPriceId: item.productUnitPriceId,
							outputId: item.outputId,
							productId: item.productId,
							unitOfMeasureId: item.unitOfMeasureId,
							productionMeters: item.productionMeters,
							planAshContent: isAkApplicable ? (item.planAshContent ?? 0) : 0,
						};
					}),
				})),
			};

			if (isEdit) {
				await api.put(API.COST.PRODUCT.UPDATE_PLANNED_BY_DEPARTMENT, payload);
			} else {
				await api.post(API.COST.PRODUCT.CREATE_PLANNED_BY_DEPARTMENT, payload);
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
					disabled={isEdit}
				/>
			</FormRow>

			<FormSeparator />

			<div className='flex flex-col gap-4'>
				{monthFields.map((field, monthIndex) => (
					<MonthSection
						key={field.id}
						form={form}
						monthIndex={monthIndex}
						canRemove={monthFields.length > 1}
						onRemoveMonth={() => remove(monthIndex)}
						products={products}
						units={units}
						akProcessGroupIds={akProcessGroupIds}
						onSyncProductUnit={syncProductUnit}
					/>
				))}
			</div>

			{typeof form.formState.errors.months?.message === 'string' && (
				<FieldError errors={[form.formState.errors.months]} />
			)}

			<Button
				type='button'
				variant='ghost'
				size='sm'
				className='h-fit w-fit bg-transparent'
				onClick={() =>
					append({
						month: '',
						items: [
							{
								productId: '',
								unitOfMeasureId: '',
								productionMeters: Number.NaN,
								planAshContent: 0,
							},
						],
					})
				}
			>
				<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
				<span>Thêm thời gian</span>
			</Button>

			<FormSeparator />

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
