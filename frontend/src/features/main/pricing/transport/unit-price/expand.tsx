import { detectTransportMode, formatMoney, TransportUnitPrice } from './columns';

function DetailTable({
	headers,
	children,
}: {
	headers: string[];
	children: React.ReactNode;
}) {
	return (
		<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
			<table className='w-full text-left text-sm'>
				<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
					<tr>
						{headers.map((h) => (
							<th key={h} className='px-3 py-2'>
								{h}
							</th>
						))}
					</tr>
				</thead>
				<tbody className='divide-y divide-gray-100'>{children}</tbody>
			</table>
		</div>
	);
}

function PriceCells({ item }: { item: TransportUnitPrice }) {
	return (
		<>
			<td className='px-3 py-2'>{formatMoney(item.materialFuelUnitPrice)}</td>
			<td className='px-3 py-2'>{formatMoney(item.powerUnitPrice)}</td>
			<td className='px-3 py-2'>{formatMoney(item.maintenanceUnitPrice)}</td>
		</>
	);
}

const PRICE_HEADERS = ['Đơn giá VL, NL', 'Đơn giá Động lực', 'Đơn giá SCTX'];

// Vận tải than/đá qua băng tải: gộp tiếp theo Tuyến vận tải, mỗi tuyến 1 khối liệt kê Đơn vị.
function ConveyorDetail({ items }: { items: TransportUnitPrice[] }) {
	const routeIds = Array.from(
		new Set(items.map((i) => i.transportRouteId).filter(Boolean)),
	) as string[];

	return (
		<div className='flex flex-col gap-3'>
			{routeIds.map((routeId) => {
				const routeItems = items.filter((i) => i.transportRouteId === routeId);
				const routeLabel = routeItems[0]?.transportRouteName || routeId;

				return (
					<div
						key={routeId}
						className='rounded-lg border border-gray-200 bg-[#fafafa] p-3'
					>
						<div className='mb-2 text-sm font-semibold text-gray-800'>
							{routeLabel}
						</div>
						<DetailTable headers={['Đơn vị', ...PRICE_HEADERS]}>
							{routeItems.map((item) => (
								<tr key={item.id}>
									<td className='px-3 py-2 font-medium text-gray-800'>
										{item.departmentName || '-'}
										{item.isLowVolumeCase && (
											<span className='ml-1 text-xs text-amber-700'>
												( &lt; 10.000t/tháng )
											</span>
										)}
									</td>
									<PriceCells item={item} />
								</tr>
							))}
						</DetailTable>
					</div>
				);
			})}
		</div>
	);
}

// Vận tải trục / Trục kéo: gộp theo Tuyến vận tải — mỗi tuyến chỉ 1 dòng giá, không có Đơn vị.
function RouteOnlyDetail({ items }: { items: TransportUnitPrice[] }) {
	return (
		<DetailTable headers={['Tuyến vận tải', ...PRICE_HEADERS]}>
			{items.map((item) => (
				<tr key={item.id}>
					<td className='px-3 py-2 font-medium text-gray-800'>
						{item.transportRouteName || '-'}
						{item.isLowVolumeCase && (
							<span className='ml-1 text-xs text-amber-700'>
								( &lt; 10.000t/tháng )
							</span>
						)}
					</td>
					<PriceCells item={item} />
				</tr>
			))}
		</DetailTable>
	);
}

// Monoray: gộp theo Nhóm vật tư, tài sản — mỗi nhóm 1 khối liệt kê Chất lượng thiết bị.
function MonorailDetail({ items }: { items: TransportUnitPrice[] }) {
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
				const equipmentLabel =
					equipmentItems[0]?.equipmentName ||
					equipmentItems[0]?.contractCodeName ||
					equipmentId;

				return (
					<div
						key={equipmentId}
						className='rounded-lg border border-gray-200 bg-[#fafafa] p-3'
					>
						<div className='mb-2 text-sm font-semibold text-gray-800'>
							{equipmentLabel}
						</div>
						<DetailTable headers={['Chất lượng thiết bị', ...PRICE_HEADERS]}>
							{equipmentItems.map((item) => (
								<tr key={item.id}>
									<td className='px-3 py-2 font-medium text-gray-800'>
										{item.equipmentQuality
											? `Thiết bị loại ${item.equipmentQuality}`
											: '-'}
									</td>
									<PriceCells item={item} />
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
function OtherDetail({ items }: { items: TransportUnitPrice[] }) {
	return (
		<DetailTable headers={['Nhóm vật tư', ...PRICE_HEADERS, 'Định mức']}>
			{items.map((item) => (
				<tr key={item.id}>
					<td className='px-3 py-2 font-medium text-gray-800'>
						{item.equipmentName ||
							item.contractCodeName ||
							item.materialName ||
							'-'}
					</td>
					<PriceCells item={item} />
					<td className='px-3 py-2'>{item.quantity ?? '-'}</td>
				</tr>
			))}
		</DetailTable>
	);
}

/**
 * Bảng Chi Tiết Mở Rộng khi nhấn nút Xem (Mắt 👁️) — tự nhận diện loại vận tải của nhóm
 * (theo mã/tên Công đoạn sản xuất) rồi gộp tiếp + ẩn cột không áp dụng cho loại đó, thay vì
 * dùng chung 1 bảng cột cố định cho mọi loại (gây nhiều ô "-" trống như trước).
 */
export function TransportDetailExpand({ row }: { row?: TransportUnitPrice }) {
	if (!row) return null;

	const items = row.items && row.items.length > 0 ? row.items : [row];
	const mode = detectTransportMode(
		row.productionProcessCode,
		row.productionProcessName,
	);

	return (
		<div className='my-2 rounded-md border border-gray-200 bg-[#fbfbfb] p-3 shadow-inner'>
			{mode === 'conveyor' && <ConveyorDetail items={items} />}
			{mode === 'monorail' && <MonorailDetail items={items} />}
			{mode === 'other' && <OtherDetail items={items} />}
			{(mode === 'shaft' || mode === 'cable_winch') && (
				<RouteOnlyDetail items={items} />
			)}
		</div>
	);
}
