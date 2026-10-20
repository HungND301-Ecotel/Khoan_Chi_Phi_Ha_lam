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
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { SavingsRateConfig } from './columns';
import {
	SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
	savingsRateConfigBatchSchema,
	SavingsRateConfigBatchSchema,
} from './schema';

const SAVINGS_RATE_CONFIG_SUPPORTS = [
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

type SavingsRateConfigFormProps = ActionDialogProps<SavingsRateConfig> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function SavingsRateConfigForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: SavingsRateConfigFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<SavingsRateConfigBatchSchema>({
		resolver: zodResolver(savingsRateConfigBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							revenueDisplay:
								row.revenueDisplay ??
								(row.maxRevenue != null ? `≤ ${row.maxRevenue}` : ''),
							savingsRateDisplay:
								row.savingsRateDisplay ??
								(row.maxSavingsRate != null ? `≤ ${row.maxSavingsRate}%` : ''),
							description: row.description ?? '',
						}
					: SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const toPayload = (item: SavingsRateConfigBatchSchema['items'][number]) => ({
		revenueDisplay: item.revenueDisplay,
		savingsRateDisplay: item.savingsRateDisplay,
		description: item.description,
	});

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: SavingsRateConfigBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.SAVINGS_RATE_CONFIG.UPDATE, {
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
			api.post(API.CATALOG.SAVINGS_RATE_CONFIG.CREATE, toPayload(item)),
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
				newItem={() => SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'revenueDisplay',
						header: 'Tổng doanh thu 3 yếu tố',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.revenueDisplay`}
								label={label}
								placeholder='Ví dụ: ≥ 300000'
								type='text'
								supports={SAVINGS_RATE_CONFIG_SUPPORTS}
							/>
						),
					},
					{
						key: 'savingsRateDisplay',
						header: 'Giá trị tiết kiệm',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.savingsRateDisplay`}
								label={label}
								placeholder='Ví dụ: 8 - 12%'
								type='text'
								supports={SAVINGS_RATE_CONFIG_SUPPORTS}
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
								placeholder='Nhập ghi chú'
							/>
						),
					},
				]}
			/>

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
