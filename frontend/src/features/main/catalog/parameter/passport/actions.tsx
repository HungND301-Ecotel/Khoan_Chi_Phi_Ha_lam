import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import {
	PASSPORT_SCHEMA_DEFAULT,
	passportSchema,
	PassportSchema,
} from '@/features/main/catalog/parameter/passport/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

const PASSPORT_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

export function PassportForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Passport> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<PassportSchema>({
		resolver: zodResolver(passportSchema),
		defaultValues: PASSPORT_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				name: row.name,
				sd: row.sd,
				sc: row.sc,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: PassportSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PARAMETER.PASSPORT.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.PASSPORT.CREATE, values);
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
				name='name'
				label='Hộ chiếu'
				placeholder='Nhập hộ chiếu'
			/>
			<FormInput
				control={form.control}
				name='sd'
				label='Sđ'
				placeholder='Nhập Sđ'
				type='text'
				supports={PASSPORT_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='sc'
				label='Sc'
				placeholder='Nhập Sc'
				type='text'
				supports={PASSPORT_SUPPORTS}
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
