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
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { AkFactorConfig } from './columns';
import {
	AK_FACTOR_CONFIG_SCHEMA_DEFAULT,
	AkFactorConfigSchema,
} from './schema';

const AK_FACTOR_CONFIG_SUPPORTS = ['≥', '≤', '<', '>', '%', '°', '=', '-', '_'];

export function AkFactorConfigForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<AkFactorConfig> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const form = useForm<AkFactorConfigSchema>({
		resolver: zodResolver(AkFactorConfigSchema),
		defaultValues: AK_FACTOR_CONFIG_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				setProcessGroups(
					[...res.result.data].sort((a, b) => a.code.localeCompare(b.code)),
				);
			})
			.catch((error) => popup.error(error));
	}, [popup]);

	useEffect(() => {
		if (!row) return;

		form.reset({
			processGroupId: row.processGroupId ?? '',
			akDiffDisplay: row.akDiffDisplay ?? '',
			adjustmentRateDisplay: row.adjustmentRateDisplay ?? '',
			description: row.description ?? '',
		});
	}, [row, form]);

	const handleSubmit = async (values: AkFactorConfigSchema) => {
		const payload = {
			processGroupId: values.processGroupId,
			akDiffDisplay: values.akDiffDisplay,
			adjustmentRateDisplay: values.adjustmentRateDisplay,
			description: values.description,
		};

		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.AK_FACTOR_CONFIG.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.AK_FACTOR_CONFIG.CREATE, payload);
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
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={processGroups.map((processGroup) => ({
					value: processGroup.id,
					label: `${processGroup.code} - ${processGroup.name}`,
				}))}
			/>
			<FormInput
				control={form.control}
				name='akDiffDisplay'
				label='Chênh lệch Ak'
				placeholder='Ví dụ: > 0 hoặc ≤ -0,5'
				type='text'
				supports={AK_FACTOR_CONFIG_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='adjustmentRateDisplay'
				label='Tỷ lệ điều chỉnh doanh thu'
				placeholder='Ví dụ: 1,5%'
				type='text'
				supports={AK_FACTOR_CONFIG_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='description'
				label='Mô tả'
				placeholder='Nhập mô tả'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
