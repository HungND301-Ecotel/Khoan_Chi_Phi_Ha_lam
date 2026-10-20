import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { cn, formatDate } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useState } from 'react';
import {
	detectTransportMode,
	formatCodeName,
	formatMoney,
	getTransportUnitLabel,
	TransportPeriod,
	TransportUnitPrice,
} from './columns';

function DetailTable({
	headers,
	children,
}: {
	headers: string[];
	children: React.ReactNode;
}) {
	return (
		<div className='scrollbar-sm overflow-x-auto rounded-md border border-neutral-200 bg-white shadow-xs'>
			<table className='w-full min-w-max text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold whitespace-nowrap text-black'>
					<tr>
						{headers.map((h) => (
							<th key={h} className='px-4 py-2.5'>
								{h}
							</th>
						))}
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-100 whitespace-nowrap text-black'>
					{children}
				</tbody>
			</table>
		</div>
	);
}

function PriceCells({ item }: { item: TransportUnitPrice }) {
	return (
		<>
			<td className='px-4 py-2'>{formatMoney(item.materialFuelUnitPrice)}</td>
			<td className='px-4 py-2'>{formatMoney(item.powerUnitPrice)}</td>
			<td className='px-4 py-2'>{formatMoney(item.maintenanceUnitPrice)}</td>
		</>
	);
}

function NoteCell({ item }: { item: TransportUnitPrice }) {
	return (
		<td className='px-4 py-2 whitespace-normal'>{item.note || '-'}</td>
	);
}

// Tiêu đề cột đơn giá kèm đơn vị tính như form tạo/sửa
const getPriceHeaders = (unitLabel: string) => [
	`Đơn giá vật liệu, nhiên liệu (${unitLabel})`,
	`Đơn giá động lực (${unitLabel})`,
	`Đơn giá SCTX (${unitLabel})`,
];

const NOTE_HEADER = 'Ghi chú';

// Ghi chú sản lượng cho tuyến đặc biệt: dòng < 10.000 luôn ghi; dòng thường chỉ ghi ≥ 10.000
// khi cùng tuyến có dòng < 10.000 (tuyến đặc biệt tách 2 dòng).
function LowVolumeNote({
	item,
	hasLowVolumeSibling,
}: {
	item: TransportUnitPrice;
	hasLowVolumeSibling: boolean;
}) {
	if (item.isLowVolumeCase) return <> (Sản lượng &lt; 10.000 tấn/tháng)</>;
	if (hasLowVolumeSibling) return <> (Sản lượng ≥ 10.000 tấn/tháng)</>;
	return null;
}

// Vận tải than/đá qua băng tải: gộp tiếp theo Tuyến vận tải, mỗi tuyến 1 khối liệt kê Đơn vị
// (kèm dòng "Đơn vị khác" khi DepartmentId rỗng).
function ConveyorDetail({
	items,
	priceHeaders,
}: {
	items: TransportUnitPrice[];
	priceHeaders: string[];
}) {
	const routeIds = Array.from(
		new Set(items.map((i) => i.transportRouteId).filter(Boolean)),
	) as string[];

	return (
		<div className='flex flex-col gap-3'>
			{routeIds.map((routeId) => {
				const routeItems = items.filter((i) => i.transportRouteId === routeId);
				const routeLabel = formatCodeName(
					routeItems[0]?.transportRouteCode,
					routeItems[0]?.transportRouteName,
				);

				return (
					<div
						key={routeId}
						className='rounded-lg border border-neutral-200 bg-[#fafafa] p-3'
					>
						<div className='mb-2 text-sm font-semibold text-black'>
							{routeLabel}
						</div>
						<DetailTable headers={['Đơn vị', ...priceHeaders, NOTE_HEADER]}>
							{routeItems.map((item) => (
								<tr
									key={item.id}
									className='transition-colors hover:bg-neutral-50'
								>
									<td className='px-4 py-2 text-black'>
										{item.departmentId
											? formatCodeName(item.departmentCode, item.departmentName)
											: 'Đơn vị khác'}
										{item.departmentId && (
											<LowVolumeNote
												item={item}
												hasLowVolumeSibling={routeItems.some(
													(i) =>
														i.departmentId === item.departmentId &&
														i.isLowVolumeCase,
												)}
											/>
										)}
									</td>
									<PriceCells item={item} />
									<NoteCell item={item} />
								</tr>
							))}
						</DetailTable>
					</div>
				);
			})}
		</div>
	);
}

// Vận tải trục / Trục kéo: chỉ theo Tuyến vận tải, không có Đơn vị.
function RouteOnlyDetail({
	items,
	priceHeaders,
}: {
	items: TransportUnitPrice[];
	priceHeaders: string[];
}) {
	return (
		<DetailTable headers={['Tuyến vận tải', ...priceHeaders, NOTE_HEADER]}>
			{items.map((item) => (
				<tr key={item.id} className='transition-colors hover:bg-neutral-50'>
					<td className='px-4 py-2 text-black'>
						{formatCodeName(item.transportRouteCode, item.transportRouteName)}
						<LowVolumeNote
							item={item}
							hasLowVolumeSibling={items.some(
								(i) =>
									i.transportRouteId === item.transportRouteId &&
									i.isLowVolumeCase,
							)}
						/>
					</td>
					<PriceCells item={item} />
					<NoteCell item={item} />
				</tr>
			))}
		</DetailTable>
	);
}

