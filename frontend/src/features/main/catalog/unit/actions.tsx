import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Unit } from '@/features/main/catalog/unit/columns';
import {
	UNIT_FORM_DEFAULT,
	unitFormSchema,
	UnitFormSchema,
} from '@/features/main/catalog/unit/shema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

type UnitFormProps = ActionDialogProps<Unit> & {
	isDuplicate?: boolean;
};

export function UnitForm({ data, row, isDuplicate = false }: UnitFormProps) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<UnitFormSchema>({
		resolver: zodResolver(unitFormSchema),
		mode: 'onSubmit',
		defaultValues: UNIT_FORM_DEFAULT,
	});

	useEffect(() => {
		if (!row) return;
		form.reset({
			name: row.name,
		});
	}, [row, form]);

	const handleSubmit = async (values: UnitFormSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.UNIT.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.UNIT.CREATE, values);
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
				label='Đơn vị tính'
				placeholder='Nhập tên đơn vị tính, ví dụ: cái'
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
