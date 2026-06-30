/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { ClientPagination } from '@/components/datatable/client-pagination';
import { FormArray } from '@/components/form/form-array';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
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
import { useEffect, useMemo, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import SearchIcon from '@mui/icons-material/Search';

export function LongtermMaterialCostForm({
	id,
	plan,
	output,
	callback,
}: ProductCostFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const [detailItems, setDetailItems] = useState<LongtermMaterialDetailItem[]>(
		[],
	);
	const groupedByProcessGroup = useMemo(() => {
		if (detailItems.length === 0) return [];
		const groupMap = new Map<
			string,
			{
				processGroupId: string;
				processGroupCode: string;
				processGroupName: string;
				indexes: number[];
			}
		>();
		detailItems.forEach((item, index) => {
			const key = item.processGroupId ?? 'ungrouped';
			if (!groupMap.has(key)) {
				groupMap.set(key, {
					processGroupId: key,
					processGroupCode: item.processGroupCode ?? '',
					processGroupName: item.processGroupName ?? 'Chưa có nhóm công đoạn',
					indexes: [],
				});
			}
			groupMap.get(key)!.indexes.push(index);
		});
		return Array.from(groupMap.values());
	}, [detailItems]);
	const [acceptanceReportId, setAcceptanceReportId] = useState<string>('');
	const [loading, setLoading] = useState(false);
	const [pageIndex, setPageIndex] = useState(0);
	const [pageSize, setPageSize] = useState(10);
	const [searchKeyword, setSearchKeyword] = useState('');
	const previousAllocationRateRef = useRef<Record<number, number>>({});

	const getMaterialCode = (item?: LongtermMaterialDetailItem) =>
		item?.materialCode || item?.partCode || '';

	const getMaterialName = (item?: LongtermMaterialDetailItem) =>
		item?.materialName || item?.partName || '';

	const normalizeText = (text: string) =>
		text
			.normalize('NFD')
			.replace(/[\u0300-\u036f]/g, '')
			.toLowerCase()
			.trim();

	const form = useForm<LongtermMaterialCostSchema>({
		resolver: zodResolver(longtermMaterialCostSchema),
		mode: 'onSubmit',
		defaultValues: LONGTERM_MATERIAL_COST_DEFAULT,
	});
	const watchedItems = useWatch({
		control: form.control,
		name: 'items',
	});

	const visibleItemIndexes = useMemo(() => {
		const normalizedSearch = normalizeText(searchKeyword);
		const sourceItems = watchedItems ?? [];

		return detailItems
			.map((item, index) => ({ item, index, formItem: sourceItems[index] }))
			.filter(({ item, formItem }) => {
				if (!normalizedSearch) return true;

				const keywords = [
					item.processGroupCode,
					item.processGroupName,
					item.materialCode,
					item.materialName,
					item.trackedMaterialCode,
					item.trackedMaterialName,
					item.partCode,
					item.partName,
					item.unitOfMeasureName,
					formItem?.note,
				]
					.filter(Boolean)
					.map((value) => normalizeText(String(value)));

				return keywords.some((value) => value.includes(normalizedSearch));
			})
			.map(({ index }) => index);
	}, [detailItems, searchKeyword, watchedItems]);

	useEffect(() => {
		if (!id) return;

		const fetchDetail = async () => {
			try {
				setLoading(true);
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
				previousAllocationRateRef.current = {};

				form.reset({
					items: resolvedItems.map((item, index) => {
						const normalizedRate =
							(item.usageTime ?? 0) <= 0
								? 0
								: item.isFullAccounting &&
									  (!item.allocationRatio || item.allocationRatio <= 0)
									? 1
									: item.allocationRatio;
						previousAllocationRateRef.current[index] = normalizedRate;

						return {
							id: item.id,
							usageTime: item.usageTime ?? 0,
							// Khi isFullAccounting = true thì để trống tỷ lệ phân bổ
							allocationRate: item.isFullAccounting
								? undefined
								: normalizedRate,
							isFullAccounting: item.isFullAccounting ?? false,
							note: item.note ?? '',
						};
					}),
				});
				setPageIndex(0);
			} catch (err) {
				error(err);
			} finally {
				setLoading(false);
			}
		};

		fetchDetail();
	}, [error, form, id]);

	const pageCount = Math.ceil(visibleItemIndexes.length / pageSize);
	const safePageIndex =
		pageCount === 0 ? 0 : Math.min(pageIndex, Math.max(pageCount - 1, 0));
	const paginatedIndexes = useMemo(() => {
		const start = safePageIndex * pageSize;
		return visibleItemIndexes.slice(start, start + pageSize);
	}, [pageSize, safePageIndex, visibleItemIndexes]);

	useEffect(() => {
		setPageIndex(0);
	}, [searchKeyword]);

	useEffect(() => {
		watchedItems?.forEach((item, index) => {
			if (!item) return;
			if ((item.usageTime ?? 0) <= 0 && (item.allocationRate ?? 0) !== 0) {
				form.setValue(`items.${index}.allocationRate`, 0);
			}
		});
	}, [form, watchedItems]);

	const calculateQuotaAccountingValue = (
		item: LongtermMaterialDetailItem | undefined,
		usageTime: number,
	) => {
		if (!item || usageTime <= 0) return 0;

		const remainingPeriod = usageTime - (item.allocatedTime ?? 0);

		if (remainingPeriod > 0) {
			return (((item.totalValueToAccount ?? 0) / (usageTime || 1)) *
				(item.plannedOutput || 0)) /
				(item.standardOutput || 1);
		}

		if (remainingPeriod === 0) {
			return item.totalValueToAccount ?? 0;
		}

		return 0;
	};

	const handleFullAccountingChange = (index: number, checked: boolean) => {
		const currentAllocationRate = form.getValues(
			`items.${index}.allocationRate`,
		);
		const currentUsageTime = form.getValues(`items.${index}.usageTime`) ?? 0;
		const currentItem = detailItems[index];
		form.setValue(`items.${index}.isFullAccounting`, checked);

		if (checked) {
			// Lưu lại tỷ lệ hiện tại trước khi xóa
			if (typeof currentAllocationRate === 'number') {
				previousAllocationRateRef.current[index] = currentAllocationRate;
			}
			const quotaAccountingValue = calculateQuotaAccountingValue(
				currentItem,
				currentUsageTime,
			);
			const accountedValueThisPeriod = currentItem?.totalValueToAccount ?? 0;
			const fullAccountingRate =
				quotaAccountingValue > 0
					? accountedValueThisPeriod / quotaAccountingValue
					: 0;
			form.setValue(`items.${index}.allocationRate`, fullAccountingRate);
		} else {
			// Khôi phục tỷ lệ phân bổ trước đó
			const previousAllocationRate = previousAllocationRateRef.current[index];
			if (typeof previousAllocationRate === 'number') {
				form.setValue(`items.${index}.allocationRate`, previousAllocationRate);
			} else if (currentUsageTime <= 0) {
				form.setValue(`items.${index}.allocationRate`, 0);
			}
		}
	};

	const handleSubmit = async (values: LongtermMaterialCostSchema) => {
		try {
			const logBody = values.items
				.map((item, index) => ({ item, source: detailItems[index] }))
				.filter(({ source }) => !source?.isAnchorSeed)
				.map(({ item }) => ({
					id: item.id,
					acceptanceReportId: acceptanceReportId,
					usageTime: item.usageTime,
					allocationRatio: item.allocationRate,
					isFullAccounting: item.isFullAccounting,
					note: item.note ?? '',
				}));
			const departmentId = output?.departmentId ?? plan?.departmentId ?? '';
			const anchorSeedBody = values.items
				.map((item, index) => ({ item, source: detailItems[index] }))
				.filter(({ source }) => source?.isAnchorSeed)
				.map(({ item, source }) => ({
					id: item.id,
					departmentId,
					trackedMaterialId: source?.trackedMaterialId ?? source?.materialId,
					materialId: source?.materialId,
					partId: source?.partId ?? source?.materialId,
					processGroupId: source?.processGroupId ?? '',
					issuedQuantity: source?.issuedQuantity ?? 0,
					unitPrice: source?.unitPrice ?? 0,
					pendingValueStartPeriod: source?.pendingValueStartPeriod ?? 0,
					usageTime: item.usageTime,
					allocatedTime: source?.allocatedTime ?? 0,
					allocationRatio: item.allocationRate,
					note: item.note ?? '',
				}));

			if (!logBody.length && !anchorSeedBody.length) {
				setOpen(false);
				return;
			}

			if (logBody.length) {
				await api.put(
					API.PRODUCTION.ACCEPTANCE_REPORT.UPDATE_LONG_TERM_TRACKING,
					logBody,
				);
			}

			if (anchorSeedBody.length) {
				if (!departmentId) {
					throw new Error(
						'Không xác định được đơn vị để cập nhật mốc gốc hạch toán dài kỳ.',
					);
				}

				await api.put(API.PRODUCTION.LONG_TERM_ANCHOR_SEED.UPDATE, {
					departmentId,
					items: anchorSeedBody,
					processGroupMetrics: [],
				});
			}

			success(`${breadcrumb} đã được cập nhật thành công.`);
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='flex max-h-[70vh] flex-col'>
				<div className='scrollbar-sm overflow-auto'>
				{loading ? (
						<div className='flex h-60 items-center justify-center'>
							<div className='flex flex-col items-center gap-3 text-sm text-slate-500'>
								<Spinner />
								<span>Đang tải dữ liệu...</span>
							</div>
						</div>
					) : (
						<>
							<div className='sticky left-0 top-0 z-20 mb-4 w-full bg-slate-50/95 pb-2 backdrop-blur-sm'>
								<div className='relative w-full'>
									<SearchIcon className='pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-slate-400' />
									<Input
										className='h-11 w-full rounded-md border-slate-300 bg-white pl-10'
										value={searchKeyword}
										placeholder='Tìm theo mã, tên vật tư, nhóm công đoạn...'
										onChange={(event) => setSearchKeyword(event.target.value)}
									/>
								</div>
							</div>
							<div className='inline-flex min-w-max flex-col gap-4 pr-4'>
								{groupedByProcessGroup.length > 1 ? (
									groupedByProcessGroup.map((group) => {
										const paginatedInGroup = paginatedIndexes.filter((i) =>
											group.indexes.includes(i),
										);
										if (paginatedInGroup.length === 0) return null;
										return (
											<div
												key={group.processGroupId}
												className='flex flex-col gap-4'
											>
												<div className='sticky left-0 rounded bg-gray-200 px-4 py-2 text-sm font-semibold text-slate-700'>
													{group.processGroupCode
														? `${group.processGroupCode} - ${group.processGroupName}`
														: group.processGroupName}
												</div>
												<FormArray
													control={form.control}
													name='items'
													renderIndexes={paginatedInGroup}
													hasAddButton={false}
													hasCloseButton={false}
												>
													{(index) => {
														const item = detailItems[index];
														const isFullAccounting =
															form.watch(`items.${index}.isFullAccounting`) ??
															false;
														const watchedUsageTime =
															form.watch(`items.${index}.usageTime`) ?? 0;
														const watchedAllocationRate = form.watch(
															`items.${index}.allocationRate`,
														);
														const effectiveAllocationRate =
															watchedUsageTime <= 0
																? 0
																: (watchedAllocationRate ?? 1);
														const isUsageTimeEditable = true;
														const isAnchorSeed = item?.isAnchorSeed === true;
														const remainingPeriod =
															watchedUsageTime - (item?.allocatedTime ?? 0);
														const displayAllocatedTime = isFullAccounting
															? watchedUsageTime
															: (item?.allocatedTime ?? 0);
														const displayRemainingPeriod = isFullAccounting
															? 0
															: remainingPeriod;
														const quotaAccountingValue =
															calculateQuotaAccountingValue(
																item,
																watchedUsageTime,
															);
														const amount =
															(item?.issuedQuantity ?? 0) *
															(item?.unitPrice ?? 0);
														const totalAccountingValue =
															(item?.pendingValueStartPeriod ?? 0) + amount;
														const currentPeriodValue = isFullAccounting
															? (item?.totalValueToAccount ?? 0)
															: Math.min(
																	item?.totalValueToAccount ?? 0,
																	quotaAccountingValue *
																		effectiveAllocationRate,
																);
														const endingBalance = isFullAccounting
															? 0
															: Math.max(
																	0,
																	totalAccountingValue - currentPeriodValue,
																);

														return (
															<>
																<div className='min-w-40 flex-1 space-y-2'>
																	<Label>Mã vật tư</Label>
																	<Input
																		readOnly
																		value={getMaterialCode(item)}
																	/>
																</div>

																<div className='min-w-48 flex-1 space-y-2'>
																	<Label>Tên vật tư</Label>
																	<Input
																		readOnly
																		value={getMaterialName(item)}
																	/>
																</div>

																<div className='min-w-24 flex-1 space-y-2'>
																	<Label>ĐVT</Label>
																	<Input
																		readOnly
																		value={item?.unitOfMeasureName ?? ''}
																	/>
																</div>

																<div className='min-w-56 flex-1 space-y-2'>
																	<Label>
																		Giá trị chờ hạch toán đầu kỳ (đ)
																	</Label>
																	<Input
																		readOnly
																		value={formatNumber(
																			item?.pendingValueStartPeriod ?? 0,
																		)}
																	/>
																</div>

																<div className='min-w-32 flex-1 space-y-2'>
																	<Label>Số lượng</Label>
																	<Input
																		readOnly
																		value={formatNumber(
																			item?.issuedQuantity ?? 0,
																		)}
																	/>
																</div>

																<div className='min-w-32 flex-1 space-y-2'>
																	<Label>Đơn giá</Label>
																	<Input
																		readOnly
																		value={formatNumber(item?.unitPrice ?? 0)}
																	/>
																</div>

																<div className='min-w-32 flex-1 space-y-2'>
																	<Label>Thành tiền</Label>
																	<Input
																		readOnly
																		value={formatNumber(amount)}
																	/>
																</div>

																<div className='min-w-56 flex-1 space-y-2'>
																	<Label>Tổng giá trị cần hạch toán (đ)</Label>
																	<Input
																		readOnly
																		value={formatNumber(totalAccountingValue)}
																	/>
																</div>

																<div className='min-w-40 flex-1 space-y-2'>
																	<Label>Nguyên giá (đ)</Label>
																	<Input
																		readOnly
																		value={formatNumber(
																			item?.originAmount ?? 0,
																		)}
																	/>
																</div>

																<div className='min-w-44 flex-1 space-y-2'>
																	{isUsageTimeEditable ? (
																		<FormNumber
																			control={form.control}
																			name={`items.${index}.usageTime`}
																			label='Thời gian sử dụng (Ti)'
																			placeholder='Nhập thời gian sử dụng'
																		/>
																	) : (
																		<>
																			<Label>Thời gian sử dụng (Ti)</Label>
																			<Input
																				readOnly
																				value={formatNumber(watchedUsageTime)}
																			/>
																		</>
																	)}
																</div>

																{/* Thời gian đã phân bổ: hiển thị = usageTime khi hạch toán hết */}
																<div className='min-w-44 flex-1 space-y-2'>
																	<Label>Thời gian đã phân bổ</Label>
																	<Input
																		readOnly
																		value={formatNumber(displayAllocatedTime)}
																	/>
																</div>

																{/* Thời gian còn lại: hiển thị = 0 khi hạch toán hết */}
																<div className='min-w-40 flex-1 space-y-2'>
																	<Label>Thời gian còn lại</Label>
																	<Input
																		readOnly
																		value={formatNumber(displayRemainingPeriod)}
																	/>
																</div>

																<div className='min-w-60 flex-1 space-y-2'>
																	<Label>
																		Giá trị cần hạch toán theo định mức (đ)
																	</Label>
																	<Input
																		readOnly
																		value={formatNumber(quotaAccountingValue)}
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
																				handleFullAccountingChange(
																					index,
																					checked,
																				)
																			}
																		/>
																	</div>
																</div>

																<div className='min-w-56 flex-1 space-y-2'>
																	<Label>Giá trị hạch toán kỳ này (đ)</Label>
																	<Input
																		readOnly
																		value={formatNumber(currentPeriodValue)}
																	/>
																</div>

																<div className='min-w-60 flex-1 space-y-2'>
																	<Label>
																		Giá trị cuối kỳ chờ hạch toán kỳ sau (đ)
																	</Label>
																	<Input
																		readOnly
																		value={formatNumber(endingBalance)}
																	/>
																</div>

																<div className='min-w-60 flex-1 space-y-2'>
																	{isAnchorSeed ? (
																		<>
																			<Label>Ghi chú</Label>
																			<Input
																				readOnly
																				value={
																					form.watch(`items.${index}.note`) ??
																					''
																				}
																			/>
																		</>
																	) : (
																		<FormInput
																			control={form.control}
																			name={`items.${index}.note`}
																			label='Ghi chú'
																			placeholder='Nhập ghi chú'
																		/>
																	)}
																</div>
															</>
														);
													}}
												</FormArray>
											</div>
										);
									})
								) : (
									<FormArray
										control={form.control}
										name='items'
										renderIndexes={paginatedIndexes}
										hasAddButton={false}
										hasCloseButton={false}
									>
										{(index) => {
											const item = detailItems[index];
											const isFullAccounting =
												form.watch(`items.${index}.isFullAccounting`) ?? false;
											const watchedUsageTime =
												form.watch(`items.${index}.usageTime`) ?? 0;
											const watchedAllocationRate = form.watch(
												`items.${index}.allocationRate`,
											);
											const effectiveAllocationRate =
												watchedUsageTime <= 0
													? 0
													: (watchedAllocationRate ?? 1);
											const isUsageTimeEditable = true;
											const isAnchorSeed = item?.isAnchorSeed === true;
											const remainingPeriod =
												watchedUsageTime - (item?.allocatedTime ?? 0);
											const displayAllocatedTime = isFullAccounting
												? watchedUsageTime
												: (item?.allocatedTime ?? 0);
											const displayRemainingPeriod = isFullAccounting
												? 0
												: remainingPeriod;
											const quotaAccountingValue =
												calculateQuotaAccountingValue(
													item,
													watchedUsageTime,
												);
											const amount =
												(item?.issuedQuantity ?? 0) * (item?.unitPrice ?? 0);
											const totalAccountingValue =
												(item?.pendingValueStartPeriod ?? 0) + amount;
											const currentPeriodValue = isFullAccounting
												? (item?.totalValueToAccount ?? 0)
												: Math.min(
														item?.totalValueToAccount ?? 0,
														quotaAccountingValue * effectiveAllocationRate,
													);
											const endingBalance = isFullAccounting
												? 0
												: Math.max(
														0,
														totalAccountingValue - currentPeriodValue,
													);

											return (
												<>
													<div className='min-w-40 flex-1 space-y-2'>
														<Label>Mã vật tư</Label>
														<Input readOnly value={getMaterialCode(item)} />
													</div>

													<div className='min-w-48 flex-1 space-y-2'>
														<Label>Tên vật tư</Label>
														<Input readOnly value={getMaterialName(item)} />
													</div>

													<div className='min-w-24 flex-1 space-y-2'>
														<Label>ĐVT</Label>
														<Input
															readOnly
															value={item?.unitOfMeasureName ?? ''}
														/>
													</div>

													<div className='min-w-56 flex-1 space-y-2'>
														<Label>Giá trị chờ hạch toán đầu kỳ (đ)</Label>
														<Input
															readOnly
															value={formatNumber(
																item?.pendingValueStartPeriod ?? 0,
															)}
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
														<Input
															readOnly
															value={formatNumber(item?.unitPrice ?? 0)}
														/>
													</div>

													<div className='min-w-32 flex-1 space-y-2'>
														<Label>Thành tiền</Label>
														<Input readOnly value={formatNumber(amount)} />
													</div>

													<div className='min-w-56 flex-1 space-y-2'>
														<Label>Tổng giá trị cần hạch toán (đ)</Label>
														<Input
															readOnly
															value={formatNumber(totalAccountingValue)}
														/>
													</div>

													<div className='min-w-40 flex-1 space-y-2'>
														<Label>Nguyên giá (đ)</Label>
														<Input
															readOnly
															value={formatNumber(item?.originAmount ?? 0)}
														/>
													</div>

													<div className='min-w-44 flex-1 space-y-2'>
														{isUsageTimeEditable ? (
															<FormNumber
																control={form.control}
																name={`items.${index}.usageTime`}
																label='Thời gian sử dụng (Ti)'
																placeholder='Nhập thời gian sử dụng'
															/>
														) : (
															<>
																<Label>Thời gian sử dụng (Ti)</Label>
																<Input
																	readOnly
																	value={formatNumber(watchedUsageTime)}
																/>
															</>
														)}
													</div>

													{/* Thời gian đã phân bổ: hiển thị = usageTime khi hạch toán hết */}
													<div className='min-w-44 flex-1 space-y-2'>
														<Label>Thời gian đã phân bổ</Label>
														<Input
															readOnly
															value={formatNumber(displayAllocatedTime)}
														/>
													</div>

													{/* Thời gian còn lại: hiển thị = 0 khi hạch toán hết */}
													<div className='min-w-40 flex-1 space-y-2'>
														<Label>Thời gian còn lại</Label>
														<Input
															readOnly
															value={formatNumber(displayRemainingPeriod)}
														/>
													</div>

													<div className='min-w-60 flex-1 space-y-2'>
														<Label>
															Giá trị cần hạch toán theo định mức (đ)
														</Label>
														<Input
															readOnly
															value={formatNumber(quotaAccountingValue)}
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
															value={formatNumber(currentPeriodValue)}
														/>
													</div>

													<div className='min-w-60 flex-1 space-y-2'>
														<Label>
															Giá trị cuối kỳ chờ hạch toán kỳ sau (đ)
														</Label>
														<Input
															readOnly
															value={formatNumber(endingBalance)}
														/>
													</div>

													<div className='min-w-60 flex-1 space-y-2'>
														{isAnchorSeed ? (
															<>
																<Label>Ghi chú</Label>
																<Input
																	readOnly
																	value={
																		form.watch(`items.${index}.note`) ?? ''
																	}
																/>
															</>
														) : (
															<FormInput
																control={form.control}
																name={`items.${index}.note`}
																label='Ghi chú'
																placeholder='Nhập ghi chú'
															/>
														)}
													</div>
												</>
											);
										}}
									</FormArray>
								)}
								{visibleItemIndexes.length > 0 && (
									<ClientPagination
										totalItems={visibleItemIndexes.length}
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

			<DataTableEditConfirm isEdit={!!id} />
		</FormProvider>
	);
}
