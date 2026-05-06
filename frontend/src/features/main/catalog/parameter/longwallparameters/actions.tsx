import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Longwallparameters } from '@/features/main/catalog/parameter/longwallparameters/columns';
import {
	LONGWALLPARAMETERS_SCHEMA_DEFAULT,
	longwallparametersSchema,
	LongwallparametersSchema,
} from '@/features/main/catalog/parameter/longwallparameters/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';

const LONGWALLPARAMETERS_SUPPORTS = [
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

export function LongwallparametersForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Longwallparameters> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<LongwallparametersSchema & any>({
		resolver: zodResolver(longwallparametersSchema),
		defaultValues: LONGWALLPARAMETERS_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				llc: row.llc,
				lkc: row.lkc,
				mk: row.mk,
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: LongwallparametersSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PARAMETER.LONGWALLPARAMETERS.UPDATE, {
					id: row?.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.LONGWALLPARAMETERS.CREATE, values);
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
				name='llc'
				label='Llc (m)'
				placeholder='Nhập Llc (m)'
				type='text'
				supports={LONGWALLPARAMETERS_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='lkc'
				label='Lkc (m)'
				placeholder='Nhập Lkc (m)'
				supports={LONGWALLPARAMETERS_SUPPORTS}
			/>
			<FormInput
				control={form.control}
				name='mk'
				label='Mk (m)'
				placeholder='Nhập Mk (m)'
				supports={LONGWALLPARAMETERS_SUPPORTS}
			/>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
