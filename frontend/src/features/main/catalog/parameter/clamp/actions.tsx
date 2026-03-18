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
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import {
	CLAMP_SCHEMA_DEFAULT,
	clampSchema,
	ClampSchema,
} from '@/features/main/catalog/parameter/clamp/schema';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { ProcessStep } from '@/features/main/catalog/process/step/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

const CLAMP_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

export function ClampForm({ data, row }: ActionDialogProps<Clamp>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [processes, setProcesses] = useState<ProcessStep[]>([]);

	const form = useForm<ClampSchema>({
		resolver: zodResolver(clampSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					value: row.value,
					coefficientValue: row.coefficientValue,
					processId: row.processId,
					hardnessId: row.hardnessId,
				}
			: CLAMP_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
		]);
		promises.then(([strengths, processes]) => {
			setStrengths(strengths.result.data);
			setProcesses(processes.result.data);
		});
	}, [row, form]);

	const handleSubmit = async (values: ClampSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PARAMETER.CLAMP.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.CLAMP.CREATE, values);
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
				name='processId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={processes.map((process) => ({
					label: process.name,
					value: process.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố than/đá (f)'
				placeholder='Chọn độ kiên cố than/đá (f)'
				options={strengths.map((strength) => ({
					label: strength.value,
					value: strength.id,
				}))}
			/>

			<FormInput
				control={form.control}
				name='value'
				label='Tỷ lệ đá kẹp (Ckẹp)'
				placeholder='Nhập tỷ lệ đá kẹp (Ckẹp)'
				supports={CLAMP_SUPPORTS}
			/>

			<FormNumber
				control={form.control}
				name='coefficientValue'
				label='Hệ số điều chỉnh định mức'
				placeholder='Nhập hệ số điều chỉnh định mức'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
