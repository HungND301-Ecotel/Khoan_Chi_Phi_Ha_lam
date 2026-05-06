import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { FixedKey } from '@/features/main/system/fixed-key/columns';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import {
	PROCESS_GROUP_SCHEMA_DEFAULT,
	ProcessGroupSchema,
	processGroupSchema,
} from '@/features/main/catalog/process/group/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function ProcessGroupForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<ProcessGroup> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [fixedKeys, setFixedKeys] = useState<FixedKey[]>([]);

	const form = useForm<ProcessGroupSchema>({
		resolver: zodResolver(processGroupSchema),
		mode: 'onSubmit',
		defaultValues: PROCESS_GROUP_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<FixedKey>(API.SYSTEM.FIXED_KEY.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				setFixedKeys(
					[...res.result.data].sort((a, b) => a.key.localeCompare(b.key)),
				);
			})
			.catch((error) => popup.error(error));
	}, [popup]);

	useEffect(() => {
		if (row) {
			form.reset({
				name: row.name,
				fixedKeyId: isDuplicate ? '' : (row.fixedKeyId ?? ''),
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
			<FormComboBox
				control={form.control}
				name='fixedKeyId'
				label='Khóa cấu hình'
				placeholder='Chọn khóa cấu hình'
				options={fixedKeys.map((fixedKey) => ({
					value: fixedKey.id,
					label: `${fixedKey.key} - ${fixedKey.name}`,
				}))}
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
