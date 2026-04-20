import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { FixedKey, FixedKeyType } from '@/constants/fixed-key';
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
import { useEffect, useMemo, useState } from 'react';
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

	const fixedKeyOptions = useMemo(
		() =>
			fixedKeys
				.sort((a, b) => a.code.localeCompare(b.code))
				.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				})),
		[fixedKeys],
	);

	useEffect(() => {
		let isMounted = true;

		const fetchFixedKeys = async () => {
			try {
				const response = await api.pagging<FixedKey>(API.CATALOG.FIXED_KEY.LIST, {
					ignorePagination: true,
					type: FixedKeyType.ProcessGroup,
				});

				if (!isMounted) return;
				setFixedKeys(response.result.data);
			} catch (error) {
				if (!isMounted) return;
				setFixedKeys([]);
				popup.error(error);
			}
		};

		fetchFixedKeys();

		return () => {
			isMounted = false;
		};
	}, [popup]);

	useEffect(() => {
		if (row) {
			form.reset({
				name: row.name,
				code: isDuplicate ? '' : row.code,
				fixedKeyId: row.fixedKeyId ?? '',
			});
		}
	}, [row, form, isDuplicate]);

	useEffect(() => {
		const subscription = form.watch((values, info) => {
			if (info.name !== 'fixedKeyId') return;
			const selectedFixedKey = fixedKeys.find(
				(item) => item.id === values.fixedKeyId,
			);
			form.setValue('code', selectedFixedKey?.code ?? '', {
				shouldValidate: true,
				shouldDirty: true,
			});
		});

		return () => subscription.unsubscribe();
	}, [form, fixedKeys]);

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
				label='Khóa hệ thống'
				placeholder='Chọn khóa hệ thống cho nhóm công đoạn'
				options={fixedKeyOptions}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã nhóm công đoạn sản xuất'
				placeholder='Mã được suy ra từ khóa hệ thống'
				readOnly
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
