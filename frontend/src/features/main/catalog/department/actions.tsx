import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Department } from '@/features/main/catalog/department/columns';
import {
	DEPARTMENT_FORM_DEFAULT,
	DepartmentFormSchema,
	departmentFormSchema,
} from '@/features/main/catalog/department/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function DepartmentForm({ data, row }: ActionDialogProps<Department>) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<DepartmentFormSchema>({
		resolver: zodResolver(departmentFormSchema),
		mode: 'onSubmit',
		defaultValues: DEPARTMENT_FORM_DEFAULT,
	});

	useEffect(() => {
		if (!row) return;
		form.reset({
			code: row.code,
			name: row.name,
		});
	}, [row, form]);

	const handleSubmit = async (values: DepartmentFormSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.DEPARTMENT.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.DEPARTMENT.CREATE, values);
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
				name='code'
				label='Mã đơn vị'
				placeholder='Nhập mã đơn vị, ví dụ: PX01'
			/>
			<FormInput
				control={form.control}
				name='name'
				label='Tên đơn vị'
				placeholder='Nhập tên đơn vị, ví dụ: Phân xưởng đào lò'
			/>
			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
