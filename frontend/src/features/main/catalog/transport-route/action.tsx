import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import {
	TRANSPORT_ROUTE_SCHEMA_DEFAULT,
	transportRouteBatchSchema,
	TransportRouteBatchSchema,
} from '@/features/main/catalog/transport-route/schema';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

type TransportRouteFormProps = ActionDialogProps<TransportRoute> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function TransportRouteForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: TransportRouteFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<TransportRouteBatchSchema>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any -- isSpecialLowVolume có .default() nên kiểu input/output khác nhau
		resolver: zodResolver(transportRouteBatchSchema) as any,
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							note: row.note || '',
							isSpecialLowVolume: row.isSpecialLowVolume ?? false,
						}
					: TRANSPORT_ROUTE_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: TransportRouteBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.TRANSPORT_ROUTE.UPDATE, {
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
			api.post(API.CATALOG.TRANSPORT_ROUTE.CREATE, item),
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
				newItem={() => TRANSPORT_ROUTE_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'code',
						header: 'Mã tuyến vận tải',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Nhập mã tuyến vận tải'
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên tuyến vận tải',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên tuyến vận tải'
							/>
						),
					},
					{
						key: 'isSpecialLowVolume',
						header: 'Đơn giá tuyến vận tải phụ thuộc sản lượng',
						// Trong bảng: căn giữa ô đánh dấu theo chiều cao ô nhập
						render: ({ index, label }) => (
							<div
								className={cn(!label && 'flex h-9 items-center justify-center')}
							>
								<FormCheckBox
									control={form.control}
									name={`items.${index}.isSpecialLowVolume`}
									label={label}
								/>
							</div>
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
