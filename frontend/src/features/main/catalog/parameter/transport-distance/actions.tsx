import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { TransportDistanceParameter } from '@/features/main/catalog/parameter/transport-distance/columns';
import {
	transportDistanceSchema,
	TransportDistanceSchema,
	TRANSPORT_DISTANCE_SCHEMA_DEFAULT,
} from '@/features/main/catalog/parameter/transport-distance/schema';
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

export function TransportDistanceForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<TransportDistanceParameter> & { isDuplicate?: boolean }) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<TransportDistanceSchema>({
		resolver: zodResolver(transportDistanceSchema),
		defaultValues: TRANSPORT_DISTANCE_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		if (row) {
			form.reset({
				value: row.value || row.name || row.distanceRange || '',
			});
		}
	}, [row, form]);

	const handleSubmit = async (values: TransportDistanceSchema) => {
		try {
			const payload = {
				value: values.value,
				name: values.value,
				code: values.value,
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.UPDATE, {
					id: row?.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.CREATE, payload);
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
				name='value'
				label='Cung độ vận tải (L)'
				placeholder='Nhập cung độ vận tải (ví dụ: L ≤ 1, L ≤ 1.1)'
				supports={LONGWALLPARAMETERS_SUPPORTS}
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
