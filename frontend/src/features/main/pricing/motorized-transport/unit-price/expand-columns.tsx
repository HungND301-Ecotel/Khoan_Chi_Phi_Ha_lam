import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { cn } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { ColumnDef } from '@tanstack/react-table';
import { Fragment, useState } from 'react';
import {
	ExpandPriceRow,
	MechanizedTransportAssignmentGroup,
	MechanizedTransportUnitPriceGroupDto,
	MotorizedC2Item,
	MotorizedC3Item,
	VEHICLE_TYPE_LABELS,
} from './types';

export type { ExpandPriceRow };

// vehicleType từ backend: 1=Scania, 2=Hút bùn, 3=Phục vụ, 4=Xúc gạt nâng
const SCANIA_VEHICLE_TYPE = 1;

function formatMonth(val?: string) {
	if (!val) return '-';
	const parts = val.substring(0, 7).split('-');
	if (parts.length === 2) return `${parts[1]}/${parts[0]}`;
	return val;
}

// ====== Columns cho bảng đơn giá (nếu dùng DataTable) ======

export const EXPAND_PRICE_COLUMNS: ColumnDef<ExpandPriceRow>[] = [
	{
		accessorKey: 'equipmentQuality',
		header: 'Chất lượng',
		cell: ({ row }) => (
			<span className='text-black'>
				Thiết bị loại {row.original.equipmentQuality}
			</span>
		),
	},
	{
		accessorKey: 'haulDistanceValue',
		header: 'Cung độ vận tải',
		cell: ({ row }) => row.original.haulDistanceValue || '-',
	},
	{
		accessorKey: 'fuelUnitPrice',
		header: 'Đơn giá Nhiên liệu (đ)',
		cell: ({ row }) => row.original.fuelUnitPrice?.toLocaleString('vi-VN') ?? 0,
	},
	{
		accessorKey: 'powerUnitPrice',
		header: 'Đơn giá Động lực (đ)',
		cell: ({ row }) =>
			row.original.powerUnitPrice?.toLocaleString('vi-VN') ?? '-',
	},
	{
		accessorKey: 'maintenanceUnitPrice',
		header: 'Đơn giá SCTX (đ)',
		cell: ({ row }) =>
			row.original.maintenanceUnitPrice?.toLocaleString('vi-VN') ?? 0,
	},
];

// ====== TẦNG 4: BẢNG CHI TIẾT ĐƠN GIÁ THEO CUNG ĐỘ ======
// Chất lượng thiết bị đã hiển thị ở cấp 1 (mỗi dòng cấp 1 = 1 chất lượng) nên không lặp lại ở đây.
// Chỉ Xe Scania có Đơn giá Động lực.

const formatPrice = (value?: number | null, empty: string | number = 0) =>
	value != null ? value.toLocaleString('vi-VN') : empty;

export function MotorizedPriceDetailTable({
	rows,
	showPower = true,
}: {
	rows: ExpandPriceRow[];
	showPower?: boolean;
}) {
	const colCount = showPower ? 4 : 3;
	return (
		<div className='scrollbar-sm w-full overflow-x-auto rounded-md border border-neutral-200 bg-white p-3 shadow-xs'>
			<table className='w-full min-w-max text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold whitespace-nowrap text-black'>
					<tr>
						<th
							scope='col'
							className='px-4 py-2.5 font-semibold whitespace-nowrap text-black'
						>
							Cung độ vận tải
						</th>
						<th
							scope='col'
							className='px-4 py-2.5 text-right font-semibold whitespace-nowrap text-black'
						>
							Đơn giá Nhiên liệu (đ)
						</th>
						{showPower && (
							<th
								scope='col'
								className='px-4 py-2.5 text-right font-semibold whitespace-nowrap text-black'
							>
								Đơn giá Động lực (đ)
							</th>
						)}
						<th
							scope='col'
							className='px-4 py-2.5 text-right font-semibold whitespace-nowrap text-black'
						>
							Đơn giá SCTX (đ)
						</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200 whitespace-nowrap'>
					{rows.length === 0 ? (
						<tr>
							<td
								colSpan={colCount}
								className='py-4 text-center text-sm text-black'
							>
								Không có dữ liệu đơn giá
							</td>
						</tr>
					) : (
						rows.map((r, rIdx) => (
							<tr
								key={r.detailId || rIdx}
								className='transition-colors hover:bg-neutral-50/80'
							>
								<td className='px-4 py-2 text-sm whitespace-nowrap text-black'>
									{r.haulDistanceValue || '-'}
								</td>
								<td className='px-4 py-2 text-right text-sm whitespace-nowrap text-black'>
									{formatPrice(r.fuelUnitPrice)}
								</td>
								{showPower && (
									<td className='px-4 py-2 text-right text-sm whitespace-nowrap text-black'>
										{formatPrice(r.powerUnitPrice, '-')}
									</td>
								)}
								<td className='px-4 py-2 text-right text-sm whitespace-nowrap text-black'>
									{formatPrice(r.maintenanceUnitPrice)}
								</td>
							</tr>
						))
					)}
				</tbody>
			</table>
		</div>
	);
}

