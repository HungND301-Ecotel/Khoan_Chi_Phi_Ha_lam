import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { NormFactor } from './columns';
import { NORM_FACTOR_SCHEMA_DEFAULT, NormFactorSchema } from './schema';
import { normFactorSchema } from './schema';
import { Strength } from '../parameter/strength/columns';
import { ProcessStep } from '../process/step/columns';
import { ContractCode } from '../contract-code/columns';
import { Clamp } from '../parameter/clamp/columns';

const STEEL_MESH_TYPE_NONE = 1;
const STEEL_MESH_TYPE_SINGLE_LAYER = 2;
const STEEL_MESH_TYPE_DOUBLE_LAYER = 3;
const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

type MaterialCodeItem = {
	id: string;
	code: string;
	name: string;
	assignmentCodeIds?: string[];
};

type NormFactorFormProps = ActionDialogProps<NormFactor> & {
	isDuplicate?: boolean;
};

export function NormFactorForm({
	data,
	row,
	isDuplicate = false,
}: NormFactorFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [productionProcesses, setProductionProcesses] = useState<ProcessStep[]>(
		[],
	);
	const [hardnesses, setHardnesses] = useState<Strength[]>([]);
	const [stoneClampRatios, setStoneClampRatios] = useState<Clamp[]>([]);
	const [assignmentCodes, setAssignmentCodes] = useState<ContractCode[]>([]);
	const [targetHardnesses, setTargetHardnesses] = useState<Strength[]>([]);
	const [materials, setMaterials] = useState<MaterialCodeItem[]>([]);
	const form = useForm<NormFactorSchema>({
		resolver: zodResolver(normFactorSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					...row,
					hardnessId: row.hardnessId ?? '',
					stoneClampRatioId: row.stoneClampRatioId ?? '',
					isMechanizedLongwall:
						(row.steelMeshType ?? STEEL_MESH_TYPE_NONE) !==
						STEEL_MESH_TYPE_NONE,
					steelMeshType: row.steelMeshType ?? STEEL_MESH_TYPE_NONE,
					assignmentCodeIds:
						row.assignmentCodes?.map((item) => item.assignmentCodeId) ??
						(row.affectAssignmentCodes ?? []).map((item) => item.id),
					assignmentCodeConfigs:
						row.assignmentCodes?.map((item) => ({
							assignmentCodeId: item.assignmentCodeId,
							materialId: item.materialId ?? '', // Map materialId từ backend
							value: item.value ?? 0,
							targetHardnessId:
								item.targetHardnessId && item.targetHardnessId !== EMPTY_GUID
									? item.targetHardnessId
									: '',
						})) ??
						(row.affectAssignmentCodes ?? []).map((item) => ({
							assignmentCodeId: item.id,
							materialId: '', // Default rỗng cho case tạo mới
							value: row.value ?? 0,
							targetHardnessId:
								row.targetHardnessId && row.targetHardnessId !== EMPTY_GUID
									? row.targetHardnessId
									: '',
						})),
				}
			: NORM_FACTOR_SCHEMA_DEFAULT,
	});

	const previousSteelMeshTypeRef = useRef<number | undefined>(undefined);

	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
			.then((res) => setGroups(res.result.data));

		api
			.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST)
			.then((res) => setProductionProcesses(res.result.data ?? []));

		api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST).then((res) => {
			setHardnesses(res.result.data ?? []);
			setTargetHardnesses(res.result.data ?? []);
		});

		api
			.pagging<Clamp>(API.CATALOG.PARAMETER.CLAMP.LIST)
			.then((res) => setStoneClampRatios(res.result.data ?? []));

		api
			.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST)
			.then((res) => setAssignmentCodes(res.result.data ?? []));

		api
			.pagging<MaterialCodeItem>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				pageSize: 99999,
			})
			.then((res) => setMaterials(res.result?.data ?? []));
	}, [row, form]);

	const handleSubmit = async (values: NormFactorSchema) => {
		try {
			const steelMeshType = values.isMechanizedLongwall
				? values.steelMeshType
				: STEEL_MESH_TYPE_NONE;
			const stoneClampRatioId = values.stoneClampRatioId || null;
			const hardnessId = values.isMechanizedLongwall
				? null
				: values.hardnessId || null;

			if (!values.isMechanizedLongwall && !hardnessId) {
				form.setError('hardnessId', {
					type: 'manual',
					message: 'Độ kiên cố than đá không được để trống.',
				});
				return;
			}

			const payload = {
				productionProcessId: values.productionProcessId,
				hardnessId,
				stoneClampRatioId,
				steelMeshType,
				assignmentCodes: values.assignmentCodeConfigs.map((config) => ({
					assignmentCodeId: config.assignmentCodeId,
					materialId: config.materialId, // Gửi materialId đi
					value: config.value,
					targetHardnessId: config.targetHardnessId || null,
				})),
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.NORM_FACTOR.UPDATE, {
					id: row?.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.NORM_FACTOR.CREATE, payload);
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

	const selectedGroupId = form.watch('processGroupId');
	const isMechanizedLongwall = form.watch('isMechanizedLongwall');
	const selectedSteelMeshType = form.watch('steelMeshType');
	const selectedHardnessId = form.watch('hardnessId');
	const selectedAssignmentCodeIds =
		useWatch({
			control: form.control,
			name: 'assignmentCodeIds',
		}) ?? [];
	const assignmentCodeConfigs =
		useWatch({
			control: form.control,
			name: 'assignmentCodeConfigs',
		}) ?? [];

	const productionOptions = productionProcesses
		.filter((p) =>
			selectedGroupId ? p.processGroupId === selectedGroupId : true,
		)
		.map((p) => ({ label: `${p.code} - ${p.name}`, value: p.id }));

	const assignmentCodeMap = useMemo(() => {
		return assignmentCodes.reduce<Record<string, ContractCode>>((acc, item) => {
			acc[item.id] = item;
			return acc;
		}, {});
	}, [assignmentCodes]);

	const assignmentConfigRows = useMemo(() => {
		return selectedAssignmentCodeIds.map((assignmentCodeId) => ({
			assignmentCodeId,
			configIndex: assignmentCodeConfigs.findIndex(
				(config) => config.assignmentCodeId === assignmentCodeId,
			),
			assignment: assignmentCodeMap[assignmentCodeId],
		}));
	}, [selectedAssignmentCodeIds, assignmentCodeConfigs, assignmentCodeMap]);

	useEffect(() => {
		const currentConfigs = form.getValues('assignmentCodeConfigs') ?? [];
		const configByAssignmentCodeId = new Map(
			currentConfigs.map((config) => [config.assignmentCodeId, config]),
		);
		const nextConfigs = selectedAssignmentCodeIds.map((assignmentCodeId) => {
			const currentConfig = configByAssignmentCodeId.get(assignmentCodeId);
			if (currentConfig) {
				return currentConfig;
			}

			return {
				assignmentCodeId,
				materialId: '', // Default khi thêm mới
				value:
					isMechanizedLongwall &&
					selectedSteelMeshType === STEEL_MESH_TYPE_SINGLE_LAYER
						? 1
						: isMechanizedLongwall &&
							  selectedSteelMeshType === STEEL_MESH_TYPE_DOUBLE_LAYER
							? 2
							: 0,
				targetHardnessId: '',
			};
		});

		const isChanged =
			nextConfigs.length !== currentConfigs.length ||
			nextConfigs.some((config, index) => {
				const currentConfig = currentConfigs[index];
				return (
					!currentConfig ||
					currentConfig.assignmentCodeId !== config.assignmentCodeId ||
					currentConfig.materialId !== config.materialId ||
					currentConfig.value !== config.value ||
					(currentConfig.targetHardnessId ?? '') !==
						(config.targetHardnessId ?? '')
				);
			});

		if (isChanged) {
			form.setValue('assignmentCodeConfigs', nextConfigs, {
				shouldValidate: true,
			});
		}
	}, [
		form,
		selectedAssignmentCodeIds,
		isMechanizedLongwall,
		selectedSteelMeshType,
	]);

	useEffect(() => {
		if (!isMechanizedLongwall) {
			previousSteelMeshTypeRef.current = selectedSteelMeshType;
			return;
		}

		if (
			previousSteelMeshTypeRef.current === undefined ||
			previousSteelMeshTypeRef.current === selectedSteelMeshType
		) {
			previousSteelMeshTypeRef.current = selectedSteelMeshType;
			return;
		}

		if (selectedSteelMeshType === STEEL_MESH_TYPE_SINGLE_LAYER) {
			const nextConfigs = (form.getValues('assignmentCodeConfigs') ?? []).map(
				(config) => ({
					...config,
					value: 1,
				}),
			);
			form.setValue('assignmentCodeConfigs', nextConfigs);
		}

		if (selectedSteelMeshType === STEEL_MESH_TYPE_DOUBLE_LAYER) {
			const nextConfigs = (form.getValues('assignmentCodeConfigs') ?? []).map(
				(config) => ({
					...config,
					value: 2,
				}),
			);
			form.setValue('assignmentCodeConfigs', nextConfigs);
		}

		previousSteelMeshTypeRef.current = selectedSteelMeshType;
	}, [isMechanizedLongwall, selectedSteelMeshType, hardnesses, form]);

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn'
				placeholder='Chọn nhóm công đoạn'
				options={groups.map((group) => ({
					label: group.name,
					value: group.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='productionProcessId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={productionOptions}
			/>

			<div className='flex items-center gap-2'>
				<Checkbox
					id='is-mechanized-longwall'
					checked={isMechanizedLongwall}
					onCheckedChange={(checked) => {
						const isChecked = !!checked;
						form.setValue('isMechanizedLongwall', isChecked);

						if (isChecked) {
							const currentSteelMeshType = form.getValues('steelMeshType');
							if (
								!currentSteelMeshType ||
								currentSteelMeshType === STEEL_MESH_TYPE_NONE
							) {
								form.setValue('steelMeshType', STEEL_MESH_TYPE_SINGLE_LAYER);
							}
							form.setValue('hardnessId', '');
							form.clearErrors('hardnessId');
							previousSteelMeshTypeRef.current = STEEL_MESH_TYPE_NONE;
						} else {
							form.setValue('steelMeshType', STEEL_MESH_TYPE_NONE);
						}
					}}
				/>
				<label
					htmlFor='is-mechanized-longwall'
					className='cursor-pointer text-sm leading-none font-medium'
				>
					Lò chợ cơ giới hóa (CGH)
				</label>
			</div>

			{isMechanizedLongwall ? (
				<FormComboBox
					control={form.control}
					name='steelMeshType'
					label='Chọn lớp lưới thép'
					placeholder='Chọn lớp lưới thép'
					options={[
						{
							label: 'Trải 1 lớp lưới thép',
							value: STEEL_MESH_TYPE_SINGLE_LAYER,
						},
						{
							label: 'Trải 2 lớp lưới thép',
							value: STEEL_MESH_TYPE_DOUBLE_LAYER,
						},
					]}
				/>
			) : (
				<FormComboBox
					control={form.control}
					name='hardnessId'
					label='Độ kiên cố than đá (f)'
					placeholder='Chọn độ kiên cố'
					options={hardnesses.map((h) => ({ label: h.value, value: h.id }))}
				/>
			)}

			<FormComboBox
				control={form.control}
				name='stoneClampRatioId'
				label='Tỷ lệ đá kẹp (Ckẹp)'
				placeholder='Chọn tỷ lệ đá kẹp (Ckẹp)'
				options={stoneClampRatios.map((s) => ({ label: s.value, value: s.id }))}
			/>

			<FormMultiSelect
				control={form.control}
				name='assignmentCodeIds'
				label='Thành phần điều chỉnh định mức (Chọn Nhóm)'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((a) => ({
					label: `${a.code} - ${a.name}`,
					value: a.id,
				}))}
			/>

			{assignmentConfigRows.length > 0 && (
				<div className='flex w-full flex-col gap-4'>
					<FormSeparator />

					{assignmentConfigRows.map((rowItem) => {
						if (rowItem.configIndex < 0) {
							return null;
						}

						// Lọc Material theo AssignmentCode
						const validMaterials = materials.filter((m) => {
							if (!m.assignmentCodeIds || m.assignmentCodeIds.length === 0)
								return true;
							return m.assignmentCodeIds.includes(rowItem.assignmentCodeId);
						});
						return (
							<FormRow key={rowItem.assignmentCodeId}>
								<div className='flex flex-1 flex-col gap-2'>
									<Label>Nhóm vật tư, tài sản</Label>
									<Input
										readOnly
										value={
											rowItem.assignment
												? `${rowItem.assignment.code} - ${rowItem.assignment.name}`
												: ''
										}
										className='read-only:cursor-not-allowed read-only:bg-gray-100'
									/>
								</div>

								<div className='flex flex-1 flex-col gap-2'>
									<FormComboBox
										control={form.control}
										name={
											`assignmentCodeConfigs.${rowItem.configIndex}.materialId` as keyof NormFactorSchema
										}
										label='Vật tư, tài sản'
										placeholder='Chọn vật tư'
										options={[
											...validMaterials.map((m) => ({
												label: `${m.code} - ${m.name}`,
												value: m.id,
											})),
										]}
									/>
								</div>

								<div className='flex min-w-[120px] flex-1 flex-col gap-2'>
									<FormNumber
										control={form.control}
										name={
											`assignmentCodeConfigs.${rowItem.configIndex}.value` as keyof NormFactorSchema
										}
										label='Hệ số điều chỉnh'
										placeholder='Nhập hệ số'
									/>
								</div>

								<div className='flex min-w-[180px] flex-1 flex-col gap-2'>
									<FormComboBox
										control={form.control}
										name={
											`assignmentCodeConfigs.${rowItem.configIndex}.targetHardnessId` as keyof NormFactorSchema
										}
										label='Định mức tham chiếu'
										placeholder='Định mức hiện tại'
										options={[
											{ label: 'Định mức hiện tại', value: '' },
											...targetHardnesses
												.filter((r) => r.id !== selectedHardnessId)
												.map((r) => ({
													label: r.value,
													value: r.id,
												})),
										]}
									/>
								</div>
							</FormRow>
						);
					})}
				</div>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
