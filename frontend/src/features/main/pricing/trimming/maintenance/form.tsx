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
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Asset } from '@/features/main/catalog/asset/types';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Trimming } from '@/features/main/pricing/trimming/maintenance/columns';
import { TunnelingDetail } from '@/features/main/pricing/trimming/maintenance/page';
import {
	TRIMMING_FORM_DEFAULT,
	trimmingFormSchema,
	TrimmingFormSchema,
} from '@/features/main/pricing/trimming/maintenance/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';

type LinkedMaterial = Asset & {
	materialType?: number;
	assignmentCodeIds?: string[];
};

export function TunnelingForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Trimming> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [equipments, setEquipments] = useState<ContractCode[]>([]);
	const [parts, setParts] = useState<LinkedMaterial[]>([]);

	const form = useForm<TrimmingFormSchema>({
		resolver: zodResolver(trimmingFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...TRIMMING_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	const watchedEquipmentIds = form.watch('equipmentIds');
	const watchedSelectedPartIds = form.watch('selectedPartIds');
	const watchedCosts = form.watch('costs');
	const watchedStartMonth = form.watch('startMonth');

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				...(row?.startMonth && { date: row.startMonth }),
			}),
			api.pagging<LinkedMaterial>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				materialType: 1,
				...(row?.startMonth && { date: row.startMonth }),
			}),
		]);
		promises.then(([equipments, assets]) => {
			setEquipments(equipments.result.data);
			setParts(assets.result.data);

			if (!row) return;

			api
				.get<TunnelingDetail>(API.PRICING.MAINTENANCE.TRIMMING_DETAIL(row.id))
				.then((res) => {
					const {
						startMonth,
						endMonth,
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
								averageMonthlyTunnelProduction:
									cost.averageMonthlyTunnelProduction,
								replacementTimeStandard: cost.replacementTimeStandard,
								equipmentId: cost.equipmentId,
							})),
						assets.result.data,
						[equipmentId],
						Array.from(
							new Set(
								maintainUnitPriceEquipment
									.filter((cost: any) => (cost.partType ?? 1) === 1)
									.map((cost: any) => cost.partId),
							),
						),
					);

					form.reset({
						type: 3,
						startMonth: startMonth.substring(0, 10),
						endMonth: endMonth.substring(0, 10),
						equipmentIds: [equipmentId],
						selectedPartIds: Array.from(
							new Set(
								maintainUnitPriceEquipment
									.filter((cost: any) => (cost.partType ?? 1) === 1)
									.map((cost: any) => cost.partId),
							),
						),
						costs,
						otherMaterialValues: { [equipmentId]: otherMaterialValue },
					});
				});
		});
	}, [form, row]);

	useEffect(() => {
		if (!watchedStartMonth || (row && !isDuplicate)) return;

		const promises = Promise.all([
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
				date: watchedStartMonth,
			}),
			api.pagging<LinkedMaterial>(API.CATALOG.ASSET.LIST, {
				ignorePagination: true,
				materialType: 1,
				date: watchedStartMonth,
			}),
		]);
		promises.then(([equipmentsRes, partsRes]) => {
			setEquipments(equipmentsRes.result.data);
			setParts(partsRes.result.data);
		});
	}, [isDuplicate, watchedStartMonth, row]);

	useEffect(() => {
		if (
			parts.length === 0 ||
			!Array.isArray(watchedEquipmentIds) ||
			!Array.isArray(watchedSelectedPartIds)
		)
			return;

		const availablePartIds = new Set(
			getLinkedParts(parts, watchedEquipmentIds).map((part) => part.id),
		);
		const normalizedSelectedPartIds = watchedSelectedPartIds.filter((partId) =>
			availablePartIds.has(partId),
		);

		if (
			normalizedSelectedPartIds.length !== watchedSelectedPartIds.length ||
			normalizedSelectedPartIds.some(
				(partId, index) => partId !== watchedSelectedPartIds[index],
			)
		) {
			form.setValue('selectedPartIds', normalizedSelectedPartIds, {
				shouldValidate: false,
			});
		}

		const existingCosts = form.getValues('costs');
		const costs = syncCostsWithCurrentPartLinks(
			existingCosts,
			parts,
			watchedEquipmentIds,
			normalizedSelectedPartIds,
		);

		form.setValue('costs', costs, {
			shouldValidate: false,
		});
	}, [form, parts, watchedEquipmentIds, watchedSelectedPartIds]);

	const selectedPartOptions = getPartOptions(parts, watchedEquipmentIds);

	const handleSubmit = async (values: TrimmingFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			const {
				costs,
				startMonth: startMonth,
				endMonth: endMonth,
				equipmentIds,
			} = processedValues;

			if (row && !isDuplicate) {
				const body = {
					id: row.id,
					equipmentId: row.equipmentId,
					startMonth: startMonth,
					endMonth: endMonth,
					type: 3,
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
							averageMonthlyTunnelProduction:
								cost.averageMonthlyTunnelProduction,
							replacementTimeStandard: cost.replacementTimeStandard,
						}),
					),
				};

				await api.put(API.PRICING.MAINTENANCE.TRIMMING_UPDATE, body);
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
								averageMonthlyTunnelProduction:
									part.averageMonthlyTunnelProduction,
								replacementTimeStandard: part.replacementTimeStandard,
							}),
						);

					return {
						equipmentId,
						startMonth,
						endMonth,
						type: 3,
						otherMaterialValue: values.otherMaterialValues?.[equipmentId] ?? 0,
						costs: parts,
					};
				});
				await api.post(API.PRICING.MAINTENANCE.TRIMMING_CREATE, body);
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
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				options={equipments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
				disabled={!!row && !isDuplicate}
			/>

			<FormMultiSelect
				control={form.control}
				name='selectedPartIds'
				label='Vật tư theo nhóm'
				placeholder='Chọn vật tư theo nhóm'
				options={selectedPartOptions}
				disabled={watchedEquipmentIds.length === 0}
			/>

			{watchedCosts.length > 0 && (
				<div className='scrollbar-sm max-h-100 overflow-auto'>
					<GroupedTunnelingCosts equipments={equipments} parts={parts} />
				</div>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
function syncCostsWithCurrentPartLinks(
	costs: TrimmingFormSchema['costs'],
	parts: LinkedMaterial[],
	equipmentIds: string[],
	selectedPartIds: string[],
): TrimmingFormSchema['costs'] {
	const selectedPartIdSet = new Set(selectedPartIds);
	const linkedParts = parts.filter((part) => selectedPartIdSet.has(part.id));
	const equipmentOrder = new Map(equipmentIds.map((id, index) => [id, index]));
	const partOrder = new Map(linkedParts.map((part, index) => [part.id, index]));
	const partEquipmentMap = new Map(
		linkedParts.map((part) => [part.id, new Set(part.assignmentCodeIds ?? [])]),
	);

	const filteredCosts = costs.filter((cost) => {
		if (!equipmentOrder.has(cost.equipmentId)) return false;
		if (!selectedPartIdSet.has(cost.partId)) return false;
		const linkedEquipmentIds = partEquipmentMap.get(cost.partId);
		return !!linkedEquipmentIds?.has(cost.equipmentId);
	});

	const existingCostKeys = new Set(
		filteredCosts.map((cost) => `${cost.equipmentId}-${cost.partId}`),
	);

	const newCosts = linkedParts.flatMap((part) =>
		(part.assignmentCodeIds ?? [])
			.filter((equipmentId) => equipmentOrder.has(equipmentId))
			.filter(
				(equipmentId) => !existingCostKeys.has(`${equipmentId}-${part.id}`),
			)
			.map((equipmentId) => ({
				partId: part.id,
				quantity: NaN,
				averageMonthlyTunnelProduction: NaN,
				replacementTimeStandard: NaN,
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

function getLinkedParts(parts: LinkedMaterial[], equipmentIds: string[]) {
	const equipmentIdSet = new Set(equipmentIds);
	return parts.filter((part) =>
		(part.assignmentCodeIds ?? []).some((equipmentId) =>
			equipmentIdSet.has(equipmentId),
		),
	);
}

function getPartOptions(parts: LinkedMaterial[], equipmentIds: string[]) {
	return getLinkedParts(parts, equipmentIds)
		.map((part) => ({
			label: `${part.code} - ${part.name}`,
			value: part.id,
		}))
		.sort((a, b) => a.label.localeCompare(b.label, 'vi'));
}

function GroupedTunnelingCosts({
	equipments,
	parts,
}: {
	equipments: ContractCode[];
	parts: LinkedMaterial[];
}) {
	const { control, setValue } = useFormContext<TrimmingFormSchema>();
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
								<PricingTunnelingCosts index={index} parts={parts} />
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

function PricingTunnelingCosts({
	index,
	parts,
}: {
	index: number;
	parts: LinkedMaterial[];
}) {
	const { control, getValues, setValue } = useFormContext<TrimmingFormSchema>();

	const watchedQuantity = useWatch({
		control,
		name: `costs.${index}.quantity`,
	});
	const watchedAverageMonthlyTunnelProduction = useWatch({
		control,
		name: `costs.${index}.averageMonthlyTunnelProduction`,
	});
	const watchedReplacementTimeStandard = useWatch({
		control,
		name: `costs.${index}.replacementTimeStandard`,
	});

	const partId = getValues(`costs.${index}.partId`);
	const part = parts.find((p) => p.id === partId);

	const regularRepairRates =
		watchedQuantity /
		watchedReplacementTimeStandard /
		watchedAverageMonthlyTunnelProduction;

	const regularRepairCost =
		(part?.costAmount ?? 0) * Number(regularRepairRates);

	const handleRemove = () => {
		const currentCosts = getValues('costs');
		const updatedCosts = currentCosts.filter((_, i: number) => i !== index);
		setValue('costs', updatedCosts, { shouldValidate: true });
	};

	return (
		<>
			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Mã vật tư</Label>
				<Input
					readOnly
					value={part?.code}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Tên vật tư</Label>
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
				label='Sản lượng xén lò bình quân (m)'
				placeholder='Nhập sản lượng'
			/>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Định mức vật tư SCTX</Label>
				<Input
					readOnly
					value={regularRepairRates ? regularRepairRates.toFixed(4) : '0'}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Chi phí vật tư SCTX (đ)</Label>
				<Input
					readOnly
					value={formatNumber(regularRepairCost || 0)}
					className='read-only:bg-transparent'
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
	costs: TrimmingFormSchema['costs'],
	equipments: ContractCode[],
	parts: LinkedMaterial[],
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

			const regularRepairRates =
				cost.quantity /
				cost.replacementTimeStandard /
				cost.averageMonthlyTunnelProduction;

			const regularRepairCost = part.costAmount * Number(regularRepairRates);

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
			totalAmount: totalAmountWithOther,
		};
	});
}

function PricingEquipmentOtherPartCosts({
	equipmentId,
	parts,
}: {
	equipmentId: string;
	parts: LinkedMaterial[];
}) {
	const { control, setValue, getValues } = useFormContext<TrimmingFormSchema>();

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

			const regularRepairRates =
				cost.quantity /
				cost.replacementTimeStandard /
				cost.averageMonthlyTunnelProduction;

			const regularRepairCost = part.costAmount * Number(regularRepairRates);

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
				<Label>Mã vật tư</Label>
				<Input readOnly value={'VTK'} className='read-only:bg-transparent' />
			</div>

			<div className='flex min-w-32 flex-1 flex-col gap-2'>
				<Label>Tên vật tư</Label>
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
				<Label>Sản lượng xén lò bình quân (m)</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<FormNumber
				control={control}
				name={`otherMaterialValues.${equipmentId}`}
				label='Định mức vật tư SCTX'
				placeholder='Nhập định mức (%)'
			/>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Chi phí vật tư SCTX (đ)</Label>
				<Input
					readOnly
					value={formatNumber(isNaN(otherCost) ? 0 : otherCost)}
					className='read-only:bg-transparent'
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