// Nút Xem / Đóng chi tiết dùng chung cho các tầng
function ToggleDetailButton({
	open,
	onClick,
	disabled = false,
}: {
	open: boolean;
	onClick: () => void;
	/** Làm mờ, không bấm được (công đoạn không có chi tiết) */
	disabled?: boolean;
}) {
	return (
		<div className='flex items-center justify-center'>
			<Button
				variant='ghost'
				size='icon'
				className={cn(
					'h-8 w-8 rounded-full bg-transparent shadow-none hover:bg-neutral-100 hover:shadow-none',
					open ? 'font-semibold text-black' : 'text-[#6e6e6e] hover:text-black',
					disabled && 'pointer-events-none opacity-30',
				)}
				title={
					disabled
						? 'Không có đơn giá theo cung độ'
						: open
							? 'Đóng chi tiết'
							: 'Xem chi tiết'
				}
				disabled={disabled}
				onClick={onClick}
			>
				{open ? (
					<VisibilityOffIcon fontSize='small' />
				) : (
					<VisibilityIcon fontSize='small' />
				)}
			</Button>
		</div>
	);
}

// ====== TẦNG 3: BẢNG CÔNG ĐOẠN SẢN XUẤT ======
// - Xe Scania: Công đoạn + Thông số (loại hàng, vị trí nhận/đổ) → tầng 4 đơn giá theo cung độ.
// - Phục vụ, Hút bùn, Xúc gạt nâng: đơn giá nằm ngay trên dòng công đoạn (Nhiên liệu, SCTX). Công
//   đoạn tính theo cung độ (Tưới đường mỏ, Giờ di chuyển...) xếp cuối, bấm Xem để mở bảng cung độ
//   (mặc định đóng); công đoạn không có cung độ thì nút Xem bị làm mờ.

