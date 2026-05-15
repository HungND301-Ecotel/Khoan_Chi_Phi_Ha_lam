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
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Electricity } from '@/features/main/pricing/trimming/electricity/columns';
import {
	ELECTRICITY_FORM_DEFAULT,
	electricityFormSchema,
	ElectricityFormSchema,
} from '@/features/main/pricing/trimming/electricity/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm, UseFormReturn } from 'react-hook-form';

export function ElectricityForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Electricity> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [equipments, setEquipments] = useState<ContractCode[]>([]);

	const form = useForm<ElectricityFormSchema>({
		resolver: zodResolver(electricityFormSchema),
		mode: 'onSubmit',
		defaultValues: ELECTRICITY_FORM_DEFAULT,
	});

	const watchedEquipmentIds = form.watch('equipmentIds');

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				...(row?.startMonth && { date: row.startMonth }),
				ignorePagination: true,
			}),
		]);

		promises.then(([equipments]) => {
			setEquipments(equipments.result.data);

			if (!row) return;

			form.reset({
				equipmentIds: [row.equipmentId],
				startMonth: row.startMonth.substring(0, 10),
				endMonth: row.endMonth.substring(0, 10),
				costs: [
					{
						equipmentId: row.equipmentId,
						monthlyElectricityCost: row.monthlyElectricityCost,
						averageMonthlyTunnelProduction: row.averageMonthlyTunnelProduction,
					},
				],
			});
		});
	}, [row, form]);

	useEffect(() => {
		if (!Array.isArray(watchedEquipmentIds)) return;

		const existingEquipmentIdsInCosts = form
			.getValues('costs')
			.map(
				(cost: {
					equipmentId: string;
					monthlyElectricityCost: number;
					averageMonthlyTunnelProduction: number;
				}) => cost.equipmentId,
			);

		const equipmentIdsToAdd = watchedEquipmentIds.filter(
			(id) => !existingEquipmentIdsInCosts.includes(id),
		);

		const newCosts = equipmentIdsToAdd.map((equipmentId) => ({
			equipmentId,
			monthlyElectricityCost: 1,
			averageMonthlyTunnelProduction: 1,
		}));

		const costsToKeep = form
			.getValues('costs')
			.filter(
				(cost: {
					equipmentId: string;
					monthlyElectricityCost: number;
					averageMonthlyTunnelProduction: number;
				}) => watchedEquipmentIds.includes(cost.equipmentId),
			);

		const costs = [...costsToKeep, ...newCosts].sort((a, b) =>
			a.equipmentId.localeCompare(b.equipmentId),
		);

		form.setValue('costs', costs, {
			shouldValidate: false,
		});
	}, [form, watchedEquipmentIds]);

	const handleSubmit = async (values: ElectricityFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row && !isDuplicate) {
				const cost = processedValues.costs.find(
					(c: {
						equipmentId: string;
						monthlyElectricityCost: number;
						averageMonthlyTunnelProduction: number;
					}) => c.equipmentId === row.equipmentId,
				);
				await api.put(API.PRICING.ELECTRICITY.TRIMMING.UPDATE, {
					id: row.id,
					equipmentId: row.equipmentId,
					startMonth: processedValues.startMonth,
					endMonth: processedValues.endMonth,
					monthlyElectricityCost: cost?.monthlyElectricityCost ?? 0,
					averageMonthlyTunnelProduction:
						cost?.averageMonthlyTunnelProduction ?? 0,
				});
			} else {
				const body = processedValues.equipmentIds.map((equipmentId: string) => {
					const cost = processedValues.costs.find(
						(c: {
							equipmentId: string;
							monthlyElectricityCost: number;
							averageMonthlyTunnelProduction: number;
						}) => c.equipmentId === equipmentId,
					);
					return {
						equipmentId,
						startMonth: processedValues.startMonth,
						endMonth: processedValues.endMonth,
						monthlyElectricityCost: cost?.monthlyElectricityCost ?? 0,
						averageMonthlyTunnelProduction:
							cost?.averageMonthlyTunnelProduction ?? 0,
					};
				});
				await api.post(API.PRICING.ELECTRICITY.TRIMMING.CREATE, body);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row && !isDuplicate ? 'cập nhật' : 'tạo mới'} thành công.`,
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

			<FormMultiSelect
				control={form.control}
				name='equipmentIds'
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
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
	values: ContractCode[];
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
						monthlyElectricityCost: number;
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
	equipment: ContractCode | undefined;
	costIndex: number;
}) {
	const watchedAverageMonthlyTunnelProduction = form.watch(
		`costs.${costIndex}.averageMonthlyTunnelProduction`,
	);
	const watchedMonthlyElectricityCost = form.watch(
		`costs.${costIndex}.monthlyElectricityCost`,
	);

	const powerRate =
		watchedAverageMonthlyTunnelProduction &&
		watchedAverageMonthlyTunnelProduction > 0
			? watchedMonthlyElectricityCost / watchedAverageMonthlyTunnelProduction
			: 0;
	const powerCost =
		powerRate > 0
			? (equipment?.currentPrice ?? 0) * Number(powerRate.toFixed(10))
			: 0;

	return (
		<FormRow>
			<div className='min-w-30 flex-1 space-y-2'>
				<Label>Nhóm vật tư, tài sản</Label>
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
				<Label>Tên nhóm vật tư, tài sản</Label>
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
				<Label>Đơn giá điện năng (đ)</Label>
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
								value={equipment?.unitOfMeasureName ?? ''}
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
				name={`costs.${costIndex}.monthlyElectricityCost`}
				label='Điện năng tiêu thụ/tháng (kWh)'
				placeholder='Nhập điện năng tiêu thụ'
			/>

			<FormNumber
				control={form.control}
				name={`costs.${costIndex}.averageMonthlyTunnelProduction`}
				label='Sản lượng mét lò bình quân (m)'
				placeholder='Nhập sản lượng'
			/>

			<div className='flex-1 space-y-2'>
				<Label>Định mức điện năng</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={powerRate ? powerRate.toFixed(2) : '0'}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{powerRate ? powerRate.toFixed(4) : '0'}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>

			<div className='flex-1 space-y-2'>
				<Label>Chi phí điện năng (đ)</Label>
				<TooltipProvider>
					<Tooltip>
						<TooltipTrigger asChild>
							<Input
								readOnly
								value={formatNumber(powerCost)}
								className='read-only:bg-transparent'
							/>
						</TooltipTrigger>
						<TooltipContent>
							<p>{formatNumber(powerCost)}</p>
						</TooltipContent>
					</Tooltip>
				</TooltipProvider>
			</div>
		</FormRow>
	);
}
