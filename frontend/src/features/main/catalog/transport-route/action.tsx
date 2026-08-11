import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import {
	TRANSPORT_ROUTE_SCHEMA_DEFAULT,
	TransportRouteSchema,
	transportRouteSchema,
} from '@/features/main/catalog/transport-route/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

type TransportRouteFormProps = ActionDialogProps<TransportRoute> & {
	isDuplicate?: boolean;
};

export function TransportRouteForm({
	data,
	row,
	isDuplicate = false,
}: TransportRouteFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const form = useForm<TransportRouteSchema>({
		resolver: zodResolver(transportRouteSchema) as any,
		mode: 'onSubmit',
		defaultValues: row
			? {
					code: isDuplicate ? '' : row.code,
					name: row.name,
					note: row.note || '',
					isSpecialLowVolume: row.isSpecialLowVolume ?? false,
				}
			: TRANSPORT_ROUTE_SCHEMA_DEFAULT,
	});

	const handleSubmit = async (values: TransportRouteSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.TRANSPORT_ROUTE.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.TRANSPORT_ROUTE.CREATE, values);
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
		<FormProvider context={form as any} onSubmit={handleSubmit as any}>
			<FormInput
				control={form.control}
				name='code'
				label='Mã tuyến vận tải'
				placeholder='Nhập mã tuyến vận tải'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên tuyến vận tải'
				placeholder='Nhập tên tuyến vận tải'
			/>

			<FormCheckBox
				control={form.control}
				name='isSpecialLowVolume'
				label='Áp dụng giá đặc biệt sản lượng thấp'
			/>

			<FormInput
				control={form.control}
				name='note'
				label='Ghi chú'
				placeholder='Nhập ghi chú (nếu có)'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
