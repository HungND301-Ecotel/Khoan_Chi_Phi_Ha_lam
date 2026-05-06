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
import { FixedKey } from '@/features/main/system/fixed-key/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { isAdjustmentFactorFixedKey } from '@/constants/adjustment-factor-type';

type FactorFormProps = ActionDialogProps<Factor> & {
	isDuplicate?: boolean;
};

export function FactorForm({
	data,
	row,
	isDuplicate = false,
}: FactorFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [fixedKeys, setFixedKeys] = useState<FixedKey[]>([]);

	const form = useForm<FactorSchema>({
		resolver: zodResolver(factorSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					fixedKeyId: isDuplicate ? '' : (row.fixedKeyId ?? ''),
					name: row.name,
					processGroupId: row.processGroupId,
				}
			: FACTOR_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		Promise.all([
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
			api.pagging<FixedKey>(API.SYSTEM.FIXED_KEY.LIST, {
				ignorePagination: true,
			}),
		]).then(([groupRes, fixedKeyRes]) => {
			setGroups(groupRes.result.data);
			setFixedKeys(
				fixedKeyRes.result.data
					.filter((fixedKey) => isAdjustmentFactorFixedKey(fixedKey.key))
					.sort((a, b) => a.key.localeCompare(b.key)),
			);
		});
	}, [row, form]);

	const handleSubmit = async (values: FactorSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.ADJUSTMENT.FACTOR.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.ADJUSTMENT.FACTOR.CREATE, values);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
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
				name='fixedKeyId'
				label='Khóa cấu hình'
				placeholder='Chọn khóa cấu hình'
				options={fixedKeys.map((fixedKey) => ({
					label: `${fixedKey.key} - ${fixedKey.name}`,
					value: fixedKey.id,
				}))}
			/>

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
				name='name'
				label='Tên hệ số điều chỉnh'
				placeholder='Nhập tên hệ số điều chỉnh'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
