import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import { Part } from '@/features/main/catalog/part/columns';
import {
	PART_SCHEMA_DEFAULT,
	partSchema,
	PartSchema,
} from '@/features/main/catalog/part/schema';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export type PartDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	equipmentId: string;
	equipmentCode: string;
	costs: Array<{
		startMonth: string;
		endMonth: string;
		costType: number;
		amount: number;
	}>;
};

export function PartForm({ data, row }: ActionDialogProps<Part>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);
	const [equipments, setEquipments] = useState<Equipment[]>([]);

	const form = useForm<PartSchema>({
		resolver: zodResolver(partSchema),
		defaultValues: PART_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	useEffect(() => {
		const promises = Promise.all([
			api
				.pagging<Unit>(API.CATALOG.UNIT.LIST)
				.then((res) => setUnits(res.result.data)),
			api
				.pagging<Equipment>(API.CATALOG.EQUIPMENT.LIST)
				.then((res) => setEquipments(res.result.data)),
		]);

		promises.then(() => {
			if (!row) return;
			api.get<PartDetail>(API.CATALOG.PART.DETAIL(row.id)).then((res) => {
				const { costs, ...part } = res.result;
				form.reset({
					...part,
					costs: costs?.length
						? costs.map((cost) => ({
								startMonth: cost.startMonth.substring(0, 10),
								endMonth: cost.endMonth.substring(0, 10),
								amount: cost.amount,
							}))
						: PART_SCHEMA_DEFAULT.costs,
				});
			});
		});
	}, [row, form]);

	const handleSubmit = async (values: PartSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id) {
				await api.put(API.CATALOG.PART.UPDATE, {
					id: row?.id,
					...processedValues,
				});
			} else {
				await api.post(API.CATALOG.PART.CREATE, processedValues);
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
			<FormComboBox
				control={form.control}
				name='equipmentId'
				label='Mã thiết bị'
				placeholder='Chọn mã thiết bị'
				options={equipments?.map((equipment) => ({
					value: equipment.id,
					label: equipment.code,
				}))}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã phụ tùng'
				placeholder='Nhập mã phụ tùng, ví dụ: DL'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên phụ tùng'
				placeholder='Nhập tên phụ tùng, ví dụ: Đào lò'
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

			<FormArray control={form.control} name='costs' label='Đơn giá vật tư (đ)'>
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
								label='Đơn giá vật tư (đ)'
								placeholder='Nhập đơn giá vật tư (đ)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
