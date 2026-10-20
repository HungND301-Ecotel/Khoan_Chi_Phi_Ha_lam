import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { ProductionOrder } from './columns';
import {
	PRODUCTION_ORDER_SCHEMA_DEFAULT,
	productionOrderBatchSchema,
	ProductionOrderBatchSchema,
} from './schema';

type ProductionOrderFormProps = ActionDialogProps<ProductionOrder> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function ProductionOrderForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: ProductionOrderFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<ProductionOrderBatchSchema>({
		resolver: zodResolver(productionOrderBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							startMonth: row.startMonth,
							endMonth: row.endMonth,
							note: row.note || '',
						}
					: PRODUCTION_ORDER_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: ProductionOrderBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.PRODUCTION_ORDER.UPDATE, {
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
			api.post(API.CATALOG.PARAMETER.PRODUCTION_ORDER.CREATE, item),
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
				// Dòng mới giữ thời gian của dòng trước
				newItem={(previous) => ({
					...PRODUCTION_ORDER_SCHEMA_DEFAULT,
					startMonth: previous?.startMonth ?? '',
					endMonth: previous?.endMonth ?? '',
				})}
				// Form thường: thời gian bắt đầu/kết thúc nằm cùng hàng
				plainClassName='grid grid-cols-2 gap-4'
				columns={[
					{
						key: 'startMonth',
						header: 'Thời gian bắt đầu',
						required: true,
						render: ({ index, label }) => (
							<FormMonthYear
								control={form.control}
								name={`items.${index}.startMonth`}
								label={label}
								placeholder='Nhập thời gian bắt đầu'
							/>
						),
					},
					{
						key: 'endMonth',
						header: 'Thời gian kết thúc',
						required: true,
						render: ({ index, label }) => (
							<FormMonthYear
								control={form.control}
								name={`items.${index}.endMonth`}
								label={label}
								placeholder='Nhập thời gian kết thúc'
							/>
						),
					},
					{
						key: 'code',
						header: 'Số quyết định, lệnh sản xuất',
						required: true,
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.code`}
									label={label}
									placeholder='Nhập số quyết định, lệnh sản xuất'
								/>
							</div>
						),
					},
					{
						key: 'name',
						header: 'Tên quyết định, lệnh sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.name`}
									label={label}
									placeholder='Nhập tên quyết định, lệnh sản xuất'
								/>
							</div>
						),
					},
					{
						key: 'note',
						header: 'Ghi chú',
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.note`}
									label={label}
									placeholder='Nhập ghi chú (nếu có)'
								/>
							</div>
						),
					},
				]}
			/>

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
