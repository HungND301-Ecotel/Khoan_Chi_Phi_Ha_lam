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
import { RevenueCostAdjustmentConfig } from './columns';
import {
	REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT,
	revenueCostAdjustmentConfigBatchSchema,
	RevenueCostAdjustmentConfigBatchSchema,
} from './schema';

const REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS = [
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

type RevenueCostAdjustmentConfigFormProps =
	ActionDialogProps<RevenueCostAdjustmentConfig> & {
		isDuplicate?: boolean;
		/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
		single?: boolean;
	};

export function RevenueCostAdjustmentConfigForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: RevenueCostAdjustmentConfigFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<RevenueCostAdjustmentConfigBatchSchema>({
		resolver: zodResolver(revenueCostAdjustmentConfigBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							profitConditionDisplay: row.profitConditionDisplay ?? '',
							rateDisplay: row.rateDisplay ?? '',
							description: row.description ?? '',
						}
					: REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const toPayload = (
		item: RevenueCostAdjustmentConfigBatchSchema['items'][number],
	) => ({
		profitConditionDisplay: item.profitConditionDisplay,
		rateDisplay: item.rateDisplay,
		description: item.description,
	});

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({
		items,
	}: RevenueCostAdjustmentConfigBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.UPDATE, {
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
				API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.CREATE,
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
				newItem={() => REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'profitConditionDisplay',
						header: 'Lợi nhuận',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.profitConditionDisplay`}
								label={label}
								placeholder='Ví dụ: ≥ 0'
								type='text'
								supports={REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS}
							/>
						),
					},
					{
						key: 'rateDisplay',
						header: 'Tỷ lệ điều chỉnh',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.rateDisplay`}
								label={label}
								placeholder='Ví dụ: 60%'
								type='text'
								supports={REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS}
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
