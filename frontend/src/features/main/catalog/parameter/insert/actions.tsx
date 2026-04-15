import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { CLAMP_SCHEMA_DEFAULT } from '@/features/main/catalog/parameter/clamp/schema';
import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import {
	insertSchema,
	InsertSchema,
} from '@/features/main/catalog/parameter/insert/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function InsertForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Insert> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<InsertSchema>({
		resolver: zodResolver(insertSchema),
		defaultValues: CLAMP_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: InsertSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PARAMETER.INSERT.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.INSERT.CREATE, values);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
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
				label='Chèn'
				placeholder='Nhập chèn'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
