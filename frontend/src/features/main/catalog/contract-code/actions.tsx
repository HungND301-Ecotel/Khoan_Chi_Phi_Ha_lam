import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
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

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

type Material = {
	id: string;
	code: string;
	name: string;
	assignmentCodeId?: string | null;
};

type ContractCodeDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId?: string | null;
	materials?: Material[];
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

	const form = useForm<ContractCodeSchema>({
		resolver: zodResolver(contractCodeSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					code: isDuplicate ? '' : row.code,
					name: row.name,
					unitOfMeasureId: row.unitOfMeasureId,
					materialIds: [],
				}
			: CONTRACT_CODE_SCHEMA_DEFAULT,
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Unit>(API.CATALOG.UNIT.LIST),
			api.pagging<Material>(API.CATALOG.ASSET.LIST),
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

						form.reset({
							code: isDuplicate ? '' : detail.code,
							name: detail.name,
							unitOfMeasureId: detail.unitOfMeasureId ?? null,
							materialIds: selected.map((item) => item.value),
						});
						setSelectedMaterials(selected);
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

	const materialOptions = useMemo(() => {
		const currentAssignmentCodeId = row?.id?.toLowerCase() ?? '';

		return Object.values(
			(materials ?? []).reduce<Record<string, MultiSelectOption>>(
				(acc, material) => {
					const assignmentCodeId = (
						material.assignmentCodeId ?? EMPTY_GUID
					).toLowerCase();
					const canSelect =
						assignmentCodeId === EMPTY_GUID ||
						(currentAssignmentCodeId !== '' &&
							assignmentCodeId === currentAssignmentCodeId);

					if (
						!canSelect ||
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
		).sort((a, b) => a.label.localeCompare(b.label));
	}, [materials, row?.id]);

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

			<MultiSelect
				label='Vật tư liên kết'
				placeholder='Chọn vật tư'
				values={selectedMaterials}
				onValuesChange={setSelectedMaterials}
				options={materialOptions}
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
