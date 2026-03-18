import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Cuttingthickness } from '@/features/main/catalog/parameter/cuttingthickness/columns';
import {
	CUTTINGTHICKNESS_SCHEMA_DEFAULT,
	cuttingthicknessSchema,
	CuttingthicknessSchema,
} from '@/features/main/catalog/parameter/cuttingthickness/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

const LONGWALLPARAMETERS_SUPPORTS = [
	'≥',
	'≤',
	'<',
	'>',
	'%',
	'°',
	'=',
	'-',
	'_',
];

export function CuttingthicknessForm({
	data,
	row,
}: ActionDialogProps<Cuttingthickness>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<CuttingthicknessSchema>({
		resolver: zodResolver(cuttingthicknessSchema),
		defaultValues: CUTTINGTHICKNESS_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: CuttingthicknessSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.CUTTINGTHICKNESS.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.CUTTINGTHICKNESS.CREATE, values);
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
				label='Chiều dày lớp khấu'
				placeholder='Nhập giá trị chiều dày lớp khấu'
				type='text'
				supports={LONGWALLPARAMETERS_SUPPORTS}
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
