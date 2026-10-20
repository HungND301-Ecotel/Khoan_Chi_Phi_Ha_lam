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
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { AkFactorConfig } from './columns';
import {
	AK_FACTOR_CONFIG_SCHEMA_DEFAULT,
	akFactorConfigBatchSchema,
	AkFactorConfigBatchSchema,
} from './schema';

const AK_FACTOR_CONFIG_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

type AkFactorConfigFormProps = ActionDialogProps<AkFactorConfig> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function AkFactorConfigForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: AkFactorConfigFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<AkFactorConfigBatchSchema>({
		resolver: zodResolver(akFactorConfigBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							processGroupId: row.processGroupId ?? '',
							akDiffDisplay: row.akDiffDisplay ?? '',
							adjustmentRateDisplay: row.adjustmentRateDisplay ?? '',
							description: row.description ?? '',
						}
					: AK_FACTOR_CONFIG_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const loadProcessGroups = useCallback(
		() =>
			api
				.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
					ignorePagination: true,
				})
				.then((res) => {
					setProcessGroups(
						[...res.result.data].sort((a, b) => a.code.localeCompare(b.code)),
					);
				})
				.catch((error) => popup.error(error)),
		[popup],
	);

	useEffect(() => {
		loadProcessGroups();
	}, [loadProcessGroups]);

	const groupQuickCreate = useCatalogQuickCreate(
		'processGroup',
		loadProcessGroups,
	);

	const toPayload = (item: AkFactorConfigBatchSchema['items'][number]) => ({
		processGroupId: item.processGroupId,
		akDiffDisplay: item.akDiffDisplay,
		adjustmentRateDisplay: item.adjustmentRateDisplay,
		description: item.description,
	});

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: AkFactorConfigBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.AK_FACTOR_CONFIG.UPDATE, {
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
			api.post(API.CATALOG.AK_FACTOR_CONFIG.CREATE, toPayload(item)),
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
					...AK_FACTOR_CONFIG_SCHEMA_DEFAULT,
					processGroupId: previous?.processGroupId ?? '',
				})}
				columns={[
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
								options={processGroups.map((processGroup) => ({
									value: processGroup.id,
									label: `${processGroup.code} - ${processGroup.name}`,
								}))}
								quickCreate={groupQuickCreate}
							/>
						),
					},
					{
						key: 'akDiffDisplay',
						header: 'Chênh lệch Ak',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.akDiffDisplay`}
								label={label}
								placeholder='Ví dụ: > 0, ≤ -0,5'
								type='text'
								supports={AK_FACTOR_CONFIG_SUPPORTS}
							/>
						),
					},
					{
						key: 'adjustmentRateDisplay',
						header: 'Tỷ lệ điều chỉnh doanh thu',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.adjustmentRateDisplay`}
								label={label}
								placeholder='Ví dụ: 1,5%'
								type='text'
								supports={AK_FACTOR_CONFIG_SUPPORTS}
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
