import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { SavingsRateConfig } from './columns';
import {
	SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
	savingsRateConfigSchema,
	SavingsRateConfigSchema,
} from './schema';

const SAVINGS_RATE_CONFIG_SUPPORTS = [
	'≥',
	'≤',
	'<',
	'>',
	'%',
	'°',
	'=',
	'-',
	'_',
];

export function SavingsRateConfigForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<SavingsRateConfig> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<SavingsRateConfigSchema>({
		resolver: zodResolver(savingsRateConfigSchema),
		defaultValues: SAVINGS_RATE_CONFIG_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				revenueDisplay:
					row.revenueDisplay ??
					(row.maxRevenue != null ? `≤ ${row.maxRevenue}` : ''),
				savingsRateDisplay:
					row.savingsRateDisplay ??
					(row.maxSavingsRate != null ? `≤ ${row.maxSavingsRate}%` : ''),
				description: row.description ?? '',
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: SavingsRateConfigSchema) => {
		const payload = {
			revenueDisplay: values.revenueDisplay,
			savingsRateDisplay: values.savingsRateDisplay,
			description: values.description,
		};

		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.SAVINGS_RATE_CONFIG.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.SAVINGS_RATE_CONFIG.CREATE, payload);
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
			<FormInput
				control={form.control}
				name='revenueDisplay'
				label='Tổng doanh thu 3 yếu tố'
				placeholder='Nhập tổng doanh thu 3 yếu tố'
				type='text'
				supports={SAVINGS_RATE_CONFIG_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='savingsRateDisplay'
				label='Giá trị tiết kiệm'
				placeholder='Nhập giá trị tiết kiệm'
				type='text'
				supports={SAVINGS_RATE_CONFIG_SUPPORTS}
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