export function MotorizedProcessExpand({
	c3Items,
	selectedC3Ids,
	onToggleC3Select,
}: {
	c3Items: MotorizedC3Item[];
	selectedC3Ids: string[];
	onToggleC3Select: (id: string) => void;
}) {
	const [expandedC3Ids, setExpandedC3Ids] = useState<string[]>([]);

	const isScania = c3Items[0]?.vehicleType === SCANIA_VEHICLE_TYPE;
	const colCount = isScania ? 4 : 5;
	const toggleIn = (setter: typeof setExpandedC3Ids, id: string) =>
		setter((prev) =>
			prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
		);
	const hasDistanceRows = (item: MotorizedC3Item) =>
		item.rows.some((r) => !!r.haulDistanceValue);
	// Nhóm khác Scania: công đoạn theo cung độ xếp cuối (giữ nguyên thứ tự còn lại)
	const orderedItems = isScania
		? c3Items
		: [
				...c3Items.filter((item) => !hasDistanceRows(item)),
				...c3Items.filter((item) => hasDistanceRows(item)),
			];

	return (
		// Bảng công đoạn rộng 70%, căn giữa bảng Thời gian / Nhóm (cấp 2) cho các cột không bị kéo quá xa
		<div className='mx-auto w-[70%] overflow-hidden rounded-md border border-neutral-200 bg-white text-black shadow-xs'>
			<table className='w-full table-fixed text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
					<tr>
						<th scope='col' className='w-10 px-3 py-2.5 text-center' />
						<th
							scope='col'
							className={cn(
								'px-4 py-2.5 font-semibold text-black',
								isScania && 'w-64',
							)}
						>
							Công đoạn sản xuất
						</th>
						{isScania ? (
							<th scope='col' className='px-4 py-2.5 font-semibold text-black'>
								Thông số
							</th>
						) : (
							<>
								<th
									scope='col'
									className='w-48 px-4 py-2.5 text-right font-semibold text-black'
								>
									Đơn giá Nhiên liệu (đ)
								</th>
								<th
									scope='col'
									className='w-48 px-4 py-2.5 text-right font-semibold text-black'
								>
									Đơn giá SCTX (đ)
								</th>
							</>
						)}
						<th
							scope='col'
							className='w-20 px-4 py-2.5 text-center font-semibold text-black'
						>
							Xem
						</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{c3Items.length === 0 ? (
						<tr>
							<td colSpan={colCount} className='py-6 text-center text-sm text-black'>
								Chưa có công đoạn sản xuất nào.
							</td>
						</tr>
					) : (
						orderedItems.map((item) => {
							const isSelected = expandedC3Ids.includes(item.id);
							// Có tầng 4 khi là Xe Scania hoặc công đoạn có đơn giá theo cung độ
							const hasDistance = hasDistanceRows(item);
							const hasDetail = isScania || hasDistance;
							// Mặc định đóng, bấm Xem để mở bảng chi tiết
							const showDetail = hasDetail && isSelected;
							const inlineRow = !hasDetail ? item.rows[0] : undefined;

							return (
								<Fragment key={item.id}>
									<tr
										className={cn(
											'border-b border-neutral-200 transition-colors hover:bg-neutral-50/80',
											showDetail && 'bg-neutral-50',
										)}
									>
										<td className='w-10 px-3 py-2 text-center'>
											<Checkbox
												checked={selectedC3Ids.includes(item.id)}
												onCheckedChange={() => onToggleC3Select(item.id)}
												aria-label={`Chọn ${item.productionProcessName}`}
											/>
										</td>
										<td className='px-4 py-2 text-sm text-black'>
											{item.productionProcessName}
										</td>
										{isScania ? (
											<td className='px-4 py-2 text-sm text-black'>
												{item.params}
											</td>
										) : (
											<>
												<td className='w-48 px-4 py-2 text-right text-sm text-black'>
													{inlineRow
														? formatPrice(inlineRow.fuelUnitPrice)
														: 'Theo cung độ'}
												</td>
												<td className='w-48 px-4 py-2 text-right text-sm text-black'>
													{inlineRow
														? formatPrice(inlineRow.maintenanceUnitPrice)
														: 'Theo cung độ'}
												</td>
											</>
										)}
										<td className='w-20 px-4 py-2 text-center'>
											<ToggleDetailButton
												open={showDetail}
												disabled={!hasDetail}
												onClick={() =>
													toggleIn(
														setExpandedC3Ids,
														item.id,
													)
												}
											/>
										</td>
									</tr>

									{/* TẦNG 4: BẢNG CHI TIẾT ĐƠN GIÁ THEO CUNG ĐỘ (Scania — có thêm Động lực) */}
									{isScania && showDetail && (
										<tr className='border-b border-neutral-200 bg-neutral-50/50'>
											<td colSpan={colCount} className='max-w-0 bg-neutral-50/40 p-3'>
												<MotorizedPriceDetailTable
													rows={item.rows}
													showPower={isScania}
												/>
											</td>
										</tr>
									)}

									{/* Nhóm khác: bảng cung độ bo tròn nằm gọn trong 3 cột Công đoạn / Nhiên liệu / SCTX,
									    2 cột đơn giá cùng độ rộng (w-48) nên thẳng hàng với dòng công đoạn phía trên */}
									{!isScania && showDetail && (
										<tr className='border-b border-neutral-200 bg-neutral-50'>
											<td className='w-10' />
											<td colSpan={3} className='py-2'>
												<div className='overflow-hidden rounded-md border border-neutral-300 bg-white shadow-xs'>
													<table className='w-full table-fixed text-left text-sm text-black'>
														<thead className='border-b border-neutral-200 bg-neutral-100 font-semibold'>
															<tr>
																<th className='px-4 py-2'>Cung độ vận tải</th>
																<th className='w-48 px-4 py-2 text-right'>
																	Đơn giá Nhiên liệu (đ)
																</th>
																<th className='w-48 px-4 py-2 text-right'>
																	Đơn giá SCTX (đ)
																</th>
															</tr>
														</thead>
														<tbody className='divide-y divide-neutral-100'>
															{item.rows.map((r, rIdx) => (
																<tr key={r.detailId || rIdx}>
																	<td className='px-4 py-1.5'>
																		{r.haulDistanceValue || '-'}
																	</td>
																	<td className='w-48 px-4 py-1.5 text-right'>
																		{formatPrice(r.fuelUnitPrice)}
																	</td>
																	<td className='w-48 px-4 py-1.5 text-right'>
																		{formatPrice(r.maintenanceUnitPrice)}
																	</td>
																</tr>
															))}
														</tbody>
													</table>
												</div>
											</td>
											<td className='w-20' />
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

// ====== TẦNG 2: BẢNG THỜI GIAN VÀ NHÓM XE ======

export function MotorizedSectionExpand({
	row,
	selectedC3Ids,
	onToggleC2Select,
	onToggleC3Select,
}: {
	row?: MechanizedTransportAssignmentGroup;
	selectedC3Ids: string[];
	onToggleC2Select: (c2Item: MotorizedC2Item, checked: boolean) => void;
	onToggleC3Select: (id: string) => void;
}) {
	const [expandedC2Ids, setExpandedC2Ids] = useState<string[]>([]);

	const handleToggleC2 = (id: string) => {
		setExpandedC2Ids((prev) =>
			prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
		);
	};

	const c2Items: MotorizedC2Item[] = row?.c2Items ?? [];

	const isC2Checked = (
		c2Item: MotorizedC2Item,
	): boolean | 'indeterminate' => {
		const items = c2Item.c3Items ?? [];
		if (items.length === 0) return false;
		const selectedCount = items.filter((item) =>
			selectedC3Ids.includes(item.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === items.length) return true;
		return 'indeterminate';
	};

	return (
		<div className='mx-6 my-2 overflow-hidden rounded-md bg-white text-black'>
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
						<th
							scope='col'
							className='w-48 px-4 py-2.5 font-semibold text-black'
						>
							Thời gian
						</th>
						<th scope='col' className='px-4 py-2.5 font-semibold text-black'>
							Nhóm
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
					{c2Items.length === 0 ? (
						<tr>
							<td colSpan={5} className='py-6 text-center text-sm text-black'>
								Chưa có thông tin nhóm phương tiện nào được thiết lập.
							</td>
						</tr>
					) : (
						c2Items.map((item, index) => {
							const isSelected = expandedC2Ids.includes(item.id);
							const checkedState = isC2Checked(item);

							return (
								<Fragment key={item.id}>
									<tr
										className={cn(
											'border-b border-neutral-200 transition-colors hover:bg-neutral-50/80',
											isSelected && 'bg-neutral-50',
										)}
									>
										<td className='w-10 px-3 py-2 text-center'>
											<Checkbox
												checked={checkedState}
												onCheckedChange={(checked) =>
													onToggleC2Select(item, !!checked)
												}
												aria-label={`Chọn ${item.vehicleLabel}`}
											/>
										</td>
										<td className='w-12 px-3 py-2 text-center text-sm text-black'>
											{index + 1}
										</td>
										<td className='w-48 px-4 py-2 text-sm text-black'>
											{formatMonth(item.startMonth)} -{' '}
											{formatMonth(item.endMonth)}
										</td>
										<td className='px-4 py-2 text-sm text-black'>
											{item.vehicleLabel}
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
													title={
														isSelected ? 'Đóng chi tiết' : 'Xem chi tiết'
													}
													onClick={() => handleToggleC2(item.id)}
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

									{/* TẦNG 3: BẢNG CÔNG ĐOẠN SẢN XUẤT VÀ THÔNG SỐ */}
									{isSelected && (
										<tr className='border-b border-neutral-200 bg-neutral-50/50'>
											<td colSpan={5} className='p-3'>
												<MotorizedProcessExpand
													c3Items={item.c3Items}
													selectedC3Ids={selectedC3Ids}
													onToggleC3Select={onToggleC3Select}
												/>
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

// ====== Helper functions (dành cho backwards compatibility nếu có nơi dùng) ======

export function buildExpandData(row: MechanizedTransportUnitPriceGroupDto) {
	const sections = row.sections || [];

	const groupedByProcess: Record<
		string,
		{
			vehicleType: number;
			vehicleLabel: string;
			productionProcessName: string;
			cargoTypeName?: string;
			receivingLocationName?: string;
			dumpingLocationName?: string;
			rows: ExpandPriceRow[];
		}
	> = {};

	sections.forEach((section) => {
		const key = `${section.vehicleType}_${section.productionProcessId}_${section.cargoTypeId || ''}_${section.receivingLocationId || ''}_${section.dumpingLocationId || ''}`;
		if (!groupedByProcess[key]) {
			groupedByProcess[key] = {
				vehicleType: section.vehicleType,
				vehicleLabel:
					VEHICLE_TYPE_LABELS[section.vehicleType] || 'Phương tiện',
				productionProcessName: section.productionProcessName || '-',
				cargoTypeName: section.cargoTypeName,
				receivingLocationName: section.receivingLocationName,
				dumpingLocationName: section.dumpingLocationName,
				rows: [],
			};
		}

		section.rows.forEach((row) => {
			groupedByProcess[key].rows.push({
				headerId: row.headerId,
				detailId: row.detailId,
				equipmentQuality: row.equipmentQuality,
				haulDistanceValue: row.haulDistanceValue,
				fuelUnitPrice: row.fuelUnitPrice,
				powerUnitPrice: row.powerUnitPrice,
				maintenanceUnitPrice: row.maintenanceUnitPrice,
			});
		});
	});

	Object.values(groupedByProcess).forEach((group) => {
		group.rows.sort((a, b) => {
			const qA = a.equipmentQuality || '';
			const qB = b.equipmentQuality || '';
			return qA.localeCompare(qB);
		});
	});

	return groupedByProcess;
}

