import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import {
	ExpandSlideCostRow,
	MAIN_PRICING_DETAIL_EXPAND_COLUMNS,
	MAIN_PRICING_SLIDE_COLUMNS,
	MAIN_PRICING_SLIDE_EXPAND_COLUMNS,
	Slide,
	SlidePeriod,
} from '@/features/main/pricing/tunneling/slide/columns';
import {
	SlideDetail,
	SlideForm,
} from '@/features/main/pricing/tunneling/slide/form';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useEffect, useMemo, useState } from 'react';

const deriveNormFromAmount = (amount: number, unitPrice: number) => {
	if (unitPrice > 0) {
		return amount / unitPrice;
	}

	return amount === 0 ? 0 : amount;
};

function buildGroupedExpandRows(
	materialCosts: SlideDetail['materialCost'],
): ExpandSlideCostRow[] {
	return materialCosts.flatMap((group) => {
		const groupItems = (group.costs ?? []).map((cost) => ({
			rowType: 'material-item' as const,
			assignmentCodeId: group.assignmentCodeId,
			assignmentCode: group.assignmentCode,
			assignmentCodeName: group.assignmentCodeName,
			materialId: cost.materialId,
			materialCode: cost.materialCode,
			materialName: cost.materialName,
			unitOfMeasureName: cost.unitOfMeasureName,
			unitPrice: cost.cost,
			norm: deriveNormFromAmount(cost.amount, cost.cost),
			totalPrice: cost.amount,
		}));
		const groupTotal = groupItems.reduce(
			(sum, item) => sum + item.totalPrice,
			0,
		);

		return [
			{
				rowType: 'group-summary' as const,
				assignmentCodeId: group.assignmentCodeId,
				assignmentCode: group.assignmentCode,
				assignmentCodeName: group.assignmentCodeName,
				materialId: `group-${group.assignmentCodeId}`,
				materialCode: '',
				materialName: '',
				unitOfMeasureName: '',
				unitPrice: null,
				norm: '',
				totalPrice: groupTotal,
			},
			...groupItems,
		];
	});
}

