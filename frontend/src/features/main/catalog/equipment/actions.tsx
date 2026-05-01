import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { MultiSelect, MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import { Part } from '@/features/main/catalog/part/main/columns';
import {
	EQUIPMENT_SCHEMA_DEFAULT,
	equipmentSchema,
	EquipmentSchema,
} from '@/features/main/catalog/equipment/schema';
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
	partIds: string[];
	parts: Array<{
		id: string;
		code: string;
		name: string;
		partType: number;
	}>;
};

type EquipmentFormProps = ActionDialogProps<Equipment> & {
	isDuplicate?: boolean;
};

export function EquipmentForm({
	data,
	row,
	isDuplicate = false,
}: EquipmentFormProps) {
	const { breadcrumb } = useMeta();
	const { setOpen } = useDialog();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);
	const [parts, setParts] = useState<Part[]>([]);
	const [otherParts, setOtherParts] = useState<Part[]>([]);
	const [selectedParts, setSelectedParts] = useState<MultiSelectOption[]>([]);
	const [selectedOtherParts, setSelectedOtherParts] = useState<
		MultiSelectOption[]
	>([]);

	const form = useForm<EquipmentSchema>({
		resolver: zodResolver(equipmentSchema),
		mode: 'onSubmit',
		defaultValues: EQUIPMENT_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
			api.pagging<Part>(API.CATALOG.PART.LIST, {
				ignorePagination: true,
				partType: 1,
			}),
			api.pagging<Part>(API.CATALOG.PART.LIST, {
				ignorePagination: true,
				partType: 2,
			}),
		]);

		promises.then(([units, parts, otherParts]) => {
			setUnits(units.result.data);
			setParts(parts.result.data);
			setOtherParts(otherParts.result.data);
			if (row) {
				api
					.get<EquipmentDetail>(API.CATALOG.EQUIPMENT.DETAIL(row.id))
					.then((res) => {
						const {
							costs,
							parts: selectedPartsFromApi,
							partIds,
							...equipment
						} = res.result;
						const selected = (selectedPartsFromApi ?? [])
							.map<MultiSelectOption>((part) => ({
								label: `${part.code} - ${part.name}`,
								value: part.id,
							}))
							.sort((a, b) => a.label.localeCompare(b.label));

						const selectedPartItems = (selectedPartsFromApi ?? [])
							.filter((part) => part.partType !== 2)
							.map<MultiSelectOption>((part) => ({
								label: `${part.code} - ${part.name}`,
								value: part.id,
							}))
							.sort((a, b) => a.label.localeCompare(b.label));

						const selectedOtherPartItems = (selectedPartsFromApi ?? [])
							.filter((part) => part.partType === 2)
							.map<MultiSelectOption>((part) => ({
								label: `${part.code} - ${part.name}`,
								value: part.id,
							}))
							.sort((a, b) => a.label.localeCompare(b.label));
						form.reset({
							...equipment,
							code: isDuplicate ? '' : equipment.code,
							partIds: partIds ?? [],
							costs:
								costs.map((cost) => ({
									startMonth: cost.startMonth.substring(0, 10),
									endMonth: cost.endMonth.substring(0, 10),
									amount: cost.amount,
								})) || EQUIPMENT_SCHEMA_DEFAULT.costs,
						});
						setSelectedParts(
							selectedPartItems.length > 0 ? selectedPartItems : selected,
						);
						setSelectedOtherParts(selectedOtherPartItems);
					});
			}
		});
	}, [row, form, isDuplicate]);

	useEffect(() => {
		form.setValue('partIds', [
			...selectedParts.map((item) => item.value),
			...selectedOtherParts.map((item) => item.value),
		]);
	}, [form, selectedParts, selectedOtherParts]);

	const handleSubmit = async (values: EquipmentSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.EQUIPMENT.UPDATE, {
					id: row?.id,
					...processedValues,
				});
			} else {
				await api.post(API.CATALOG.EQUIPMENT.CREATE, processedValues);
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

			<MultiSelect
				label='Phụ tùng theo thiết bị'
				placeholder='Chọn phụ tùng theo thiết bị'
				values={selectedParts}
				onValuesChange={setSelectedParts}
				options={Object.values(
					(parts ?? []).reduce<
						Record<string, { value: string; label: string }>
					>((acc, item) => {
						if (!item.id || !item.code || acc[item.id]) {
							return acc;
						}
						acc[item.id] = {
							value: item.id,
							label: `${item.code} - ${item.name}`,
						};
						return acc;
					}, {}),
				)}
			/>

			<MultiSelect
				label='Phụ tùng khác'
				placeholder='Chọn phụ tùng khác'
				values={selectedOtherParts}
				onValuesChange={setSelectedOtherParts}
				options={Object.values(
					(otherParts ?? []).reduce<
						Record<string, { value: string; label: string }>
					>((acc, item) => {
						if (!item.id || !item.code || acc[item.id]) {
							return acc;
						}
						acc[item.id] = {
							value: item.id,
							label: `${item.code} - ${item.name}`,
						};
						return acc;
					}, {}),
				)}
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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
