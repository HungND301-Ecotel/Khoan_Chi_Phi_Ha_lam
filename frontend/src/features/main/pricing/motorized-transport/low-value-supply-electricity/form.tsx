import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { Button } from '@/components/ui/button';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { MotorizedLowValueSupplyElectricityUnitPrice } from './columns';
import {
	DEFAULT_PERIOD_ITEM,
	MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT,
	motorizedLowValueSupplyElectricityFormSchema,
	MotorizedLowValueSupplyElectricityFormSchema,
} from './schema';

type MotorizedLowValueSupplyElectricityFormProps =
	ActionDialogProps<MotorizedLowValueSupplyElectricityUnitPrice> & {
		isDuplicate?: boolean;
	};

export function MotorizedLowValueSupplyElectricityForm({
	data,
	row,
	isDuplicate = false,
}: MotorizedLowValueSupplyElectricityFormProps) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();
	const [departments, setDepartments] = useState<any[]>([]);
	const [processGroups, setProcessGroups] = useState<any[]>([]);

	const form = useForm<MotorizedLowValueSupplyElectricityFormSchema>({
		resolver: zodResolver(motorizedLowValueSupplyElectricityFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT,
	});

	const { fields, append, remove, insert } = useFieldArray({
		control: form.control,
		name: 'periods',
	});

	const handleCopyPeriod = (index: number) => {
		const currentValues = form.getValues(`periods.${index}`);
		insert(index + 1, {
			startMonth: currentValues.startMonth || '',
			endMonth: currentValues.endMonth || '',
			lowValueSupplyUnitPrice: currentValues.lowValueSupplyUnitPrice || 0,
			electricityUnitPrice: currentValues.electricityUnitPrice || 0,
		});
	};

	useEffect(() => {
		Promise.all([
			api.pagging<any>(API.CATALOG.DEPARTMENT.LIST, { ignorePagination: true }),
			api.pagging<any>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			}),
		])
			.then(([depRes, pgRes]) => {
				const fetchedDepartments = depRes.result?.data || [];
				const fetchedProcessGroups = pgRes.result?.data || [];
				setDepartments(fetchedDepartments);
				setProcessGroups(fetchedProcessGroups);

				if (!row) return;

				const periods =
					row.periods && row.periods.length > 0
						? row.periods.map((p) => ({
								id: isDuplicate ? undefined : p.id,
								startMonth: p.startMonth ? p.startMonth.substring(0, 7) : '',
								endMonth: p.endMonth ? p.endMonth.substring(0, 7) : '',
								lowValueSupplyUnitPrice:
									p.lowValueSupplyUnitPrice ??
									p.lowValuePerishableSupplyUnitPrice ??
									0,
								electricityUnitPrice: p.electricityUnitPrice ?? 0,
							}))
						: row.startMonth
							? [
									{
										id: isDuplicate ? undefined : row.id,
										startMonth: row.startMonth.substring(0, 7),
										endMonth: row.endMonth?.substring(0, 7) || '',
										lowValueSupplyUnitPrice:
											row.lowValuePerishableSupplyUnitPrice ??
											row.lowValueSupplyUnitPrice ??
											0,
										electricityUnitPrice: row.electricityUnitPrice ?? 0,
									},
								]
							: [{ ...DEFAULT_PERIOD_ITEM }];

				form.reset({
					departmentId: row.departmentId || '',
					processGroupId: row.processGroupId || '',
					periods,
				});
			})
			.catch((err) => {
				console.error(err);
			});
	}, [form, row, isDuplicate]);

	const handleSubmit = async (
		values: MotorizedLowValueSupplyElectricityFormSchema,
	) => {
		try {
			const pgObj = processGroups.find((p) => p.id === values.processGroupId);
			const requests = values.periods.map((period) => {
				const payload = {
					departmentId: values.departmentId,
					processGroupId: values.processGroupId,
					processGroupName: pgObj
						? `${pgObj.code} - ${pgObj.name}`
						: values.processGroupId,
					startMonth:
						period.startMonth.length === 7
							? `${period.startMonth}-01`
							: period.startMonth,
					endMonth:
						period.endMonth.length === 7
							? `${period.endMonth}-01`
							: period.endMonth,
					lowValuePerishableSupplyUnitPrice: period.lowValueSupplyUnitPrice,
					lowValueSupplyUnitPrice: period.lowValueSupplyUnitPrice,
					electricityUnitPrice: period.electricityUnitPrice,
				};

				if (period.id && !isDuplicate) {
					return api.put(
						API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.UPDATE,
						{
							id: period.id,
							...payload,
						},
					);
				} else {
					return api.post(
						API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.CREATE,
						payload,
					);
				}
			});

			await Promise.all(requests);
			popup.success(
				row && !isDuplicate
					? 'Cập nhật đơn giá thành công'
					: 'Thêm mới đơn giá thành công',
			);

			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form as any} onSubmit={handleSubmit}>
			<FormComboBox
				control={form.control as any}
				name='departmentId'
				label='Đơn vị'
				placeholder='Chọn đơn vị'
				options={departments.map((item) => ({
					label: item.code ? `${item.code} - ${item.name}` : item.name,
					value: item.id,
				}))}
			/>

			<FormComboBox
				control={form.control as any}
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn'
				options={processGroups.map((item) => ({
					label: item.code ? `${item.code} - ${item.name}` : item.name,
					value: item.id,
				}))}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			<div className='flex flex-col gap-4'>
				{fields.map((field, index) => (
					<div
						key={field.id}
						className='flex items-start gap-3 rounded-lg border border-neutral-200 bg-neutral-50/50 p-4'
					>
						<div className='flex flex-1 flex-col gap-3'>
							<FormRow>
								<FormMonthYear
									control={form.control as any}
									name={`periods.${index}.startMonth`}
									label='Thời gian bắt đầu'
									className='flex-1'
								/>
								<FormMonthYear
									control={form.control as any}
									name={`periods.${index}.endMonth`}
									label='Thời gian kết thúc'
									className='flex-1'
								/>
							</FormRow>

							<FormRow>
								<div className='flex-1'>
									<FormNumber
										control={form.control as any}
										name={`periods.${index}.lowValueSupplyUnitPrice`}
										label='Vật tư mau hỏng rẻ tiền (đ/tháng)'
										placeholder='Nhập đơn giá vật tư mau hỏng rẻ tiền'
									/>
								</div>
								<div className='flex-1'>
									<FormNumber
										control={form.control as any}
										name={`periods.${index}.electricityUnitPrice`}
										label='Điện năng (đ/tháng)'
										placeholder='Nhập đơn giá điện năng'
									/>
								</div>
							</FormRow>
						</div>

						<div className='flex items-center gap-2 pt-6'>
							<Button
								type='button'
								variant='ghost'
								size='icon'
								onClick={() => handleCopyPeriod(index)}
								className='hover:text-primary h-fit w-fit border-0 bg-transparent p-0 text-neutral-500 shadow-none hover:bg-transparent'
								title='Sao chép'
							>
								<Copy className='size-5' />
							</Button>
							{fields.length > 1 && (
								<Button
									type='button'
									variant='ghost'
									size='icon'
									onClick={() => remove(index)}
									className='h-fit w-fit border-0 bg-transparent p-0 text-red-500 shadow-none hover:bg-transparent hover:text-red-700'
									title='Xóa'
								>
									<XCircleIcon className='size-5 text-red-500' />
								</Button>
							)}
						</div>
					</div>
				))}

				<div className='flex justify-start pt-1'>
					<Button
						type='button'
						variant='ghost'
						size='sm'
						onClick={() => append({ ...DEFAULT_PERIOD_ITEM })}
						className='flex h-fit w-fit items-center gap-1.5 border-0 bg-transparent p-0 shadow-none hover:bg-transparent'
					>
						<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
						<span className='text-sm text-black'>Thêm thời gian</span>
					</Button>
				</div>
			</div>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
