import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
	Tooltip,
	TooltipContent,
	TooltipProvider,
	TooltipTrigger,
} from '@/components/ui/tooltip';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import { LongwallElectricity } from '@/features/main/pricing/longwall-panel/electricity/columns';
import {
	ELECTRICITY_FORM_DEFAULT,
	electricityFormSchema,
	ElectricityFormSchema,
} from '@/features/main/pricing/longwall-panel/electricity/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm, UseFormReturn } from 'react-hook-form';

export function ElectricityForm({
	data,
	row,
	onSuccess,
 	isDuplicate = false,
}: ActionDialogProps<LongwallElectricity> & {
	onSuccess?: () => Promise<void> | void;
	isDuplicate?: boolean;
}) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [equipments, setEquipments] = useState<Equipment[]>([]);

	const form = useForm<ElectricityFormSchema>({
		resolver: zodResolver(electricityFormSchema),
		mode: 'onSubmit',
		defaultValues: ELECTRICITY_FORM_DEFAULT,
	});

	const watchedEquipmentIds = form.watch('equipmentIds');

	useEffect(() => {
		const fetchEquipments = async () => {
			try {
				const response = await api.pagging<Equipment>(
					API.CATALOG.EQUIPMENT.LIST,
				);
				setEquipments(
					filterEquipmentsByProcessGroupType(
						response.result.data || [],
						ProcessGroupType.LC,
					),
				);
			} catch (error) {
				popup.error(error);
			}
		};

		fetchEquipments();
	}, []);

	useEffect(() => {
		if (!row?.id) return;

		const fetchDetail = async () => {
			try {
				const response = await api.get<LongwallElectricity>(
					API.PRICING.ELECTRICITY.LONGWALL_PANEL.DETAIL(row.id),
				);
				const detail = response.result;

				form.reset({
					equipmentIds: [detail.equipmentId],
					startMonth: detail.startMonth.substring(0, 10),
					endMonth: detail.endMonth.substring(0, 10),
					costs: [
						{
							equipmentId: detail.equipmentId,
							quantity: detail.quantity,
							pdm: detail.pdm,
							kyc: detail.kyc,
							kdt: detail.kdt,
							workingHour: detail.workingHour,
							workingDate: detail.workingDate,
							averageMonthlyTunnelProduction:
								detail.longwallAverageMonthlyTunnelProduction,
						},
					],
				});
			} catch (error) {
				popup.error(error);
			}
		};

		fetchDetail();
	}, [row?.id]);

	useEffect(() => {
		if (!Array.isArray(watchedEquipmentIds)) return;

		const existingEquipmentIdsInCosts = form
			.getValues('costs')
			.map(
				(cost: {
					equipmentId: string;
					quantity: number;
					pdm: number;
					kyc: number;
					kdt: number;
					workingHour: number;
					workingDate: number;
					averageMonthlyTunnelProduction: number;
				}) => cost.equipmentId,
			);

		const equipmentIdsToAdd = watchedEquipmentIds.filter(
			(id) => !existingEquipmentIdsInCosts.includes(id),
		);

		const newCosts = equipmentIdsToAdd.map((equipmentId) => ({
			equipmentId,
			quantity: 1,
			pdm: 1,
			kyc: 1,
			kdt: 1,
			workingHour: 1,
			workingDate: 1,
			averageMonthlyTunnelProduction: 1,
		}));

		const costsToKeep = form
			.getValues('costs')
			.filter(
				(cost: {
					equipmentId: string;
					quantity: number;
					pdm: number;
					kyc: number;
					kdt: number;
					workingHour: number;
					workingDate: number;
					averageMonthlyTunnelProduction: number;
				}) => watchedEquipmentIds.includes(cost.equipmentId),
			);

		const costs = [...costsToKeep, ...newCosts].sort((a, b) =>
			a.equipmentId.localeCompare(b.equipmentId),
		);

		form.setValue('costs', costs, {
			shouldValidate: false,
		});
	}, [watchedEquipmentIds]);

	const handleSubmit = async (values: ElectricityFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row && !isDuplicate) {
				// UPDATE - single record
				const updatePayload = {
					id: row.id,
					...processedValues.costs[0],
					startMonth: processedValues.startMonth,
					endMonth: processedValues.endMonth,
				};
				await api.put(
					API.PRICING.ELECTRICITY.LONGWALL_PANEL.UPDATE,
					updatePayload,
				);
			} else {
				// CREATE - could be multiple records based on selected equipment
				const createPayload = processedValues.costs.map((cost) => ({
					...cost,
					startMonth: processedValues.startMonth,
					endMonth: processedValues.endMonth,
				}));
				await api.post(
					API.PRICING.ELECTRICITY.LONGWALL_PANEL.CREATE,
					createPayload,
				);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row && !isDuplicate ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await onSuccess?.();
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

			<FormMultiSelect
				control={form.control}
				name='equipmentIds'
				label='Mã thiết bị'
				placeholder='Chọn mã thiết bị'
				options={equipments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
				disabled={!!row && !isDuplicate}
			/>

			<ElectricityCost form={form} values={equipments} />

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function ElectricityCost({
	form,
	values,
}: {
	form: UseFormReturn<ElectricityFormSchema>;
	values: Equipment[];
}) {
	const watchedEquipmentIds = form.watch('equipmentIds');
	const watchedCosts = form.watch('costs');

	if (!watchedEquipmentIds || watchedEquipmentIds.length === 0) return null;

	return (
		<div className='scrollbar-sm max-h-100 overflow-auto pb-4'>
			{watchedEquipmentIds.map((equipmentId: string) => {
				const equipment = values.find((value) => value.id === equipmentId);
				const costIndex = watchedCosts.findIndex(
					(c: {
						equipmentId: string;
						quantity: number;
						pdm: number;
						kyc: number;
						kdt: number;
						workingHour: number;
						workingDate: number;
						averageMonthlyTunnelProduction: number;
					}) => c.equipmentId === equipmentId,
				);

				if (costIndex === -1) return null;

				return (
					<div key={equipmentId} className='space-y-4 p-2'>
						<ElectricityCostRow
							form={form}
							equipment={equipment}
							costIndex={costIndex}
						/>
					</div>
				);
			})}
		</div>
	);
}

function ElectricityCostRow({
	form,
	equipment,
	costIndex,
}: {
	form: UseFormReturn<ElectricityFormSchema>;
	equipment: Equipment | undefined;
	costIndex: number;
}) {
	const watchedQuantity = form.watch(`costs.${costIndex}.quantity`);
	const watchedPdm = form.watch(`costs.${costIndex}.pdm`);
	const watchedKyc = form.watch(`costs.${costIndex}.kyc`);
	const watchedKdt = form.watch(`costs.${costIndex}.kdt`);
	const watchedWorkingHour = form.watch(`costs.${costIndex}.workingHour`);
	const watchedWorkingDate = form.watch(`costs.${costIndex}.workingDate`);
	const watchedAverageMonthlyTunnelProduction = form.watch(
		`costs.${costIndex}.averageMonthlyTunnelProduction`,
	);

	// SPđm = Pđm * Số lượng
	const sPdm = watchedPdm * watchedQuantity;

	// Ptt = SPđm * Kyc * Kđt
	const pTt = sPdm * watchedKyc * watchedKdt;

	// Điện năng cho 1 thiết bị / 1 tấn than = (Ptt * Thời gian(h) * Ngày hoạt động) / (1000 * Sản lượng than)
	const electricityPerTon =
		watchedAverageMonthlyTunnelProduction > 0
			? (pTt * watchedWorkingHour * watchedWorkingDate) /
				(1000 * watchedAverageMonthlyTunnelProduction)
			: 0;

	// Chi phí điện năng cho 1 thiết bị / 1 tấn than = Đơn giá * Điện năng cho 1 thiết bị / 1 tấn than
	const electricityCostPerTon =
		(equipment?.currentPrice ?? 0) * electricityPerTon;

	return (
		<FormRow>
			<div className='min-w-30 flex-1 space-y-2'>
				<Label>Mã thiết bị</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={equipment?.code}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{equipment?.code}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<div className='min-w-30 flex-1 space-y-2'>
				<Label>Tên thiết bị</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={equipment?.name}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{equipment?.name}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<div className='flex-1 space-y-2'>
				<Label>Đơn giá điện năng (đ/kwh)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={formatNumber(equipment?.currentPrice || 0)}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{formatNumber(equipment?.currentPrice || 0)}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<div className='flex-1 space-y-2'>
				<Label>Đơn vị tính</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={equipment?.unitOfMeasureName}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{equipment?.unitOfMeasureName}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.quantity`}
				label='Số lượng'
				placeholder='Nhập số lượng'
				className='min-w-20'
			/>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.pdm`}
				label='Pđm (kW)'
				placeholder='Nhập Pđm'
				className='min-w-20'
			/>

			<div className='min-w-20 flex-1 space-y-2'>
				<Label>SPđm (kW)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={sPdm ? sPdm.toFixed(0) : '0'}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{sPdm ? sPdm.toFixed(4) : '0'}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.kyc`}
				label='Kyc'
				placeholder='Nhập Kyc'
				className='min-w-20'
			/>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.kdt`}
				label='Kđt'
				placeholder='Nhập Kđt'
				className='min-w-20'
			/>

			<div className='min-w-30 flex-1 space-y-2'>
				<Label>Ptt (kW)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={pTt ? formatNumber(pTt) : '0'}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{pTt ? formatNumber(pTt) : '0'}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.workingHour`}
				label='Thời gian (h)'
				placeholder='Nhập thời gian'
			/>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.workingDate`}
				label='Ngày hoạt động'
				placeholder='Nhập ngày hoạt động'
			/>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.averageMonthlyTunnelProduction`}
				label='Sản lượng than bình quân tháng (1000 tấn)'
				placeholder='Nhập sản lượng'
			/>

			<div className='flex-1 space-y-2'>
				<Label>Điện năng cho 1 thiết bị/ 1 tấn than (kWh/tấn)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={electricityPerTon ? electricityPerTon.toFixed(3) : '0'}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{electricityPerTon ? electricityPerTon.toFixed(4) : '0'}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<div className='flex-1 space-y-2'>
				<Label>Chi phí điện năng cho 1 thiết bị/ 1 tấn than (đ/tấn)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={formatNumber(electricityCostPerTon, {
									maximumFractionDigits: 0,
								})}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>
								{formatNumber(electricityCostPerTon, {
									maximumFractionDigits: 0,
								})}
							</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>
		</FormRow>
	);
}

function filterEquipmentsByProcessGroupType(
	items: Equipment[],
	processGroupType: ProcessGroupType,
) {
	return items.filter((item) =>
		(item.processGroups ?? []).some((group) => group.type === processGroupType),
	);
}
