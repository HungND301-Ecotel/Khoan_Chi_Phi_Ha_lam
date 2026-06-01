import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { Technology } from '@/features/main/catalog/parameter/technology/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import {
	SUPPORT_AND_DRILLING_FORM_DEFAULT,
	supportAndDrillingFormSchema,
	type SupportAndDrillingFormSchema,
} from './support-and-drilling-schema';
import {
	SupportAndDrillingMaterial,
	SupportAndDrillingMaterialDetail,
} from './type';

export function SupportAndDrillingForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<SupportAndDrillingMaterial> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [technologies, setTechnologies] = useState<Technology[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [selectedCodes, setSelectedCodes] = useState<MultiSelectOption[]>([]);
	const prevSelectedCodesRef = useRef<MultiSelectOption[]>([]);

	const form = useForm<SupportAndDrillingFormSchema>({
		resolver: zodResolver(supportAndDrillingFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...SUPPORT_AND_DRILLING_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
			api.pagging<Technology>(API.CATALOG.PARAMETER.TECHNOLOGY.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
		]);

		promises.then(
			([
				processesRes,
				technologiesRes,
				passportsRes,
				strengthsRes,
				contractsRes,
			]) => {
				setProcesses(processesRes.result.data);
				setTechnologies(technologiesRes.result.data);
				setPassports(passportsRes.result.data);
				setStrengths(strengthsRes.result.data);
				const contractsData = contractsRes.result.data;
				setContracts(contractsData);

				if (row) {
					api
						.get<SupportAndDrillingMaterialDetail>(
							API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DETAIL(row.id),
						)
						.then((res) => {
							const detail = res.result;

							const selectedFromApi = contractsData
								.filter((c) =>
									(detail.costs ?? []).some(
										(mc) => mc.assignmentCodeId === c.id,
									),
								)
								.map<MultiSelectOption>((c) => ({
									label: `${c.code} - ${c.name}`,
									value: c.id,
								}));

							prevSelectedCodesRef.current = selectedFromApi;
							setSelectedCodes(selectedFromApi);

							form.reset({
								startMonth: detail.startMonth.substring(0, 10),
								endMonth: detail.endMonth.substring(0, 10),
								code: isDuplicate ? '' : detail.code,
								processId: detail.processId,
								technologyId: detail.technologyId ?? '',
								passportId: detail.passportId,
								hardnessId: detail.hardnessId,
								costs: detail.costs ?? [],
								otherMaterialValue: detail.otherMaterialValue,
							});
						});
				}
			},
		);
	}, [form, isDuplicate, row]);

	useEffect(() => {
		const prevContracts = prevSelectedCodesRef.current;
		const prevIds = new Set(prevContracts.map((c) => c.value));
		const currentIds = new Set(selectedCodes.map((c) => c.value));

		const addedIds = selectedCodes
			.filter((c) => !prevIds.has(c.value))
			.map((c) => c.value);

		const removedIds = prevContracts
			.filter((c) => !currentIds.has(c.value))
			.map((c) => c.value);

		const currentCosts = form.getValues('costs') || [];

		let updatedCosts = currentCosts.filter(
			(cost) => !removedIds.includes(cost.assignmentCodeId),
		);

		const newCosts = addedIds.map((id) => ({
			assignmentCodeId: id,
			totalPrice: NaN,
		}));

		updatedCosts = [...updatedCosts, ...newCosts].sort((a, b) => {
			const aCode =
				contracts.find((c) => c.id === a.assignmentCodeId)?.code ?? '';
			const bCode =
				contracts.find((c) => c.id === b.assignmentCodeId)?.code ?? '';
			return aCode.localeCompare(bCode);
		});

		form.setValue('costs', updatedCosts);
		prevSelectedCodesRef.current = selectedCodes;
	}, [selectedCodes, contracts, form]);

	const handleSubmit = async (values: SupportAndDrillingFormSchema) => {
		try {
			const payload = {
				code: values.code,
				processId: values.processId,
				passportId: values.passportId,
				hardnessId: values.hardnessId,
				startMonth: values.startMonth,
				endMonth: values.endMonth,
				otherMaterialValue: values.otherMaterialValue ?? 0,
				costs: values.costs,
				technologyId: values.technologyId,
			};

			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.UPDATE, {
					id: row.id,
					...payload,
				});
			} else {
				await api.post(
					API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.CREATE,
					payload,
				);
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
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Tháng bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Tháng kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã đơn giá'
				placeholder='Nhập mã đơn giá'
			/>

			<FormComboBox
				control={form.control}
				name='processId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={processes.map((p) => ({ label: p.name, value: p.id }))}
			/>

			<FormComboBox
				control={form.control}
				name='technologyId'
				label='Công nghệ'
				placeholder='Chọn công nghệ'
				options={technologies.map((t) => ({ label: t.value, value: t.id }))}
			/>

			<FormComboBox
				control={form.control}
				name='passportId'
				label='Hộ chiếu'
				placeholder='Chọn hộ chiếu'
				options={passports.map((p) => ({
					label: `H/c ${p.name}; ${p.sd}; ${p.sc}`,
					value: p.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố than đá'
				placeholder='Chọn độ kiên cố than đá'
				options={strengths.map((s) => ({ label: s.value, value: s.id }))}
			/>

			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				values={selectedCodes}
				onValuesChange={setSelectedCodes}
				options={contracts.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			<GroupedSupportAndDrillingCosts contracts={contracts} />

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function GroupedSupportAndDrillingCosts({
	contracts,
}: {
	contracts: ContractCode[];
}) {
	const { control } = useFormContext<SupportAndDrillingFormSchema>();

	const costs = useWatch({ control, name: 'costs' }) ?? [];
	const otherMaterialValue = useWatch({ control, name: 'otherMaterialValue' });

	if (costs.length === 0) return null;

	const sumCosts = costs.reduce((acc, c) => {
		const v = Number(c.totalPrice);
		return acc + (isNaN(v) ? 0 : v);
	}, 0);

	const vtkValue = isNaN(Number(otherMaterialValue))
		? 0
		: Number(otherMaterialValue);

	const total = sumCosts + vtkValue;

	return (
		<div className='flex w-full flex-col gap-4'>
			<FormSeparator />

			<div className='flex flex-col gap-2'>
				<Label>Tổng tiền (đ/m)</Label>
				<Input
					readOnly
					value={formatNumber(total)}
					className='font-semibold read-only:bg-transparent'
				/>
			</div>

			{costs.map((_, index) => (
				<SupportAndDrillingCostRow
					key={index}
					index={index}
					contracts={contracts}
				/>
			))}

			<SupportAndDrillingVtkRow />
		</div>
	);
}

function SupportAndDrillingCostRow({
	index,
	contracts,
}: {
	index: number;
	contracts: ContractCode[];
}) {
	const { control, getValues } = useFormContext<SupportAndDrillingFormSchema>();
	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const contract = contracts.find((c) => c.id === assignmentCodeId);

	return (
		<FormRow>
			<div className='flex flex-1 flex-col gap-2'>
				<Label>Nhóm vật tư, tài sản</Label>
				<Input
					readOnly
					value={contract ? `${contract.code} - ${contract.name}` : ''}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name={`costs.${index}.totalPrice`}
					label='Đơn giá vật liệu (đ/m)'
					placeholder='Nhập đơn giá vật liệu'
				/>
			</div>
		</FormRow>
	);
}

function SupportAndDrillingVtkRow() {
	const { control } = useFormContext<SupportAndDrillingFormSchema>();

	return (
		<FormRow>
			<div className='flex flex-1 flex-col gap-2'>
				<Label>Nhóm vật tư, tài sản</Label>
				<Input
					readOnly
					value='VTK - Vật tư khác'
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name='otherMaterialValue'
					label='Đơn giá vật liệu (đ/m)'
					placeholder='Nhập đơn giá vật liệu'
				/>
			</div>
		</FormRow>
	);
}
