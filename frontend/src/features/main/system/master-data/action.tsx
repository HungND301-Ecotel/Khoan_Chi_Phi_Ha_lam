import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormSelect } from '@/components/form/form-select';
import {
	FIXED_KEY_TYPE_OPTIONS,
	FixedKey,
} from '@/constants/fixed-key';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MASTER_DATA_SCHEMA_DEFAULT,
	MasterDataSchema,
	masterDataSchema,
} from '@/features/main/system/master-data/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { usePopup } from '@/components/popup';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

type MasterDataFormProps = ActionDialogProps<FixedKey> & {
	isDuplicate?: boolean;
};

export function MasterDataForm({
	data,
	row,
	isDuplicate = false,
}: MasterDataFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<MasterDataSchema>({
		resolver: zodResolver(masterDataSchema),
		mode: 'onSubmit',
		defaultValues: MASTER_DATA_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		if (!row) return;
		form.reset({
			code: isDuplicate ? '' : row.code,
			name: row.name,
			type: String(row.type),
			isSystem: row.isSystem,
		});
	}, [row, form, isDuplicate]);

	const handleSubmit = async (values: MasterDataSchema) => {
		try {
			const payload = {
				...values,
				type: Number(values.type),
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.FIXED_KEY.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.FIXED_KEY.CREATE, payload);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='grid grid-cols-1 gap-4 lg:grid-cols-2'>
				<FormInput
					control={form.control}
					name='code'
					label='Mã fixed key'
					placeholder='Nhập mã fixed key, ví dụ: Maintain'
				/>
				<FormSelect
					control={form.control}
					name='type'
					label='Loại fixed key'
					placeholder='Chọn loại fixed key'
					options={FIXED_KEY_TYPE_OPTIONS.map((item) => ({
						value: item.value,
						label: item.label,
					}))}
				/>
			</div>
			<FormInput
				control={form.control}
				name='name'
				label='Tên fixed key'
				placeholder='Nhập tên hiển thị của fixed key'
			/>
			<FormCheckBox
				control={form.control}
				name='isSystem'
				label='Khoá hệ thống'
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}