import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { ProductionOrder } from './columns';
import {
	PRODUCTION_ORDER_SCHEMA_DEFAULT,
	ProductionOrderSchema,
} from './schema';
import { FormMonthYear } from '@/components/form/form-month-year';

export function ProductionOrderForm({
	data,
	row,
}: ActionDialogProps<ProductionOrder>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<ProductionOrderSchema>({
		resolver: zodResolver(ProductionOrderSchema),
		defaultValues: PRODUCTION_ORDER_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				code: row.code,
				name: row.name,
				startMonth: row.startMonth,
				endMonth: row.endMonth,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: ProductionOrderSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.PRODUCTION_ORDER.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.PRODUCTION_ORDER.CREATE, values);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='flex w-full gap-4'>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu'
					placeholder='Nhập thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					placeholder='Nhập thời gian kết thúc'
					className='flex-1'
				/>
			</div>

			<FormInput
				control={form.control}
				name='code'
				label='Số quyết định, lệnh sản xuất'
				placeholder='Nhập số quyết định, lệnh sản xuất'
			/>
			<FormInput
				control={form.control}
				name='name'
				label='Tên quyết định, lệnh sản xuất'
				placeholder='Nhập tên quyết định, lệnh sản xuất'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
