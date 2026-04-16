import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { Part } from '@/features/main/catalog/part/main/columns';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';
import z from 'zod';

const popupSchema = z.object({
	startMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	endMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	processGroupId: z
		.string()
		.nonempty('Nhóm công đoạn sản xuất không được để trống'),
	equipmentIds: z
		.array(z.string().nonempty({ error: 'Mã thiết bị không được để trống' }))
		.nonempty({ error: 'Mã thiết bị không được để trống' }),
	costs: z
		.array(
			z.object({
				partId: z.string().nonempty({ error: 'ID mã số không được để trống' }),
				replacementTimeStandard: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val) && val > 0, {
						message: 'Không được để trống',
					}),
				quantity: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val), {
						message: 'Không được để trống',
					}),
				averageMonthlyTunnelProduction: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val), {
						message: 'Không được để trống',
					}),
				equipmentId: z
					.string()
					.nonempty({ error: 'Mã thiết bị không được để trống' }),
			}),
		)
		.nonempty({ error: 'Mục đầu vào không được để trống' }),
	otherMaterialValues: z.record(z.string(), z.number().optional()).optional(),
});

type PopupSchema = z.infer<typeof popupSchema>;

type ProcessGroupOption = {
	value: string;
	label: string;
	type: number;
};

type EquipmentOption = {
	id: string;
	code: string;
	name: string;
};

type MaintainUnitPriceCreateDialogProps = {
	open: boolean;
	onOpenChange: (open: boolean) => void;
	partCode: string;
	processGroupOptions: ProcessGroupOption[];
	equipmentOptions: EquipmentOption[];
	onCreated: () => void;
};

