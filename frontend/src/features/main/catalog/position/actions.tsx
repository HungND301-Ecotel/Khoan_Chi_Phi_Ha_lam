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
import { Position } from '@/features/main/catalog/position/columns';
import {
	POSITION_FORM_DEFAULT,
	positionBatchSchema,
	PositionBatchSchema,
} from '@/features/main/catalog/position/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

type PositionFormProps = ActionDialogProps<Position> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function PositionForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: PositionFormProps) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<PositionBatchSchema>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any -- level dùng z.coerce nên kiểu input/output khác nhau
		resolver: zodResolver(positionBatchSchema) as any,
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code || '',
							name: row.name,
							level: row.level,
							description: row.description || '',
						}
					: POSITION_FORM_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: PositionBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.POSITION.UPDATE, {
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
			api.post(API.CATALOG.POSITION.CREATE, item),
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
				newItem={() => POSITION_FORM_DEFAULT}
				columns={[
					{
						key: 'code',
						header: 'Mã chức vụ',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Nhập mã chức vụ'
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên chức vụ',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên chức vụ'
							/>
						),
					},
					{
						key: 'level',
						header: 'Cấp bậc',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.level`}
								label={label}
								type='number'
								placeholder='Nhập cấp bậc'
							/>
						),
					},
					{
						key: 'description',
						header: 'Ghi chú',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.description`}
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
