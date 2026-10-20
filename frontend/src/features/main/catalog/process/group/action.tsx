import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
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
	processGroupBatchSchema,
	ProcessGroupBatchSchema,
} from '@/features/main/catalog/process/group/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type ProcessGroupFormProps = ActionDialogProps<ProcessGroup> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function ProcessGroupForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: ProcessGroupFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [fixedKeys, setFixedKeys] = useState<FixedKey[]>([]);

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<ProcessGroupBatchSchema>({
		resolver: zodResolver(processGroupBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							name: row.name,
							fixedKeyId: isDuplicate ? '' : (row.fixedKeyId ?? ''),
							note: row.note || '',
						}
					: PROCESS_GROUP_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

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

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: ProcessGroupBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PROCESS.GROUP.UPDATE, {
					id: row.id,
					...items[0],
				});
				setOpen(false);
				popup.success(`${breadcrumb} đã được Cập nhật thành công.`);
				await refresh();
			} catch (error) {
				popup.error(error);
			}
			return;
		}

		const result = await batch.submit(items, (item) =>
			api.post(API.CATALOG.PROCESS.GROUP.CREATE, item),
		);
		await reportBatchResult(result, {
			breadcrumb,
			popup,
			close: () => setOpen(false),
			refresh,
		});
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormBatch
				form={form}
				batch={batch}
				canAdd={!isEdit && !single}
				newItem={() => PROCESS_GROUP_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'fixedKeyId',
						header: 'Mã nhóm công đoạn sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormComboBox
								control={form.control}
								name={`items.${index}.fixedKeyId`}
								label={label}
								placeholder='Chọn mã nhóm công đoạn sản xuất'
								options={fixedKeys.map((fixedKey) => ({
									value: fixedKey.id,
									label: `${fixedKey.key} - ${fixedKey.name}`,
								}))}
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên nhóm công đoạn sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên nhóm công đoạn sản xuất, ví dụ: Đào lò'
							/>
						),
					},
					{
						key: 'note',
						header: 'Ghi chú',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.note`}
								label={label}
								placeholder='Nhập ghi chú (nếu có)'
							/>
						),
					},
				]}
			/>

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
