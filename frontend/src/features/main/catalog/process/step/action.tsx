import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { ProcessStep } from '@/features/main/catalog/process/step/columns';
import {
	PROCESS_STEP_SCHEMA_DEFAULT,
	ProcessStepSchema,
	processStepSchema,
} from '@/features/main/catalog/process/step/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function ProcessStepForm({ data, row }: ActionDialogProps<ProcessStep>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>();

	const form = useForm<ProcessStepSchema>({
		resolver: zodResolver(processStepSchema),
		mode: 'onSubmit',
		defaultValues: row
			? { code: row.code, name: row.name, processGroupId: row.processGroupId }
			: PROCESS_STEP_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
			.then((res) => setGroups(res.result.data));
	}, []);

	const handleSubmit = async (values: ProcessStepSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.PROCESS.STEP.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PROCESS.STEP.CREATE, values);
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
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={groups?.map((group) => ({
					value: group.id,
					label: group.name,
				}))}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã công đoạn sản xuất'
				placeholder='Nhập mã công đoạn sản xuất, ví dụ: DL'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên công đoạn sản xuất'
				placeholder='Nhập tên nhóm công đoạn sản xuất, ví dụ: Đào lò'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
