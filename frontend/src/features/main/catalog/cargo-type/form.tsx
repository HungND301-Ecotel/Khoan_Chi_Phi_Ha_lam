import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormText } from '@/components/form/form-text';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { usePopup } from '@/components/popup';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { CargoType } from './columns';
import {
	CARGO_TYPE_FORM_DEFAULT,
	cargoTypeFormSchema,
	CargoTypeFormSchema,
} from './schema';

type CargoTypeFormProps = ActionDialogProps<CargoType> & {
	isDuplicate?: boolean;
};

export function CargoTypeForm({
	data,
	row,
	isDuplicate = false,
}: CargoTypeFormProps) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const form = useForm<CargoTypeFormSchema>({
		resolver: zodResolver(cargoTypeFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: CARGO_TYPE_FORM_DEFAULT,
	});

	useEffect(() => {
		if (row) {
			form.reset({
				code: isDuplicate ? `${row.code}_COPY` : row.code,
				name: row.name,
				note: row.note || '',
			});
		}
	}, [form, isDuplicate, row]);

	const handleSubmit = async (values: CargoTypeFormSchema) => {
		try {
			if (row && !isDuplicate) {
				await api.put(API.CATALOG.CARGO_TYPE.UPDATE, {
					id: row.id,
					...values,
				});
				popup.success('Cập nhật chủng loại hàng thành công');
			} else {
				await api.post(API.CATALOG.CARGO_TYPE.CREATE, values);
				popup.success('Thêm mới chủng loại hàng thành công');
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
			<FormInput
				control={form.control as any}
				name='code'
				label='Mã chủng loại hàng'
				placeholder='Nhập mã chủng loại hàng'
			/>
			<FormInput
				control={form.control as any}
				name='name'
				label='Chủng loại hàng'
				placeholder='Nhập tên chủng loại hàng'
			/>
			<FormText
				control={form.control as any}
				name='note'
				label='Ghi chú'
				placeholder='Nhập ghi chú (nếu có)'
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
