import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { SavingsRateConfig } from './columns';
import {
	SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
	savingsRateConfigSchema,
	SavingsRateConfigSchema,
} from './schema';
import { FormNumber } from '@/components/form/form-number';

export function SavingsRateConfigForm({
	data,
	row,
}: ActionDialogProps<SavingsRateConfig>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<SavingsRateConfigSchema>({
		resolver: zodResolver(savingsRateConfigSchema),
		defaultValues: SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});
	const isUnlimited = useWatch({
		control: form.control,
		name: 'isUnlimited',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				maxRevenue: row.maxRevenue ?? null,
				isUnlimited: row.maxRevenue == null,
				maxSavingsRate: row.maxSavingsRate ?? null,
				description: row.description ?? '',
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: SavingsRateConfigSchema) => {
		const payload = {
			maxRevenue: values.isUnlimited ? null : values.maxRevenue,
			maxSavingsRate: values.maxSavingsRate,
			description: values.description,
		};

		try {
			if (row?.id) {
				await api.put(API.CATALOG.SAVINGS_RATE_CONFIG.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.SAVINGS_RATE_CONFIG.CREATE, payload);
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
			<FormNumber
				control={form.control}
				name='maxRevenue'
				label='Tổng doanh thu 3 yếu tố'
				placeholder='Nhập tổng doanh thu 3 yếu tố'
				disabled={isUnlimited}
			/>
			<FormCheckBox
				control={form.control}
				name='isUnlimited'
				label='Không giới hạn (dùng cho ngưỡng cuối cùng)'
			/>
			<FormNumber
				control={form.control}
				name='maxSavingsRate'
				label='Giá trị tiết kiệm'
				placeholder='Nhập giá trị tiết kiệm'
			/>
			<FormInput
				control={form.control}
				name='description'
				label='Description'
				placeholder='Nhập mô tả'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
