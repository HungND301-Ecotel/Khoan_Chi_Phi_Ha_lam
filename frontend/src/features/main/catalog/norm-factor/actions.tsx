import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { NormFactor } from './columns';
import { NORM_FACTOR_SCHEMA_DEFAULT, NormFactorSchema } from './schema';
import { normFactorSchema } from './schema';
import { Strength } from '../parameter/strength/columns';
import { ProcessStep } from '../process/step/columns';
import { ContractCode } from '../contract-code/columns';
import { Clamp } from '../parameter/clamp/columns';

export function NormFactorForm({ data, row }: ActionDialogProps<NormFactor>) {
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

	const form = useForm<NormFactorSchema>({
		resolver: zodResolver(normFactorSchema),
		mode: 'onSubmit',
		defaultValues: row
			? {
					...row,
					assignmentCodeIds: row.affectAssignmentCodeIds ?? [],
				}
			: NORM_FACTOR_SCHEMA_DEFAULT,
	});

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
	}, [row, form]);

	// Ensure reference dropdown defaults to 'Định mức hiện tại' when editing
	// if the referenced factor cannot be mapped to available referenceFactors.
	useEffect(() => {
		if (!row) return;
		const refId = row.targetHardnessId;
		if (!refId) {
			form.setValue('targetHardnessId', '');
			return;
		}
		// treat all-zero guid or missing in fetched list as unmapped
		const isAllZeroGuid = refId === '00000000-0000-0000-0000-000000000000';
		const found = targetHardnesses.some((r) => r.id === refId);
		if (isAllZeroGuid || !found) {
			form.setValue('targetHardnessId', '');
		} else {
			form.setValue('targetHardnessId', refId);
		}
	}, [targetHardnesses, row, form]);

	const handleSubmit = async (values: NormFactorSchema) => {
		try {
			const payload = {
				...values,
				targetHardnessId: values.targetHardnessId || null,
			};

			if (row?.id) {
				await api.put(API.CATALOG.NORM_FACTOR.UPDATE, {
					id: row?.id,
					...payload,
				});
			} else {
				await api.post(API.CATALOG.NORM_FACTOR.CREATE, payload);
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

	const selectedGroupId = form.watch('processGroupId');

	const productionOptions = productionProcesses
		.filter((p) =>
			selectedGroupId ? p.processGroupId === selectedGroupId : true,
		)
		.map((p) => ({ label: `${p.code} - ${p.name}`, value: p.id }));

	const selectedHardnessId = form.watch('hardnessId');

	useEffect(() => {
		const currentTargetHardnessId = form.getValues('targetHardnessId');
		if (
			selectedHardnessId &&
			currentTargetHardnessId &&
			currentTargetHardnessId === selectedHardnessId
		) {
			form.setValue('targetHardnessId', '');
		}
	}, [selectedHardnessId, form]);

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

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố than đá (f)'
				placeholder='Chọn độ kiên cố'
				options={hardnesses.map((h) => ({ label: h.value, value: h.id }))}
			/>

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
				label='Thành phần điều chỉnh định mức'
				placeholder='Chọn thành phần điều chỉnh định mức'
				options={assignmentCodes.map((a) => ({
					label: `${a.code} - ${a.name}`,
					value: a.id,
				}))}
			/>

			<div className='grid grid-cols-2 gap-4'>
				<FormNumber
					control={form.control}
					name='value'
					label='Hệ số điều chỉnh định mức'
					placeholder='Nhập hệ số'
				/>

				<FormComboBox
					control={form.control}
					name='targetHardnessId'
					label='Định mức tham chiếu'
					placeholder='Chọn định mức tham chiếu'
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

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
