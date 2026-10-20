import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Factor } from '@/features/main/catalog/adjustment/factor/columns';
import {
	FACTOR_SCHEMA_DEFAULT,
	factorBatchSchema,
	FactorBatchSchema,
} from '@/features/main/catalog/adjustment/factor/schema';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { FixedKey } from '@/features/main/system/fixed-key/columns';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { isAdjustmentFactorFixedKey } from '@/constants/adjustment-factor-type';

type FactorFormProps = ActionDialogProps<Factor> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function FactorForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: FactorFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [fixedKeys, setFixedKeys] = useState<FixedKey[]>([]);

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<FactorBatchSchema>({
		resolver: zodResolver(factorBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							fixedKeyId: isDuplicate ? '' : (row.fixedKeyId ?? ''),
							name: row.name,
							processGroupId: row.processGroupId,
							note: row.note || '',
						}
					: FACTOR_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const loadGroups = useCallback(
		() =>
			api
				.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
				.then((res) => setGroups(res.result.data)),
		[],
	);

	const groupQuickCreate = useCatalogQuickCreate('processGroup', loadGroups);

	useEffect(() => {
		loadGroups();
	}, [loadGroups]);

	useEffect(() => {
		api
			.pagging<FixedKey>(API.SYSTEM.FIXED_KEY.LIST, {
				ignorePagination: true,
			})
			.then((fixedKeyRes) => {
				setFixedKeys(
					fixedKeyRes.result.data
						.filter((fixedKey) => isAdjustmentFactorFixedKey(fixedKey.key))
						.sort((a, b) => a.key.localeCompare(b.key)),
				);
			});
	}, [row, form]);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: FactorBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.ADJUSTMENT.FACTOR.UPDATE, {
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
			api.post(API.CATALOG.ADJUSTMENT.FACTOR.CREATE, item),
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
				// Dòng mới giữ nhóm công đoạn của dòng trước
				newItem={(previous) => ({
					...FACTOR_SCHEMA_DEFAULT,
					processGroupId: previous?.processGroupId ?? '',
				})}
				columns={[
					{
						key: 'fixedKeyId',
						header: 'Hệ số điều chỉnh',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormComboBox
								control={form.control}
								name={`items.${index}.fixedKeyId`}
								label={label}
								placeholder='Chọn hệ số điều chỉnh'
								options={fixedKeys.map((fixedKey) => ({
									label: `${fixedKey.key} - ${fixedKey.name}`,
									value: fixedKey.id,
								}))}
							/>
						),
					},
					{
						key: 'processGroupId',
						header: 'Nhóm công đoạn sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormComboBox
								control={form.control}
								name={`items.${index}.processGroupId`}
								label={label}
								placeholder='Chọn nhóm công đoạn'
								options={groups.map((group) => ({
									label: group.name,
									value: group.id,
								}))}
								quickCreate={groupQuickCreate}
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên hệ số điều chỉnh',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Nhập tên hệ số'
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
