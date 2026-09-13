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
import type { Electricity } from '@/features/main/pricing/tunneling/electricity/columns';
import {
	ELECTRICITY_COMMON_FORM_DEFAULT,
	electricityCommonFormSchema,
	type ElectricityCommonFormSchema,
	type ElectricityPeriodItem,
	validateElectricityPeriods,
} from '@/features/main/pricing/tunneling/electricity/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

export function ElectricityForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Electricity> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();

	const [equipments, setEquipments] = useState<ContractCode[]>([]);
	const [deletedPeriodIds, setDeletedPeriodIds] = useState<string[]>([]);

	const [periods, setPeriods] = useState<ElectricityPeriodItem[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			monthlyElectricityCost: 1,
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
			setEquipments(res.result.data);
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
						monthlyElectricityCost: p.monthlyElectricityCost ?? 1,
						averageMonthlyTunnelProduction:
							p.averageMonthlyTunnelProduction ?? 1,
					})),
				);
			} else {
				setPeriods([
					{
						tempId: row.id,
						id: isDuplicate ? undefined : row.id,
						startMonth: (row.startMonth ?? new Date().toISOString()).substring(0, 10),
						endMonth: (row.endMonth ?? new Date().toISOString()).substring(0, 10),
						monthlyElectricityCost: row.monthlyElectricityCost ?? 1,
						averageMonthlyTunnelProduction:
							row.averageMonthlyTunnelProduction ?? 1,
					},
				]);
			}
		}
	}, [row, isDuplicate, form]);

	const handleAddPeriod = () => {
		setPeriods((prev) => [
			...prev,
			{
				tempId: `period_${Date.now()}_${Math.random()}`,
				startMonth: new Date().toISOString().substring(0, 10),
				endMonth: new Date().toISOString().substring(0, 10),
				monthlyElectricityCost: 1,
				averageMonthlyTunnelProduction: 1,
			},
		]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;
		const copied: ElectricityPeriodItem = {
			...target,
			tempId: `period_${Date.now()}_${Math.random()}`,
			id: undefined,
		};
		setPeriods((prev) => [
			...prev.slice(0, index + 1),
			copied,
			...prev.slice(index + 1),
		]);
	};

	const handleDeletePeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;
		if (target.id) {
			setDeletedPeriodIds((prev) => [...prev, target.id!]);
		}
		setPeriods((prev) => prev.filter((_, i) => i !== index));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: ElectricityPeriodItem,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleSubmit = async (values: ElectricityCommonFormSchema) => {
		const validationError = validateElectricityPeriods(periods);
		if (validationError) {
			popup.error(validationError);
			return;
		}

		try {
			const equipmentId = values.equipmentId;
			if (row && !isDuplicate) {
				// Cập nhật các khoảng thời gian đã tồn tại
				for (const p of periods) {
					if (p.id) {
						await api.put(API.PRICING.ELECTRICITY.TUNNELING.UPDATE, {
							id: p.id,
							equipmentId,
							startMonth: p.startMonth,
							endMonth: p.endMonth,
							monthlyElectricityCost: p.monthlyElectricityCost ?? 0,
							averageMonthlyTunnelProduction:
								p.averageMonthlyTunnelProduction ?? 0,
						});
					}
				}

				// Thêm mới các khoảng thời gian được thêm thêm
				const newPeriods = periods.filter((p) => !p.id);
				if (newPeriods.length > 0) {
					const newBodies = newPeriods.map((p) => ({
						equipmentId,
						startMonth: p.startMonth,
						endMonth: p.endMonth,
						monthlyElectricityCost: p.monthlyElectricityCost ?? 0,
						averageMonthlyTunnelProduction:
							p.averageMonthlyTunnelProduction ?? 0,
					}));
					await api.post(API.PRICING.ELECTRICITY.TUNNELING.CREATE, newBodies);
				}

				// Xóa các khoảng thời gian đã bị xóa
				for (const deleteId of deletedPeriodIds) {
					await api.delete(API.PRICING.ELECTRICITY.TUNNELING.DELETE(deleteId));
				}
			} else {
				// Thêm mới hoàn toàn
				const body = periods.map((p) => ({
					equipmentId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					monthlyElectricityCost: p.monthlyElectricityCost ?? 0,
					averageMonthlyTunnelProduction:
						p.averageMonthlyTunnelProduction ?? 0,
				}));
				await api.post(API.PRICING.ELECTRICITY.TUNNELING.CREATE, body);
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
					<ElectricityPeriodCard
						key={period.tempId}
						period={period}
						totalPeriods={periods.length}
						equipment={selectedEquipment}
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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

interface ElectricityPeriodCardProps {
	period: ElectricityPeriodItem;
	totalPeriods: number;
	equipment?: ContractCode;
	onUpdate: (updated: ElectricityPeriodItem) => void;
	onDelete: () => void;
	onCopy: () => void;
}

function ElectricityPeriodCard({
	period,
	totalPeriods,
	equipment,
	onUpdate,
	onDelete,
	onCopy,
}: ElectricityPeriodCardProps) {
	const monthlyElectricityCost = period.monthlyElectricityCost ?? 0;
	const averageMonthlyTunnelProduction =
		period.averageMonthlyTunnelProduction ?? 0;

	const powerRate =
		averageMonthlyTunnelProduction > 0
			? monthlyElectricityCost / averageMonthlyTunnelProduction
			: 0;
	const powerCost =
		powerRate > 0
			? (equipment?.currentPrice ?? 0) * Number(powerRate.toFixed(10))
			: 0;

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

			{/* Hàng 2: FormRow các ô thông số điện năng */}
			<div className='scrollbar-sm overflow-x-auto pb-2'>
				<FormRow className='min-w-full w-max'>
					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Nhóm vật tư, tài sản</Label>
						<Input
							readOnly
							value={equipment?.code || ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Tên nhóm vật tư, tài sản</Label>
						<Input
							readOnly
							value={equipment?.name || ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Đơn giá điện năng (đ)</Label>
						<Input
							readOnly
							value={formatNumber(equipment?.currentPrice || 0)}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Đơn vị tính</Label>
						<Input
							readOnly
							value={equipment?.unitOfMeasureName ?? ''}
							className='read-only:bg-transparent'
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Điện năng tiêu thụ/tháng (kWh)</Label>
						<FormNumberInput
							value={period.monthlyElectricityCost}
							onValueChange={(val) =>
								onUpdate({
									...period,
									monthlyElectricityCost: val ?? 0,
								})
							}
							placeholder='Nhập điện năng tiêu thụ'
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Sản lượng mét lò bình quân (m)</Label>
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

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Định mức điện năng</Label>
						<Input
							readOnly
							value={powerRate ? powerRate.toFixed(2) : '0'}
							className='read-only:bg-transparent'
							title={powerRate ? powerRate.toFixed(4) : '0'}
						/>
					</div>

					<div className='flex min-w-max flex-1 flex-col gap-2'>
						<Label>Chi phí điện năng (đ)</Label>
						<Input
							readOnly
							value={formatNumber(powerCost)}
							className='read-only:bg-transparent'
						/>
					</div>
				</FormRow>
			</div>
		</div>
	);
}
