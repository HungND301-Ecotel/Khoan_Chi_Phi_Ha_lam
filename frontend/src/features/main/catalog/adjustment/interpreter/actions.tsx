import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
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
	InterpreterSchema,
	interpreterSchema,
} from '@/features/main/catalog/adjustment/interpreter/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function InterpreterForm({ data, row }: ActionDialogProps<Interpreter>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [factors, setFactors] = useState<Factor[]>([]);

	const form = useForm<InterpreterSchema>({
		resolver: zodResolver(interpreterSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					description: row.description,
					adjustmentFactorId: row.adjustmentFactorId,
					maintenanceAdjustmentValue: row.maintenanceAdjustmentValue,
					electricityAdjustmentValue:
						row.electricityAdjustmentValue || undefined,
				}
			: INTERPRETER_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<Factor>(API.CATALOG.ADJUSTMENT.FACTOR.LIST)
			.then((res) => setFactors(res.result.data));
	}, [row, form]);

	const handleSubmit = async (values: InterpreterSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.ADJUSTMENT.INTERPRETER.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.ADJUSTMENT.INTERPRETER.CREATE, values);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormComboBox
				control={form.control}
				name='adjustmentFactorId'
				label='Mã hệ số điều chỉnh'
				placeholder='Chọn mã hệ số điều chỉnh'
				options={factors.map((factor) => ({
					label: `${factor.processGroupCode} - ${factor.code}`,
					value: factor.id,
				}))}
			/>

			<FormInput
				control={form.control}
				name='description'
				label='Diễn giải'
				placeholder='Nhập diễn giải'
			/>

			<FormNumber
				control={form.control}
				name='maintenanceAdjustmentValue'
				label='Trị số điều chỉnh SCTX'
				placeholder='Nhập trị số điều chỉnh SCTX'
			/>

			<FormNumber
				control={form.control}
				name='electricityAdjustmentValue'
				label='Trị số điều chỉnh điện năng'
				placeholder='Nhập trị số điều chỉnh điện năng'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
