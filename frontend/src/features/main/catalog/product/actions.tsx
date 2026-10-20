import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
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
	productBatchSchema,
	ProductBatchSchema,
} from '@/features/main/catalog/product/schema';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type ProductFormProps = ActionDialogProps<Product> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

export function ProductForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: ProductFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>();

	const isEdit = !!row?.id && !isDuplicate;

	const form = useForm<ProductBatchSchema>({
		resolver: zodResolver(productBatchSchema),
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code,
							name: row.name,
							processGroupId: row.processGroupId,
							startMonth: row.startMonth,
							endMonth: row.endMonth,
							note: row.note || '',
						}
					: PRODUCT_FORM_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	const loadGroups = useCallback(
		() =>
			api
				.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
				.then((res) => setGroups(res.result.data)),
		[],
	);

	useEffect(() => {
		loadGroups();
	}, [loadGroups]);

	const groupQuickCreate = useCatalogQuickCreate('processGroup', loadGroups);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: ProductBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(API.CATALOG.PRODUCT.UPDATE, {
					id: row.id,
					...items[0],
				});
				setOpen(false);
				popup.success(`${breadcrumb} đã được Cập nhật thành công.`);
				await refresh();
			} catch (error) {
				popup.error(error);
			}
			return;
		}

		const result = await batch.submit(items, (item) =>
			api.post(API.CATALOG.PRODUCT.CREATE, item),
		);
		await reportBatchResult(result, {
			breadcrumb,
			popup,
			close: () => setOpen(false),
			refresh,
		});
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormBatch
				form={form}
				batch={batch}
				canAdd={!isEdit && !single}
				// Dòng mới giữ thời gian và nhóm công đoạn của dòng trước
				newItem={(previous) => ({
					...PRODUCT_FORM_DEFAULT,
					startMonth: previous?.startMonth ?? '',
					endMonth: previous?.endMonth ?? '',
					processGroupId: previous?.processGroupId ?? '',
				})}
				// Form thường: thời gian bắt đầu/kết thúc nằm cùng hàng
				plainClassName='grid grid-cols-2 gap-4'
				columns={[
					{
						key: 'startMonth',
						header: 'Thời gian bắt đầu',
						required: true,
						render: ({ index, label }) => (
							<FormMonthYear
								control={form.control}
								name={`items.${index}.startMonth`}
								label={label}
								placeholder='Nhập thời gian bắt đầu'
							/>
						),
					},
					{
						key: 'endMonth',
						header: 'Thời gian kết thúc',
						required: true,
						render: ({ index, label }) => (
							<FormMonthYear
								control={form.control}
								name={`items.${index}.endMonth`}
								label={label}
								placeholder='Nhập thời gian kết thúc'
							/>
						),
					},
					{
						key: 'code',
						header: 'Mã sản phẩm',
						required: true,
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.code`}
									label={label}
									placeholder='Nhập mã sản phẩm, ví dụ: DL'
								/>
							</div>
						),
					},
					{
						key: 'name',
						header: 'Tên sản phẩm',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormText
									control={form.control}
									name={`items.${index}.name`}
									label={label}
									placeholder='Nhập tên sản phẩm'
								/>
							</div>
						),
					},
					{
						key: 'processGroupId',
						header: 'Nhóm công đoạn sản xuất',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormComboBox
									control={form.control}
									name={`items.${index}.processGroupId`}
									label={label}
									placeholder='Chọn nhóm công đoạn sản xuất'
									options={groups?.map((group) => ({
										value: group.id,
										label: group.code,
									}))}
									quickCreate={groupQuickCreate}
								/>
							</div>
						),
					},
					{
						key: 'note',
						header: 'Ghi chú',
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.note`}
									label={label}
									placeholder='Nhập ghi chú (nếu có)'
								/>
							</div>
						),
					},
				]}
			/>

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
