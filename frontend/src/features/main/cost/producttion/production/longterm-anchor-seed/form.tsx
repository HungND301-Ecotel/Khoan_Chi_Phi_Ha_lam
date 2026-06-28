/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { ClientPagination } from '@/components/datatable/client-pagination';
import { FormArray } from '@/components/form/form-array';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
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
import { useEffect, useMemo, useState } from 'react';
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
	const [loading, setLoading] = useState(false);
	const [pageIndex, setPageIndex] = useState(0);
	const [pageSize, setPageSize] = useState(10);

	const form = useForm<LongTermAnchorSeedSchema>({
		resolver: zodResolver(longTermAnchorSeedSchema),
		mode: 'onSubmit',
		defaultValues: LONG_TERM_ANCHOR_SEED_DEFAULT,
	});

	useEffect(() => {
		if (!departmentId) return;

		const fetchDetail = async () => {
			try {
				setLoading(true);
				const response = await api.get<LongTermAnchorSeedDetail>(
					API.PRODUCTION.LONG_TERM_ANCHOR_SEED.DETAIL(departmentId),
				);

				if (!response.result) return;

				setDetailItems(response.result);
				setPageIndex(0);
				form.reset({
					departmentId,
					processGroupMetrics: response.result.processGroupMetrics.map(
						(metric) => ({
							id: metric.id,
							processGroupId: metric.processGroupId,
							plannedOutput: metric.plannedOutput,
							standardOutput: metric.standardOutput,
						}),
					),
					items: response.result.items.map((item) => ({
						id: item.id,
						materialId: item.materialId || item.partId,
						partId: item.partId,
						processGroupId: item.processGroupId,
						issuedQuantity: item.issuedQuantity,
						unitPrice: item.unitPrice,
						pendingValueStartPeriod: item.pendingValueStartPeriod,
						usageTime: item.usageTime,
						allocatedTime: item.allocatedTime,
						allocationRatio: item.allocationRatio ?? 0,
						note: item.note ?? '',
					})),
				});
			} catch (err) {
				error(err);
			} finally {
				setLoading(false);
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

	const totalItems = detailItems?.items.length ?? 0;
	const pageCount = Math.ceil(totalItems / pageSize);
	const safePageIndex =
		pageCount === 0 ? 0 : Math.min(pageIndex, Math.max(pageCount - 1, 0));
	const paginatedIndexes = useMemo(() => {
		const start = safePageIndex * pageSize;
		return Array.from(
			{ length: Math.min(pageSize, Math.max(totalItems - start, 0)) },
			(_, index) => start + index,
		);
	}, [pageSize, safePageIndex, totalItems]);

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='flex max-h-[70vh] flex-col overflow-hidden bg-transparent'>
				<div className='sticky top-0 z-20 shrink-0 pb-4'>
					<div className='border-border/60 space-y-4 rounded-sm border bg-transparent p-4'>
						<div className='text-sm font-semibold'>
							Sản lượng nhóm công đoạn
						</div>
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
														: (metric?.processGroupName ?? '')
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
				</div>

				<div className='scrollbar-sm min-h-0 flex-1 overflow-auto pt-4'>
					{loading ? (
						<div className='flex h-60 items-center justify-center'>
							<div className='flex flex-col items-center gap-3 text-sm text-slate-500'>
								<Spinner />
								<span>Đang tải dữ liệu...</span>
							</div>
						</div>
					) : (
						<>
							<div className='inline-flex min-w-max flex-col gap-4 pr-4'>
								<FormArray
									control={form.control}
									name='items'
									renderIndexes={paginatedIndexes}
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
																: (item?.processGroupName ?? '')
														}
													/>
												</div>

												<div className='min-w-40 flex-1 space-y-2'>
													<Label>Mã vật tư</Label>
													<Input
														readOnly
														value={item?.materialCode || item?.partCode || ''}
													/>
												</div>

												<div className='min-w-56 flex-1 space-y-2'>
													<Label>Tên vật tư</Label>
													<Input
														readOnly
														value={item?.materialName || item?.partName || ''}
													/>
												</div>

												<div className='min-w-24 flex-1 space-y-2'>
													<Label>ĐVT</Label>
													<Input
														readOnly
														value={item?.unitOfMeasureName ?? ''}
													/>
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
																(form.watch(`items.${index}.allocatedTime`) ??
																	0),
														)}
													/>
												</div>

												<div className='min-w-40 flex-1 space-y-2'>
													<Label>Thành tiền</Label>
													<Input
														readOnly
														value={formatNumber(
															(form.watch(`items.${index}.issuedQuantity`) ??
																0) *
																(form.watch(`items.${index}.unitPrice`) ?? 0),
														)}
													/>
												</div>

												<div className='min-w-64 flex-1 space-y-2'>
													<Label>Ghi chú</Label>
													<Input
														value={form.watch(`items.${index}.note`) ?? ''}
														onChange={(event) =>
															form.setValue(
																`items.${index}.note`,
																event.target.value,
															)
														}
													/>
												</div>
											</>
										);
									}}
								</FormArray>
								{totalItems > 0 && (
									<ClientPagination
										totalItems={totalItems}
										pageIndex={safePageIndex}
										pageSize={pageSize}
										onPageIndexChange={setPageIndex}
										onPageSizeChange={(nextPageSize) => {
											setPageSize(nextPageSize);
											setPageIndex(0);
										}}
										className='px-0'
									/>
								)}
							</div>
						</>
					)}
				</div>
			</div>

			<DataTableEditConfirm isEdit />
		</FormProvider>
	);
}
