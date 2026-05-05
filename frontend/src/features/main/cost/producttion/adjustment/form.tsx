import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
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
import { Department } from '@/features/main/catalog/department/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { CostProduct } from '@/features/main/cost/plan/types';

import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Production } from '../production/columns';
import {
	ACTUAL_FORM_DEFAULT,
	ActualFormSchema,
	actualFormSchema,
} from './schema';
import {
	AdjustmentCostProductDetail,
	mapAdjustmentCostProductDetail,
} from './type';
import { formatDate } from '@/lib/utils';
import { FormArray } from '@/components/form/form-array';
import { FormNumber } from '@/components/form/form-number';
import { FormMonthYear } from '@/components/form/form-month-year';

export function AdjustmentForm({ data, row }: ActionDialogProps<CostProduct>) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [products, setProducts] = useState<Product[]>([]);
	const [units, setUnits] = useState<Unit[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [productionOutputs, setProductionOutputs] = useState<Production[]>([]);

	const form = useForm<ActualFormSchema>({
		resolver: zodResolver(actualFormSchema),
		mode: 'onSubmit',
		defaultValues: ACTUAL_FORM_DEFAULT,
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
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST),
			api.pagging<Production>(API.PRODUCTION.PRODUCTION_OUTPUT.LIST),
		]);

		promises.then(([products, units, departments, productionOutputs]) => {
			setProducts(products.result.data);
			setUnits(units.result.data);
			setDepartments(departments.result.data);
			setProductionOutputs(productionOutputs.result.data);

			if (!row) return;

			api
				.get<AdjustmentCostProductDetail>(
					API.COST.PRODUCT.DETAIL_ADJUSTMENT(row.id),
				)
				.then((res) => {
					const detail = mapAdjustmentCostProductDetail(res.result);
					const {
						productId,
						unitOfMeasureId,
						departmentId,
						outputs,
						productionOutputs,
					} = detail;
					form.reset({
						productId,
						unitOfMeasureId,
						departmentId,
						productionOutputs:
							productionOutputs?.map((p) => p.productionOutputId) || [],
						outputs: outputs || [],
					});
				});
		});
	}, [row, form]);

	const handleSubmit = async ({ ...values }: ActualFormSchema) => {
		try {
			if (row) {
				await api.put(API.COST.PRODUCT.UPDATE_ADJUSTMENT, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.COST.PRODUCT.CREATE, { ...values });
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
					disabled
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
					disabled
				/>
				<FormComboBox
					control={form.control}
					name='departmentId'
					label='Đơn vị'
					placeholder='Chọn đơn vị'
					options={departments.map((department) => ({
						label: `${department.code} - ${department.name}`,
						value: department.id,
					}))}
					disabled
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
				defaultValue={ACTUAL_FORM_DEFAULT.outputs[0]}
				hasAddButton={!row}
				hasCloseButton={!row}
			>
				{(index) => {
					return (
						<div className='flex w-full gap-4'>
							<FormMonthYear
								control={form.control}
								name={`outputs.${index}.startMonth`}
								label='Thời gian bắt đầu'
								className='flex-1'
								disabled
							/>
							<FormMonthYear
								control={form.control}
								name={`outputs.${index}.endMonth`}
								label='Thời gian kết thúc'
								className='flex-1'
								disabled
							/>
							<div className='flex-1'>
								<FormNumber
									control={form.control}
									name={`outputs.${index}.productionMeters`}
									label='Sản lượng kế hoạch ban đầu'
									placeholder='Nhập sản lượng kế hoạch ban đầu'
									disabled
								/>
							</div>
						</div>
					);
				}}
			</FormArray>

			<FormSeparator />

			<FormArray
				control={form.control}
				name='productionOutputs'
				label='Chi phí'
				defaultValue=''
			>
				{(index) => {
					const watchedOutputId = form.watch(`productionOutputs.${index}`);
					const selectedOutput = productionOutputs.find(
						(output) => output.id === watchedOutputId,
					);

					return (
						<FormRow>
							<FormComboBox
								control={form.control}
								name={`productionOutputs.${index}`}
								label='Chi phí'
								placeholder='Chọn chi phí'
								options={productionOutputs.map((output) => ({
									label: `${formatDate(output.startMonth)} - ${formatDate(output.endMonth)}`,
									value: output.id,
								}))}
							/>

							<div className='flex-1 space-y-2'>
								<Label>Sản lượng thực tế</Label>
								<Input
									readOnly
									value={selectedOutput?.productionMeters || ''}
									placeholder='Chọn sản lượng thực tế'
								/>
							</div>

							<div className='flex-1 space-y-2'>
								<Label>Sản lượng định mức (Qđm)</Label>
								<Input
									readOnly
									value={selectedOutput?.standardProductionMeters || ''}
									placeholder='Chọn sản lượng định mức'
								/>
							</div>
						</FormRow>
					);
				}}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
