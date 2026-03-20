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
import type { Longwallparameters } from '@/features/main/catalog/parameter/longwallparameters/columns';
import type { Seamface } from '@/features/main/catalog/parameter/seamface/columns';
import type { Cuttingthickness } from '@/features/main/catalog/parameter/cuttingthickness/columns';
import type { LongwallMaterial } from '@/features/main/pricing/longwall-panel/material/columns';
import {
	LONGWALL_MATERIAL_FORM_DEFAULT,
	longwallMaterialFormSchema,
	type LongwallMaterialFormSchema,
} from '@/features/main/pricing/longwall-panel/material/schema';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { ProcessStep } from '@/features/main/catalog/process/step/columns';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';

interface Technology {
	id: string;
	value: string;
}

interface LongwallMaterialDetail {
	id: string;
	code: string;
	longwallParameters: { id: string; llc: string; lkc: number; mk: number };
	cuttingThickness: { id: string; from: string; to: string };
	seamFaceId?: string;
	technologyId?: string;
	processId: string;
	startMonth: string;
	endMonth: string;
	costs: Array<{
		assignmentCodeId: string;
		totalPrice: number;
	}>;
	otherMaterialValue?: number;
}

interface LongwallMaterialFormProps
	extends ActionDialogProps<LongwallMaterial> {}

