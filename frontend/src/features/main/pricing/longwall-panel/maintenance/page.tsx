import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_PRICING_LONGWALL_PANEL_COLUMNS,
	MAIN_PRICING_LONGWALL_PANEL_EXPAND_COLUMNS,
	MaintainUnitPriceLongwallPanel,
	MaintenancePeriod,
	LongwallPanel,
} from '@/features/main/pricing/longwall-panel/maintenance/columns';
import { LongwallPanelForm } from '@/features/main/pricing/longwall-panel/maintenance/form';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useMemo, useState } from 'react';

export type LongwallPanelDetail = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	startMonth: string;
	endMonth: string;
	otherMaterialValue?: number;
	totalPrice: number;
	maintainUnitPriceEquipment: MaintainUnitPriceLongwallPanel[];
};

export function MainPricingMaintenanceLongwallPanelPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const query = useMemo(
		() => ({ maintainType: 2, ignorePagination: true }),
		[],
	);

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (row: LongwallPanel): boolean | 'indeterminate' => {
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

	const handleRowSelectChange = (row: LongwallPanel, checked: boolean) => {
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

	const handleSelectAllRowsChange = (
		checked: boolean,
		rows: LongwallPanel[],
	) => {
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

	const handleDelete = async ({ data }: ActionDialogProps<LongwallPanel>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.MAINTENANCE.LONGWALL_DELETES,
				selectedPeriodIds,
			);

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
			const filename = await api.export(
				API.PRICING.MAINTENANCE.LONGWALL_EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallPanel>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MAINTENANCE.LONGWALL_IMPORT,
				file,
			);
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

	const transformData = (rows: LongwallPanel[]): LongwallPanel[] => {
		const groupMap = new Map<string, LongwallPanel>();

		rows.forEach((row) => {
			const key = row.equipmentId || row.equipmentCode;
			const existing = groupMap.get(key);

			const period: MaintenancePeriod = {
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
			columns={MAIN_PRICING_LONGWALL_PANEL_COLUMNS}
			url={API.PRICING.MAINTENANCE.LIST}
			query={query}
			transformData={transformData}
			getRowId={(row) => row.id}
			filters={[{ key: 'equipmentCode', label: 'Nhóm vật tư, tài sản' }]}
			onCreate={
				hasPermission('pricing.longwallmaintainunitprice.create')
					? (props) => <LongwallPanelForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission('pricing.longwallmaintainunitprice.create')
					? (props) => <LongwallPanelForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission('pricing.longwallmaintainunitprice.update')
					? (props) => <LongwallPanelForm {...props} />
					: undefined
			}
			onExpand={(props) => (
				<LongwallPanelExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
			onDelete={
				hasPermission('pricing.longwallmaintainunitprice.delete')
					? handleDelete
					: undefined
			}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={
				hasPermission('pricing.longwallmaintainunitprice.export')
					? handleExport
					: undefined
			}
			onImport={
				hasPermission('pricing.longwallmaintainunitprice.import')
					? handleImport
					: undefined
			}
		/>
	);
}

export function LongwallPanelExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: ActionDialogProps<LongwallPanel> & {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}) {
	const popup = usePopup();
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const [periodDetailsMap, setPeriodDetailsMap] = useState<
		Record<string, MaintainUnitPriceLongwallPanel[]>
	>({});
	const [loadingPeriodMap, setLoadingPeriodMap] = useState<
		Record<string, boolean>
	>({});

	const handleTogglePeriodDetails = async (period: MaintenancePeriod) => {
		const isCurrentlyExpanded = expandedPeriodIds.includes(period.id);

		if (isCurrentlyExpanded) {
			setExpandedPeriodIds((prev) => prev.filter((id) => id !== period.id));
			return;
		}

		setExpandedPeriodIds((prev) => [...prev, period.id]);

		if (!periodDetailsMap[period.id]) {
			setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: true }));
			try {
				const res = await api.get<LongwallPanelDetail>(
					API.PRICING.MAINTENANCE.LONGWALL_DETAIL(period.id),
				);
				const detail = res.result;
				const items = detail?.maintainUnitPriceEquipment || [];
				const totalPartsCost = items.reduce(
					(sum: number, item: MaintainUnitPriceLongwallPanel) => {
						return sum + (item.materialCostPerMetres || 0);
					},
					0,
				);
				const itemsWithOther: MaintainUnitPriceLongwallPanel[] = [...items];
				if (detail?.otherMaterialValue) {
					const otherCost = (totalPartsCost * detail.otherMaterialValue) / 100;
					itemsWithOther.push({
						id: 'vtk-other',
						equipmentId: detail.equipmentId,
						equipmentCode: detail.equipmentCode,
						partId: 'vtk',
						partCode: 'VTK',
						partName: 'Vật tư khác',
						unitOfMeasureId: '',
						unitOfMeasureName: '',
						partCost: 0,
						replacementTimeStandard: 0,
						averageMonthlyTunnelProduction: 0,
						quantity: 0,
						materialRatePerMetres: detail.otherMaterialValue,
						materialCostPerMetres: otherCost,
					});
				}
				setPeriodDetailsMap((prev) => ({
					...prev,
					[period.id]: itemsWithOther,
				}));
			} catch (error) {
				popup.error(error);
			} finally {
				setLoadingPeriodMap((prev) => ({
					...prev,
					[period.id]: false,
				}));
			}
		}
	};

	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 overflow-hidden rounded-md border border-neutral-200 bg-white text-black'>
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
							Chi phí vật tư SCTX cho 1 thiết bị /1 tấn than NK (đ/t)
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
							const periodItems = periodDetailsMap[period.id] ?? [];
							const isLoading = loadingPeriodMap[period.id] ?? false;

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
												onCheckedChange={() => onTogglePeriodSelect(period.id)}
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
											{formatNumber(period.totalPrice)}
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
													title={isSelected ? 'Đóng chi tiết' : 'Xem chi tiết'}
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
													{isLoading ? (
														<div className='flex items-center justify-center py-6 text-sm text-black'>
															Đang tải dữ liệu chi tiết vật tư...
														</div>
													) : (
														<DataTable
															columns={
																MAIN_PRICING_LONGWALL_PANEL_EXPAND_COLUMNS
															}
															items={periodItems}
															hasActions={false}
															hasPagination={false}
															hasSort={false}
															compact={true}
															hideIndexHeader={true}
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
	);
}
