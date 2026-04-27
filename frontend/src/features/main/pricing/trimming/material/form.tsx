import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Insert } from '@/features/main/catalog/parameter/insert/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Step } from '@/features/main/catalog/parameter/step/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import {
	MATERIAL_FORM_DEFAULT,
	materialFormSchema,
	type MaterialFormSchema,
} from '@/features/main/pricing/trimming/material/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Material, MaterialDetail } from './type';

export function MaterialForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Material> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [inserts, setInserts] = useState<Insert[]>([]);
	const [steps, setSteps] = useState<Step[]>([]);
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [selectedCodes, setSelectedCodes] = useState<MultiSelectOption[]>([]);

	const prevSelectedCodesRef = useRef<MultiSelectOption[]>([]);

	const form = useForm<MaterialFormSchema, unknown, MaterialFormSchema>({
		resolver: zodResolver(materialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<Insert>(API.CATALOG.PARAMETER.INSERT.LIST),
			api.pagging<Step>(API.CATALOG.PARAMETER.STEP.LIST),
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
		]);

		promises.then(
			([
				processesRes,
				passportsRes,
				strengthsRes,
				insertsRes,
				stepsRes,
				contractsRes,
			]) => {
				setProcesses(processesRes.result.data);
				setPassports(passportsRes.result.data);
				setStrengths(strengthsRes.result.data);
				setInserts(insertsRes.result.data);
				setSteps(stepsRes.result.data);

				const contractsData = contractsRes.result.data;
				setContracts(contractsData);

				if (row) {
					form.reset({
						startMonth: row.startMonth.substring(0, 10),
						endMonth: row.endMonth.substring(0, 10),
						code: isDuplicate ? '' : row.code,
						processId: row.processId,
						passportId: row.passportId,
						hardnessId: row.hardnessId,
						insertItemId: row.insertItemId,
						supportStepId: row.supportStepId,
					});

					api
						.get<MaterialDetail>(API.PRICING.MATERIAL.TRIMMING.DETAIL(row.id))
						.then((res) => {
							const { costs, otherMaterialValue } = res.result;

							const selectedFromAPI = contractsData
								.filter((c) => costs.some((mc) => mc.assignmentCodeId === c.id))
								.map<MultiSelectOption>((c) => ({
									label: `${c.code} - ${c.name}`,
									value: c.id,
								}));

							prevSelectedCodesRef.current = selectedFromAPI;
							setSelectedCodes(selectedFromAPI);

							form.setValue('costs', costs);
							form.setValue('otherMaterialValue', otherMaterialValue);
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

	const handleSubmit = async (values: MaterialFormSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.MATERIAL.TRIMMING.UPDATE, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.PRICING.MATERIAL.TRIMMING.CREATE, values);
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
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức vật liệu'
				placeholder='Nhập mã định mức vật liệu'
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
				name='passportId'
				label='Hộ chiếu, Sđ, Sc'
				placeholder='Chọn hộ chiếu'
				options={passports.map((p) => ({
					label: `H/c ${p.name}; ${p.sd}; ${p.sc}`,
					value: p.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố đá, than (f)'
				placeholder='Chọn Độ kiên cố đá, than (f)'
				options={strengths.map((s) => ({ label: s.value, value: s.id }))}
			/>

			<FormComboBox
				control={form.control}
				name='insertItemId'
				label='Chèn'
				placeholder='Chọn chèn'
				options={inserts.map((i) => ({ label: i.value, value: i.id }))}
			/>

			<FormComboBox
				control={form.control}
				name='supportStepId'
				label='Bước chống'
				placeholder='Chọn bước chống'
				options={steps.map((s) => ({ label: s.value, value: s.id }))}
			/>

			<MultiSelect
				label='Mã giao khoán'
				placeholder='Chọn mã giao khoán'
				values={selectedCodes}
				onValuesChange={setSelectedCodes}
				options={contracts.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			<GroupedMaterialCosts contracts={contracts} />

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function GroupedMaterialCosts({ contracts }: { contracts: ContractCode[] }) {
	const { control } = useFormContext<MaterialFormSchema>();

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
				<ContractCostRow key={index} index={index} contracts={contracts} />
			))}

			<VtkRow />
		</div>
	);
}

function ContractCostRow({
	index,
	contracts,
}: {
	index: number;
	contracts: ContractCode[];
}) {
	const { control, getValues } = useFormContext<MaterialFormSchema>();
	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const contract = contracts.find((c) => c.id === assignmentCodeId);

	return (
		<FormRow>
			<div className='flex flex-1 flex-col gap-2'>
				<Label>Mã giao khoán</Label>
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

function VtkRow() {
	const { control } = useFormContext<MaterialFormSchema>();

	return (
		<FormRow>
			<div className='flex flex-1 flex-col gap-2'>
				<Label>Mã giao khoán</Label>
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
