import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Power } from '@/features/main/catalog/parameter/power/columns';
import {
	POWER_SCHEMA_DEFAULT,
	powerBatchSchema,
	PowerBatchSchema,
} from '@/features/main/catalog/parameter/power/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

type PowerFormProps = ActionDialogProps<Power> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function PowerForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: PowerFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<PowerBatchSchema>({
		resolver: zodResolver(powerBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row ? { value: row.value, note: row.note || '' } : POWER_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: PowerBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.POWER.UPDATE, {
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
			api.post(API.CATALOG.PARAMETER.POWER.CREATE, item),
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
				newItem={() => POWER_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'value',
						header: 'Công suất',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.value`}
								label={label}
								placeholder='Nhập công suất'
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
