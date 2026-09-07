import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { useMeta } from '@/data/meta/meta-hook';
import { API } from '@/constants/api-enpoint';
import {
	LongwallElectricity,
	LongwallElectricityPeriod,
	LONGWALL_ELECTRICITY_COLUMNS,
} from '@/features/main/pricing/longwall-panel/electricity/columns';
import { ElectricityForm } from '@/features/main/pricing/longwall-panel/electricity/form';
import { api } from '@/lib/api';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import { usePermission } from '@/hooks/use-permission';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useState } from 'react';

export function MainPricingLongwallElectricityPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (
		row: LongwallElectricity,
	): boolean | 'indeterminate' => {
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

	const handleRowSelectChange = (
		row: LongwallElectricity,
		checked: boolean,
	) => {
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
		rows: LongwallElectricity[],
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

	const handleDelete = async ({
		data,
	}: ActionDialogProps<LongwallElectricity>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.DELETES,
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
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallElectricity>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.IMPORT,
				file,
			);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
			}
			await data?.refresh();
		} catch (error) {
			popup.error(error);
		}
	};

	const transformData = (
		rows: LongwallElectricity[],
	): LongwallElectricity[] => {
		const groupMap = new Map<string, LongwallElectricity>();

		rows.forEach((row) => {
			const key = row.equipmentId || row.equipmentCode;
			const existing = groupMap.get(key);

			const sPdmVal =
				row.sPdm ?? (row.pdm && row.quantity ? row.pdm * row.quantity : 0);

			const period: LongwallElectricityPeriod = {
				id: row.id,
				startMonth: row.startMonth ?? '',
				endMonth: row.endMonth ?? '',
				equipmentElectricityCost: row.equipmentElectricityCost ?? 0,
				quantity: row.quantity ?? 0,
				pdm: row.pdm ?? 0,
				sPdm: sPdmVal,
				kyc: row.kyc ?? 0,
				kdt: row.kdt ?? 0,
				workingHour: row.workingHour ?? 0,
				workingDate: row.workingDate ?? 0,
				longwallAverageMonthlyTunnelProduction:
					row.longwallAverageMonthlyTunnelProduction ?? 0,
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
							equipmentElectricityCost: latestPeriod.equipmentElectricityCost,
							quantity: latestPeriod.quantity,
							pdm: latestPeriod.pdm,
							sPdm: latestPeriod.sPdm,
							kyc: latestPeriod.kyc,
							kdt: latestPeriod.kdt,
							workingHour: latestPeriod.workingHour,
							workingDate: latestPeriod.workingDate,
							longwallAverageMonthlyTunnelProduction:
								latestPeriod.longwallAverageMonthlyTunnelProduction,
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
			columns={LONGWALL_ELECTRICITY_COLUMNS}
			url={API.PRICING.ELECTRICITY.LONGWALL_PANEL.LIST}
			transformData={transformData}
			getRowId={(row) => row.equipmentId || row.id}
			filters={[
				{ key: 'equipmentCode', label: 'Nhóm vật tư, tài sản' },
				{ key: 'equipmentName', label: 'Tên nhóm vật tư, tài sản' },
			]}
			onCreate={
				hasPermission('pricing.longwallelectricityunitprice.create')
					? (props) => <ElectricityForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission('pricing.longwallelectricityunitprice.create')
					? (props) => <ElectricityForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission('pricing.longwallelectricityunitprice.update')
					? (props) => <ElectricityForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission('pricing.longwallelectricityunitprice.delete')
					? handleDelete
					: undefined
			}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={
				hasPermission('pricing.longwallelectricityunitprice.export')
					? handleExport
					: undefined
			}
			onImport={
				hasPermission('pricing.longwallelectricityunitprice.import')
					? handleImport
					: undefined
			}
			onExpand={(props) => (
				<LongwallElectricityPeriodExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

function LongwallElectricityPeriodExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: ActionDialogProps<LongwallElectricity> & {
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
							const spdm =
								period.sPdm ?? (period.pdm ? period.pdm * period.quantity : 0);
							const ptt = spdm * (period.kyc ?? 0) * (period.kdt ?? 0);

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

									{/* TẦNG 3: BẢNG CHI TIẾT CÁC THÔNG SỐ VÀ ĐỊNH MỨC ĐIỆN NĂNG */}
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
																	Số lượng
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Pđm (kW)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	SPđm (kW)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Kyc
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Kdt
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Ptt (kW)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Thời gian (h)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Ngày hoạt động
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Sản lượng than bình quân tháng (1000 tấn)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Điện năng cho 01 thiết bị/1 tấn than (kWh/tấn)
																</th>
																<th
																	scope='col'
																	className='px-4 py-2.5 text-right font-semibold text-black whitespace-nowrap'
																>
																	Chi phí Điện năng cho 1 thiết bị/1 tấn than (đ/tấn)
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
																	{formatNumber(period.quantity)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.pdm)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(spdm)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.kyc ?? 0)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.kdt ?? 0)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(ptt)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.workingHour ?? 0)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(period.workingDate ?? 0)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(
																		period.longwallAverageMonthlyTunnelProduction,
																	)}
																</td>
																<td className='px-4 py-2 text-right text-sm text-black whitespace-nowrap'>
																	{formatNumber(
																		period.electricityConsumePerMetres ?? 0,
																	)}
																</td>
																<td className='px-4 py-2 text-right text-sm font-semibold text-black whitespace-nowrap'>
																	{formatNumber(
																		period.electricityCostPerMetres ?? 0,
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