// Monoray: gộp theo Nhóm vật tư, tài sản — mỗi nhóm 1 khối liệt kê Chất lượng thiết bị.
function MonorailDetail({
	items,
	priceHeaders,
}: {
	items: TransportUnitPrice[];
	priceHeaders: string[];
}) {
	const equipmentIds = Array.from(
		new Set(items.map((i) => i.equipmentId).filter(Boolean)),
	) as string[];

	return (
		<div className='flex flex-col gap-3'>
			{equipmentIds.map((equipmentId) => {
				const equipmentItems = items
					.filter((i) => i.equipmentId === equipmentId)
					.sort((a, b) =>
						(a.equipmentQuality || '').localeCompare(
							b.equipmentQuality || '',
							'vi',
						),
					);
				const equipmentLabel = formatCodeName(
					equipmentItems[0]?.equipmentCode,
					equipmentItems[0]?.equipmentName ||
						equipmentItems[0]?.contractCodeName,
				);

				return (
					<div
						key={equipmentId}
						className='rounded-lg border border-neutral-200 bg-[#fafafa] p-3'
					>
						<div className='mb-2 text-sm font-semibold text-black'>
							{equipmentLabel}
						</div>
						<DetailTable
							headers={['Chất lượng thiết bị', ...priceHeaders, NOTE_HEADER]}
						>
							{equipmentItems.map((item) => (
								<tr
									key={item.id}
									className='transition-colors hover:bg-neutral-50'
								>
									<td className='px-4 py-2 text-black'>
										{item.equipmentQuality
											? `Thiết bị loại ${item.equipmentQuality}`
											: '-'}
									</td>
									<PriceCells item={item} />
									<NoteCell item={item} />
								</tr>
							))}
						</DetailTable>
					</div>
				);
			})}
		</div>
	);
}

// Thiết bị khác: không có Tuyến/Chất lượng thiết bị — chỉ Nhóm vật tư + Định mức.
function OtherDetail({
	items,
	priceHeaders,
}: {
	items: TransportUnitPrice[];
	priceHeaders: string[];
}) {
	return (
		<DetailTable
			headers={[
				'Nhóm vật tư, tài sản',
				...priceHeaders,
				'Định mức',
				NOTE_HEADER,
			]}
		>
			{items.map((item) => (
				<tr key={item.id} className='transition-colors hover:bg-neutral-50'>
					<td className='px-4 py-2 text-black'>
						{formatCodeName(
							item.equipmentCode,
							item.equipmentName || item.contractCodeName || item.materialName,
						)}
					</td>
					<PriceCells item={item} />
					<td className='px-4 py-2'>{item.quantity ?? '-'}</td>
					<NoteCell item={item} />
				</tr>
			))}
		</DetailTable>
	);
}

/**
 * TẦNG 3: BẢNG CHI TIẾT ĐƠN GIÁ VÀ ĐỊNH MỨC THEO LOẠI VẬN TẢI
 */
export function TransportDetailExpand({
	row,
}: {
	row?: TransportUnitPrice | TransportPeriod;
}) {
	if (!row) return null;

	const items =
		row.items && row.items.length > 0 ? row.items : [row as TransportUnitPrice];
	const mode = detectTransportMode(
		row.productionProcessCode,
		row.productionProcessName,
	);
	const priceHeaders = getPriceHeaders(getTransportUnitLabel(mode));

	return (
		<div className='w-full space-y-3'>
			{mode === 'conveyor' && (
				<ConveyorDetail items={items} priceHeaders={priceHeaders} />
			)}
			{mode === 'monorail' && (
				<MonorailDetail items={items} priceHeaders={priceHeaders} />
			)}
			{mode === 'other' && (
				<OtherDetail items={items} priceHeaders={priceHeaders} />
			)}
			{(mode === 'shaft' || mode === 'cable_winch') && (
				<RouteOnlyDetail items={items} priceHeaders={priceHeaders} />
			)}
		</div>
	);
}

/**
 * TẦNG 2: DANH SÁCH CÁC KHOẢNG THỜI GIAN ÁP DỤNG CỦA CÔNG ĐOẠN SẢN XUẤT
 */
export function TransportProcessPeriodExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: {
	row?: TransportUnitPrice;
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}) {
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);

	const handleTogglePeriodDetails = (periodId: string) => {
		setExpandedPeriodIds((prev) =>
			prev.includes(periodId)
				? prev.filter((id) => id !== periodId)
				: [...prev, periodId],
		);
	};

	const periods = row?.periods ?? [];

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
						<th scope='col' className='px-4 py-2.5 font-semibold text-black'>
							Thời gian
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

									{/* TẦNG 3: BẢNG CHI TIẾT ĐƠN GIÁ VÀ ĐỊNH MỨC */}
									{isSelected && (
										<tr className='border-b border-neutral-200 bg-neutral-50/50'>
											<td colSpan={4} className='max-w-0 bg-neutral-50/40 p-3'>
												<div className='scrollbar-sm w-full overflow-x-auto rounded-md border border-neutral-200 bg-white p-3 shadow-xs'>
													<TransportDetailExpand row={period} />
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
