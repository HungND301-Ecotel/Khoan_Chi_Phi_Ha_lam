/* eslint-disable react-hooks/incompatible-library */
import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import { Part } from '@/features/main/catalog/part/main/columns';
import { LongwallPanel } from '@/features/main/pricing/longwall-panel/maintenance/columns';
import { LongwallPanelDetail } from '@/features/main/pricing/longwall-panel/maintenance/page';
import {
	LONGWALL_PANEL_FORM_DEFAULT,
	longwallPanelFormSchema,
	LongwallPanelFormSchema,
} from '@/features/main/pricing/longwall-panel/maintenance/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';

const LONGWALL_TON_CONVERSION_FACTOR = 1000;

function calculateRegularRepairRates(
	quantity: number,
	replacementTimeStandard: number,
	averageMonthlyTunnelProduction: number,
) {
	return quantity / replacementTimeStandard / averageMonthlyTunnelProduction;
}

function calculateLongwallRegularRepairCost(
	partCost: number,
	regularRepairRates: number,
) {
	return (partCost * regularRepairRates) / LONGWALL_TON_CONVERSION_FACTOR;
}

export function LongwallPanelForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<LongwallPanel> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [equipments, setEquipments] = useState<Equipment[]>([]);
	const [parts, setParts] = useState<Part[]>([]);

	const form = useForm<LongwallPanelFormSchema>({
		resolver: zodResolver(longwallPanelFormSchema),
		mode: 'onBlur',
		defaultValues: {
			...LONGWALL_PANEL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	const watchedEquipmentIds = form.watch('equipmentIds');
	const watchedCosts = form.watch('costs');
	const watchedStartMonth = form.watch('startMonth');

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Equipment>(API.CATALOG.EQUIPMENT.LIST, {
				ignorePagination: true,
				...(row?.startMonth && { date: row.startMonth }),
			}),
			api.pagging<Part>(API.CATALOG.PART.LIST, {
				ignorePagination: true,
				partType: 1,
				...(row?.startMonth && { date: row.startMonth }),
			}),
		]);
		promises.then(([equipments, assets]) => {
			setEquipments(
				filterEquipmentsByProcessGroupType(
					equipments.result.data,
					ProcessGroupType.LC,
				),
			);
			setParts(assets.result.data);

			if (!row) return;

			api
				.get<LongwallPanelDetail>(API.PRICING.MAINTENANCE.DETAIL(row.id))
				.then((res) => {
					const {
						startMonth,
						equipmentId,
						maintainUnitPriceEquipment,
						otherMaterialValue,
					} = res.result;
					const costs = syncCostsWithCurrentPartLinks(
						maintainUnitPriceEquipment
							.filter((cost: any) => (cost.partType ?? 1) === 1)
							.map((cost: any) => ({
								partId: cost.partId,
								quantity: cost.quantity,
								replacementTimeStandard: cost.replacementTimeStandard,
								averageMonthlyTunnelProduction:
									cost.averageMonthlyTunnelProduction,
								equipmentId: cost.equipmentId,
							})),
						assets.result.data,
						[equipmentId],
					);

					form.reset({
						type: 2,
						startMonth: startMonth.substring(0, 10),
						endMonth: res.result.endMonth.substring(0, 10),
						equipmentIds: [equipmentId],
						costs,
						otherMaterialValues: { [equipmentId]: otherMaterialValue },
					});
				});
		});
	}, [form, row]);

	useEffect(() => {
		if (!watchedStartMonth || (row && !isDuplicate)) return;

		const promises = Promise.all([
			api.pagging<Equipment>(API.CATALOG.EQUIPMENT.LIST, {
				ignorePagination: true,
				date: watchedStartMonth,
			}),
			api.pagging<Part>(API.CATALOG.PART.LIST, {
				ignorePagination: true,
				partType: 1,
				date: watchedStartMonth,
			}),
		]);
		promises.then(([equipmentsRes, partsRes]) => {
			setEquipments(
				filterEquipmentsByProcessGroupType(
					equipmentsRes.result.data,
					ProcessGroupType.LC,
				),
			);
			setParts(partsRes.result.data);
		});
	}, [isDuplicate, watchedStartMonth, row]);

	useEffect(() => {
		if (parts.length === 0 || !Array.isArray(watchedEquipmentIds)) return;

		const existingCosts = form.getValues('costs');
		const costs = syncCostsWithCurrentPartLinks(
			existingCosts,
			parts,
			watchedEquipmentIds,
		);

		form.setValue('costs', costs, {
			shouldValidate: false,
		});
	}, [form, parts, watchedEquipmentIds]);

	const handleSubmit = async (values: LongwallPanelFormSchema) => {
		try {
			const processedValues = {
				...values,
			};

			const { costs, startMonth: startMonth, equipmentIds } = processedValues;

			if (row && !isDuplicate) {
				const body = {
					equipmentId: row.equipmentId,
					startMonth: startMonth,
					endMonth: processedValues.endMonth,
					type: 2,
					otherMaterialValue:
						values.otherMaterialValues?.[row.equipmentId] ?? 0,
					partUnitPrices: costs.map(
						(cost: {
							partId: string;
							replacementTimeStandard: number;
							quantity: number;
							averageMonthlyTunnelProduction: number;
							equipmentId: string;
						}) => ({
							partId: cost.partId,
							quantity: cost.quantity,
							replacementTimeStandard: cost.replacementTimeStandard,
							averageMonthlyTunnelProduction:
								cost.averageMonthlyTunnelProduction,
						}),
					),
				};

				await api.put(API.PRICING.MAINTENANCE.UPDATE, body);
			} else {
				const body = equipmentIds.map((equipmentId: string) => {
					const parts = costs
						.filter(
							(cost: {
								partId: string;
								replacementTimeStandard: number;
								quantity: number;
								averageMonthlyTunnelProduction: number;
								equipmentId: string;
							}) => cost.equipmentId === equipmentId,
						)
						.map(
							(part: {
								partId: string;
								replacementTimeStandard: number;
								quantity: number;
								averageMonthlyTunnelProduction: number;
								equipmentId: string;
							}) => ({
								partId: part.partId,
								quantity: part.quantity,
								replacementTimeStandard: part.replacementTimeStandard,
								averageMonthlyTunnelProduction:
									part.averageMonthlyTunnelProduction,
							}),
						);

					return {
						equipmentId,
						startMonth,
						endMonth: processedValues.endMonth,
						type: 2,
						otherMaterialValue: values.otherMaterialValues?.[equipmentId] ?? 0,
						costs: parts,
					};
				});
				await api.post(API.PRICING.MAINTENANCE.CREATE, body);
			}

			setOpen(false);
			popup.success(`${breadcrumb} đã được nhập thành công.`);
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

			{watchedCosts.length > 0 && (
				<div className='scrollbar-sm max-h-100 overflow-auto'>
					<GroupedLongwallPanelCosts equipments={equipments} parts={parts} />
				</div>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
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

function syncCostsWithCurrentPartLinks(
	costs: LongwallPanelFormSchema['costs'],
	parts: Part[],
	equipmentIds: string[],
): LongwallPanelFormSchema['costs'] {
	const equipmentOrder = new Map(equipmentIds.map((id, index) => [id, index]));
	const partOrder = new Map(parts.map((part, index) => [part.id, index]));
	const partEquipmentMap = new Map(
		parts.map((part) => [part.id, new Set(part.equipmentIds ?? [])]),
	);

	const filteredCosts = costs.filter((cost) => {
		if (!equipmentOrder.has(cost.equipmentId)) return false;
		const linkedEquipmentIds = partEquipmentMap.get(cost.partId);
		return !!linkedEquipmentIds?.has(cost.equipmentId);
	});

	const existingCostKeys = new Set(
		filteredCosts.map((cost) => `${cost.equipmentId}-${cost.partId}`),
	);

	const newCosts = parts.flatMap((part) =>
		(part.equipmentIds ?? [])
			.filter((equipmentId) => equipmentOrder.has(equipmentId))
			.filter(
				(equipmentId) => !existingCostKeys.has(`${equipmentId}-${part.id}`),
			)
			.map((equipmentId) => ({
				partId: part.id,
				replacementTimeStandard: NaN,
				quantity: NaN,
				averageMonthlyTunnelProduction: NaN,
				equipmentId,
			})),
	);

	return [...filteredCosts, ...newCosts].sort((a, b) => {
		const equipmentCompare =
			(equipmentOrder.get(a.equipmentId) ?? Number.MAX_SAFE_INTEGER) -
			(equipmentOrder.get(b.equipmentId) ?? Number.MAX_SAFE_INTEGER);
		if (equipmentCompare !== 0) return equipmentCompare;

		return (
			(partOrder.get(a.partId) ?? Number.MAX_SAFE_INTEGER) -
			(partOrder.get(b.partId) ?? Number.MAX_SAFE_INTEGER)
		);
	});
}

function GroupedLongwallPanelCosts({
	equipments,
	parts,
}: {
	equipments: Equipment[];
	parts: Part[];
}) {
	const { control, setValue } = useFormContext<LongwallPanelFormSchema>();
	const costs = useWatch({ control, name: 'costs' }) || [];
	const otherMaterialValues =
		useWatch({ control, name: 'otherMaterialValues' }) || {};

	const groups = groupCostsByEquipment(
		costs,
		equipments,
		parts,
		otherMaterialValues,
	);

	if (groups.length === 0) return null;

	return (
		<div className='flex w-full flex-wrap gap-4'>
			{groups.map((group) => {
				// Kiểm tra xem có key trong object không, thay vì kiểm tra giá trị
				const hasOtherMaterial = group.equipmentId in otherMaterialValues;

				return (
					<div key={group.equipmentId} className='flex flex-col gap-4'>
						<FormSeparator
							className='w-full'
							label={`${group.equipmentCode} - ${group.equipmentName} - ${formatNumber(group.totalAmount)} (đ)`}
						/>
						{group.indices.map((index) => (
							<FormRow
								key={`${group.equipmentId}-${costs[index]?.partId ?? 'unknown'}-${index}`}
							>
								<PricingLongwallPanelCosts index={index} parts={parts} />
							</FormRow>
						))}

						{hasOtherMaterial ? (
							<PricingEquipmentOtherPartCosts
								equipmentId={group.equipmentId}
								parts={parts}
							/>
						) : (
							<div
								className='flex cursor-pointer items-center gap-2'
								onClick={() =>
									setValue(`otherMaterialValues.${group.equipmentId}`, 0)
								}
							>
								<Button
									type='button'
									variant='ghost'
									size='icon'
									className='bg-transparent text-cyan-600 hover:text-cyan-700'
									title='Thêm vật tư khác'
								>
									<PlusCircleIcon className='size-6' />
								</Button>
								<span className='text-sm text-black'>Thêm vật tư khác</span>
							</div>
						)}
					</div>
				);
			})}
		</div>
	);
}

function PricingLongwallPanelCosts({
	index,
	parts,
}: {
	index: number;
	parts: Part[];
}) {
	const { control, getValues, setValue } =
		useFormContext<LongwallPanelFormSchema>();

	const watchedReplacementTimeStandard = useWatch({
		control,
		name: `costs.${index}.replacementTimeStandard`,
	});
	const watchedQuantity = useWatch({
		control,
		name: `costs.${index}.quantity`,
	});
	const watchedAverageMonthlyTunnelProduction = useWatch({
		control,
		name: `costs.${index}.averageMonthlyTunnelProduction`,
	});

	const partId = getValues(`costs.${index}.partId`);
	const part = parts.find((p) => p.id === partId);
	const effectiveReplacementTimeStandard = Number.isFinite(
		watchedReplacementTimeStandard,
	)
		? watchedReplacementTimeStandard
		: 0;

	const regularRepairRates = calculateRegularRepairRates(
		watchedQuantity,
		effectiveReplacementTimeStandard,
		watchedAverageMonthlyTunnelProduction,
	);

	const regularRepairCost = calculateLongwallRegularRepairCost(
		part?.costAmount ?? 0,
		Number(regularRepairRates),
	);

	const handleRemove = () => {
		const currentCosts = getValues('costs');
		const updatedCosts = currentCosts.filter(
			(
				_: {
					partId: string;
					replacementTimeStandard: number;
					quantity: number;
					averageMonthlyTunnelProduction: number;
					equipmentId: string;
				},
				i: number,
			) => i !== index,
		);
		setValue('costs', updatedCosts, { shouldValidate: true });
	};

	return (
		<>
			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Mã phụ tùng</Label>
				<Input
					readOnly
					value={part?.code}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Tên phụ tùng</Label>
				<Input
					readOnly
					value={part?.name}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn giá (đ)</Label>
				<Input
					readOnly
					value={formatNumber(part?.costAmount || 0)}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn vị tính</Label>
				<Input
					readOnly
					value={part?.unitOfMeasureName}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name={`costs.${index}.replacementTimeStandard`}
					label='Định mức thời gian thay thế (tháng)'
					placeholder='Nhập định mức'
				/>
			</div>

			<FormNumber
				control={control}
				name={`costs.${index}.quantity`}
				label='Số lượng vật tư 1 lần thay thế'
				placeholder='Nhập số lượng'
			/>

			<FormNumber
				control={control}
				name={`costs.${index}.averageMonthlyTunnelProduction`}
				label='Sản lượng than bình quân tháng (1000 tấn)'
				placeholder='Nhập sản lượng'
			/>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Định mức SCTX 1 thiết bị/1000 tấn</Label>
				<Input
					readOnly
					value={regularRepairRates ? regularRepairRates.toFixed(4) : '0'}
					className={`read-only:bg-transparent ${isNaN(regularRepairRates) ? 'border-destructive' : ''}`}
					title={
						isNaN(regularRepairRates) ? 'Vui lòng nhập dữ liệu hợp lệ' : ''
					}
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Chi phí SCTX 1 thiết bị/1 tấn than (đ/t)</Label>
				<Input
					readOnly
					value={formatNumber(Math.round(regularRepairCost) || 0)}
					className={`read-only:bg-transparent ${isNaN(regularRepairCost) ? 'border-destructive' : ''}`}
					title={isNaN(regularRepairCost) ? 'Vui lòng nhập dữ liệu hợp lệ' : ''}
				/>
			</div>

			<Button
				type='button'
				variant='ghost'
				size='icon'
				className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
				onClick={handleRemove}
			>
				<XCircleIcon className='size-6' />
			</Button>
		</>
	);
}

function groupCostsByEquipment(
	costs: LongwallPanelFormSchema['costs'],
	equipments: Equipment[],
	parts: Part[],
	otherMaterialValues?: Record<string, number | undefined>,
): Array<{
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	indices: number[];
	totalAmount: number;
}> {
	const grouped = new Map<
		string,
		{
			equipmentId: string;
			equipmentCode: string;
			equipmentName: string;
			indices: number[];
		}
	>();

	costs.forEach(
		(
			cost: {
				partId: string;
				replacementTimeStandard: number;
				quantity: number;
				averageMonthlyTunnelProduction: number;
				equipmentId: string;
			},
			index: number,
		) => {
			const equipmentId = cost.equipmentId;
			const equipment = equipments.find((e) => e.id === equipmentId);

			if (!grouped.has(equipmentId)) {
				grouped.set(equipmentId, {
					equipmentId,
					equipmentCode: equipment?.code || '',
					equipmentName: equipment?.name || '',
					indices: [],
				});
			}

			grouped.get(equipmentId)?.indices.push(index);
		},
	);

	const groups = Array.from(grouped.values()).sort((a, b) =>
		a.equipmentCode.localeCompare(b.equipmentCode),
	);

	// Calculate total amount for each group (including other-material percent)
	return groups.map((group) => {
		let totalAmount = 0;

		group.indices.forEach((index) => {
			const cost = costs[index];
			if (!cost) return;

			const part = parts.find((p) => p.id === cost.partId);
			if (!part) return;
			const replacementTimeStandard = Number.isFinite(
				cost.replacementTimeStandard,
			)
				? cost.replacementTimeStandard
				: 0;

			const regularRepairRates = calculateRegularRepairRates(
				cost.quantity,
				replacementTimeStandard,
				cost.averageMonthlyTunnelProduction,
			);

			const regularRepairCost = calculateLongwallRegularRepairCost(
				part.costAmount,
				Number(regularRepairRates),
			);

			if (!isNaN(regularRepairCost)) {
				totalAmount += regularRepairCost;
			}
		});

		const otherPercent = otherMaterialValues?.[group.equipmentId] ?? 0;
		const otherCost = (totalAmount * (otherPercent ?? 0)) / 100;
		const totalAmountWithOther =
			totalAmount + (isNaN(otherCost) ? 0 : otherCost);

		return {
			...group,
			totalAmount: Math.round(totalAmountWithOther),
		};
	});
}

function PricingEquipmentOtherPartCosts({
	equipmentId,
	parts,
}: {
	equipmentId: string;
	parts: Part[];
}) {
	const { control, setValue, getValues } =
		useFormContext<LongwallPanelFormSchema>();

	const watchedCosts = useWatch({
		control,
		name: 'costs',
	});
	const watchedOtherMaterialValue = useWatch({
		control,
		name: `otherMaterialValues.${equipmentId}`,
	});

	if (!watchedCosts || watchedCosts.length === 0) return null;

	// Filter costs for this equipment only
	const equipmentCosts = watchedCosts.filter(
		(cost: {
			partId: string;
			replacementTimeStandard: number;
			quantity: number;
			averageMonthlyTunnelProduction: number;
			equipmentId: string;
		}) => cost.equipmentId === equipmentId,
	);

	if (equipmentCosts.length === 0) return null;

	let totalAmount = 0;

	equipmentCosts.forEach(
		(cost: {
			partId: string;
			replacementTimeStandard: number;
			quantity: number;
			averageMonthlyTunnelProduction: number;
			equipmentId: string;
		}) => {
			const part = parts.find((part) => part.id === cost.partId);
			if (!part) return;
			const replacementTimeStandard = Number.isFinite(
				cost.replacementTimeStandard,
			)
				? cost.replacementTimeStandard
				: 0;

			const regularRepairRates = calculateRegularRepairRates(
				cost.quantity,
				replacementTimeStandard,
				cost.averageMonthlyTunnelProduction,
			);

			const regularRepairCost = calculateLongwallRegularRepairCost(
				part.costAmount,
				Number(regularRepairRates),
			);

			if (!isNaN(regularRepairCost)) {
				totalAmount += regularRepairCost;
			}
		},
	);

	const handleRemove = () => {
		const currentValues = getValues('otherMaterialValues') || {};
		const { [equipmentId]: _, ...rest } = currentValues;
		setValue('otherMaterialValues', rest, {
			shouldValidate: true,
		});
	};

	const otherCost = (totalAmount * (watchedOtherMaterialValue ?? 0)) / 100;

	return (
		<FormRow>
			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Mã phụ tùng</Label>
				<Input readOnly value={'VTK'} className='read-only:bg-transparent' />
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Tên phụ tùng</Label>
				<Input
					readOnly
					value='Vật tư khác'
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn giá (đ)</Label>
				<Input readOnly className='read-only:bg-transparent' />
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Đơn vị tính</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Định mức thời gian thay thế (tháng)</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Số lượng vật tư 1 lần thay thế</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Sản lượng than bình quân tháng (1000 tấn)</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<FormNumber
				control={control}
				name={`otherMaterialValues.${equipmentId}`}
				label='Định mức SCTX 1 thiết bị/1000 tấn'
				placeholder='Nhập định mức (%)'
			/>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Chi phí SCTX 1 thiết bị/1 tấn than (đ/t)</Label>
				<Input
					readOnly
					value={formatNumber(isNaN(otherCost) ? 0 : Math.round(otherCost))}
					className={`read-only:bg-transparent ${isNaN(otherCost) ? 'border-destructive' : ''}`}
					title={isNaN(otherCost) ? 'Vui lòng kiểm tra dữ liệu' : ''}
				/>
			</div>

			<Button
				type='button'
				variant='ghost'
				size='icon'
				className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
				onClick={handleRemove}
			>
				<XCircleIcon className='size-6' />
			</Button>
		</FormRow>
	);
}
