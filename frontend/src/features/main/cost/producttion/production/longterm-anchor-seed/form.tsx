/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { ClientPagination } from '@/components/datatable/client-pagination';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
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
import SearchIcon from '@mui/icons-material/Search';

type LookupOption = {
	value: string;
	label: string;
};

type CodeNameLookupItem = {
	id: string;
	code: string;
	name: string;
};

const NONE_ASSIGNMENT_CODE_VALUE = '__none_assignment_code__';
const NONE_PRODUCTION_ORDER_VALUE = '__none_production_order__';

function normalizeNullableLookupValue(value?: string | null) {
	if (!value) return null;

	const normalizedValue = value.trim();
	if (
		normalizedValue === NONE_ASSIGNMENT_CODE_VALUE ||
		normalizedValue === NONE_PRODUCTION_ORDER_VALUE
	) {
		return null;
	}

	return normalizedValue.length > 0 ? normalizedValue : null;
}

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
	const [searchKeyword, setSearchKeyword] = useState('');
	const [assignmentCodeOptions, setAssignmentCodeOptions] = useState<
		LookupOption[]
	>([]);
	const [productionOrderOptions, setProductionOrderOptions] = useState<
		LookupOption[]
	>([]);
	const [processGroupOptions, setProcessGroupOptions] = useState<
		LookupOption[]
	>([]);

	const form = useForm<LongTermAnchorSeedSchema>({
		resolver: zodResolver(longTermAnchorSeedSchema),
		mode: 'onSubmit',
		defaultValues: LONG_TERM_ANCHOR_SEED_DEFAULT,
	});

	const normalizeText = (text: string) =>
		text
			.normalize('NFD')
			.replace(/[\u0300-\u036f]/g, '')
			.toLowerCase()
			.trim();

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
						categoryAssignmentCodeId:
							item.categoryAssignmentCodeId ??
							item.categoryEquipmentId ??
							NONE_ASSIGNMENT_CODE_VALUE,
						categoryProductionOrderId:
							item.categoryProductionOrderId ?? NONE_PRODUCTION_ORDER_VALUE,
						issuedQuantity: 0,
						unitPrice: 0,
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

	useEffect(() => {
		let isMounted = true;

		const fetchAssignmentCodes = async () => {
			try {
				const response = await api.pagging<CodeNameLookupItem>(
					API.CATALOG.CONTRACT_CODE.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				setAssignmentCodeOptions(
					(response.result.data ?? [])
						.slice()
						.sort((a, b) => a.code.localeCompare(b.code))
						.map((item) => ({
							value: item.id,
							label: `${item.code} - ${item.name}`,
						})),
				);
			} catch (err) {
				if (!isMounted) return;
				setAssignmentCodeOptions([]);
				error(err);
			}
		};

		fetchAssignmentCodes();

		return () => {
			isMounted = false;
		};
	}, [error]);

	useEffect(() => {
		let isMounted = true;

		const fetchProductionOrders = async () => {
			try {
				const response = await api.pagging<CodeNameLookupItem>(
					API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				setProductionOrderOptions(
					(response.result.data ?? [])
						.slice()
						.sort((a, b) => a.code.localeCompare(b.code))
						.map((item) => ({
							value: item.id,
							label: `${item.code} - ${item.name}`,
						})),
				);
			} catch (err) {
				if (!isMounted) return;
				setProductionOrderOptions([]);
				error(err);
			}
		};

		fetchProductionOrders();

		return () => {
			isMounted = false;
		};
	}, [error]);

	useEffect(() => {
		let isMounted = true;

		const fetchProcessGroups = async () => {
			try {
				const response = await api.pagging<CodeNameLookupItem>(
					API.CATALOG.PROCESS.GROUP.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				setProcessGroupOptions(
					(response.result.data ?? [])
						.slice()
						.sort((a, b) => a.code.localeCompare(b.code))
						.map((item) => ({
							value: item.id,
							label: `${item.code} - ${item.name}`,
						})),
				);
			} catch (err) {
				if (!isMounted) return;
				setProcessGroupOptions([]);
				error(err);
			}
		};

		fetchProcessGroups();

		return () => {
			isMounted = false;
		};
	}, [error]);

	const handleSubmit = async (values: LongTermAnchorSeedSchema) => {
		try {
			const payload = {
				...values,
				items: values.items.map((item) => ({
					...item,
					categoryAssignmentCodeId: normalizeNullableLookupValue(
						item.categoryAssignmentCodeId,
					),
					categoryProductionOrderId: normalizeNullableLookupValue(
						item.categoryProductionOrderId,
					),
				})),
			};

			await api.put(API.PRODUCTION.LONG_TERM_ANCHOR_SEED.UPDATE, {
				...payload,
				departmentId,
			});
			success('Mốc gốc hạch toán dài kỳ đã được cập nhật thành công.');
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	const visibleItemIndexes = useMemo(() => {
		const normalizedSearch = normalizeText(searchKeyword);
		const items = detailItems?.items ?? [];

		return items
			.map((item, index) => ({ item, index }))
			.filter(({ item }) => {
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
					item.categoryAssignmentCode,
					item.categoryAssignmentCodeName,
					item.categoryProductionOrderCode,
					item.categoryProductionOrderName,
					item.note,
				]
					.filter(Boolean)
					.map((value) => normalizeText(String(value)));

				return keywords.some((value) => value.includes(normalizedSearch));
			})
			.map(({ index }) => index);
	}, [detailItems, searchKeyword]);

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

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<div className='flex max-h-[70vh] flex-col overflow-hidden bg-transparent'>
				<div className='scrollbar-sm min-h-0 flex-1 overflow-auto'>
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
													<FormComboBox
														control={form.control}
														name={`items.${index}.processGroupId`}
														label='Nhóm công đoạn'
														options={processGroupOptions}
														placeholder='Chọn nhóm công đoạn'
													/>
												</div>

												<div className='min-w-64 flex-1 space-y-2'>
													<FormComboBox
														control={form.control}
														name={`items.${index}.categoryAssignmentCodeId`}
														label='Nhóm vật tư, tài sản'
														options={[
															{
																value: NONE_ASSIGNMENT_CODE_VALUE,
																label: 'Không thuộc nhóm vật tư, tài sản',
															},
															...assignmentCodeOptions,
														]}
														placeholder='Chọn nhóm vật tư, tài sản'
													/>
												</div>

												<div className='min-w-64 flex-1 space-y-2'>
													<FormComboBox
														control={form.control}
														name={`items.${index}.categoryProductionOrderId`}
														label='Lệnh sản xuất'
														options={[
															{
																value: NONE_PRODUCTION_ORDER_VALUE,
																label: 'Không thuộc Lệnh sản xuất',
															},
															...productionOrderOptions,
														]}
														placeholder='Chọn lệnh sản xuất'
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

												<div className='min-w-56 flex-1'>
													<FormNumber
														control={form.control}
														name={`items.${index}.pendingValueStartPeriod`}
														label='Tổng giá trị cần hạch toán (đ)'
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

			<DataTableEditConfirm isEdit />
		</FormProvider>
	);
}
