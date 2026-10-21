import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormText } from '@/components/form/form-text';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { usePopup } from '@/components/popup';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { CargoType } from './columns';
import {
	CARGO_TYPE_FORM_DEFAULT,
	cargoTypeBatchSchema,
	CargoTypeBatchSchema,
} from './schema';

type CargoTypeFormProps = ActionDialogProps<CargoType> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function CargoTypeForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: CargoTypeFormProps) {
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<CargoTypeBatchSchema>({
		resolver: zodResolver(cargoTypeBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							note: row.note || '',
						}
					: CARGO_TYPE_FORM_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: CargoTypeBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.CARGO_TYPE.UPDATE, {
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
			api.post(API.CATALOG.CARGO_TYPE.CREATE, item),
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
				newItem={() => CARGO_TYPE_FORM_DEFAULT}
				columns={[
					{
						key: 'code',
						header: 'Mã loại hàng',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Nhập mã loại hàng'
							/>
						),
					},
					{
						key: 'name',
						header: 'Loại hàng',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên loại hàng'
							/>
						),
					},
					{
						key: 'note',
						header: 'Ghi chú',
						render: ({ index, label }) => (
							<FormText
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
