import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
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
	PART_SCHEMA_DEFAULT,
	partSchema,
	PartSchema,
} from '@/features/main/catalog/part/main/schema';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';

export type PartDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	equipmentIds: string[];
	equipmentCodes: string[];
	processGroupIds: string[];
	processGroups: Array<{
		id: string;
		code: string;
		name: string;
	}>;
	replacementTimeStandard: number;
	costs: Array<{
		startMonth: string;
		endMonth: string;
		costType: number;
		amount: number;
		actualAmount: number;
	}>;
};

export function PartForm({ data, row }: ActionDialogProps<Part>) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);
	const [equipments, setEquipments] = useState<Equipment[]>([]);
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);
	const [selectedEquipments, setSelectedEquipments] = useState<
		MultiSelectOption[]
	>([]);

	const form = useForm<PartSchema>({
		resolver: zodResolver(partSchema),
		defaultValues: PART_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	const lastSyncedPlanRef = useRef<Record<number, number>>({});
	const costs = useWatch({
		control: form.control,
		name: 'costs',
	});

	useEffect(() => {
		costs?.forEach((cost, index) => {
			const lastSyncedPlan = lastSyncedPlanRef.current[index];
			const isActualAmountEmpty =
				cost.actualAmount === undefined ||
				cost.actualAmount === null ||
				Number.isNaN(cost.actualAmount);
			const hasPlanAmount =
				cost.amount !== undefined &&
				cost.amount !== null &&
				!Number.isNaN(cost.amount);
			const wasAutoFilled =
				lastSyncedPlan !== undefined && cost.actualAmount === lastSyncedPlan;

			if ((isActualAmountEmpty || wasAutoFilled) && hasPlanAmount) {
				form.setValue(`costs.${index}.actualAmount`, cost.amount, {
					shouldDirty: true,
				});
				lastSyncedPlanRef.current[index] = cost.amount;
			}
		});
	}, [costs, form]);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
			api.pagging<Equipment>(API.CATALOG.EQUIPMENT.LIST),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
		]);

		promises.then(([unitsRes, equipmentsRes, processGroupsRes]) => {
			setUnits(unitsRes.result.data);
			setEquipments(equipmentsRes.result.data);
			setProcessGroups(processGroupsRes.result.data);

			if (!row) return;
			api.get<PartDetail>(API.CATALOG.PART.DETAIL(row.id)).then((res) => {
				const { costs, processGroupIds, equipmentIds, ...part } = res.result;
				const selectedEquipmentsFromAPI = equipmentsRes.result.data
					.filter((equipment) => equipmentIds.includes(equipment.id))
					.map<MultiSelectOption>((equipment) => ({
						label: `${equipment.code} - ${equipment.name}`,
						value: equipment.id,
					}));
				form.reset({
					...part,
					equipmentIds: equipmentIds ?? [],
					processGroupIds: processGroupIds ?? [],
					costs: costs?.length
						? costs.map((cost) => ({
								startMonth: cost.startMonth.substring(0, 10),
								endMonth: cost.endMonth.substring(0, 10),
								amount: cost.amount,
								actualAmount: cost.actualAmount,
							}))
						: PART_SCHEMA_DEFAULT.costs,
				});
				setSelectedEquipments(selectedEquipmentsFromAPI);
			});
		});
	}, [row, form]);

	useEffect(() => {
		form.setValue(
			'equipmentIds',
			selectedEquipments.map((item) => item.value),
		);
	}, [form, selectedEquipments]);

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
			<MultiSelect
				label='Mã Thiết bị'
				placeholder='Chọn mã thiết bị'
				values={selectedEquipments}
				onValuesChange={setSelectedEquipments}
				options={equipments.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			<FormMultiSelect
				control={form.control}
				name='processGroupIds'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={processGroups.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
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

			<FormNumber
				control={form.control}
				name={`replacementTimeStandard`}
				label='Định mức thời gian thay thế (tháng)'
				placeholder='Nhập định mức thời gian thay thế (tháng)'
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
								label='Đơn giá kế hoạch (đ)'
								placeholder='Nhập đơn giá kế hoạch (đ)'
							/>
						</div>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.actualAmount`}
								label='Đơn giá thực tế (đ)'
								placeholder='Nhập đơn giá thực tế (đ)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
