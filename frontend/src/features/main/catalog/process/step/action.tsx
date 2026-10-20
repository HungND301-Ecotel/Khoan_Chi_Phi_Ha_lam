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
import { ProcessStep } from '@/features/main/catalog/process/step/columns';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import {
	PROCESS_STEP_SCHEMA_DEFAULT,
	ProcessStepBatchSchema,
	processStepBatchSchema,
} from '@/features/main/catalog/process/step/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type ProcessStepFormProps = ActionDialogProps<ProcessStep> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function ProcessStepForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: ProcessStepFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>();
	const [units, setUnits] = useState<{ id: string; name: string }[]>();

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<ProcessStepBatchSchema>({
		resolver: zodResolver(processStepBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							processGroupId: row.processGroupId,
							unitOfMeasureId: row.unitOfMeasureId || '',
							note: row.note || '',
						}
					: PROCESS_STEP_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const loadGroups = useCallback(
		() =>
			api
				.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
					ignorePagination: true,
				})
				.then((res) => setGroups(res.result.data)),
		[],
	);

	const loadUnits = useCallback(
		() =>
			api
				.pagging<{ id: string; name: string }>(API.CATALOG.UNIT.LIST, {
					ignorePagination: true,
				})
				.then((res) => setUnits(res.result.data)),
		[],
	);

	useEffect(() => {
		loadGroups();
		loadUnits();
	}, [loadGroups, loadUnits]);

	const groupQuickCreate = useCatalogQuickCreate('processGroup', loadGroups);
	const unitQuickCreate = useCatalogQuickCreate('unit', loadUnits);

	// Đơn vị tính không bắt buộc: gửi null thay vì chuỗi rỗng (backend nhận Guid?)
	const toPayload = (item: ProcessStepBatchSchema['items'][number]) => ({
		...item,
		unitOfMeasureId: item.unitOfMeasureId || null,
	});

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: ProcessStepBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PROCESS.STEP.UPDATE, {
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
			api.post(API.CATALOG.PROCESS.STEP.CREATE, toPayload(item)),
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
				// Dòng mới giữ nhóm công đoạn và đơn vị tính của dòng trước
				newItem={(previous) => ({
					...PROCESS_STEP_SCHEMA_DEFAULT,
					processGroupId: previous?.processGroupId ?? '',
					unitOfMeasureId: previous?.unitOfMeasureId ?? '',
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
								placeholder='Chọn nhóm công đoạn sản xuất'
								options={groups?.map((group) => ({
									value: group.id,
									label: group.name,
								}))}
								quickCreate={groupQuickCreate}
							/>
						),
					},
					{
						key: 'code',
						header: 'Mã công đoạn sản xuất',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Ví dụ: DL'
							/>
						),
					},
					{
						key: 'name',
						header: 'Tên công đoạn sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.name`}
								label={label}
								placeholder='Ví dụ: Đào lò'
							/>
						),
					},
					{
						key: 'unitOfMeasureId',
						header: 'Đơn vị tính',
						required: true,
						render: ({ index, label }) => (
							<FormComboBox
								control={form.control}
								name={`items.${index}.unitOfMeasureId`}
								label={label}
								placeholder='Chọn đơn vị tính'
								options={units?.map((u) => ({
									value: u.id,
									label: u.name,
								}))}
								quickCreate={unitQuickCreate}
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
