import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import {
	CONTRACT_CODE_SCHEMA_DEFAULT,
	ContractCodeSchema,
	contractCodeSchema,
} from '@/features/main/catalog/contract-code/schema';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function ContractCodeForm({
	data,
	row,
}: ActionDialogProps<ContractCode>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);

	const form = useForm<ContractCodeSchema>({
		resolver: zodResolver(contractCodeSchema),
		mode: 'onSubmit',
		defaultValues: row
			? { code: row.code, name: row.name, unitOfMeasureId: row.unitOfMeasureId }
			: CONTRACT_CODE_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		api
			.pagging<Unit>(API.CATALOG.UNIT.LIST)
			.then((res) => setUnits(res.result.data));
	}, []);

	const handleSubmit = async (values: ContractCodeSchema) => {
		try {
			if (row?.id) {
				await api.put(API.CATALOG.CONTRACT_CODE.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.CONTRACT_CODE.CREATE, values);
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
			<FormInput
				control={form.control}
				name='code'
				label='Mã giao khoán'
				placeholder='Nhập mã giao khoán, ví dụ: VLN'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên mã giao khoán'
				placeholder='Nhập tên giao khoán, ví dụ: Vật liệu nổ'
			/>

			<FormComboBox
				control={form.control}
				name='unitOfMeasureId'
				label='Đơn vị tính'
				placeholder='Chọn đơn vị tính'
				options={units.map((unit) => ({
					value: unit.id,
					label: unit.name,
				}))}
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
