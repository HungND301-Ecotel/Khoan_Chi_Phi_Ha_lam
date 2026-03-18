import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { CLAMP_SCHEMA_DEFAULT } from '@/features/main/catalog/parameter/clamp/schema';
import {
	insertSchema,
	InsertSchema,
} from '@/features/main/catalog/parameter/insert/schema';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { StepSchema } from '@/features/main/catalog/parameter/step/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function StepForm({ data, row }: ActionDialogProps<Step>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<StepSchema>({
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
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.STEP.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.STEP.CREATE, values);
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
				label='Bước chống'
				placeholder='Nhập bước chống'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
