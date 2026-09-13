import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormSeparator } from '@/components/form/form-separator';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { usePopup } from '@/components/popup';
import { Department } from '@/features/main/catalog/department/columns';
import {
	normalizeProcessGroup,
	ProcessGroup,
} from '@/features/main/catalog/process/group/columns';
import {
	LOW_VALUE_PERISHABLE_COMMON_FORM_DEFAULT,
	lowValuePerishableCommonFormSchema,
	type LowValuePerishableCommonFormSchema,
	type LowValuePerishablePeriodItem,
} from '@/features/main/pricing/low-value-perishable-supply/schema';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { LowValuePerishableSupplyUnitPrice } from './columns';

type LowValuePerishableSupplyFormProps =
	ActionDialogProps<LowValuePerishableSupplyUnitPrice> & {
		type: LowValuePerishableSupplyType;
		isDuplicate?: boolean;
	};

const validatePeriods = (
	periods: LowValuePerishablePeriodItem[],
): string | null => {
	if (periods.length === 0) {
		return 'Vui lòng thêm ít nhất một khoảng thời gian áp dụng.';
	}

	for (let i = 0; i < periods.length; i++) {
		const p = periods[i];
		if (!p.startMonth) {
			return `Khoảng thời gian ${i + 1}: Chưa chọn thời gian bắt đầu.`;
		}
		if (!p.endMonth) {
			return `Khoảng thời gian ${i + 1}: Chưa chọn thời gian kết thúc.`;
		}
		const s = new Date(p.startMonth).getTime();
		const e = new Date(p.endMonth).getTime();
		if (s > e) {
			return `Khoảng thời gian ${i + 1}: Thời gian bắt đầu không được lớn hơn thời gian kết thúc.`;
		}
		if (
			p.totalPrice === undefined ||
			p.totalPrice === null ||
			Number.isNaN(Number(p.totalPrice)) ||
			Number(p.totalPrice) < 0
		) {
			return `Khoảng thời gian ${i + 1}: Đơn giá phải lớn hơn hoặc bằng 0.`;
		}
	}

	for (let i = 0; i < periods.length; i++) {
		for (let j = i + 1; j < periods.length; j++) {
			const p1 = periods[i];
			const p2 = periods[j];
			const s1 = new Date(p1.startMonth).getTime();
			const e1 = new Date(p1.endMonth).getTime();
			const s2 = new Date(p2.startMonth).getTime();
			const e2 = new Date(p2.endMonth).getTime();

			if (s1 <= e2 && s2 <= e1) {
				const label1 = `Khoảng thời gian ${i + 1} (${formatDate(p1.startMonth, 'MM/yyyy')} - ${formatDate(p1.endMonth, 'MM/yyyy')})`;
				const label2 = `Khoảng thời gian ${j + 1} (${formatDate(p2.startMonth, 'MM/yyyy')} - ${formatDate(p2.endMonth, 'MM/yyyy')})`;
				return `Thời gian áp dụng của ${label1} và ${label2} bị trùng lặp. Vui lòng kiểm tra lại.`;
			}
		}
	}

	return null;
};

