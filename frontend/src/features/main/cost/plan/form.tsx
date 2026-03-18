import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Product } from '@/features/main/catalog/product/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import {
	PLAN_FORM_DEFAULT,
	planFormSchema,
	PlanFormSchema,
} from '@/features/main/cost/plan/schema';
import {
	CostProduct,
	CostProductDetail,
} from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function PlanForm({ data, row }: ActionDialogProps<CostProduct>) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [products, setProducts] = useState<Product[]>([]);
	const [units, setUnits] = useState<Unit[]>([]);

	const form = useForm<PlanFormSchema>({
		resolver: zodResolver(planFormSchema),
		mode: 'onSubmit',
		defaultValues: PLAN_FORM_DEFAULT,
	});

	// eslint-disable-next-line react-hooks/incompatible-library
	const watchedProductId = form.watch('productId');
	const selectedProduct = products.find(
		(product) => product.id === watchedProductId,
	);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Product>(API.CATALOG.PRODUCT.LIST),
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
		]);

		promises.then(([products, units]) => {
			setProducts(products.result.data);
			setUnits(units.result.data);

			if (!row) return;

			api
				.get<CostProductDetail>(API.COST.PRODUCT.DETAIL_PLANNED(row.id))
				.then((res) => {
					const { productId, unitOfMeasureId, outputs } = res.result;
					form.reset({
						productId,
						unitOfMeasureId,
						outputs: outputs.map(
							({ startMonth, endMonth, outputType, productionMeters, id }) => ({
								startMonth: startMonth.substring(0, 10),
								endMonth: endMonth.substring(0, 10),
								outputType,
								productionMeters,
								id,
							}),
						),
					});
				});
		});
	}, [row, form]);

	const handleSubmit = async ({ outputs, ...values }: PlanFormSchema) => {
		try {
			const formattedOutputs = outputs.map((output) => ({
				...output,
				endMonth: output.startMonth, // Gán giá trị ở đây
			}));

			if (row) {
				await api.put(API.COST.PRODUCT.UPDATE, {
					id: row.id,
					...values,
					type: 1,
					outputs: formattedOutputs,
				});
			} else {
				await api.post(API.COST.PRODUCT.CREATE, {
					outputs: formattedOutputs,
					...values,
				});
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row ? 'cập nhật' : 'tạo mới'} thành công.`,
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
				<FormComboBox
					control={form.control}
					name='productId'
					label='Mã sản phẩm'
					placeholder='Chọn mã sản phẩm'
					options={products.map((product) => ({
						label: product.code,
						value: product.id,
					}))}
				/>

				<FormComboBox
					control={form.control}
					name='unitOfMeasureId'
					label='Đơn vị tính'
					placeholder='Chọn đơn vị tính'
					options={units.map((unit) => ({
						label: unit.name,
						value: unit.id,
					}))}
				/>
			</FormRow>

			<div className='space-y-2'>
				<Label>Tên sản phẩm</Label>
				<Textarea
					readOnly
					value={selectedProduct?.name}
					placeholder='Chọn mã sản phẩm'
				/>
			</div>

			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Mã nhóm công đoạn sản xuất</Label>
					<Input
						readOnly
						value={selectedProduct?.processGroupCode}
						placeholder='Chọn mã sản phẩm'
					/>
				</div>
				<div className='flex-1 space-y-2'>
					<Label>Tên nhóm công đoạn sản xuất</Label>
					<Input
						readOnly
						value={selectedProduct?.processGroupName}
						placeholder='Chọn mã sản phẩm'
					/>
				</div>
			</FormRow>

			<FormSeparator />

			<FormArray
				control={form.control}
				name='outputs'
				label='Sản lượng'
				defaultValue={PLAN_FORM_DEFAULT.outputs[0]}
			>
				{(index) => {
					return (
						<div className='flex w-full gap-4'>
							<FormMonthYear
								control={form.control}
								name={`outputs.${index}.startMonth`}
								label='Thời gian'
								className='flex-1'
							/>
							<div className='flex-1'>
								<FormNumber
									control={form.control}
									name={`outputs.${index}.productionMeters`}
									label='Sản lượng kế hoạch ban đầu'
									placeholder='Nhập sản lượng kế hoạch ban đầu'
								/>
							</div>
						</div>
					);
				}}
			</FormArray>

			<FormSeparator />

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
