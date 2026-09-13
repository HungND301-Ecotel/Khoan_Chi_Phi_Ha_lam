/* eslint-disable react-hooks/incompatible-library */
import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Tunneling } from '@/features/main/pricing/tunneling/maintenance/columns';
import type { TunnelingDetail } from '@/features/main/pricing/tunneling/maintenance/page';
import {
	type SCTXPeriodCost,
	type SCTXPeriodItemData,
	TUNNELING_COMMON_FORM_DEFAULT,
	tunnelingCommonFormSchema,
	type TunnelingCommonFormSchema,
	validateSCTXPeriods,
} from '@/features/main/pricing/tunneling/maintenance/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import {
	Copy,
	PlusCircleIcon,
	XCircleIcon,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

type LinkedMaterial = Asset & {
	materialType?: number;
	assignmentCodeIds?: string[];
};

export function TunnelingForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Tunneling> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [equipments, setEquipments] = useState<ContractCode[]>([]);
	const [parts, setParts] = useState<LinkedMaterial[]>([]);
	const [isLoadingPeriods, setIsLoadingPeriods] = useState(false);
	const [deletedPeriodIds, setDeletedPeriodIds] = useState<string[]>([]);

	const [periods, setPeriods] = useState<SCTXPeriodItemData[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			selectedPartIds: [],
			costs: [],
			otherMaterialValue: undefined,
		},
	]);

	const form = useForm<TunnelingCommonFormSchema>({
		resolver: zodResolver(tunnelingCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...TUNNELING_COMMON_FORM_DEFAULT,
			equipmentId: row?.equipmentId || '',
		},
	});

	const watchedEquipmentId = form.watch('equipmentId');

	const selectedEquipment = useMemo(() => {
		return equipments.find((e) => e.id === watchedEquipmentId);
	}, [equipments, watchedEquipmentId]);

	// Tải danh mục Thiết bị và Vật tư/Phụ tùng
	useEffect(() => {
		api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
			ignorePagination: true,
		}).then((res) => {
			setEquipments(res.result.data);
		});

		api.pagging<LinkedMaterial>(API.CATALOG.ASSET.LIST, {
			ignorePagination: true,
			materialType: 1,
		}).then((res) => {
			setParts(res.result.data);
		});
	}, []);

	// Tải dữ liệu các khoảng thời gian khi mở form ở chế độ Sửa
	useEffect(() => {
		if (!row) return;

		form.setValue('equipmentId', row.equipmentId);

		const periodsToLoad =
			row.periods && row.periods.length > 0
				? row.periods
				: [
						{
							id: row.id,
							startMonth: row.startMonth ?? '',
							endMonth: row.endMonth ?? '',
							totalPrice: row.totalPrice ?? 0,
						},
					];

		setIsLoadingPeriods(true);

		Promise.all(
			periodsToLoad.map((p) =>
				api.get<TunnelingDetail>(API.PRICING.MAINTENANCE.TUNNEL_DETAIL(p.id)),
			),
		)
			.then((results) => {
				const loadedPeriods: SCTXPeriodItemData[] = results.map(
					(res, index) => {
						const detail = res.result;
						const p = periodsToLoad[index];
						const items = (
							detail?.maintainUnitPriceEquipment || []
						).filter((c: any) => (c.partType ?? 1) === 1);

						const costs: SCTXPeriodCost[] = items.map((item: any) => ({
							partId: item.partId,
							quantity: item.quantity,
							averageMonthlyTunnelProduction:
								item.averageMonthlyTunnelProduction,
							replacementTimeStandard: item.replacementTimeStandard,
							equipmentId: detail?.equipmentId,
						}));

						return {
							tempId: p.id,
							id: isDuplicate ? undefined : p.id,
							startMonth:
								p.startMonth ||
								detail?.startMonth ||
								new Date().toISOString().substring(0, 10),
							endMonth:
								p.endMonth ||
								detail?.endMonth ||
								new Date().toISOString().substring(0, 10),
							selectedPartIds: costs.map((c) => c.partId),
							costs,
							otherMaterialValue:
								detail?.otherMaterialValue !== undefined &&
								detail?.otherMaterialValue !== null
									? detail.otherMaterialValue
									: undefined,
						};
					},
				);
				setPeriods(loadedPeriods);
			})
			.catch((err) => {
				popup.error(err);
			})
			.finally(() => {
				setIsLoadingPeriods(false);
			});
	}, [row, isDuplicate, form]);

	const handleAddPeriod = () => {
		let nextStart = new Date().toISOString().substring(0, 10);
		let nextEnd = new Date().toISOString().substring(0, 10);

		if (periods.length > 0) {
			const sortedEndDates = periods
				.map((p) => p.endMonth)
				.filter(Boolean)
				.sort();
			const latestEnd = sortedEndDates[sortedEndDates.length - 1];
			if (latestEnd) {
				const date = new Date(latestEnd);
				const nextStartDate = new Date(
					date.getFullYear(),
					date.getMonth() + 1,
					1,
				);
				const nextEndDate = new Date(
					date.getFullYear() + 1,
					date.getMonth() + 1,
					0,
				);
				nextStart = nextStartDate.toISOString().substring(0, 10);
				nextEnd = nextEndDate.toISOString().substring(0, 10);
			}
		}

		const newPeriod: SCTXPeriodItemData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextStart,
			endMonth: nextEnd,
			selectedPartIds: [],
			costs: [],
			otherMaterialValue: undefined,
		};

		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		const newPeriod: SCTXPeriodItemData = {
			...target,
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			selectedPartIds: [...(target.selectedPartIds || [])],
			costs: (target.costs || []).map((c) => ({
				...c,
				equipmentId: undefined,
			})),
			otherMaterialValue: target.otherMaterialValue,
		};

		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, newPeriod);
			return next;
		});
	};

	const handleDeletePeriod = (indexToDelete: number) => {
		if (periods.length <= 1) {
			popup.error('Mã đơn giá cần có ít nhất một khoảng thời gian.');
			return;
		}

		const periodToDelete = periods[indexToDelete];
		if (periodToDelete.id && !isDuplicate) {
			setDeletedPeriodIds((prev) => [...prev, periodToDelete.id!]);
		}

		setPeriods((prev) => prev.filter((_, idx) => idx !== indexToDelete));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: SCTXPeriodItemData,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleSubmit = async (values: TunnelingCommonFormSchema) => {
		const validationError = validateSCTXPeriods(periods);
		if (validationError) {
			popup.error(validationError);
			return;
		}

		try {
			// 1. Xóa các kỳ đã bị xóa trong chế độ Sửa
			if (deletedPeriodIds.length > 0 && !isDuplicate) {
				await api.delete(
					API.PRICING.MAINTENANCE.TUNNEL_DELETES,
					deletedPeriodIds,
				);
			}

			// 2. Thêm mới các kỳ mới (chưa có id hoặc đang duplicate)
			const newPeriods = periods.filter((p) => !p.id || isDuplicate);
			if (newPeriods.length > 0) {
				const body = newPeriods.map((p) => ({
					equipmentId: values.equipmentId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					type: 1,
					otherMaterialValue: p.otherMaterialValue ?? 0,
					costs: p.costs.map((c) => ({
						partId: c.partId,
						quantity: c.quantity,
						averageMonthlyTunnelProduction: c.averageMonthlyTunnelProduction,
						replacementTimeStandard: c.replacementTimeStandard,
					})),
				}));
				await api.post(API.PRICING.MAINTENANCE.TUNNEL_CREATE, body);
			}

			// 3. Cập nhật các kỳ đã có id trong chế độ Sửa
			const existingPeriods = periods.filter((p) => p.id && !isDuplicate);
			for (const p of existingPeriods) {
				const body = {
					id: p.id,
					equipmentId: values.equipmentId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					type: 1,
					otherMaterialValue: p.otherMaterialValue ?? 0,
					partUnitPrices: p.costs.map((c) => ({
						partId: c.partId,
						quantity: c.quantity,
						averageMonthlyTunnelProduction: c.averageMonthlyTunnelProduction,
						replacementTimeStandard: c.replacementTimeStandard,
					})),
				};
				await api.put(API.PRICING.MAINTENANCE.TUNNEL_UPDATE, body);
			}

			setOpen(false);
			popup.success(`${breadcrumb} đã được lưu thành công.`);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit} className='gap-4'>
			{/* Phần 1: Thông tin chung Thiết bị */}
			<FormComboBox
				control={form.control}
				name='equipmentId'
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn Nhóm vật tư, tài sản'
				options={equipments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
				disabled={!!row && !isDuplicate}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			{/* Phần 2: Danh sách các khoảng thời gian */}
			{isLoadingPeriods ? (
				<div className='flex items-center justify-center py-8 text-sm text-neutral-500'>
					<Spinner className='mr-2 size-4' /> Đang tải dữ liệu các khoảng thời
					gian...
				</div>
			) : (
				<div className='flex flex-col gap-5'>
					{periods.map((period, index) => (
						<SCTXPeriodSection
							key={period.tempId}
							period={period}
							totalPeriods={periods.length}
							equipment={selectedEquipment}
							parts={parts}
							onUpdate={(updated) => handleUpdatePeriod(index, updated)}
							onDelete={() => handleDeletePeriod(index)}
							onCopy={() => handleCopyPeriod(index)}
						/>
					))}

					{/* Nút Thêm thời gian ở dưới cùng bên trái */}
					<div className='flex justify-start pt-1'>
						<Button
							type='button'
							variant='ghost'
							size='sm'
							onClick={handleAddPeriod}
							className='flex h-fit w-fit items-center gap-1.5 border-0 bg-transparent p-0 shadow-none hover:bg-transparent'
						>
							<PlusCircleIcon className='size-4 text-primary' strokeWidth={2} />
							<span className='text-sm text-black'>Thêm thời gian</span>
						</Button>
					</div>
				</div>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

interface SCTXPeriodSectionProps {
	period: SCTXPeriodItemData;
	totalPeriods: number;
	equipment?: ContractCode;
	parts: LinkedMaterial[];
	onUpdate: (updated: SCTXPeriodItemData) => void;
	onDelete: () => void;
	onCopy: () => void;
}

function SCTXPeriodSection({
	period,
	totalPeriods,
	equipment,
	parts,
	onUpdate,
	onDelete,
	onCopy,
}: SCTXPeriodSectionProps) {
	// Map tra cứu nhanh vật tư theo ID
	const partsMap = useMemo(() => {
		return new Map(parts.map((p) => [p.id, p]));
	}, [parts]);

	// Danh sách phụ tùng thuộc thiết bị này
	const partOptions: MultiSelectOption[] = useMemo(() => {
		if (!equipment?.id) return [];
		return parts
			.filter((p) => (p.assignmentCodeIds ?? []).includes(equipment.id))
			.map((p) => ({
				value: p.id,
				label: `${p.code} - ${p.name}`,
			}));
	}, [parts, equipment?.id]);

	// Danh sách options đang được chọn
	const selectedPartOptions: MultiSelectOption[] = useMemo(() => {
		const optionMap = new Map(partOptions.map((o) => [o.value, o]));
		return period.selectedPartIds.map((id) => {
			const existingOpt = optionMap.get(id);
			if (existingOpt) return existingOpt;
			const part = partsMap.get(id);
			return {
				value: id,
				label: part ? `${part.code} - ${part.name}` : id,
			};
		});
	}, [partOptions, period.selectedPartIds, partsMap]);

	// Tính toán chi phí phụ tùng và vật tư khác
	const { otherCost, totalAmount } = useMemo(() => {
		let partsTotal = 0;
		period.costs.forEach((c) => {
			const part = partsMap.get(c.partId);
			const rate =
				c.replacementTimeStandard > 0 && c.averageMonthlyTunnelProduction > 0
					? c.quantity /
					  c.replacementTimeStandard /
					  c.averageMonthlyTunnelProduction
					: 0;
			const cost = (part?.costAmount ?? 0) * rate;
			if (!isNaN(cost)) {
				partsTotal += cost;
			}
		});

		const other =
			period.otherMaterialValue !== undefined
				? (partsTotal * (period.otherMaterialValue || 0)) / 100
				: 0;

		return {
			otherCost: other,
			totalAmount: partsTotal + (isNaN(other) ? 0 : other),
		};
	}, [period.costs, period.otherMaterialValue, partsMap]);

	// Thay đổi danh sách phụ tùng đã chọn
	const handlePartsChange = (selectedOptions: MultiSelectOption[]) => {
		const newSelectedIds = selectedOptions.map((o) => o.value);
		const existingCostMap = new Map(period.costs.map((c) => [c.partId, c]));

		const nextCosts: SCTXPeriodCost[] = newSelectedIds.map((partId) => {
			const existing = existingCostMap.get(partId);
			if (existing) {
				return existing;
			}
			return {
				partId,
				quantity: 0,
				averageMonthlyTunnelProduction: 0,
				replacementTimeStandard: 0,
			};
		});

		onUpdate({
			...period,
			selectedPartIds: newSelectedIds,
			costs: nextCosts,
		});
	};

	// Cập nhật giá trị số của từng phụ tùng
	const handleCostChange = (
		costIndex: number,
		field:
			| 'replacementTimeStandard'
			| 'quantity'
			| 'averageMonthlyTunnelProduction',
		val?: number,
	) => {
		const nextCosts = [...period.costs];
		nextCosts[costIndex] = {
			...nextCosts[costIndex],
			[field]: val ?? 0,
		};
		onUpdate({
			...period,
			costs: nextCosts,
		});
	};

	// Xóa 1 phụ tùng khỏi kỳ
	const handleRemovePart = (partId: string) => {
		const nextSelectedIds = period.selectedPartIds.filter((id) => id !== partId);
		const nextCosts = period.costs.filter((c) => c.partId !== partId);
		onUpdate({
			...period,
			selectedPartIds: nextSelectedIds,
			costs: nextCosts,
		});
	};

	const hasOtherMaterial = period.otherMaterialValue !== undefined;

	return (
		<div className='bg-neutral-50/70 border-neutral-300 shadow-xs flex flex-col gap-4 rounded-xl border p-5 transition-all'>
			{/* Hàng 1: Thời gian bắt đầu (50%), Thời gian kết thúc (50%), Action buttons */}
			<div className='flex items-end gap-3'>
				<MonthYearInput
					label='Thời gian bắt đầu'
					value={period.startMonth}
					onChange={(val) => onUpdate({ ...period, startMonth: val })}
					className='flex-1'
				/>
				<MonthYearInput
					label='Thời gian kết thúc'
					value={period.endMonth}
					onChange={(val) => onUpdate({ ...period, endMonth: val })}
					className='flex-1'
				/>
				<div className='mb-2 flex items-center gap-2'>
					<Button
						type='button'
						variant='ghost'
						size='icon'
						onClick={onCopy}
						className='h-fit w-fit border-0 bg-transparent p-0 text-neutral-500 shadow-none hover:bg-transparent hover:text-primary'
						title='Sao chép'
					>
						<Copy className='size-5' />
					</Button>
					{totalPeriods > 1 && (
						<Button
							type='button'
							variant='ghost'
							size='icon'
							onClick={onDelete}
							className='h-fit w-fit border-0 bg-transparent p-0 text-red-500 shadow-none hover:bg-transparent hover:text-red-700'
							title='Xóa'
						>
							<XCircleIcon className='size-5 text-red-500' />
						</Button>
					)}
				</div>
			</div>

			{/* Chọn Vật tư theo nhóm */}
			<MultiSelect
				label='Vật tư theo nhóm'
				placeholder='Chọn vật tư theo nhóm'
				options={partOptions}
				values={selectedPartOptions}
				onValuesChange={handlePartsChange}
			/>

			{/* Danh sách vật tư theo nhóm - FormRow chuẩn như ảnh */}
			{period.costs.length > 0 && (
				<div className='flex flex-col gap-4'>
					{equipment && (
						<FormSeparator
							className='w-full'
							label={`${equipment.code} - ${equipment.name} - ${formatNumber(totalAmount)} (đ)`}
						/>
					)}

					<div className='scrollbar-sm flex flex-col gap-4 overflow-x-auto pb-2'>
						{period.costs.map((cost, costIdx) => {
							const part = partsMap.get(cost.partId);
							const rate =
								cost.replacementTimeStandard > 0 &&
								cost.averageMonthlyTunnelProduction > 0
									? cost.quantity /
									  cost.replacementTimeStandard /
									  cost.averageMonthlyTunnelProduction
									: 0;
							const rowCost = (part?.costAmount ?? 0) * rate;

							return (
								<FormRow key={cost.partId} className='min-w-full w-max'>
									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Mã vật tư</Label>
										<Input
											readOnly
											value={part?.code || ''}
											className='read-only:bg-transparent'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Tên vật tư</Label>
										<Input
											readOnly
											value={part?.name || ''}
											className='read-only:bg-transparent'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Đơn giá (đ)</Label>
										<Input
											readOnly
											value={formatNumber(part?.costAmount || 0)}
											className='read-only:bg-transparent'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Đơn vị tính</Label>
										<Input
											readOnly
											value={part?.unitOfMeasureName || ''}
											className='read-only:bg-transparent'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Định mức thời gian thay thế (tháng)</Label>
										<FormNumberInput
											value={cost.replacementTimeStandard}
											onValueChange={(val) =>
												handleCostChange(
													costIdx,
													'replacementTimeStandard',
													val,
												)
											}
											placeholder='Nhập định mức'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Số lượng vật tư 1 lần thay thế</Label>
										<FormNumberInput
											value={cost.quantity}
											onValueChange={(val) =>
												handleCostChange(costIdx, 'quantity', val)
											}
											placeholder='Nhập số lượng'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Sản lượng đào lò bình quân (m)</Label>
										<FormNumberInput
											value={cost.averageMonthlyTunnelProduction}
											onValueChange={(val) =>
												handleCostChange(
													costIdx,
													'averageMonthlyTunnelProduction',
													val,
												)
											}
											placeholder='Nhập sản lượng'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Định mức vật tư SCTX</Label>
										<Input
											readOnly
											value={rate ? rate.toFixed(4) : '0'}
											className='read-only:bg-transparent'
										/>
									</div>

									<div className='flex min-w-max flex-1 flex-col gap-2'>
										<Label>Chi phí vật tư SCTX (đ)</Label>
										<Input
											readOnly
											value={formatNumber(isNaN(rowCost) ? 0 : rowCost)}
											className='read-only:bg-transparent'
										/>
									</div>

									<Button
										type='button'
										variant='ghost'
										size='icon'
										className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
										onClick={() => handleRemovePart(cost.partId)}
									>
										<XCircleIcon className='size-6' />
									</Button>
								</FormRow>
							);
						})}

						{/* Dòng Vật tư khác (VTK) */}
						{hasOtherMaterial ? (
							<FormRow className='min-w-full w-max'>
								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Mã vật tư</Label>
									<Input
										readOnly
										value='VTK'
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Tên vật tư</Label>
									<Input
										readOnly
										value='Vật tư khác'
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Đơn giá (đ)</Label>
									<Input readOnly className='read-only:bg-transparent' />
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Đơn vị tính</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Định mức thời gian thay thế (tháng)</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Số lượng vật tư 1 lần thay thế</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Sản lượng đào lò bình quân (m)</Label>
									<Input
										readOnly
										value=''
										className='read-only:bg-transparent'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
									<Label>Định mức vật tư SCTX</Label>
									<FormNumberInput
										value={period.otherMaterialValue}
										onValueChange={(val) =>
											onUpdate({
												...period,
												otherMaterialValue: val ?? 0,
											})
										}
										placeholder='Nhập định mức (%)'
									/>
								</div>

								<div className='flex min-w-max flex-1 flex-col gap-2'>
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
									onClick={() =>
										onUpdate({
											...period,
											otherMaterialValue: undefined,
										})
									}
								>
									<XCircleIcon className='size-6' />
								</Button>
							</FormRow>
						) : (
							<div
								className='flex cursor-pointer items-center gap-2'
								onClick={() => onUpdate({ ...period, otherMaterialValue: 0 })}
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
				</div>
			)}
		</div>
	);
}
