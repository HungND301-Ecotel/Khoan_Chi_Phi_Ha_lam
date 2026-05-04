import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import {
	FixedKey,
	FIXED_KEY_TYPE_OPTIONS,
} from '@/features/main/system/fixed-key/columns';
import {
	FIXED_KEY_SCHEMA_DEFAULT,
	fixedKeySchema,
	FixedKeySchema,
} from '@/features/main/system/fixed-key/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

export function FixedKeyForm({ data, row }: ActionDialogProps<FixedKey>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<FixedKeySchema>({
		resolver: zodResolver(fixedKeySchema),
		mode: 'onSubmit',
		defaultValues: FIXED_KEY_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		if (!row) return;

		form.reset({
			key: row.key,
			name: row.name,
			type: row.type,
		});
	}, [row, form]);

	const handleSubmit = async (values: FixedKeySchema) => {
		try {
			if (!row?.id) {
				return;
			}

			await api.put(API.SYSTEM.FIXED_KEY.UPDATE, {
				id: row.id,
				...values,
			});

			setOpen(false);
			popup.success(`${breadcrumb} đã được cập nhật thành công.`);
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
				name='key'
				label='Code'
				placeholder='Nhập code, ví dụ: DL1'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên khóa cấu hình'
				placeholder='Nhập tên khóa cấu hình'
			/>

			<FormComboBox
				control={form.control}
				name='type'
				label='Loại nghiệp vụ'
				placeholder='Chọn loại nghiệp vụ'
				options={FIXED_KEY_TYPE_OPTIONS}
				disabled
			/>

			<DataTableEditConfirm isEdit={true} />
		</FormProvider>
	);
}
