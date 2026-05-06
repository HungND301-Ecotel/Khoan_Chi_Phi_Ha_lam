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
import { RevenueCostAdjustmentConfig } from './columns';
import {
	REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT,
	RevenueCostAdjustmentConfigSchema,
	revenueCostAdjustmentConfigSchema,
} from './schema';

const REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS = [
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

export function RevenueCostAdjustmentConfigForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<RevenueCostAdjustmentConfig> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<RevenueCostAdjustmentConfigSchema>({
		resolver: zodResolver(revenueCostAdjustmentConfigSchema),
		defaultValues: REVENUE_COST_ADJUSTMENT_CONFIG_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				profitConditionDisplay: row.profitConditionDisplay ?? '',
				rateDisplay: row.rateDisplay ?? '',
				description: row.description ?? '',
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: RevenueCostAdjustmentConfigSchema) => {
		const payload = {
			profitConditionDisplay: values.profitConditionDisplay,
			rateDisplay: values.rateDisplay,
			description: values.description,
		};

		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(
					API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.CREATE,
					payload,
				);
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
				name='profitConditionDisplay'
				label='Lợi nhuận'
				placeholder='Nhập điều kiện lợi nhuận'
				type='text'
				supports={REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='rateDisplay'
				label='Tỷ lệ điều chỉnh'
				placeholder='Nhập tỷ lệ điều chỉnh'
				type='text'
				supports={REVENUE_COST_ADJUSTMENT_CONFIG_SUPPORTS}
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