export function LongwallMaterialForm({ data, row }: LongwallMaterialFormProps) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [longwallParameters, setLongwallParameters] = useState<
		Longwallparameters[]
	>([]);
	const [technologies, setTechnologies] = useState<Technology[]>([]);
	const [seamfaces, setSeamfaces] = useState<Seamface[]>([]);
	const [cuttingthicknesses, setCuttingthicknesses] = useState<
		Cuttingthickness[]
	>([]);
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [selectedCodes, setSelectedCodes] = useState<MultiSelectOption[]>([]);

	const prevSelectedCodesRef = useRef<MultiSelectOption[]>([]);

	const form = useForm<
		LongwallMaterialFormSchema,
		unknown,
		LongwallMaterialFormSchema
	>({
		resolver: zodResolver(longwallMaterialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...LONGWALL_MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	useEffect(() => {
		const loadData = async () => {
			try {
				const [
					techRes,
					longwallRes,
					cuttingRes,
					seamRes,
					processRes,
					contractsRes,
				] = await Promise.all([
					api.pagging<Technology>(API.CATALOG.PARAMETER.TECHNOLOGY.LIST),
					api.pagging<Longwallparameters>(
						API.CATALOG.PARAMETER.LONGWALLPARAMETERS.LIST,
					),
					api.pagging<Cuttingthickness>(
						API.CATALOG.PARAMETER.CUTTINGTHICKNESS.LIST,
					),
					api.pagging<Seamface>(API.CATALOG.PARAMETER.SEAMFACE.LIST),
					api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
					api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
				]);

				setTechnologies(techRes.result.data);
				setLongwallParameters(longwallRes.result.data);
				setCuttingthicknesses(cuttingRes.result.data);
				setSeamfaces(seamRes.result.data);
				setProcesses(processRes.result.data);

				const contractsData = contractsRes.result.data;
				setContracts(contractsData);

				if (row?.id) {
					try {
						const detailRes = await api.get<LongwallMaterialDetail>(
							API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(row.id),
						);
						const detail = detailRes.result;

						// Populate basic fields
						form.reset({
							id: detail.id,
							code: detail.code,
							longwallParametersId: detail.longwallParameters?.id || '',
							cuttingThicknessId: detail.cuttingThickness?.id || '',
							seamFaceId: detail.seamFaceId || row.seamFaceId || '',
							technologyId: detail.technologyId || row.technologyId || '',
							processId: detail.processId || row.processId || '',
							startMonth: detail.startMonth.substring(0, 10),
							endMonth: detail.endMonth.substring(0, 10),
						});

						// Populate costs + contracts
						const selectedFromAPI = contractsData
							.filter((c) =>
								detail.costs.some((mc) => mc.assignmentCodeId === c.id),
							)
							.map<MultiSelectOption>((c) => ({
								label: `${c.code} - ${c.name}`,
								value: c.id,
							}));

						prevSelectedCodesRef.current = selectedFromAPI;
						setSelectedCodes(selectedFromAPI);

						form.setValue('costs', detail.costs);
						form.setValue('otherMaterialValue', detail.otherMaterialValue);
					} catch {
						// Fallback to row data if detail fails
						form.reset({
							id: row.id,
							code: row.code,
							longwallParametersId: row.longwallParametersId || '',
							cuttingThicknessId: row.cuttingthicknessId || '',
							seamFaceId: row.seamFaceId || '',
							technologyId: row.technologyId || '',
							processId: row.processId || '',
							startMonth: row.startMonth.substring(0, 10),
							endMonth: row.endMonth.substring(0, 10),
						});
					}
				}
			} catch (error) {
				popup.error(error);
			}
		};

		loadData();
	}, [row?.id]);

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

	const handleSubmit = async (values: LongwallMaterialFormSchema) => {
		try {
			const payload = {
				code: values.code,
				longwallParametersId: values.longwallParametersId,
				cuttingThicknessId: values.cuttingThicknessId,
				seamFaceId: values.seamFaceId,
				technologyId: values.technologyId,
				processId: values.processId,
				startMonth: values.startMonth,
				endMonth: values.endMonth,
				costs: values.costs,
				otherMaterialValue: values.otherMaterialValue,
			};

			if (row?.id) {
				await api.put(API.PRICING.MATERIAL.LONGWALL_PANEL.UPDATE, {
					id: row.id,
					...payload,
				});
				popup.success(`${breadcrumb} đã được cập nhật thành công.`);
			} else {
				await api.post(API.PRICING.MATERIAL.LONGWALL_PANEL.CREATE, payload);
				popup.success(`${breadcrumb} đã được tạo mới thành công.`);
			}

			setOpen(false);
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
				name='technologyId'
				label='Công nghệ khai thác'
				placeholder='Chọn công nghệ khai thác'
				options={technologies.map((t) => ({ label: t.value, value: t.id }))}
			/>

			<FormComboBox
				control={form.control}
				name='longwallParametersId'
				label='Thông số lò chợ'
				placeholder='Chọn thông số lò chợ'
				options={longwallParameters.map((p) => ({
					label: `${p.llc}; ${p.lkc}; ${p.mk}`,
					value: p.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='cuttingThicknessId'
				label='Chiều dày lớp khấu (m)'
				placeholder='Chọn chiều dày lớp khấu (m)'
				options={cuttingthicknesses.map((ct) => ({
					label: ct.value,
					value: ct.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='seamFaceId'
				label='Mặt vỉa (m)'
				placeholder='Chọn mặt vỉa (m)'
				options={seamfaces.map((s) => ({ label: s.value, value: s.id }))}
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

			<DataTableEditConfirm isEdit={!!row?.id} />
		</FormProvider>
	);
}

function GroupedMaterialCosts({ contracts }: { contracts: ContractCode[] }) {
	const { control } = useFormContext<LongwallMaterialFormSchema>();

	const costs = useWatch({ control, name: 'costs' }) ?? [];
	const otherMaterialValue = useWatch({ control, name: 'otherMaterialValue' });

	if (costs.length === 0) return null;

	const sumCosts = costs.reduce((acc, c) => {
		const v = Number(c.totalPrice);
		return acc + (isNaN(v) ? 0 : v);
	}, 0);

	const vtkValue = isNaN(Number(otherMaterialValue))
		? 0
		: (Number(otherMaterialValue) / 100) * sumCosts;

	const total = sumCosts + vtkValue;

	return (
		<div className='flex w-full flex-col gap-4'>
			<FormSeparator />

			<div className='flex flex-col gap-2'>
				<Label>Tổng tiền (đ/tấn)</Label>
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
	const { control, getValues } = useFormContext<LongwallMaterialFormSchema>();
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
					label='Đơn giá vật liệu (đ/tấn)'
					placeholder='Nhập đơn giá vật liệu'
				/>
			</div>
		</FormRow>
	);
}

function VtkRow() {
	const { control } = useFormContext<LongwallMaterialFormSchema>();

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
					label='Tỷ lệ VTK (%)'
					placeholder='Nhập tỷ lệ VTK'
				/>
			</div>
		</FormRow>
	);
}
