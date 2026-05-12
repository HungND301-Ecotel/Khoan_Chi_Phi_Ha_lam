/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { LongTermAnchorSeedDetail } from '@/features/main/cost/producttion/production/longterm-anchor-seed/anchor-seed-types';
import {
	LONG_TERM_ANCHOR_SEED_DEFAULT,
	longTermAnchorSeedSchema,
	LongTermAnchorSeedSchema,
} from '@/features/main/cost/producttion/production/longterm-anchor-seed/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type LongtermAnchorSeedFormProps = {
	departmentId: string;
	callback?: () => Promise<void> | void;
};

export function LongtermAnchorSeedForm({
	departmentId,
	callback,
}: LongtermAnchorSeedFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const [detailItems, setDetailItems] = useState<LongTermAnchorSeedDetail>();

	const form = useForm<LongTermAnchorSeedSchema>({
		resolver: zodResolver(longTermAnchorSeedSchema),
		mode: 'onSubmit',
		defaultValues: LONG_TERM_ANCHOR_SEED_DEFAULT,
	});

	useEffect(() => {
		if (!departmentId) return;

		const fetchDetail = async () => {
			try {
				const response = await api.get<LongTermAnchorSeedDetail>(
					API.PRODUCTION.LONG_TERM_ANCHOR_SEED.DETAIL(departmentId),
				);

				if (!response.result) return;

				setDetailItems(response.result);
				form.reset({
					departmentId,
					processGroupMetrics: response.result.processGroupMetrics.map((metric) => ({
						id: metric.id,
						processGroupId: metric.processGroupId,
						plannedOutput: metric.plannedOutput,
						standardOutput: metric.standardOutput,
					})),
					items: response.result.items.map((item) => ({
						id: item.id,
						partId: item.partId,
						processGroupId: item.processGroupId,
						issuedQuantity: item.issuedQuantity,
						unitPrice: item.unitPrice,
						pendingValueStartPeriod: item.pendingValueStartPeriod,
						usageTime: item.usageTime,
						allocatedTime: item.allocatedTime,
						allocationRatio: item.allocationRatio,
						note: item.note ?? '',
					})),
				});
			} catch (err) {
				error(err);
			}
		};

		fetchDetail();
	}, [departmentId, error, form]);

	const handleSubmit = async (values: LongTermAnchorSeedSchema) => {
		try {
			await api.put(API.PRODUCTION.LONG_TERM_ANCHOR_SEED.UPDATE, {
				...values,
				departmentId,
			});
			success('Mốc gốc hạch toán dài kỳ đã được cập nhật thành công.');
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='scrollbar-sm max-h-100 space-y-6 overflow-auto'>
				<div className='space-y-4 rounded-sm border p-4'>
					<div className='text-sm font-semibold'>Sản lượng nhóm công đoạn</div>
					<FormArray
						control={form.control}
						name='processGroupMetrics'
						hasAddButton={false}
						hasCloseButton={false}
					>
						{(index) => {
							const metric = detailItems?.processGroupMetrics[index];

							return (
								<>
									<div className='min-w-48 flex-1 space-y-2'>
										<Label>Nhóm công đoạn</Label>
										<Input
											readOnly
											value={
												metric?.processGroupCode
													? `${metric.processGroupCode} - ${metric.processGroupName}`
													: metric?.processGroupName ?? ''
											}
										/>
									</div>

									<div className='min-w-40 flex-1'>
										<FormNumber
											control={form.control}
											name={`processGroupMetrics.${index}.plannedOutput`}
											label='Sản lượng kế hoạch'
										/>
									</div>

									<div className='min-w-40 flex-1'>
										<FormNumber
											control={form.control}
											name={`processGroupMetrics.${index}.standardOutput`}
											label='Sản lượng định mức'
										/>
									</div>
								</>
							);
						}}
					</FormArray>
				</div>

				<FormArray
					control={form.control}
					name='items'
					hasAddButton={false}
					hasCloseButton={false}
				>
					{(index) => {
						const item = detailItems?.items[index];

						return (
							<>
								<div className='min-w-48 flex-1 space-y-2'>
									<Label>Nhóm công đoạn</Label>
									<Input
										readOnly
										value={
											item?.processGroupCode
												? `${item.processGroupCode} - ${item.processGroupName}`
												: item?.processGroupName ?? ''
										}
									/>
								</div>

								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Mã phụ tùng</Label>
									<Input readOnly value={item?.partCode ?? ''} />
								</div>

								<div className='min-w-56 flex-1 space-y-2'>
									<Label>Tên phụ tùng</Label>
									<Input readOnly value={item?.partName ?? ''} />
								</div>

								<div className='min-w-24 flex-1 space-y-2'>
									<Label>ĐVT</Label>
									<Input readOnly value={item?.unitOfMeasureName ?? ''} />
								</div>

								<div className='min-w-32 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.issuedQuantity`}
										label='Số lượng'
									/>
								</div>

								<div className='min-w-32 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.unitPrice`}
										label='Đơn giá'
									/>
								</div>

								<div className='min-w-56 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.pendingValueStartPeriod`}
										label='Giá trị chờ hạch toán đầu kỳ'
									/>
								</div>

								<div className='min-w-40 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.usageTime`}
										label='Thời gian sử dụng'
									/>
								</div>

								<div className='min-w-40 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.allocatedTime`}
										label='Thời gian đã phân bổ'
									/>
								</div>

								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Thời gian còn lại</Label>
									<Input
										readOnly
										value={formatNumber(
											(form.watch(`items.${index}.usageTime`) ?? 0) -
												(form.watch(`items.${index}.allocatedTime`) ?? 0),
										)}
									/>
								</div>

								<div className='min-w-32 flex-1'>
									<FormNumber
										control={form.control}
										name={`items.${index}.allocationRatio`}
										label='Tỷ lệ phân bổ'
									/>
								</div>

								<div className='min-w-40 flex-1 space-y-2'>
									<Label>Thành tiền</Label>
									<Input
										readOnly
										value={formatNumber(
											(form.watch(`items.${index}.issuedQuantity`) ?? 0) *
												(form.watch(`items.${index}.unitPrice`) ?? 0),
										)}
									/>
								</div>

								<div className='min-w-64 flex-1 space-y-2'>
									<Label>Ghi chú</Label>
									<Input
										value={form.watch(`items.${index}.note`) ?? ''}
										onChange={(event) =>
											form.setValue(`items.${index}.note`, event.target.value)
										}
									/>
								</div>
							</>
						);
					}}
				</FormArray>
			</div>

			<DataTableEditConfirm isEdit />
		</FormProvider>
	);
}