export function MainPricingSlidePage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (row: Slide): boolean | 'indeterminate' => {
		const periods = row.periods ?? [];
		if (periods.length === 0) {
			return selectedPeriodIds.includes(row.id);
		}
		const selectedCount = periods.filter((p) =>
			selectedPeriodIds.includes(p.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === periods.length) return true;
		return 'indeterminate';
	};

	const handleRowSelectChange = (row: Slide, checked: boolean) => {
		const periodIds =
			row.periods && row.periods.length > 0
				? row.periods.map((p) => p.id)
				: [row.id];

		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...periodIds]));
			} else {
				return prev.filter((id) => !periodIds.includes(id));
			}
		});
	};

	const handleSelectAllRowsChange = (checked: boolean, rows: Slide[]) => {
		const allPeriodIds = rows.flatMap((r) =>
			r.periods && r.periods.length > 0 ? r.periods.map((p) => p.id) : [r.id],
		);
		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allPeriodIds]));
			} else {
				return prev.filter((id) => !allPeriodIds.includes(id));
			}
		});
	};

	const handleTogglePeriodSelect = (periodId: string) => {
		setSelectedPeriodIds((prev) => {
			if (prev.includes(periodId)) {
				return prev.filter((id) => id !== periodId);
			} else {
				return [...prev, periodId];
			}
		});
	};

	const handleDelete = async ({ data }: ActionDialogProps<Slide>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(API.PRICING.SLIDE.DELETES, selectedPeriodIds);

			popup.success(
				`Đã xoá thành công ${selectedPeriodIds.length} khoảng thời gian ${breadcrumb}.`,
			);
			setSelectedPeriodIds([]);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.SLIDE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Slide>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.SLIDE.IMPORT, file);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	const transformData = (rows: Slide[]): Slide[] => {
		const groupMap = new Map<string, Slide>();

		rows.forEach((row) => {
			const key = `${row.code}__${row.processGroupId}__${row.passportId}__${row.hardnessId}`;
			const existing = groupMap.get(key);

			const period: SlidePeriod = {
				id: row.id,
				startMonth: row.startMonth ?? '',
				endMonth: row.endMonth ?? '',
				totalPrice: row.totalPrice ?? 0,
			};

			if (!existing) {
				groupMap.set(key, {
					...row,
					periods: [period],
				});
			} else {
				existing.periods = existing.periods ?? [];
				if (!existing.periods.some((p) => p.id === period.id)) {
					existing.periods.push(period);
				}
			}
		});

		return Array.from(groupMap.values()).map((item) => ({
			...item,
			periods: (item.periods ?? []).sort((a, b) =>
				b.startMonth.localeCompare(a.startMonth),
			),
		}));
	};

	return (
		<DataTable
			url={API.PRICING.SLIDE.LIST}
			transformData={transformData}
			columns={MAIN_PRICING_SLIDE_COLUMNS}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã định mức máng trượt' },
				{ key: 'processGroupName', label: 'Nhóm công đoạn sản xuất' },
				{ key: 'materialDetail', label: 'Thông số' },
			]}
			onCreate={
				hasPermission('pricing.slideunitprice.create')
					? (props) => <SlideForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission('pricing.slideunitprice.create')
					? (props) => <SlideForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission('pricing.slideunitprice.update')
					? (props) => <SlideForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission('pricing.slideunitprice.delete')
					? handleDelete
					: undefined
			}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={
				hasPermission('pricing.slideunitprice.export')
					? handleExport
					: undefined
			}
			onImport={
				hasPermission('pricing.tunnelslideunitprice.import')
					? handleImport
					: undefined
			}
			onExpand={(props) => (
				<SlideDetailExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

function SlideDetailExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: ActionDialogProps<Slide> & {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}) {
	const popup = usePopup();
	const [passport, setPassport] = useState<Passport>();
	const [strength, setStrength] = useState<Strength>();
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const [periodCostsMap, setPeriodCostsMap] = useState<
		Record<string, ExpandSlideCostRow[]>
	>({});
	const [loadingPeriodMap, setLoadingPeriodMap] = useState<
		Record<string, boolean>
	>({});

	useEffect(() => {
		if (!row) return;
		const promises = Promise.all([
			row.passportId
				? api.get<Passport>(
						API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId),
					)
				: Promise.resolve(undefined),
			row.hardnessId
				? api.get<Strength>(
						API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId),
					)
				: Promise.resolve(undefined),
		]);

		promises.then(([passportRes, strengthRes]) => {
			setPassport(passportRes?.result);
			setStrength(strengthRes?.result);
		});
	}, [row]);

	const handleTogglePeriodDetails = async (period: SlidePeriod) => {
		const isCurrentlyExpanded = expandedPeriodIds.includes(period.id);

		if (isCurrentlyExpanded) {
			setExpandedPeriodIds((prev) => prev.filter((id) => id !== period.id));
			return;
		}

		setExpandedPeriodIds((prev) => [...prev, period.id]);

		if (!periodCostsMap[period.id]) {
			setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: true }));
			try {
				const res = await api.get<SlideDetail>(
					API.PRICING.SLIDE.DETAIL(period.id),
				);
				const rows = buildGroupedExpandRows(res.result.materialCost ?? []);
				setPeriodCostsMap((prev) => ({ ...prev, [period.id]: rows }));
			} catch (error) {
				popup.error(error);
			} finally {
				setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: false }));
			}
		}
	};

	const detailItems = useMemo(
		() => [{ passport, strength }],
		[passport, strength],
	);
	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 flex flex-col gap-3 text-black'>
			{/* TẦNG 2: 1. BẢNG NHỎ THÔNG SỐ KỸ THUẬT */}
			<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
				<DataTable
					columns={MAIN_PRICING_DETAIL_EXPAND_COLUMNS}
					items={detailItems}
					hasActions={false}
					hasPagination={false}
					hasSort={false}
					hasIndex={false}
					compact={true}
				/>
			</div>

			{/* TẦNG 2: 2. BẢNG THỜI GIAN VÀ ĐƠN GIÁ MÁNG TRƯỢT */}
			<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
				<table className='w-full text-left text-sm text-black'>
					<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
						<tr>
							<th scope='col' className='w-10 px-3 py-2.5 text-center' />
							<th
								scope='col'
								className='w-12 px-3 py-2.5 text-center font-semibold text-black'
							>
								STT
							</th>
							<th scope='col' className='px-4 py-2.5 font-semibold text-black'>
								Thời gian áp dụng
							</th>
							<th
								scope='col'
								className='px-4 py-2.5 text-right font-semibold text-black'
							>
								Đơn giá máng trượt (đ/m)
							</th>
							<th
								scope='col'
								className='w-20 px-4 py-2.5 text-center font-semibold text-black'
							>
								Xem
							</th>
						</tr>
					</thead>
					<tbody className='divide-y divide-neutral-200'>
						{periods.length === 0 ? (
							<tr>
								<td colSpan={5} className='py-6 text-center text-sm text-black'>
									Chưa có khoảng thời gian nào được thiết lập.
								</td>
							</tr>
						) : (
							periods.map((period, index) => {
								const isSelected = expandedPeriodIds.includes(period.id);
								const periodCosts = periodCostsMap[period.id] ?? [];
								const isLoadingPeriodCosts =
									loadingPeriodMap[period.id] ?? false;

								return (
									<Fragment key={period.id}>
										<tr
											className={cn(
												'border-b border-neutral-200 transition-colors hover:bg-neutral-50/80',
												isSelected && 'bg-neutral-50',
											)}
										>
											<td className='w-10 px-3 py-2 text-center'>
												<Checkbox
													checked={selectedPeriodIds.includes(period.id)}
													onCheckedChange={() =>
														onTogglePeriodSelect(period.id)
													}
													aria-label={`Chọn khoảng thời gian ${formatDate(period.startMonth)} - ${formatDate(period.endMonth)}`}
												/>
											</td>
											<td className='w-12 px-3 py-2 text-center text-sm text-black'>
												{index + 1}
											</td>
											<td className='px-4 py-2 text-sm text-black'>
												{formatDate(period.startMonth)} -{' '}
												{formatDate(period.endMonth)}
											</td>
											<td className='px-4 py-2 text-right text-sm font-semibold text-black'>
												{formatNumber(period.totalPrice)} đ
											</td>
											<td className='px-4 py-2 text-center'>
												<div className='flex items-center justify-center'>
													<Button
														variant='ghost'
														size='icon'
														className={cn(
															'h-8 w-8 rounded-full bg-transparent shadow-none hover:bg-neutral-100 hover:shadow-none',
															isSelected
																? 'font-semibold text-black'
																: 'text-[#6e6e6e] hover:text-black',
														)}
														title={
															isSelected ? 'Đóng chi tiết' : 'Xem chi tiết'
														}
														onClick={() => handleTogglePeriodDetails(period)}
													>
														{isSelected ? (
															<VisibilityOffIcon fontSize='small' />
														) : (
															<VisibilityIcon fontSize='small' />
														)}
													</Button>
												</div>
											</td>
										</tr>

										{/* TẦNG 3: BẢNG CHI TIẾT NGAY DƯỚI KHOẢNG THỜI GIAN ĐƯỢC CHỌN */}
										{isSelected && (
											<tr className='border-b border-neutral-200 bg-neutral-50/50'>
												<td colSpan={5} className='bg-neutral-50/40 p-3'>
													<div className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow-xs'>
														{isLoadingPeriodCosts ? (
															<div className='flex items-center justify-center py-6 text-sm text-black'>
																Đang tải dữ liệu chi tiết vật tư...
															</div>
														) : (
															<DataTable
																columns={MAIN_PRICING_SLIDE_EXPAND_COLUMNS}
																items={periodCosts}
																hasActions={false}
																hasPagination={false}
																hasSort={false}
																hasIndex={false}
																compact={true}
															/>
														)}
													</div>
												</td>
											</tr>
										)}
									</Fragment>
								);
							})
						)}
					</tbody>
				</table>
			</div>
		</div>
	);
}
