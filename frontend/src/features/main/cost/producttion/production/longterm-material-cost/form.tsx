/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import {
	LONGTERM_MATERIAL_COST_DEFAULT,
	longtermMaterialCostSchema,
	LongtermMaterialCostSchema,
} from '@/features/main/cost/producttion/production/longterm-material-cost/schema';
import {
	LongtermMaterialCostDetail,
	LongtermMaterialDetailItem,
} from '@/features/main/cost/producttion/production/longterm-material-cost/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function LongtermMaterialCostForm({
	id,
	callback,
}: ProductCostFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const [detailItems, setDetailItems] = useState<LongtermMaterialDetailItem[]>(
		[],
	);
	const [acceptanceReportId, setAcceptanceReportId] = useState<string>('');

	const form = useForm<LongtermMaterialCostSchema>({
		resolver: zodResolver(longtermMaterialCostSchema),
		mode: 'onSubmit',
		defaultValues: LONGTERM_MATERIAL_COST_DEFAULT,
	});

	useEffect(() => {
		if (!id) return;

		const fetchDetail = async () => {
			try {
				const response = await api.get<LongtermMaterialCostDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.LONG_TERM_TRACKING_DETAIL(id),
				);

				if (!response.result) return;

				const detail = response.result;
				const resolvedItems =
					detail.items && detail.items.length > 0
						? detail.items
						: (detail.processGroups || []).flatMap(
								(group) => group.items || [],
							);

				setDetailItems(resolvedItems);
				setAcceptanceReportId(detail.acceptanceReportId);

				form.reset({
					items: resolvedItems.map((item) => ({
						id: item.id,
						allocationRate: item.allocationRatio,
						isFullAccounting: item.isFullAccounting ?? false,
						note: item.note ?? '',
					})),
				});
			} catch (err) {
				error(err);
			}
		};

		fetchDetail();
	}, [form, id]);

	const handleFullAccountingChange = (index: number, checked: boolean) => {
		form.setValue(`items.${index}.isFullAccounting`, checked);

		if (checked) {
			form.setValue(`items.${index}.allocationRate`, 0);
			form.setValue(`items.${index}.note`, 'Hạch toán hết');
		} else {
			form.setValue(`items.${index}.note`, '');
		}
	};

	const handleSubmit = async (values: LongtermMaterialCostSchema) => {
		try {
			const body = values.items.map((item) => ({
				id: item.id,
				acceptanceReportId: acceptanceReportId,
				allocationRatio: item.allocationRate,
				isFullAccounting: item.isFullAccounting,
				note: item.note ?? '',
			}));

			await api.put(
				API.PRODUCTION.ACCEPTANCE_REPORT.UPDATE_LONG_TERM_TRACKING,
				body,
			);

			success(`${breadcrumb} đã được cập nhật thành công.`);
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='scrollbar-sm max-h-100 overflow-auto'>
				<FormArray
					control={form.control}
					name='items'
					hasAddButton={false}
					hasCloseButton={false}
				>
					{(index) => {
						const item = detailItems[index];
						const isFullAccounting =
							form.watch(`items.${index}.isFullAccounting`) ?? false;
						const watchedAllocationRate = form.watch(
							`items.${index}.allocationRate`,
						);

						const remainingPeriod =
							(item?.usageTime ?? 0) - (item?.allocatedTime ?? 0);

						const quotaAccountingValue =
							remainingPeriod > 0
								? (((item?.totalValueToAccount ?? 0) / (item?.usageTime || 1)) *
										(item?.actualOutput || 0)) /
									(item?.standardOutput || 1)
								: remainingPeriod === 0
									? (item?.totalValueToAccount ?? 0)
									: 0;

						const amount = (item?.issuedQuantity ?? 0) * (item?.unitPrice ?? 0);
						const totalAccountingValue =
							(item?.pendingValueStartPeriod ?? 0) + amount;

						const currentPeriodValue = isFullAccounting
							? (item?.totalValueToAccount ?? 0)
							: quotaAccountingValue * (watchedAllocationRate || 1);

						const endingBalance = isFullAccounting
							? 0
							: totalAccountingValue - currentPeriodValue;

						return (
							<>
								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Mã phụ tùng</Label>
									<Input readOnly value={item?.partCode ?? ''} />
								</div>

								<div className='min-w-48 flex-1 space-y-2'>
									<Label>Tên phụ tùng</Label>
									<Input readOnly value={item?.partName ?? ''} />
								</div>

								<div className='min-w-24 flex-1 space-y-2'>
									<Label>ĐVT</Label>
									<Input readOnly value={item?.unitOfMeasureName ?? ''} />
								</div>

								<div className='min-w-56 flex-1 space-y-2'>
									<Label>Giá trị chờ hạch toán đầu kỳ (đ)</Label>
									<Input
										readOnly
										value={formatNumber(item?.pendingValueStartPeriod ?? 0, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Số lượng</Label>
									<Input
										readOnly
										value={formatNumber(item?.issuedQuantity ?? 0)}
									/>
								</div>

								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Đơn giá</Label>
									<Input readOnly value={formatNumber(item?.unitPrice ?? 0)} />
								</div>

								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Thành tiền</Label>
									<Input
										readOnly
										value={formatNumber(amount, { maximumFractionDigits: 0 })}
									/>
								</div>

								<div className='min-w-56 flex-1 space-y-2'>
									<Label>Tổng giá trị cần hạch toán (đ)</Label>
									<Input
										readOnly
										value={formatNumber(totalAccountingValue, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Nguyên giá (đ)</Label>
									<Input
										readOnly
										value={formatNumber(item?.originAmount ?? 0, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-44 flex-1 space-y-2'>
									<Label>Thời gian sử dụng (Ti)</Label>
									<Input readOnly value={formatNumber(item?.usageTime ?? 0)} />
								</div>

								<div className='min-w-44 flex-1 space-y-2'>
									<Label>Thời gian đã phân bổ</Label>
									<Input
										readOnly
										value={formatNumber(item?.allocatedTime ?? 0)}
									/>
								</div>

								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Thời gian còn lại</Label>
									<Input readOnly value={formatNumber(remainingPeriod)} />
								</div>

								<div className='min-w-60 flex-1 space-y-2'>
									<Label>Giá trị cần hạch toán theo định mức (đ)</Label>
									<Input
										readOnly
										value={formatNumber(quotaAccountingValue, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-32 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.allocationRate`}
										label='Tỷ lệ phân bổ'
										placeholder='Nhập tỷ lệ phân bổ'
										disabled={isFullAccounting}
									/>
								</div>

								{/* Checkbox Hạch toán hết */}
								<div className='min-w-fit flex-1 space-y-2'>
									<Label htmlFor={`full-accounting-${index}`}>
										Hạch toán hết
									</Label>
									<div className='flex h-9 items-center'>
										<Switch
											checked={isFullAccounting}
											className='cursor-pointer data-[state=checked]:bg-blue-600'
											onCheckedChange={(checked) =>
												handleFullAccountingChange(index, checked)
											}
										/>
									</div>
								</div>

								<div className='min-w-56 flex-1 space-y-2'>
									<Label>Giá trị hạch toán kỳ này (đ)</Label>
									<Input
										readOnly
										value={formatNumber(currentPeriodValue, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-60 flex-1 space-y-2'>
									<Label>Giá trị cuối kỳ chờ hạch toán kỳ sau (đ)</Label>
									<Input
										readOnly
										value={formatNumber(endingBalance, {
											maximumFractionDigits: 0,
										})}
									/>
								</div>

								<div className='min-w-60 flex-1 space-y-2'>
									<FormInput
										control={form.control}
										name={`items.${index}.note`}
										label='Ghi chú'
										placeholder='Nhập ghi chú'
									/>
								</div>
							</>
						);
					}}
				</FormArray>
			</div>

			<DataTableEditConfirm isEdit={!!id} />
		</FormProvider>
	);
}
