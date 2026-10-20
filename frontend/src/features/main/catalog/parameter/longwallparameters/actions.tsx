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
import { Longwallparameters } from '@/features/main/catalog/parameter/longwallparameters/columns';
import {
	LONGWALLPARAMETERS_SCHEMA_DEFAULT,
	longwallparametersBatchSchema,
	LongwallparametersBatchSchema,
} from '@/features/main/catalog/parameter/longwallparameters/schema';
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

type LongwallparametersFormProps = ActionDialogProps<Longwallparameters> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function LongwallparametersForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: LongwallparametersFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<LongwallparametersBatchSchema>({
		resolver: zodResolver(longwallparametersBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							// Kiểu cột là number nhưng schema form là string
							llc: row.llc,
							lkc: String(row.lkc ?? ''),
							mk: String(row.mk ?? ''),
							note: row.note || '',
						}
					: LONGWALLPARAMETERS_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: LongwallparametersBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.LONGWALLPARAMETERS.UPDATE, {
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
			api.post(API.CATALOG.PARAMETER.LONGWALLPARAMETERS.CREATE, item),
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
				newItem={() => LONGWALLPARAMETERS_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'llc',
						header: 'Llc (m)',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.llc`}
								label={label}
								placeholder='Nhập Llc (m)'
								type='text'
								supports={LONGWALLPARAMETERS_SUPPORTS}
							/>
						),
					},
					{
						key: 'lkc',
						header: 'Lkc (m)',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.lkc`}
								label={label}
								placeholder='Nhập Lkc (m)'
								supports={LONGWALLPARAMETERS_SUPPORTS}
							/>
						),
					},
					{
						key: 'mk',
						header: 'Mk (m)',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.mk`}
								label={label}
								placeholder='Nhập Mk (m)'
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