export function MaintainUnitPriceCreateDialog({
	open,
	onOpenChange,
	partCode,
	processGroupOptions,
	equipmentOptions,
	onCreated,
}: MaintainUnitPriceCreateDialogProps) {
	const popup = usePopup();
	const [parts, setParts] = useState<Part[]>([]);

	const today = useMemo(() => new Date().toISOString().substring(0, 10), []);

	const form = useForm<PopupSchema>({
		resolver: zodResolver(popupSchema),
		mode: 'onSubmit',
		defaultValues: {
			startMonth: today,
			endMonth: today,
			processGroupId: processGroupOptions[0]?.value ?? '',
			equipmentIds: [],
			costs: [],
			otherMaterialValues: {},
		},
	});

	const watchedEquipmentIds = form.watch('equipmentIds');
	const watchedCosts = form.watch('costs');
	const watchedStartMonth = form.watch('startMonth');

	useEffect(() => {
		if (!open) return;
		api
			.pagging<Part>(API.CATALOG.PART.LIST, {
				ignorePagination: true,
				partType: 1,
				...(watchedStartMonth && { date: watchedStartMonth }),
			})
			.then((res) => {
				setParts(res.result.data);
			})
			.catch(() => {
				setParts([]);
			});
	}, [open, watchedStartMonth]);

	useEffect(() => {
		if (!open) return;
		if (parts.length === 0 || !Array.isArray(watchedEquipmentIds)) return;

		const existingEquipmentIdsInCosts = form
			.getValues('costs')
			.map((cost) => cost.equipmentId);

		const equipmentIdsToAdd = watchedEquipmentIds.filter(
			(id) => !existingEquipmentIdsInCosts.includes(id),
		);

		const newCosts = parts.flatMap((part) =>
			(part.equipmentIds ?? [])
				.filter((equipmentId) => equipmentIdsToAdd.includes(equipmentId))
				.map((equipmentId) => ({
					partId: part.id,
					quantity: NaN,
					averageMonthlyTunnelProduction: NaN,
					replacementTimeStandard: NaN,
					equipmentId,
				})),
		);

		const costsToKeep = form
			.getValues('costs')
			.filter((cost) => watchedEquipmentIds.includes(cost.equipmentId));

		const costs = [...costsToKeep, ...newCosts].sort((a, b) =>
			a.equipmentId.localeCompare(b.equipmentId),
		);

		form.setValue('costs', costs, {
			shouldValidate: false,
		});
	}, [form, open, parts, watchedEquipmentIds]);

	const handleSubmit = async (values: PopupSchema) => {
		try {
			const selectedProcessGroup = processGroupOptions.find(
				(item) => item.value === values.processGroupId,
			);
			if (!selectedProcessGroup) {
				popup.error('Không xác định được nhóm công đoạn sản xuất');
				return;
			}

			const body = values.equipmentIds.map((equipmentId) => {
				const equipmentCosts = values.costs
					.filter((cost) => cost.equipmentId === equipmentId)
					.map((cost) => ({
						partId: cost.partId,
						quantity: cost.quantity,
						averageMonthlyTunnelProduction: cost.averageMonthlyTunnelProduction,
						replacementTimeStandard: cost.replacementTimeStandard,
					}));

				return {
					equipmentId,
					startMonth: values.startMonth,
					endMonth: values.endMonth,
					type: selectedProcessGroup.type,
					otherMaterialValue: values.otherMaterialValues?.[equipmentId] ?? 0,
					costs: equipmentCosts,
				};
			});

			await api.post(API.PRICING.MAINTENANCE.CREATE, body);
			popup.success(`Đã tạo đơn giá SCTX cho phụ tùng ${partCode}`);
			onCreated();
			onOpenChange(false);
			form.reset({
				startMonth: today,
				endMonth: today,
				processGroupId: processGroupOptions[0]?.value ?? '',
				equipmentIds: [],
				costs: [],
				otherMaterialValues: {},
			});
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent className='max-h-[90vh] overflow-hidden sm:max-w-6xl'>
				<DialogHeader>
					<DialogTitle>Tạo mới Đơn giá và định mức SCTX</DialogTitle>
					<DialogDescription>
						Phụ tùng: <b>{partCode}</b>
					</DialogDescription>
				</DialogHeader>

				<FormProvider
					context={form}
					onSubmit={handleSubmit}
					className='min-w-0 overflow-hidden'
				>
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

					<FormComboBox
						control={form.control}
						name='processGroupId'
						label='Nhóm công đoạn sản xuất'
						placeholder='Chọn nhóm công đoạn sản xuất'
						options={processGroupOptions.map((item) => ({
							label: item.label,
							value: item.value,
						}))}
					/>

					<FormMultiSelect
						control={form.control}
						name='equipmentIds'
						label='Mã thiết bị'
						placeholder='Chọn mã thiết bị'
						options={equipmentOptions.map((item) => ({
							label: `${item.code} - ${item.name}`,
							value: item.id,
						}))}
					/>

					{watchedCosts.length > 0 && (
						<div className='scrollbar-sm max-h-100 min-w-0 overflow-auto'>
							<div className='min-w-max'>
								<GroupedCosts parts={parts} equipments={equipmentOptions} />
							</div>
						</div>
					)}

					<DialogFooter>
						<Button
							type='button'
							variant='outline'
							onClick={() => onOpenChange(false)}
						>
							Huỷ
						</Button>
						<Button
							type='submit'
							disabled={
								form.formState.isSubmitting || watchedCosts.length === 0
							}
						>
							Xác nhận
						</Button>
					</DialogFooter>
				</FormProvider>
			</DialogContent>
		</Dialog>
	);
}

function GroupedCosts({
	parts,
	equipments,
}: {
	parts: Part[];
	equipments: EquipmentOption[];
}) {
	const { control, setValue } = useFormContext<PopupSchema>();
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
				const hasOtherMaterial = group.equipmentId in otherMaterialValues;

				return (
					<div key={group.equipmentId} className='flex flex-col gap-4'>
						<FormSeparator
							className='w-full'
							label={`${group.equipmentCode} - ${group.equipmentName} - ${formatNumber(group.totalAmount)} (đ)`}
						/>
						{group.indices.map((index) => (
							<FormRow key={index}>
								<PricingCostRow index={index} parts={parts} />
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

function PricingCostRow({ index, parts }: { index: number; parts: Part[] }) {
	const { control, getValues, setValue } = useFormContext<PopupSchema>();

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

	const currentPartId = getValues(`costs.${index}.partId`);
	const part = parts.find((p) => p.id === currentPartId);

	const regularRepairRates =
		watchedQuantity /
		watchedReplacementTimeStandard /
		watchedAverageMonthlyTunnelProduction;
	const regularRepairCost =
		(part?.costAmount ?? 0) * Number(regularRepairRates);

	const handleRemove = () => {
		const currentCosts = getValues('costs');
		const updatedCosts = currentCosts.filter((_, i) => i !== index);
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
				label='Sản lượng đào lò bình quân (m)'
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
					value={formatNumber(Math.round(regularRepairCost) || 0)}
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
	costs: PopupSchema['costs'],
	equipments: EquipmentOption[],
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

	costs.forEach((cost, index) => {
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
	});

	const groups = Array.from(grouped.values()).sort((a, b) =>
		a.equipmentCode.localeCompare(b.equipmentCode),
	);

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

			if (!Number.isNaN(regularRepairCost)) {
				totalAmount += regularRepairCost;
			}
		});

		const otherPercent = otherMaterialValues?.[group.equipmentId] ?? 0;
		const otherCost = (totalAmount * (otherPercent ?? 0)) / 100;
		const totalAmountWithOther =
			totalAmount + (Number.isNaN(otherCost) ? 0 : otherCost);

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
	const { control, setValue, getValues } = useFormContext<PopupSchema>();

	const watchedCosts = useWatch({
		control,
		name: 'costs',
	});
	const watchedOtherMaterialValue = useWatch({
		control,
		name: `otherMaterialValues.${equipmentId}`,
	});

	if (!watchedCosts || watchedCosts.length === 0) return null;

	const equipmentCosts = watchedCosts.filter(
		(cost) => cost.equipmentId === equipmentId,
	);
	if (equipmentCosts.length === 0) return null;

	let totalAmount = 0;

	equipmentCosts.forEach((cost) => {
		const part = parts.find((item) => item.id === cost.partId);
		if (!part) return;

		const regularRepairRates =
			cost.quantity /
			cost.replacementTimeStandard /
			cost.averageMonthlyTunnelProduction;

		const regularRepairCost = part.costAmount * Number(regularRepairRates);
		if (!Number.isNaN(regularRepairCost)) {
			totalAmount += regularRepairCost;
		}
	});

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
				<Input readOnly value='VTK' className='read-only:bg-transparent' />
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
				<Label>Sản lượng đào lò bình quân (m)</Label>
				<Input readOnly value='' className='read-only:bg-transparent' />
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name={`otherMaterialValues.${equipmentId}`}
					label='Định mức vật tư SCTX'
					placeholder='Nhập định mức'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Chi phí vật tư SCTX (đ)</Label>
				<Input
					readOnly
					value={formatNumber(Math.round(otherCost) || 0)}
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
