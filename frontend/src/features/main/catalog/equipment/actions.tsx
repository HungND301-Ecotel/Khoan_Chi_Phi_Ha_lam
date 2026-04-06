import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import {
	EQUIPMENT_SCHEMA_DEFAULT,
	equipmentSchema,
	EquipmentSchema,
} from '@/features/main/catalog/equipment/schema';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export type EquipmentDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	currentPrice: number;
	costs: Array<{
		startMonth: string;
		endMonth: string;
		costType: number;
		amount: number;
	}>;
	processGroups: Array<{
		id: string;
		code: string;
		name: string;
	}>;
};

export function EquipmentForm({ data, row }: ActionDialogProps<Equipment>) {
	const { breadcrumb } = useMeta();
	const { setOpen } = useDialog();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const form = useForm<EquipmentSchema>({
		resolver: zodResolver(equipmentSchema),
		mode: 'onSubmit',
		defaultValues: EQUIPMENT_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
		]);

		promises.then(([units, processGroups]) => {
			setUnits(units.result.data);
			setProcessGroups(processGroups.result.data);
			if (row) {
				api
					.get<EquipmentDetail>(API.CATALOG.EQUIPMENT.DETAIL(row.id))
					.then((res) => {
						const { costs, processGroups, ...equipment } = res.result;
						form.reset({
							...equipment,
							processGroupIds: processGroups.map((item) => item.id),
							costs:
								costs.map((cost) => ({
									startMonth: cost.startMonth.substring(0, 10),
									endMonth: cost.endMonth.substring(0, 10),
									amount: cost.amount,
								})) || EQUIPMENT_SCHEMA_DEFAULT.costs,
						});
					});
			}
		});
	}, [row, form]);

	const handleSubmit = async (values: EquipmentSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id) {
				await api.put(API.CATALOG.EQUIPMENT.UPDATE, {
					id: row?.id,
					...processedValues,
				});
			} else {
				await api.post(API.CATALOG.EQUIPMENT.CREATE, processedValues);
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
				label='Mã thiết bị'
				placeholder='Nhập mã thiết bị'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên thiết bị'
				placeholder='Nhập tên thiết bị'
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

			<FormMultiSelect
				control={form.control}
				name='processGroupIds'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={processGroups.map((processGroup) => ({
					value: processGroup.id,
					label: `${processGroup.code} - ${processGroup.name}`,
				}))}
			/>

			<FormArray
				control={form.control}
				name='costs'
				label='Đơn giá điện năng (đ/kWh)'
				canEmpty
			>
				{(index) => (
					<div className='flex w-full gap-4'>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.startMonth`}
							label='Thời gian bắt đầu'
							className='flex-1'
						/>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.endMonth`}
							label='Thời gian kết thúc'
							className='flex-1'
						/>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.amount`}
								label='Đơn giá điện năng (đ/kWh)'
								placeholder='Nhập đơn giá điện năng (đ/kWh)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
