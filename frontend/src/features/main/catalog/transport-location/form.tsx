import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormSelect } from '@/components/form/form-select';
import { FormText } from '@/components/form/form-text';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { LocationType, TransportLocation } from './columns';
import {
	TRANSPORT_LOCATION_FORM_DEFAULT,
	transportLocationBatchSchema,
	TransportLocationBatchSchema,
	TransportLocationFormSchema,
} from './schema';

type TransportLocationFormProps = ActionDialogProps<TransportLocation> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

// Phân loại để trống cho đến khi người dùng chọn
const TRANSPORT_LOCATION_ITEM_DEFAULT =
	TRANSPORT_LOCATION_FORM_DEFAULT as TransportLocationFormSchema;

export function TransportLocationForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: TransportLocationFormProps) {
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<TransportLocationBatchSchema>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any -- locationType dùng z.coerce nên kiểu input/output khác nhau
		resolver: zodResolver(transportLocationBatchSchema) as any,
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							locationType: row.locationType,
							note: row.note || '',
						}
					: TRANSPORT_LOCATION_ITEM_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: TransportLocationBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.TRANSPORT_LOCATION.UPDATE, {
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
			api.post(API.CATALOG.TRANSPORT_LOCATION.CREATE, item),
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
				// Dòng mới giữ phân loại của dòng trước
				newItem={(previous) => ({
					...TRANSPORT_LOCATION_ITEM_DEFAULT,
					locationType:
						previous?.locationType ??
						TRANSPORT_LOCATION_ITEM_DEFAULT.locationType,
				})}
				columns={[
					{
						key: 'code',
						header: 'Mã vị trí',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Nhập mã vị trí (ví dụ: VT-01)'
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên vị trí',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên vị trí (ví dụ: Lò +30, Bãi thải)'
							/>
						),
					},
					{
						key: 'locationType',
						header: 'Phân loại',
						required: true,
						render: ({ index, label }) => (
							<FormSelect
								control={form.control}
								name={`items.${index}.locationType`}
								label={label}
								placeholder='Chọn phân loại'
								options={[
									{
										label: 'Vị trí nhận tải',
										value: LocationType.Receiving.toString(),
									},
									{
										label: 'Vị trí đổ tải',
										value: LocationType.Dumping.toString(),
									},
								]}
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
