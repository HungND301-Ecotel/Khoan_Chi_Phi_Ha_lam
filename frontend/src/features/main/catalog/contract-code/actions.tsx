import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { MultiSelect, MultiSelectOption } from '@/components/multi-select';
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
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

type Material = {
	id: string;
	code: string;
	name: string;
};

type ContractCodeDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId?: string | null;
	costs?: Array<{
		startMonth: string;
		endMonth: string;
		amount: number;
	}>;
	materials?: Material[];
	otherMaterials?: Material[];
};

type ContractCodeFormProps = ActionDialogProps<ContractCode> & {
	isDuplicate?: boolean;
};

export function ContractCodeForm({
	data,
	row,
	isDuplicate = false,
}: ContractCodeFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);
	const [materials, setMaterials] = useState<Material[]>([]);
	const [selectedMaterials, setSelectedMaterials] = useState<
		MultiSelectOption[]
	>([]);
	const [selectedOtherMaterials, setSelectedOtherMaterials] = useState<
		MultiSelectOption[]
	>([]);

	const form = useForm<ContractCodeSchema>({
		resolver: zodResolver(contractCodeSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					code: isDuplicate ? '' : row.code,
					name: row.name,
					unitOfMeasureId: row.unitOfMeasureId,
					materialIds: [],
					otherMaterialIds: [],
					costs: CONTRACT_CODE_SCHEMA_DEFAULT.costs,
				}
			: CONTRACT_CODE_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
			api.pagging<Material>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
			}),
		]);

		promises.then(([unitsRes, materialsRes]) => {
			setUnits(unitsRes.result.data);
			setMaterials(materialsRes.result.data);

			if (row) {
				api
					.get<ContractCodeDetail>(API.CATALOG.CONTRACT_CODE.DETAIL(row.id))
					.then((detailRes) => {
						const detail = detailRes.result;
						const selected = (detail.materials ?? [])
							.map<MultiSelectOption>((material) => ({
								value: material.id,
								label: `${material.code} - ${material.name}`,
							}))
							.sort((a, b) => a.label.localeCompare(b.label));
						const selectedOther = (detail.otherMaterials ?? [])
							.map<MultiSelectOption>((material) => ({
								value: material.id,
								label: `${material.code} - ${material.name}`,
							}))
							.sort((a, b) => a.label.localeCompare(b.label));

						form.reset({
							code: isDuplicate ? '' : detail.code,
							name: detail.name,
							unitOfMeasureId: detail.unitOfMeasureId ?? null,
							materialIds: selected.map((item) => item.value),
							otherMaterialIds: selectedOther.map((item) => item.value),
							costs: detail.costs?.length
								? detail.costs.map((cost) => ({
										startMonth: cost.startMonth.substring(0, 10),
										endMonth: cost.endMonth.substring(0, 10),
										amount: cost.amount,
									}))
								: CONTRACT_CODE_SCHEMA_DEFAULT.costs,
						});
						setSelectedMaterials(selected);
						setSelectedOtherMaterials(selectedOther);
					});
			}
		});
	}, [form, row, isDuplicate]);

	useEffect(() => {
		form.setValue(
			'materialIds',
			selectedMaterials.map((item) => item.value),
		);
	}, [form, selectedMaterials]);

	useEffect(() => {
		form.setValue(
			'otherMaterialIds',
			selectedOtherMaterials.map((item) => item.value),
		);
	}, [form, selectedOtherMaterials]);

	const allMaterialOptions = useMemo(
		() =>
		Object.values(
			(materials ?? []).reduce<Record<string, MultiSelectOption>>(
				(acc, material) => {
					if (
						!material.id ||
						!material.code ||
						acc[material.id]
					) {
						return acc;
					}

					acc[material.id] = {
						value: material.id,
						label: `${material.code} - ${material.name}`,
					};
					return acc;
				},
				{},
			),
		).sort((a, b) => a.label.localeCompare(b.label)),
		[materials],
	);

	const materialOptions = useMemo(
		() =>
			allMaterialOptions.filter(
				(option) =>
					selectedOtherMaterials.every((item) => item.value !== option.value) ||
					selectedMaterials.some((item) => item.value === option.value),
			),
		[allMaterialOptions, selectedMaterials, selectedOtherMaterials],
	);

	const otherMaterialOptions = useMemo(
		() =>
			allMaterialOptions.filter(
				(option) =>
					selectedMaterials.every((item) => item.value !== option.value) ||
					selectedOtherMaterials.some((item) => item.value === option.value),
			),
		[allMaterialOptions, selectedMaterials, selectedOtherMaterials],
	);

	const handleSubmit = async (values: ContractCodeSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.CONTRACT_CODE.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.CONTRACT_CODE.CREATE, values);
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
				label='Nhóm vật tư, tài sản'
				placeholder='Nhập nhóm vật tư, tài sản, ví dụ: VLN'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên nhóm vật tư, tài sản'
				placeholder='Nhập tên nhóm vật tư, tài sản, ví dụ: Vật liệu nổ'
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
				label='Vật tư, tài sản'
				placeholder='Chọn vật tư, tài sản'
				values={selectedMaterials}
				onValuesChange={setSelectedMaterials}
				options={materialOptions}
			/>

			<MultiSelect
				label='Vật tư, tài sản khác'
				placeholder='Chọn vật tư, tài sản khác'
				values={selectedOtherMaterials}
				onValuesChange={setSelectedOtherMaterials}
				options={otherMaterialOptions}
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
								placeholder='Đơn giá điện năng (đ/kWh)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
