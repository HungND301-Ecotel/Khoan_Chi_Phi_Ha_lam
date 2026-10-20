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
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import {
	PASSPORT_SCHEMA_DEFAULT,
	passportBatchSchema,
	PassportBatchSchema,
} from '@/features/main/catalog/parameter/passport/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

const PASSPORT_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

type PassportFormProps = ActionDialogProps<Passport> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function PassportForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: PassportFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<PassportBatchSchema>({
		resolver: zodResolver(passportBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? { name: row.name, sd: row.sd, sc: row.sc, note: row.note || '' }
					: PASSPORT_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: PassportBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PARAMETER.PASSPORT.UPDATE, {
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
			api.post(API.CATALOG.PARAMETER.PASSPORT.CREATE, item),
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
				newItem={() => PASSPORT_SCHEMA_DEFAULT}
				columns={[
					{
						key: 'name',
						header: 'Hộ chiếu',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập hộ chiếu'
							/>
						),
					},
					{
						key: 'sd',
						header: 'Sđ',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.sd`}
								label={label}
								placeholder='Nhập Sđ'
								type='text'
								supports={PASSPORT_SUPPORTS}
							/>
						),
					},
					{
						key: 'sc',
						header: 'Sc',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.sc`}
								label={label}
								placeholder='Nhập Sc'
								type='text'
								supports={PASSPORT_SUPPORTS}
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
