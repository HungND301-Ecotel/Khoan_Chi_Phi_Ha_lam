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
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import {
	STRENGTH_SCHEMA_DEFAULT,
	strengthBatchSchema,
	StrengthBatchSchema,
} from '@/features/main/catalog/parameter/strength/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

const STRENGTH_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

type StrengthFormProps = ActionDialogProps<Strength> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function StrengthForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: StrengthFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<StrengthBatchSchema>({
		resolver: zodResolver(strengthBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? { value: row.value, note: row.note || '' }
					: STRENGTH_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: StrengthBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.STRENGTH.UPDATE, {
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
			api.post(API.CATALOG.PARAMETER.STRENGTH.CREATE, item),
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
				newItem={() => STRENGTH_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'value',
						header: 'Độ kiên cố than, đá (f)',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.value`}
								label={label}
								placeholder='Nhập độ kiên cố (f)'
								supports={STRENGTH_SUPPORTS}
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
