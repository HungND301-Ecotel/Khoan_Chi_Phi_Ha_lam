import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
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
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { LongwallElectricity } from '@/features/main/pricing/longwall-panel/electricity/columns';
import {
	ELECTRICITY_COMMON_FORM_DEFAULT,
	electricityCommonFormSchema,
	type ElectricityCommonFormSchema,
	type LongwallElectricityPeriodItem,
	validateLongwallElectricityPeriods,
} from '@/features/main/pricing/longwall-panel/electricity/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

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

	const [equipments, setEquipments] = useState<ContractCode[]>([]);
	const [deletedPeriodIds, setDeletedPeriodIds] = useState<string[]>([]);

	const [periods, setPeriods] = useState<LongwallElectricityPeriodItem[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			quantity: 1,
			pdm: 1,
			kyc: 1,
			kdt: 1,
			workingHour: 1,
			workingDate: 1,
			averageMonthlyTunnelProduction: 1,
		},
	]);

	const form = useForm<ElectricityCommonFormSchema>({
		resolver: zodResolver(electricityCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...ELECTRICITY_COMMON_FORM_DEFAULT,
			equipmentId: row?.equipmentId || '',
		},
	});

	const watchedEquipmentId = form.watch('equipmentId');

	const selectedEquipment = useMemo(() => {
		const found = equipments.find((e) => e.id === watchedEquipmentId);
		if (found) return found;
		if (row && row.equipmentId === watchedEquipmentId) {
			return {
				id: row.equipmentId,
				code: row.equipmentCode,
				name: row.equipmentName,
				unitOfMeasureName: row.unitOfMeasureName,
				currentPrice: row.equipmentElectricityCost,
			} as ContractCode;
		}
		return undefined;
	}, [equipments, watchedEquipmentId, row]);

	useEffect(() => {
		api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
			ignorePagination: true,
		}).then((res) => {
			setEquipments(res.result.data || []);
		});

		if (row) {
			form.reset({
				equipmentId: row.equipmentId,
			});

			if (row.periods && row.periods.length > 0) {
				setPeriods(
					row.periods.map((p) => ({
						tempId: p.id,
						id: isDuplicate ? undefined : p.id,
						startMonth: (p.startMonth ?? '').substring(0, 10),
						endMonth: (p.endMonth ?? '').substring(0, 10),
						quantity: p.quantity ?? 1,
						pdm: p.pdm ?? 1,
						kyc: p.kyc ?? 1,
						kdt: p.kdt ?? 1,
						workingHour: p.workingHour ?? 1,
						workingDate: p.workingDate ?? 1,
						averageMonthlyTunnelProduction:
							p.longwallAverageMonthlyTunnelProduction ?? 1,
					})),
				);
			} else {
				setPeriods([
					{
						tempId: `period_${Date.now()}`,
						id: isDuplicate ? undefined : row.id,
						startMonth: (row.startMonth ?? '').substring(0, 10),
						endMonth: (row.endMonth ?? '').substring(0, 10),
						quantity: row.quantity ?? 1,
						pdm: row.pdm ?? 1,
						kyc: row.kyc ?? 1,
						kdt: row.kdt ?? 1,
						workingHour: row.workingHour ?? 1,
						workingDate: row.workingDate ?? 1,
						averageMonthlyTunnelProduction:
							row.longwallAverageMonthlyTunnelProduction ?? 1,
					},
				]);
			}
		}
	}, [row, isDuplicate, form]);

	const handleAddPeriod = () => {
		const newPeriod: LongwallElectricityPeriodItem = {
			tempId: `period_${Date.now()}_${Math.random()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			quantity: 1,
			pdm: 1,
			kyc: 1,
			kdt: 1,
			workingHour: 1,
			workingDate: 1,
			averageMonthlyTunnelProduction: 1,
		};
		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		const copied: LongwallElectricityPeriodItem = {
			...target,
			id: undefined,
			tempId: `period_${Date.now()}_${Math.random()}`,
		};
		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, copied);
			return next;
		});
	};

	const handleDeletePeriod = (index: number) => {
		if (periods.length <= 1) return;
		const target = periods[index];
		if (target.id) {
			setDeletedPeriodIds((prev) => [...prev, target.id!]);
		}
		setPeriods((prev) => prev.filter((_, i) => i !== index));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: LongwallElectricityPeriodItem,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleSubmit = async (values: ElectricityCommonFormSchema) => {
		try {
			const validationError = validateLongwallElectricityPeriods(periods);
			if (validationError) {
				popup.error(validationError);
				return;
			}

			const equipmentId = values.equipmentId;

			if (row && !isDuplicate) {
				const existingPeriods = periods.filter((p) => p.id);
				for (const p of existingPeriods) {
					const updatePayload = {
						id: p.id,
						equipmentId,
						startMonth: p.startMonth,
						endMonth: p.endMonth,
						quantity: p.quantity,
						pdm: p.pdm,
						kyc: p.kyc,
						kdt: p.kdt,
						workingHour: p.workingHour,
						workingDate: p.workingDate,
						averageMonthlyTunnelProduction: p.averageMonthlyTunnelProduction,
					};
					try {
						await api.put(
							API.PRICING.ELECTRICITY.LONGWALL_PANEL.UPDATE,
							updatePayload,
						);
					} catch (err: any) {
						if (err?.message?.includes('already exists') || err?.status === 409) {
							popup.error(
								`Khoảng thời gian ${p.startMonth} - ${p.endMonth} bị trùng lặp trên hệ thống.`,
							);
							return;
						}
						throw err;
					}
				}

				const newPeriods = periods.filter((p) => !p.id);
				if (newPeriods.length > 0) {
					const newBodies = newPeriods.map((p) => ({
						equipmentId,
						startMonth: p.startMonth,
						endMonth: p.endMonth,
						quantity: p.quantity,
						pdm: p.pdm,
						kyc: p.kyc,
						kdt: p.kdt,
						workingHour: p.workingHour,
						workingDate: p.workingDate,
						averageMonthlyTunnelProduction: p.averageMonthlyTunnelProduction,
					}));
					await api.post(
						API.PRICING.ELECTRICITY.LONGWALL_PANEL.CREATE,
						newBodies,
					);
				}

				for (const deleteId of deletedPeriodIds) {
					await api.delete(
						API.PRICING.ELECTRICITY.LONGWALL_PANEL.DELETE(deleteId),
					);
				}
			} else {
				const createPayload = periods.map((p) => ({
					equipmentId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					quantity: p.quantity,
					pdm: p.pdm,
					kyc: p.kyc,
					kdt: p.kdt,
					workingHour: p.workingHour,
					workingDate: p.workingDate,
					averageMonthlyTunnelProduction: p.averageMonthlyTunnelProduction,
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

			<div className='flex flex-col gap-5'>
				{periods.map((period, index) => (
					<LongwallElectricityPeriodCard
						key={period.tempId}
						period={period}
						totalPeriods={periods.length}
						equipment={selectedEquipment}
						onUpdate={(updated) => handleUpdatePeriod(index, updated)}
						onDelete={() => handleDeletePeriod(index)}
						onCopy={() => handleCopyPeriod(index)}
					/>
				))}

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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

interface LongwallElectricityPeriodCardProps {
	period: LongwallElectricityPeriodItem;
	totalPeriods: number;
	equipment?: ContractCode;
	onUpdate: (updated: LongwallElectricityPeriodItem) => void;
	onDelete: () => void;
	onCopy: () => void;
}

function LongwallElectricityPeriodCard({
	period,
	totalPeriods,
	equipment,
	onUpdate,
	onDelete,
	onCopy,
}: LongwallElectricityPeriodCardProps) {
	// SPđm = Pđm * Số lượng
	const sPdm = (period.pdm ?? 0) * (period.quantity ?? 0);

	// Ptt = SPđm * Kyc * Kđt
	const pTt = sPdm * (period.kyc ?? 0) * (period.kdt ?? 0);

	// Điện năng cho 1 thiết bị / 1 tấn than = (Ptt * Thời gian(h) * Ngày hoạt động) / (1000 * Sản lượng than)
	const electricityPerTon =
		(period.averageMonthlyTunnelProduction ?? 0) > 0
			? (pTt * (period.workingHour ?? 0) * (period.workingDate ?? 0)) /
				(1000 * (period.averageMonthlyTunnelProduction ?? 0))
			: 0;

	// Chi phí điện năng cho 1 thiết bị / 1 tấn than = Đơn giá * Điện năng cho 1 thiết bị / 1 tấn than
	const electricityCostPerTon =
		(equipment?.currentPrice ?? 0) * electricityPerTon;

	return (
		<div className='bg-neutral-50/70 border-neutral-300 shadow-xs flex flex-col gap-4 rounded-xl border p-5 transition-all'>
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

			<div className='scrollbar-sm overflow-x-auto pb-2'>
				<FormRow className='min-w-full w-max'>
					<div className='min-w-max flex-1 space-y-2'>
						<Label>Nhóm vật tư, tài sản</Label>
						<Input
							readOnly
							value={equipment?.code || ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Tên nhóm vật tư, tài sản</Label>
						<Input
							readOnly
							value={equipment?.name || ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Đơn giá điện năng (đ/kwh)</Label>
						<Input
							readOnly
							value={formatNumber(equipment?.currentPrice || 0)}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Đơn vị tính</Label>
						<Input
							readOnly
							value={equipment?.unitOfMeasureName ?? ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Số lượng</Label>
						<FormNumberInput
							value={period.quantity}
							onValueChange={(val) =>
								onUpdate({ ...period, quantity: val ?? 0 })
							}
							placeholder='Nhập số lượng'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Pđm (kW)</Label>
						<FormNumberInput
							value={period.pdm}
							onValueChange={(val) =>
								onUpdate({ ...period, pdm: val ?? 0 })
							}
							placeholder='Nhập Pđm'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>SPđm (kW)</Label>
						<Input
							readOnly
							value={sPdm ? sPdm.toFixed(0) : '0'}
							className='read-only:bg-transparent'
							title={sPdm ? sPdm.toFixed(4) : '0'}
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Kyc</Label>
						<FormNumberInput
							value={period.kyc}
							onValueChange={(val) =>
								onUpdate({ ...period, kyc: val ?? 0 })
							}
							placeholder='Nhập Kyc'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Kđt</Label>
						<FormNumberInput
							value={period.kdt}
							onValueChange={(val) =>
								onUpdate({ ...period, kdt: val ?? 0 })
							}
							placeholder='Nhập Kđt'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Ptt (kW)</Label>
						<Input
							readOnly
							value={pTt ? formatNumber(pTt) : '0'}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Thời gian (h)</Label>
						<FormNumberInput
							value={period.workingHour}
							onValueChange={(val) =>
								onUpdate({ ...period, workingHour: val ?? 0 })
							}
							placeholder='Nhập thời gian'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Ngày hoạt động</Label>
						<FormNumberInput
							value={period.workingDate}
							onValueChange={(val) =>
								onUpdate({ ...period, workingDate: val ?? 0 })
							}
							placeholder='Nhập ngày hoạt động'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Sản lượng than bình quân tháng (1000 tấn)</Label>
						<FormNumberInput
							value={period.averageMonthlyTunnelProduction}
							onValueChange={(val) =>
								onUpdate({
									...period,
									averageMonthlyTunnelProduction: val ?? 0,
								})
							}
							placeholder='Nhập sản lượng'
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Điện năng cho 1 thiết bị/ 1 tấn than (kWh/tấn)</Label>
						<Input
							readOnly
							value={electricityPerTon ? electricityPerTon.toFixed(3) : '0'}
							className='read-only:bg-transparent'
							title={electricityPerTon ? electricityPerTon.toFixed(4) : '0'}
						/>
					</div>

					<div className='min-w-max flex-1 space-y-2'>
						<Label>Chi phí điện năng cho 1 thiết bị/ 1 tấn than (đ/tấn)</Label>
						<Input
							readOnly
							value={formatNumber(electricityCostPerTon)}
							className='read-only:bg-transparent'
						/>
					</div>
				</FormRow>
			</div>
		</div>
	);
}
