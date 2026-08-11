import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { MotorizedLowValueSupplyElectricityUnitPrice } from './columns';
import {
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
	const [processGroups, setProcessGroups] = useState<any[]>([]);

	const form = useForm<MotorizedLowValueSupplyElectricityFormSchema>({
		resolver: zodResolver(motorizedLowValueSupplyElectricityFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<any>(API.CATALOG.PROCESS.GROUP.LIST, { ignorePagination: true })
			.then((res) => {
				const fetched = res.result?.data || [];
				setProcessGroups(fetched);

				if (!row) return;

				form.reset({
					startMonth: row.startMonth?.substring(0, 7),
					endMonth: row.endMonth?.substring(0, 7),
					processGroupId: row.processGroupId || row.processGroupName,
					lowValueSupplyUnitPrice:
						row.lowValuePerishableSupplyUnitPrice ??
						row.lowValueSupplyUnitPrice ??
						0,
					electricityUnitPrice: row.electricityUnitPrice ?? 0,
				});
			})
			.catch((err) => {
				console.error(err);
			});
	}, [form, row]);

	const handleSubmit = async (
		values: MotorizedLowValueSupplyElectricityFormSchema,
	) => {
		try {
			const pgObj = processGroups.find((p) => p.id === values.processGroupId);
			const payload = {
				processGroupId: values.processGroupId,
				processGroupName: pgObj
					? `${pgObj.code} - ${pgObj.name}`
					: values.processGroupId,
				startMonth:
					values.startMonth.length === 7
						? `${values.startMonth}-01`
						: values.startMonth,
				endMonth:
					values.endMonth.length === 7
						? `${values.endMonth}-01`
						: values.endMonth,
				lowValuePerishableSupplyUnitPrice: values.lowValueSupplyUnitPrice,
				lowValueSupplyUnitPrice: values.lowValueSupplyUnitPrice,
				electricityUnitPrice: values.electricityUnitPrice,
			};

			if (row && !isDuplicate) {
				await api.put(
					API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.UPDATE,
					{
						id: row.id,
						...payload,
					},
				);
				popup.success('Cập nhật đơn giá thành công');
			} else {
				await api.post(
					API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.CREATE,
					payload,
				);
				popup.success('Thêm mới đơn giá thành công');
			}

			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form as any} onSubmit={handleSubmit}>
			<FormRow>
				<FormMonthYear
					control={form.control as any}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control as any}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

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

			<FormSeparator />

			<FormRow>
				<div className='flex-1'>
					<FormNumber
						control={form.control as any}
						name='lowValueSupplyUnitPrice'
						label='Vật tư mau hỏng rẻ tiền (đ/tháng)'
						placeholder='Nhập đơn giá vật tư mau hỏng rẻ tiền'
					/>
				</div>
				<div className='flex-1'>
					<FormNumber
						control={form.control as any}
						name='electricityUnitPrice'
						label='Điện năng (đ/tháng)'
						placeholder='Nhập đơn giá điện năng'
					/>
				</div>
			</FormRow>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
