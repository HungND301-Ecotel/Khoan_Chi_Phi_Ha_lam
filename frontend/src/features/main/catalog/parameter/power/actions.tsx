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
import { Power } from './columns';
import { POWER_SCHEMA_DEFAULT, powerSchema, PowerSchema } from './schema';

export function PowerForm({ data, row }: ActionDialogProps<Power>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<PowerSchema>({
		resolver: zodResolver(powerSchema),
		defaultValues: POWER_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: PowerSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.POWER.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.POWER.CREATE, values);
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
			<FormInput
				control={form.control}
				name='value'
				label='Công suất'
				placeholder='Nhập công suất'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
