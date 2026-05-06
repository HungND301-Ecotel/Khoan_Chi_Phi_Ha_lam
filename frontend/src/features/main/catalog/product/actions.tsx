import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { FormText } from '@/components/form/form-text';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Product } from '@/features/main/catalog/product/columns';
import {
	PRODUCT_FORM_DEFAULT,
	productFormSchema,
	ProductFormSchema,
} from '@/features/main/catalog/product/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type ProductFormProps = ActionDialogProps<Product> & {
	isDuplicate?: boolean;
};

export function ProductForm({
	data,
	row,
	isDuplicate = false,
}: ProductFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>();

	const form = useForm<ProductFormSchema>({
		resolver: zodResolver(productFormSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					code: isDuplicate ? '' : row.code,
					name: row.name,
					processGroupId: row.processGroupId,
				}
			: PRODUCT_FORM_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
			.then((res) => setGroups(res.result.data));
	}, [row, form]);

	const handleSubmit = async (values: ProductFormSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PRODUCT.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.PRODUCT.CREATE, values);
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
				label='Mã nhóm công đoạn sản xuất'
				placeholder='Chọn mã nhóm công đoạn sản xuất'
				options={groups?.map((group) => ({
					value: group.id,
					label: group.code,
				}))}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã sản phẩm'
				placeholder='Nhập mã sản phẩm, ví dụ: DL'
			/>

			<FormText
				control={form.control}
				name='name'
				label='Tên sản phẩm'
				placeholder='Nhập tên sản phẩm'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
