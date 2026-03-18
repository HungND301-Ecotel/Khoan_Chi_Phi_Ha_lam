import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Technology } from '@/features/main/catalog/parameter/technology/columns';
import {
	technologySchema,
	TechnologySchema,
	TECHNOLOGY_SCHEMA_DEFAULT,
} from '@/features/main/catalog/parameter/technology/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function TechnologyForm({ data, row }: ActionDialogProps<Technology>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<TechnologySchema>({
		resolver: zodResolver(technologySchema),
		defaultValues: TECHNOLOGY_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: TechnologySchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.TECHNOLOGY.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.TECHNOLOGY.CREATE, values);
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
				label='Công nghệ khai thác'
				placeholder='Nhập công nghệ khai thác'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
