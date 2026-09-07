import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	Electricity,
	ElectricityPeriod,
	MAIN_PRICING_ELECTRICITY_COLUMNS,
} from '@/features/main/pricing/trimming/electricity/columns';
import { ElectricityForm } from '@/features/main/pricing/trimming/electricity/form';
import { api } from '@/lib/api';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import { usePermission } from '@/hooks/use-permission';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useState } from 'react';

export function MainPricingTrimmingElectricityPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (row: Electricity): boolean | 'indeterminate' => {
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

	const handleRowSelectChange = (row: Electricity, checked: boolean) => {
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

	const handleSelectAllRowsChange = (checked: boolean, rows: Electricity[]) => {
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

	const handleDelete = async ({ data }: ActionDialogProps<Electricity>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.ELECTRICITY.TRIMMING.DELETES,
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
				API.PRICING.ELECTRICITY.TRIMMING.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Electricity>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.ELECTRICITY.TRIMMING.IMPORT,
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

	const transformData = (rows: Electricity[]): Electricity[] => {
		const groupMap = new Map<string, Electricity>();

		rows.forEach((row) => {
			const key = row.equipmentId || row.equipmentCode;
			const existing = groupMap.get(key);

			const period: ElectricityPeriod = {
				id: row.id,
				startMonth: row.startMonth ?? '',
				endMonth: row.endMonth ?? '',
				equipmentElectricityCost: row.equipmentElectricityCost ?? 0,
				monthlyElectricityCost: row.monthlyElectricityCost ?? 0,
				averageMonthlyTunnelProduction: row.averageMonthlyTunnelProduction ?? 0,
				electricityConsumePerMetres: row.electricityConsumePerMetres ?? 0,
				electricityCostPerMetres: row.electricityCostPerMetres ?? 0,
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

		return Array.from(groupMap.values()).map((item) => {
			const sortedPeriods = (item.periods ?? []).sort((a, b) =>
				(b.startMonth || '').localeCompare(a.startMonth || ''),
			);
			const latestPeriod = sortedPeriods[0];
			return {
				...item,
				...(latestPeriod
					? {
							id: latestPeriod.id,
							startMonth: latestPeriod.startMonth,
							endMonth: latestPeriod.endMonth,
							monthlyElectricityCost: latestPeriod.monthlyElectricityCost,
							averageMonthlyTunnelProduction:
								latestPeriod.averageMonthlyTunnelProduction,
							equipmentElectricityCost: latestPeriod.equipmentElectricityCost,
							electricityConsumePerMetres:
								latestPeriod.electricityConsumePerMetres,
							electricityCostPerMetres: latestPeriod.electricityCostPerMetres,
						}
					: {}),
				periods: sortedPeriods,
			};
		});
	};

	return (
		<DataTable
			columns={MAIN_PRICING_ELECTRICITY_COLUMNS}
			url={API.PRICING.ELECTRICITY.TRIMMING.LIST}
			transformData={transformData}
			getRowId={(row) => row.equipmentId || row.id}
			filters={[
				{ key: 'equipmentCode', label: 'Nhóm vật tư, tài sản' },
				{ key: 'equipmentName', label: 'Tên nhóm vật tư, tài sản' },
			]}
			onCreate={
				hasPermission('pricing.trimmingelectricityunitprice.create')
					? (props) => <ElectricityForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission('pricing.trimmingelectricityunitprice.create')
					? (props) => <ElectricityForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission('pricing.trimmingelectricityunitprice.update')
					? (props) => <ElectricityForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission('pricing.trimmingelectricityunitprice.delete')
					? handleDelete
					: undefined
			}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={
				hasPermission('pricing.trimmingelectricityunitprice.export')
					? handleExport
					: undefined
			}
			onImport={
				hasPermission('pricing.trimmingelectricityunitprice.import')
					? handleImport
					: undefined
			}
			onExpand={(props) => (
				<ElectricityPeriodExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

function ElectricityPeriodExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: ActionDialogProps<Electricity> & {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}) {
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const periods = row?.periods ?? [];

	const handleTogglePeriodDetails = (periodId: string) => {
		setExpandedPeriodIds((prev) =>
			prev.includes(periodId)
				? prev.filter((id) => id !== periodId)
				: [...prev, periodId],
		);
	};

	return (
		<div className='mx-6 my-2 overflow-hidden rounded-md border border-neutral-200 bg-white text-black'>
			<table className='w-full table-fixed text-left text-sm text-black'>
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
							className='w-20 px-4 py-2.5 text-center font-semibold text-black'
						>
							Xem
						</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{periods.length === 0 ? (
						<tr>
							<td colSpan={4} className='py-6 text-center text-sm text-black'>
								Chưa có khoảng thời gian nào được thiết lập.
							</td>
						</tr>
					) : (
						periods.map((period, index) => {
							const isSelected = expandedPeriodIds.includes(period.id);

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
										<td className='w-20 px-4 py-2 text-center'>
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
													onClick={() => handleTogglePeriodDetails(period.id)}
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

									{/* TẦNG 3: BẢNG CHI TIẾT ĐỊNH MỨC VÀ CHI PHÍ ĐIỆN NĂNG */}
									{isSelected && (
										<tr className='border-b border-neutral-200 bg-neutral-50/50'>
											<td colSpan={4} className='max-w-0 bg-neutral-50/40 p-3'>
												<div className='scrollbar-sm w-full overflow-x-auto rounded-md border border-neutral-200 bg-white shadow-xs'>
													<table className='w-full min-w-max text-left text-sm text-black'>
														<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black whitespace-nowrap'>
															<tr>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Đơn giá điện năng (đ/kwh)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Điện năng tiêu thụ 1 thiết bị/tháng
																	(Kwh/tháng)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Sản lượng xén lò bình quân tháng (m)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Điện năng tiêu thụ 1 thiết bị/1 mét lò xén
																	(kwh/m)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Chi phí điện năng 1 thiết bị/1 mét lò xén
																	(đ/m)
																</th>
															</tr>
														</thead>
														<tbody className='divide-y divide-neutral-200 whitespace-nowrap'>
															<tr className='transition-colors hover:bg-neutral-50/80'>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(
																		period.equipmentElectricityCost,
																	)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.monthlyElectricityCost)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(
																		period.averageMonthlyTunnelProduction,
																	)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(
																		period.electricityConsumePerMetres,
																	)}
																</td>
																<td className='px-4 py-2 text-right text-sm font-semibold text-black whitespace-nowrap'>
																	{formatNumber(
																		period.electricityCostPerMetres,
																	)}
																</td>
															</tr>
														</tbody>
													</table>
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
