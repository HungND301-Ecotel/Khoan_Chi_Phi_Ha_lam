import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Factor } from '@/features/main/catalog/adjustment/factor/columns';
import { Interpreter } from '@/features/main/catalog/adjustment/interpreter/columns';
import {
	INTERPRETER_SCHEMA_DEFAULT,
	InterpreterBatchSchema,
	interpreterBatchSchema,
} from '@/features/main/catalog/adjustment/interpreter/schema';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type InterpreterFormProps = ActionDialogProps<Interpreter> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function InterpreterForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: InterpreterFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [factors, setFactors] = useState<Factor[]>([]);

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<InterpreterBatchSchema>({
		resolver: zodResolver(interpreterBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							description: row.description,
							adjustmentFactorId: row.adjustmentFactorId,
							maintenanceAdjustmentValue: row.maintenanceAdjustmentValue,
							electricityAdjustmentValue:
								row.electricityAdjustmentValue || undefined,
							note: row.note || '',
						}
					: INTERPRETER_SCHEMA_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const loadFactors = useCallback(
		() =>
			api
				.pagging<Factor>(API.CATALOG.ADJUSTMENT.FACTOR.LIST)
				.then((res) => setFactors(res.result.data)),
		[],
	);

	useEffect(() => {
		loadFactors();
	}, [loadFactors]);

	const factorQuickCreate = useCatalogQuickCreate(
		'adjustmentFactor',
		loadFactors,
	);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: InterpreterBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.ADJUSTMENT.INTERPRETER.UPDATE, {
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
			api.post(API.CATALOG.ADJUSTMENT.INTERPRETER.CREATE, item),
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
				// Dòng mới giữ mã hệ số điều chỉnh của dòng trước
				newItem={(previous) => ({
					...INTERPRETER_SCHEMA_DEFAULT,
					adjustmentFactorId: previous?.adjustmentFactorId ?? '',
				})}
				columns={[
					{
						key: 'adjustmentFactorId',
						header: 'Mã hệ số điều chỉnh',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormComboBox
								control={form.control}
								name={`items.${index}.adjustmentFactorId`}
								label={label}
								placeholder='Chọn mã hệ số'
								options={factors.map((factor) => ({
									label: `${factor.processGroupCode} - ${factor.code}`,
									value: factor.id,
								}))}
								quickCreate={factorQuickCreate}
							/>
						),
					},
					{
						key: 'description',
						header: 'Diễn giải',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.description`}
								label={label}
								placeholder='Nhập diễn giải'
							/>
						),
					},
					{
						key: 'maintenanceAdjustmentValue',
						header: 'Trị số điều chỉnh SCTX',
						required: true,
						render: ({ index, label }) => (
							<FormNumber
								control={form.control}
								name={`items.${index}.maintenanceAdjustmentValue`}
								label={label}
								placeholder='Nhập trị số SCTX'
							/>
						),
					},
					{
						key: 'electricityAdjustmentValue',
						header: 'Trị số điều chỉnh điện năng',
						render: ({ index, label }) => (
							<FormNumber
								control={form.control}
								name={`items.${index}.electricityAdjustmentValue`}
								label={label}
								placeholder='Nhập trị số điện năng'
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
