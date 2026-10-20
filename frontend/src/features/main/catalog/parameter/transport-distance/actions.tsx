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
import { TransportDistanceParameter } from '@/features/main/catalog/parameter/transport-distance/columns';
import {
	TRANSPORT_DISTANCE_SCHEMA_DEFAULT,
	transportDistanceBatchSchema,
	TransportDistanceBatchSchema,
} from '@/features/main/catalog/parameter/transport-distance/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

const LONGWALLPARAMETERS_SUPPORTS = [
	'≥',
	'≤',
	'<',
	'>',
	'%',
	'°',
	'=',
	'-',
	'_',
];

type TransportDistanceFormProps =
	ActionDialogProps<TransportDistanceParameter> & {
		isDuplicate?: boolean;
		/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
		single?: boolean;
	};

export function TransportDistanceForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: TransportDistanceFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<TransportDistanceBatchSchema>({
		resolver: zodResolver(transportDistanceBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							value: row.value || row.name || row.distanceRange || '',
							note: row.note || '',
						}
					: TRANSPORT_DISTANCE_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	// Backend lưu name/code theo chính giá trị cung độ vận tải
	const toPayload = (item: TransportDistanceBatchSchema['items'][number]) => ({
		value: item.value,
		name: item.value,
		code: item.value,
		note: item.note,
	});

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: TransportDistanceBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.UPDATE, {
					id: row.id,
					...toPayload(items[0]),
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
			api.post(
				API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.CREATE,
				toPayload(item),
			),
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
				newItem={() => TRANSPORT_DISTANCE_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'value',
						header: 'Cung độ vận tải (L)',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.value`}
								label={label}
								placeholder='Ví dụ: L ≤ 1, L ≤ 1.1'
								supports={LONGWALLPARAMETERS_SUPPORTS}
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
