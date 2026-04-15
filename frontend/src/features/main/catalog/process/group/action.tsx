import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import {
	PROCESS_GROUP_SCHEMA_DEFAULT,
	ProcessGroupSchema,
	processGroupSchema,
} from '@/features/main/catalog/process/group/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function ProcessGroupForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<ProcessGroup> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<ProcessGroupSchema>({
		resolver: zodResolver(processGroupSchema),
		mode: 'onSubmit',
		defaultValues: PROCESS_GROUP_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		if (row) {
			form.reset({
				name: row.name,
				code: isDuplicate ? '' : row.code,
			});
		}
	}, [row, form, isDuplicate]);

	const handleSubmit = async (values: ProcessGroupSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PROCESS.GROUP.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PROCESS.GROUP.CREATE, values);
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
				name='code'
				label='Mã nhóm công đoạn sản xuất'
				placeholder='Nhập mã nhóm công đoạn sản xuất, ví dụ: DL'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên nhóm công đoạn sản xuất'
				placeholder='Nhập tên nhóm công đoạn sản xuất, ví dụ: Đào lò'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
