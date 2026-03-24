import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import {
	CLAMP_SCHEMA_DEFAULT,
	clampSchema,
	ClampSchema,
} from '@/features/main/catalog/parameter/clamp/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

const CLAMP_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

export function ClampForm({ data, row }: ActionDialogProps<Clamp>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<ClampSchema>({
		resolver: zodResolver(clampSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					value: row.value,
				}
			: CLAMP_SCHEMA_DEFAULT,
	});

	const handleSubmit = async (values: ClampSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.CLAMP.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.CLAMP.CREATE, values);
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
				label='Tỷ lệ đá kẹp (Ckẹp)'
				placeholder='Nhập tỷ lệ đá kẹp (Ckẹp)'
				supports={CLAMP_SUPPORTS}
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
