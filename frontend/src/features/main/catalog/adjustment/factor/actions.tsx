import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
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
	factorSchema,
	FactorSchema,
} from '@/features/main/catalog/adjustment/factor/schema';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function FactorForm({ data, row }: ActionDialogProps<Factor>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);

	const form = useForm<FactorSchema>({
		resolver: zodResolver(factorSchema),
		mode: 'onSubmit',
		defaultValues: row
			? { code: row.code, name: row.name, processGroupId: row.processGroupId }
			: FACTOR_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
			.then((res) => setGroups(res.result.data));
	}, [row, form]);

	const handleSubmit = async (values: FactorSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.ADJUSTMENT.FACTOR.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.ADJUSTMENT.FACTOR.CREATE, values);
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
				options={groups.map((group) => ({
					label: group.name,
					value: group.id,
				}))}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã hệ số điều chỉnh'
				placeholder='Nhập mã hệ số điều chỉnh'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên hệ số điều chỉnh'
				placeholder='Nhập tên hệ số điều chỉnh'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