export function LowValuePerishableSupplyForm({
	data,
	row,
	type,
	isDuplicate = false,
}: LowValuePerishableSupplyFormProps) {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const { setOpen } = useDialog();
	const [departments, setDepartments] = useState<Department[]>([]);
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const [periods, setPeriods] = useState<LowValuePerishablePeriodItem[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			totalPrice: 0,
		},
	]);
	const [deletedPeriodIds, setDeletedPeriodIds] = useState<string[]>([]);

	const form = useForm<LowValuePerishableCommonFormSchema>({
		resolver: zodResolver(lowValuePerishableCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: LOW_VALUE_PERISHABLE_COMMON_FORM_DEFAULT,
	});

	useEffect(() => {
		Promise.all([
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			}),
		]).then(([departmentsRes, processGroupsRes]) => {
			setDepartments(departmentsRes.result.data);
			setProcessGroups(
				(processGroupsRes.result.data ?? []).map(normalizeProcessGroup),
			);

			if (!row) {
				return;
			}

			form.reset({
				departmentId: row.departmentId,
				processGroupId: row.processGroupId,
			});

			if (row.periods && row.periods.length > 0) {
				setPeriods(
					row.periods.map((p) => ({
						tempId: p.id,
						id: isDuplicate ? undefined : p.id,
						startMonth: p.startMonth.substring(0, 10),
						endMonth: p.endMonth.substring(0, 10),
						totalPrice: p.totalPrice,
					})),
				);
			} else if (row.startMonth && row.endMonth) {
				setPeriods([
					{
						tempId: row.id,
						id: isDuplicate ? undefined : row.id,
						startMonth: row.startMonth.substring(0, 10),
						endMonth: row.endMonth.substring(0, 10),
						totalPrice: row.totalPrice ?? 0,
					},
				]);
			}
		});
	}, [form, row, isDuplicate]);

	const handleAddPeriod = () => {
		setPeriods((prev) => [
			...prev,
			{
				tempId: `period_${Date.now()}_${Math.random()}`,
				startMonth: new Date().toISOString().substring(0, 10),
				endMonth: new Date().toISOString().substring(0, 10),
				totalPrice: 0,
			},
		]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		const newPeriod: LowValuePerishablePeriodItem = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).substring(2, 6)}`,
			startMonth: target.startMonth,
			endMonth: target.endMonth,
			totalPrice: target.totalPrice,
		};
		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, newPeriod);
			return next;
		});
	};

	const handleDeletePeriod = (index: number) => {
		const target = periods[index];
		if (target?.id && !isDuplicate) {
			setDeletedPeriodIds((prev) => [...prev, target.id!]);
		}
		setPeriods((prev) => prev.filter((_, i) => i !== index));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: LowValuePerishablePeriodItem,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleSubmit = async (values: LowValuePerishableCommonFormSchema) => {
		const error = validatePeriods(periods);
		if (error) {
			popup.error(error);
			return;
		}

		const apiConfig =
			type === LowValuePerishableSupplyType.TunnelExcavation
				? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TUNNELING
				: type === LowValuePerishableSupplyType.Longwall
				? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.LONGWALL
				: API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TRANSPORT;

		try {
			if (deletedPeriodIds.length > 0 && !isDuplicate) {
				await api.delete(
					API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.DELETES,
					deletedPeriodIds,
				);
			}

			const newPayloads = periods
				.filter((p) => !p.id || isDuplicate)
				.map((p) => ({
					departmentId: values.departmentId,
					processGroupId: values.processGroupId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					totalPrice: p.totalPrice,
					type,
				}));

			if (newPayloads.length > 0) {
				await api.post(apiConfig.CREATE, newPayloads);
			}

			const existingPeriods = periods.filter((p) => p.id && !isDuplicate);
			for (const p of existingPeriods) {
				await api.put(apiConfig.UPDATE, {
					id: p.id,
					departmentId: values.departmentId,
					processGroupId: values.processGroupId,
					startMonth: p.startMonth,
					endMonth: p.endMonth,
					totalPrice: p.totalPrice,
					type,
				});
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row && !isDuplicate ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (err) {
			popup.error(err);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormComboBox
				control={form.control}
				name='departmentId'
				label='Đơn vị'
				placeholder='Chọn đơn vị'
				options={departments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn'
				placeholder='Chọn nhóm công đoạn'
				options={processGroups.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			<div className='flex flex-col gap-4'>
				{periods.map((period, index) => (
					<div
						key={period.tempId}
						className='bg-neutral-50/70 border-neutral-300 shadow-xs flex flex-col gap-4 rounded-xl border p-5'
					>
						{/* Hàng 1: Thời gian bắt đầu (50%), Thời gian kết thúc (50%), Nút sao chép & xóa thời gian */}
						<div className='flex items-end gap-3'>
							<MonthYearInput
								label='Thời gian bắt đầu'
								value={period.startMonth}
								onChange={(val) =>
									handleUpdatePeriod(index, {
										...period,
										startMonth: val,
									})
								}
								className='flex-1'
							/>
							<MonthYearInput
								label='Thời gian kết thúc'
								value={period.endMonth}
								onChange={(val) =>
									handleUpdatePeriod(index, {
										...period,
										endMonth: val,
									})
								}
								className='flex-1'
							/>
							<div className='mb-2 flex items-center gap-2'>
								<Button
									type='button'
									variant='ghost'
									size='icon'
									onClick={() => handleCopyPeriod(index)}
									className='h-fit w-fit border-0 bg-transparent p-0 text-neutral-500 shadow-none hover:bg-transparent hover:text-primary'
									title='Sao chép'
								>
									<Copy className='size-5' />
								</Button>
								{periods.length > 1 && (
									<Button
										type='button'
										variant='ghost'
										size='icon'
										onClick={() => handleDeletePeriod(index)}
										className='h-fit w-fit border-0 bg-transparent p-0 text-red-500 shadow-none hover:bg-transparent hover:text-red-700'
										title='Xóa'
									>
										<XCircleIcon className='size-5 text-red-500' />
									</Button>
								)}
							</div>
						</div>

						{/* Hàng 2: Đơn giá (đ/tháng) */}
						<div className='flex flex-col gap-2'>
							<Label>Đơn giá (đ/tháng)</Label>
							<FormNumberInput
								value={period.totalPrice}
								onValueChange={(val) =>
									handleUpdatePeriod(index, {
										...period,
										totalPrice: val ?? 0,
									})
								}
								placeholder='Nhập đơn giá'
							/>
						</div>
					</div>
				))}

				{/* Nút Thêm thời gian ở dưới cùng bên trái */}
				<div className='flex justify-start pt-1'>
					<Button
						type='button'
						variant='ghost'
						size='sm'
						onClick={handleAddPeriod}
						className='h-fit w-fit bg-transparent flex items-center gap-1.5 p-0 hover:bg-transparent'
					>
						<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
						<span className='text-sm text-black'>Thêm thời gian</span>
					</Button>
				</div>
			</div>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
