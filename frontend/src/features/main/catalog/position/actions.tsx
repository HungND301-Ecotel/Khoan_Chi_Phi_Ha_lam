import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Position } from '@/features/main/catalog/position/columns';
import {
	POSITION_FORM_DEFAULT,
	PositionFormSchema,
	positionFormSchema,
} from '@/features/main/catalog/position/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

type PositionFormProps = ActionDialogProps<Position> & {
	isDuplicate?: boolean;
};

export function PositionForm({
	data,
	row,
	isDuplicate = false,
}: PositionFormProps) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<PositionFormSchema>({
		resolver: zodResolver(positionFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: POSITION_FORM_DEFAULT,
	});

	useEffect(() => {
		if (!row) return;
		form.reset({
			name: row.name,
			level: row.level,
			description: row.description || '',
		});
	}, [row, form, isDuplicate]);

	const handleSubmit = async (values: PositionFormSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.POSITION.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.POSITION.CREATE, values);
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
		<FormProvider context={form as any} onSubmit={handleSubmit as any}>
			<FormInput
				control={form.control}
				name='name'
				label='Tên chức vụ'
				placeholder='Nhập tên chức vụ'
			/>
			<FormInput
				control={form.control}
				name='level'
				label='Cấp bậc'
				type='number'
				placeholder='Nhập cấp bậc'
			/>
			<FormInput
				control={form.control}
				name='description'
				label='Mô tả'
				placeholder='Nhập mô tả chi tiết'
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
