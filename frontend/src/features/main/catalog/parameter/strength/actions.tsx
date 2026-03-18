import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import {
	STRENGTH_SCHEMA_DEFAULT,
	strengthSchema,
	StrengthSchema,
} from '@/features/main/catalog/parameter/strength/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

const STRENGTH_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

export function StrengthForm({ data, row }: ActionDialogProps<Strength>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<StrengthSchema>({
		resolver: zodResolver(strengthSchema),
		defaultValues: STRENGTH_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: StrengthSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.STRENGTH.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.STRENGTH.CREATE, values);
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
				label='Độ kiên cố than, đá (f)'
				placeholder='Nhập độ kiên cố than, đá (f)'
				supports={STRENGTH_SUPPORTS}
			/>
			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
