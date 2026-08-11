import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormSelect } from '@/components/form/form-select';
import { FormText } from '@/components/form/form-text';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { LocationType, TransportLocation } from './columns';
import {
	TRANSPORT_LOCATION_FORM_DEFAULT,
	transportLocationFormSchema,
	TransportLocationFormSchema,
} from './schema';

type TransportLocationFormProps = ActionDialogProps<TransportLocation> & {
	isDuplicate?: boolean;
};

export function TransportLocationForm({
	data,
	row,
	isDuplicate = false,
}: TransportLocationFormProps) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const form = useForm<TransportLocationFormSchema>({
		resolver: zodResolver(transportLocationFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: TRANSPORT_LOCATION_FORM_DEFAULT as any,
	});

	useEffect(() => {
		if (row) {
			form.reset({
				code: isDuplicate ? `${row.code}_COPY` : row.code,
				name: row.name,
				locationType: row.locationType,
				note: row.note || '',
			});
		} else {
			form.reset(TRANSPORT_LOCATION_FORM_DEFAULT as any);
		}
	}, [form, isDuplicate, row]);

	const handleSubmit = async (values: TransportLocationFormSchema) => {
		try {
			if (row && !isDuplicate) {
				await api.put(API.CATALOG.TRANSPORT_LOCATION.UPDATE, {
					id: row.id,
					...values,
				});
				popup.success('Cập nhật vị trí thành công');
			} else {
				await api.post(API.CATALOG.TRANSPORT_LOCATION.CREATE, values);
				popup.success('Thêm mới vị trí thành công');
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
				label='Mã vị trí'
				placeholder='Nhập mã vị trí (ví dụ: VT-01)'
			/>
			<FormInput
				control={form.control as any}
				name='name'
				label='Tên vị trí'
				placeholder='Nhập tên vị trí (ví dụ: Lò +30, Bãi thải)'
			/>
			<FormSelect
				control={form.control as any}
				name='locationType'
				label='Loại vị trí'
				placeholder='Chọn loại vị trí'
				options={[
					{ label: 'Vị trí nhận', value: LocationType.Receiving.toString() },
					{ label: 'Vị trí đổ', value: LocationType.Dumping.toString() },
				]}
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
