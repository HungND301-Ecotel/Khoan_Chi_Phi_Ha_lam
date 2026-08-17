import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { Product } from '@/features/main/catalog/product/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import { Label } from '@/components/ui/label';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { UseFormReturn, useFieldArray, useWatch } from 'react-hook-form';

export type MonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	canRemove: boolean;
	onRemoveMonth: () => void;
	products: Product[];
	units: Unit[];
	akProcessGroupIds: Set<string>;
	shouldPreserveInvalidSelection: (
		item: DepartmentPlanFormSchema['months'][number]['items'][number],
		month?: string,
	) => boolean;
	onSyncProductUnit: (
		productId: string,
		unitOfMeasureId: string,
		origin: { monthIndex: number; itemIndex: number },
	) => void;
};

export const isProductAvailableForMonth = (product: Product, month?: string) => {
	if (!month) return true;
	return product.startMonth <= month && month <= product.endMonth;
};

export const formatMonthLabel = (month?: string) => {
	if (!month) return 'chưa chọn tháng';
	const [year, monthValue] = month.split('-');
	if (!year || !monthValue) return month;
	return `Tháng ${monthValue}/${year}`;
};

export const getInvalidSelectedProduct = (
	products: Product[],
	productId?: string,
	month?: string,
) => {
	if (!productId || !month) return null;
	const product = products.find((item) => item.id === productId);
	if (!product) return null;
	return isProductAvailableForMonth(product, month) ? null : product;
};

export function MonthSection({
	form,
	monthIndex,
	canRemove,
	onRemoveMonth,
	products,
	units,
	akProcessGroupIds,
	shouldPreserveInvalidSelection,
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

	const availableProducts = products.filter((product) =>
		isProductAvailableForMonth(product, watchedMonth?.month),
	);

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
					const invalidSelectedProduct =
						currentItem &&
						shouldPreserveInvalidSelection(currentItem, watchedMonth?.month)
						? getInvalidSelectedProduct(
								products,
								currentItem?.productId,
								watchedMonth?.month,
							)
						: null;
					const selectableProducts = invalidSelectedProduct
						? [...availableProducts, invalidSelectedProduct].filter(
								(productOption: Product, index: number, allOptions: Product[]) =>
									allOptions.findIndex(
										(candidate: Product) => candidate.id === productOption.id,
									) === index,
							)
						: availableProducts;
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
									onValueChange={(value: string) =>
										handleProductChange(itemIndex, value)
									}
									options={selectableProducts.map((productOption: Product) => ({
										label: `${productOption.code} - ${productOption.name}`,
										value: productOption.id,
									}))}
								/>
								<FormComboBox
									label='Đơn vị tính'
									placeholder='Chọn đơn vị tính'
									value={currentItem?.unitOfMeasureId || ''}
									onValueChange={(value: string) => handleUnitChange(itemIndex, value)}
									options={units.map((unit: Unit) => ({
										label: unit.name,
										value: unit.id,
									}))}
								/>
								<div className='flex flex-1 flex-col gap-2'>
									<Label>Sản lượng kế hoạch ban đầu</Label>
									<FormNumberInput
										value={currentItem?.productionMeters}
										onValueChange={(value: number | undefined) =>
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
											onValueChange={(value: number | undefined) =>
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
