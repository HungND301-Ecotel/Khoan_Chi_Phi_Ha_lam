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
import { Checkbox } from '@/components/ui/checkbox';
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
import { InputGroup, InputGroupInput } from '@/components/ui/input-group';
import { NumericFormat } from 'react-number-format';

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

	// Interpolation state
	const [useInterpolation, setUseInterpolation] = useState(false);
	const [interpolationPoint, setInterpolationPoint] = useState<
		number | undefined
	>(undefined);
	const [upperNorms, setUpperNorms] = useState<LongwallMaterial[]>([]);
	const [selectedUpperNormId, setSelectedUpperNormId] = useState<string>('');
	const [selectedLowerNormId, setSelectedLowerNormId] = useState<string>('');
	const [upperPoint, setUpperPoint] = useState<number | undefined>(undefined);
	const [lowerPoint, setLowerPoint] = useState<number | undefined>(undefined);

	const prevSelectedCodesRef = useRef<MultiSelectOption[]>([]);
	// Track whether costs were set by interpolation to avoid overwriting
	const interpolationAppliedRef = useRef(false);

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
					upperNormsRes,
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
					api.pagging<LongwallMaterial>(
						API.PRICING.MATERIAL.LONGWALL_PANEL.LIST,
					),
				]);

				setTechnologies(techRes.result.data);
				setLongwallParameters(longwallRes.result.data);
				setCuttingthicknesses(cuttingRes.result.data);
				setSeamfaces(seamRes.result.data);
				setProcesses(processRes.result.data);
				setUpperNorms(upperNormsRes.result.data);

				const contractsData = contractsRes.result.data;
				setContracts(contractsData);

				if (row?.id) {
					try {
						const detailRes = await api.get<LongwallMaterialDetail>(
							API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(row.id),
						);
						const detail = detailRes.result;

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
							otherMaterialValue: 0,
						});

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
					} catch {
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
							otherMaterialValue: 0,
						});
					}
				}
			} catch (error) {
				popup.error(error);
			}
		};

		loadData();
	}, [row?.id]);

	// Effect to apply interpolation when all required values are set
	useEffect(() => {
		if (!useInterpolation) return;
		if (!selectedUpperNormId || !selectedLowerNormId) return;
		if (upperPoint === undefined || lowerPoint === undefined) return;
		if (interpolationPoint === undefined) return;
		if (upperPoint <= lowerPoint) return;

		const upperNorm = upperNorms.find((n) => n.id === selectedUpperNormId);
		const lowerNorm = upperNorms.find((n) => n.id === selectedLowerNormId);
		if (!upperNorm || !lowerNorm) return;

		const upperIds = new Set(
			(upperNorm.costs ?? []).map((c) => c.assignmentCodeId),
		);
		const lowerIds = new Set(
			(lowerNorm.costs ?? []).map((c) => c.assignmentCodeId),
		);
		const allIds = new Set([...upperIds, ...lowerIds]);

		// Block if any id is not shared between both norms
		for (const id of allIds) {
			if (!upperIds.has(id) || !lowerIds.has(id)) return;
		}

		const upperCostMap = new Map(
			(upperNorm.costs ?? []).map((c) => [c.assignmentCodeId, c.totalPrice]),
		);
		const lowerCostMap = new Map(
			(lowerNorm.costs ?? []).map((c) => [c.assignmentCodeId, c.totalPrice]),
		);

		const newCosts = Array.from(allIds).map((id) => {
			const inBoth = upperCostMap.has(id) && lowerCostMap.has(id);
			if (inBoth) {
				const yUpper = upperCostMap.get(id)!;
				const yLower = lowerCostMap.get(id)!;
				// Linear interpolation
				const totalPrice =
					yLower +
					((interpolationPoint - lowerPoint) / (upperPoint - lowerPoint)) *
						(yUpper - yLower);
				return { assignmentCodeId: id, totalPrice };
			}
			return { assignmentCodeId: id, totalPrice: 0 };
		});

		// Sort by contract code
		newCosts.sort((a, b) => {
			const aCode =
				contracts.find((c) => c.id === a.assignmentCodeId)?.code ?? '';
			const bCode =
				contracts.find((c) => c.id === b.assignmentCodeId)?.code ?? '';
			return aCode.localeCompare(bCode);
		});

		// Update MultiSelect options
		const newSelectedCodes = Array.from(allIds)
			.map((id) => {
				const contract = contracts.find((c) => c.id === id);
				if (!contract) return null;
				return { label: `${contract.code} - ${contract.name}`, value: id };
			})
			.filter(Boolean) as MultiSelectOption[];

		interpolationAppliedRef.current = true;
		prevSelectedCodesRef.current = newSelectedCodes;
		setSelectedCodes(newSelectedCodes);
		form.setValue('costs', newCosts);
	}, [
		useInterpolation,
		selectedUpperNormId,
		selectedLowerNormId,
		upperPoint,
		lowerPoint,
		interpolationPoint,
		upperNorms,
		contracts,
	]);

	// Effect to handle manual MultiSelect changes (non-interpolation path)
	useEffect(() => {
		// If interpolation just applied, skip this run
		if (interpolationAppliedRef.current) {
			interpolationAppliedRef.current = false;
			return;
		}

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
				otherMaterialValue: 0,
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

	// Norm options excluding the one already selected as the other
	const upperNormOptions = upperNorms.filter(
		(n) => n.id !== selectedLowerNormId,
	);
	const lowerNormOptions = upperNorms.filter(
		(n) => n.id !== selectedUpperNormId,
	);

	// Check for mismatched assignmentCodeIds between the two selected norms
	const mismatchedCodes: string[] = (() => {
		if (!selectedUpperNormId || !selectedLowerNormId) return [];
		const upperNorm = upperNorms.find((n) => n.id === selectedUpperNormId);
		const lowerNorm = upperNorms.find((n) => n.id === selectedLowerNormId);
		if (!upperNorm || !lowerNorm) return [];

		const upperIds = new Set(
			(upperNorm.costs ?? []).map((c) => c.assignmentCodeId),
		);
		const lowerIds = new Set(
			(lowerNorm.costs ?? []).map((c) => c.assignmentCodeId),
		);
		const allIds = new Set([...upperIds, ...lowerIds]);
		const mismatched: string[] = [];

		for (const id of allIds) {
			if (!upperIds.has(id) || !lowerIds.has(id)) {
				const contract = contracts.find((c) => c.id === id);
				mismatched.push(contract ? `${contract.code} - ${contract.name}` : id);
			}
		}
		return mismatched;
	})();

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

			{/* Interpolation checkbox */}
			<div className='flex items-center gap-2'>
				<Checkbox
					id='use-interpolation'
					checked={useInterpolation}
					onCheckedChange={(checked) => {
						setUseInterpolation(!!checked);
						if (!checked) {
							// Reset interpolation state
							setSelectedUpperNormId('');
							setSelectedLowerNormId('');
							setUpperPoint(undefined);
							setLowerPoint(undefined);
							setInterpolationPoint(undefined);
						}
					}}
				/>
				<label
					htmlFor='use-interpolation'
					className='cursor-pointer text-sm leading-none font-medium peer-disabled:cursor-not-allowed peer-disabled:opacity-70'
				>
					Tạo định mức bảng phương pháp nội suy
				</label>
			</div>

			{/* Interpolation panel */}
			{useInterpolation && (
				<div className='flex flex-col gap-4 rounded-md border p-4'>
					{/* Điểm nội suy */}
					<div className='flex flex-col gap-2'>
						<Label>Điểm nội suy</Label>
						<InputGroup>
							<NumericFormat
								decimalSeparator=','
								thousandSeparator='.'
								placeholder='Nhập điểm nội suy'
								value={interpolationPoint ?? ''}
								onValueChange={(v) => setInterpolationPoint(v.floatValue)}
								customInput={InputGroupInput}
								type='text'
								inputMode='decimal'
							/>
						</InputGroup>
					</div>

					{/* Định mức cận trên + Điểm cận trên */}
					<div className='grid grid-cols-2 gap-4'>
						<FormComboBox
							label='Định mức cận trên'
							placeholder='Chọn định mức cận trên'
							value={selectedUpperNormId}
							onValueChange={setSelectedUpperNormId}
							options={upperNormOptions.map((n) => ({
								label: n.code,
								value: n.id,
							}))}
						/>

						<div className='flex flex-col gap-2'>
							<Label>Điểm cận trên</Label>
							<InputGroup>
								<NumericFormat
									decimalSeparator=','
									thousandSeparator='.'
									placeholder='Nhập điểm cận trên'
									value={upperPoint ?? ''}
									onValueChange={(v) => setUpperPoint(v.floatValue)}
									customInput={InputGroupInput}
									type='text'
									inputMode='decimal'
								/>
							</InputGroup>
							{upperPoint !== undefined &&
								lowerPoint !== undefined &&
								upperPoint <= lowerPoint && (
									<p className='text-destructive text-xs'>
										Điểm cận trên phải lớn hơn điểm cận dưới
									</p>
								)}
						</div>
					</div>

					{/* Định mức cận dưới + Điểm cận dưới */}
					<div className='grid grid-cols-2 gap-4'>
						<FormComboBox
							label='Định mức cận dưới'
							placeholder='Chọn định mức cận dưới'
							value={selectedLowerNormId}
							onValueChange={setSelectedLowerNormId}
							options={lowerNormOptions.map((n) => ({
								label: n.code,
								value: n.id,
							}))}
						/>

						<div className='flex flex-col gap-2'>
							<Label>Điểm cận dưới</Label>
							<InputGroup>
								<NumericFormat
									decimalSeparator=','
									thousandSeparator='.'
									placeholder='Nhập điểm cận dưới'
									value={lowerPoint ?? ''}
									onValueChange={(v) => setLowerPoint(v.floatValue)}
									customInput={InputGroupInput}
									type='text'
									inputMode='decimal'
								/>
							</InputGroup>
							{upperPoint !== undefined &&
								lowerPoint !== undefined &&
								upperPoint <= lowerPoint && (
									<p className='text-destructive text-xs'>
										Điểm cận dưới phải nhỏ hơn điểm cận trên
									</p>
								)}
						</div>
					</div>

					{/* Validation hints */}
					{selectedUpperNormId &&
						selectedLowerNormId &&
						selectedUpperNormId === selectedLowerNormId && (
							<p className='text-destructive text-xs'>
								Định mức cận trên và cận dưới không được trùng nhau
							</p>
						)}

					{mismatchedCodes.length > 0 && (
						<div className='bg-destructive/10 border-destructive/30 rounded-md border p-3'>
							<p className='text-destructive text-xs font-medium'>
								2 định mức có mã giao khoán không khớp nhau:
							</p>
							<ul className='text-destructive mt-1 list-disc pl-4 text-xs'>
								{mismatchedCodes.map((code) => (
									<li key={code}>{code}</li>
								))}
							</ul>
						</div>
					)}
				</div>
			)}

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

	if (costs.length === 0) return null;

	const sumCosts = costs.reduce((acc, c) => {
		const v = Number(c.totalPrice);
		return acc + (isNaN(v) ? 0 : v);
	}, 0);

	return (
		<div className='flex w-full flex-col gap-4'>
			<FormSeparator />

			<div className='flex flex-col gap-2'>
				<Label>Tổng tiền (đ/tấn)</Label>
				<Input
					readOnly
					value={formatNumber(sumCosts)}
					className='font-semibold read-only:bg-transparent'
				/>
			</div>

			{costs.map((_, index) => (
				<ContractCostRow key={index} index={index} contracts={contracts} />
			))}
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
