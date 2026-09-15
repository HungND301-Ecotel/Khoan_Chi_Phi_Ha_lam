import { Fragment, useMemo, useState, useCallback, useEffect } from 'react';
import { CostProduct } from '@/features/main/cost/plan/types';
import { formatNumber, formatDate, cn } from '@/lib/utils';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';

export type VtlCategoryType = 'conveyor_shaft' | 'monorail' | 'other';

export function getVtlCategory(item: CostProduct): VtlCategoryType {
	const code = (item.productionProcessCode || '').toLowerCase();
	const name = (item.productionProcessName || '').toLowerCase();

	if (
		item.transportRouteId ||
		item.transportRouteName ||
		item.transportRouteCode ||
		code.includes('bt_') ||
		code.includes('bang') ||
		code.includes('truc') ||
		name.includes('băng tải') ||
		name.includes('trục') ||
		(item.routeDepartmentCode && item.routeDepartmentCode !== '-')
	) {
		return 'conveyor_shaft';
	}

	if (
		code.includes('monoray') ||
		code.includes('monorail') ||
		name.includes('monoray') ||
		name.includes('monorail') ||
		(item.equipmentQuality && item.equipmentQuality !== '-')
	) {
		return 'monorail';
	}

	return 'other';
}

// Nhóm Cấp 2
interface L2Group {
	key: string;
	code: string;
	name: string;
	displayName: string;
	unitOfMeasureName: string;
	startMonth: string;
	totalProduction: number;
	totalCost: number;
	items: CostProduct[];
}

// Nhóm Cấp 1
interface ProcessGroup {
	id: string;
	processCode: string;
	processName: string;
	displayName: string;
	category: VtlCategoryType;
	totalProduction: number;
	totalCost: number;
	items: CostProduct[];
	l2Groups: L2Group[];
}

interface VtlPlanTreeTableProps {
	items: CostProduct[];
	monthId: string;
	selectAllRows?: boolean;
	onSelectedRowsChange?: (rows: CostProduct[]) => void;
	onRefresh?: () => Promise<void> | void;
}

export function VtlPlanTreeTable({
	items,
	selectAllRows = false,
	onSelectedRowsChange,
}: VtlPlanTreeTableProps) {
	// Quản lý trạng thái mở/đóng các cấp
	const [openedProcessIds, setOpenedProcessIds] = useState<Set<string>>(new Set());
	const [openedL2Keys, setOpenedL2Keys] = useState<Set<string>>(new Set());
	const [openedL3Keys, setOpenedL3Keys] = useState<Set<string>>(new Set());

	// Trạng thái chọn checkbox
	const [selectedRowIds, setSelectedRowIds] = useState<Set<string>>(new Set());

	// Gom nhóm phân cấp 4 tầng
	const processGroups = useMemo(() => {
		const groupMap = new Map<string, ProcessGroup>();

		items.forEach((item) => {
			const processCode = item.productionProcessCode || '';
			const processName = item.productionProcessName || 'Công đoạn sản xuất';
			const groupId = `${processCode}_${processName}`;
			const category = getVtlCategory(item);

			const displayName =
				processCode && processCode !== '-'
					? `${processCode} - ${processName}`
					: processName;

			if (!groupMap.has(groupId)) {
				groupMap.set(groupId, {
					id: groupId,
					processCode,
					processName,
					displayName,
					category,
					totalProduction: 0,
					totalCost: 0,
					items: [],
					l2Groups: [],
				});
			}

			const group = groupMap.get(groupId)!;
			group.items.push(item);
			group.totalProduction += item.totalProductionMeters || 0;
			group.totalCost += item.plannedTotalCost || 0;
		});

		// Tạo nhóm Cấp 2 cho từng CĐSX
		groupMap.forEach((group) => {
			const l2Map = new Map<string, L2Group>();

			group.items.forEach((item) => {
				let l2Key = '';
				let l2Code = '';
				let l2Name = '';

				if (group.category === 'conveyor_shaft') {
					// Tuyến vận tải cho Băng tải & Trục
					l2Key =
						item.transportRouteId ||
						item.transportRouteCode ||
						item.transportRouteName ||
						item.contractCodeCode ||
						'route_unknown';
					l2Code = item.transportRouteCode || item.contractCodeCode || '';
					l2Name =
						item.transportRouteName ||
						item.contractCodeName ||
						'Tuyến vận tải không xác định';
				} else if (group.category === 'monorail') {
					// Nhóm vật tư tài sản cho Monoray
					l2Key = item.contractCodeCode || item.contractCodeName || 'vtts_unknown';
					l2Code = item.contractCodeCode || '';
					l2Name = item.contractCodeName || 'Nhóm vật tư tài sản';
				} else {
					// Thiết bị khác
					l2Key = item.id;
					l2Code = item.contractCodeCode || '';
					l2Name = item.contractCodeName || item.productName || 'Thiết bị';
				}

				// Format "Mã - Tên" chuẩn không viết thừa
				const displayName =
					l2Code && l2Code !== '-' && l2Code !== l2Name
						? `${l2Code} - ${l2Name}`
						: l2Name;

				const fullL2Key = `${group.id}__${l2Key}`;

				if (!l2Map.has(fullL2Key)) {
					l2Map.set(fullL2Key, {
						key: fullL2Key,
						code: l2Code,
						name: l2Name,
						displayName,
						unitOfMeasureName: item.unitOfMeasureName || '-',
						startMonth: item.startMonth || '',
						totalProduction: 0,
						totalCost: 0,
						items: [],
					});
				}

				const l2 = l2Map.get(fullL2Key)!;
				l2.items.push(item);
				l2.totalProduction += item.totalProductionMeters || 0;
				l2.totalCost += item.plannedTotalCost || 0;
			});

			group.l2Groups = Array.from(l2Map.values());
		});

		return Array.from(groupMap.values()).sort((a, b) =>
			a.processCode.localeCompare(b.processCode),
		);
	}, [items]);

	// Mặc định mở tất cả CĐSX Cấp 1
	useEffect(() => {
		if (processGroups.length > 0 && openedProcessIds.size === 0) {
			setOpenedProcessIds(new Set(processGroups.map((g) => g.id)));
		}
	}, [processGroups]);

	// Xử lý selectAllRows
	useEffect(() => {
		if (selectAllRows) {
			setSelectedRowIds(new Set(items.map((it) => it.id)));
		} else {
			setSelectedRowIds(new Set());
		}
	}, [selectAllRows, items]);

	// Gửi danh sách các dòng được chọn ra ngoài
	const notifySelectedChange = useCallback(
		(newSelected: Set<string>) => {
			setSelectedRowIds(newSelected);
			if (onSelectedRowsChange) {
				const selectedItems = items.filter((it) => newSelected.has(it.id));
				onSelectedRowsChange(selectedItems);
			}
		},
		[items, onSelectedRowsChange],
	);

	// Toggle mở/đóng Cấp 1
	const toggleProcess = (groupId: string) => {
		setOpenedProcessIds((prev) => {
			const next = new Set(prev);
			if (next.has(groupId)) {
				next.delete(groupId);
			} else {
				next.add(groupId);
			}
			return next;
		});
	};

	// Toggle mở/đóng Cấp 2
	const toggleL2 = (l2Key: string) => {
		setOpenedL2Keys((prev) => {
			const next = new Set(prev);
			if (next.has(l2Key)) {
				next.delete(l2Key);
			} else {
				next.add(l2Key);
			}
			return next;
		});
	};

	// Toggle mở/đóng Cấp 3
	const toggleL3 = (l3Key: string) => {
		setOpenedL3Keys((prev) => {
			const next = new Set(prev);
			if (next.has(l3Key)) {
				next.delete(l3Key);
			} else {
				next.add(l3Key);
			}
			return next;
		});
	};

	// Checkbox: Toggle 1 item
	const toggleItemSelect = (itemId: string) => {
		const next = new Set(selectedRowIds);
		if (next.has(itemId)) {
			next.delete(itemId);
		} else {
			next.add(itemId);
		}
		notifySelectedChange(next);
	};

	// Checkbox: Toggle Cấp 2
	const toggleL2Select = (l2: L2Group) => {
		const l2ItemIds = l2.items.map((it) => it.id);
		const allSelected = l2ItemIds.every((id) => selectedRowIds.has(id));
		const next = new Set(selectedRowIds);

		if (allSelected) {
			l2ItemIds.forEach((id) => next.delete(id));
		} else {
			l2ItemIds.forEach((id) => next.add(id));
		}
		notifySelectedChange(next);
	};

	// Checkbox: Toggle Cấp 1 (Công đoạn sản xuất)
	const toggleGroupSelect = (group: ProcessGroup) => {
		const groupItemIds = group.items.map((it) => it.id);
		const allSelected = groupItemIds.every((id) => selectedRowIds.has(id));
		const next = new Set(selectedRowIds);

		if (allSelected) {
			groupItemIds.forEach((id) => next.delete(id));
		} else {
			groupItemIds.forEach((id) => next.add(id));
		}
		notifySelectedChange(next);
	};

	if (items.length === 0) {
		return null;
	}

	return (
		<div className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow'>
			{/* ============================================================ */}
			{/* CẤP 1: BẢNG CÔNG ĐOẠN SẢN XUẤT (GỘP CHUNG 1 CỘT MÃ - TÊN)      */}
			{/* ============================================================ */}
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>Công đoạn sản xuất</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{processGroups.map((group) => {
						const isOpenedL1 = openedProcessIds.has(group.id);
						const groupItemIds = group.items.map((it) => it.id);
						const allGroupSelected =
							groupItemIds.length > 0 &&
							groupItemIds.every((id) => selectedRowIds.has(id));

						return (
							<Fragment key={group.id}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isOpenedL1 && 'bg-neutral-50/50',
									)}
								>
									{/* Checkbox CĐSX */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={allGroupSelected}
											onCheckedChange={() => toggleGroupSelect(group)}
											aria-label={`Chọn ${group.displayName}`}
										/>
									</td>

									{/* Cột Công đoạn sản xuất: hiển thị "Mã - Tên công đoạn sản xuất" */}
									<td className='px-4 py-2.5 align-middle font-medium text-black'>
										{group.displayName}
									</td>

									{/* Nút Xem Cấp 1 */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Button
											variant='ghost'
											size='icon'
											className='size-7 rounded-full text-[#6e6e6e] hover:bg-[#f0f0f0] hover:text-[#333]'
											onClick={() => toggleProcess(group.id)}
											aria-label={isOpenedL1 ? 'Thu gọn' : 'Mở rộng'}
										>
											{isOpenedL1 ? (
												<VisibilityOffIcon fontSize='small' />
											) : (
												<VisibilityIcon fontSize='small' />
											)}
										</Button>
									</td>
								</tr>

								{/* ============================================================ */}
								{/* CẤP 2: BẢNG NHÓM VẬT TƯ TÀI SẢN / TUYẾN VẬN TẢI               */}
								{/* ============================================================ */}
								{isOpenedL1 && (
									<tr className='border-b border-neutral-200 bg-neutral-50/30'>
										<td colSpan={3} className='p-3 pl-8'>
											<L2Table
												group={group}
												openedL2Keys={openedL2Keys}
												openedL3Keys={openedL3Keys}
												selectedRowIds={selectedRowIds}
												onToggleL2={toggleL2}
												onToggleL3={toggleL3}
												onToggleL2Select={toggleL2Select}
												onToggleItemSelect={toggleItemSelect}
											/>
										</td>
									</tr>
								)}
							</Fragment>
						);
					})}
				</tbody>
			</table>
		</div>
	);
}

// ============================================================================
// CẤP 2: BẢNG NHÓM VẬT TƯ TÀI SẢN / TUYẾN VẬN TẢI
// ============================================================================
interface L2TableProps {
	group: ProcessGroup;
	openedL2Keys: Set<string>;
	openedL3Keys: Set<string>;
	selectedRowIds: Set<string>;
	onToggleL2: (key: string) => void;
	onToggleL3: (key: string) => void;
	onToggleL2Select: (l2: L2Group) => void;
	onToggleItemSelect: (id: string) => void;
}

function L2Table({
	group,
	openedL2Keys,
	openedL3Keys,
	selectedRowIds,
	onToggleL2,
	onToggleL3,
	onToggleL2Select,
	onToggleItemSelect,
}: L2TableProps) {
	const isConveyor = group.category === 'conveyor_shaft';

	return (
		<div className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow'>
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>
							{isConveyor ? 'Tuyến vận tải' : 'Nhóm vật tư tài sản'}
						</th>
						<th className='w-28 px-3 py-2.5 text-center'>Đơn vị tính</th>
						<th className='w-32 px-3 py-2.5 text-center'>Thời gian</th>
						<th className='w-48 px-4 py-2.5 text-right'>Sản lượng kế hoạch ban đầu</th>
						<th className='w-56 px-4 py-2.5 text-right'>Doanh thu kế hoạch ban đầu (đ)</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{group.l2Groups.map((l2) => {
						const isOpenedL2 = openedL2Keys.has(l2.key);
						const l2ItemIds = l2.items.map((it) => it.id);
						const allL2Selected =
							l2ItemIds.length > 0 && l2ItemIds.every((id) => selectedRowIds.has(id));

						return (
							<Fragment key={l2.key}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isOpenedL2 && 'bg-neutral-50/50',
									)}
								>
									{/* Checkbox Cấp 2 */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={allL2Selected}
											onCheckedChange={() => onToggleL2Select(l2)}
											aria-label={`Chọn ${l2.displayName}`}
										/>
									</td>

									{/* Cột Nhóm vật tư tài sản / Tuyến: hiển thị "Mã - Tên" */}
									<td className='px-4 py-2.5 align-middle font-medium text-black'>
										{l2.displayName}
									</td>

									{/* Đơn vị tính */}
									<td className='px-3 py-2.5 text-center align-middle text-black'>
										{l2.unitOfMeasureName}
									</td>

									{/* Thời gian */}
									<td className='px-3 py-2.5 text-center align-middle text-black'>
										{formatDate(l2.startMonth)}
									</td>

									{/* Sản lượng kế hoạch ban đầu */}
									<td className='px-4 py-2.5 text-right align-middle text-black'>
										{formatNumber(l2.totalProduction)}
									</td>

									{/* Doanh thu kế hoạch ban đầu (đ) */}
									<td className='px-4 py-2.5 text-right align-middle font-semibold text-black'>
										{formatNumber(l2.totalCost)}
									</td>

									{/* Nút Xem Cấp 2 */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Button
											variant='ghost'
											size='icon'
											className='size-7 rounded-full text-[#6e6e6e] hover:bg-[#f0f0f0] hover:text-[#333]'
											onClick={() => onToggleL2(l2.key)}
											aria-label={isOpenedL2 ? 'Thu gọn' : 'Mở rộng'}
										>
											{isOpenedL2 ? (
												<VisibilityOffIcon fontSize='small' />
											) : (
												<VisibilityIcon fontSize='small' />
											)}
										</Button>
									</td>
								</tr>

								{/* Mở Cấp 2:
								    - Thiết bị khác: Cấp 3 là form chi tiết luôn
								    - Monoray / Băng tải & Trục: Bảng Cấp 3 */}
								{isOpenedL2 && (
									<tr className='border-b border-neutral-200 bg-neutral-50/20'>
										<td colSpan={7} className='p-3 pl-8'>
											{group.category === 'other' ? (
												<L4DetailPanel product={l2.items[0]} />
											) : (
												<L3Table
													group={group}
													l2={l2}
													openedL3Keys={openedL3Keys}
													selectedRowIds={selectedRowIds}
													onToggleL3={onToggleL3}
													onToggleItemSelect={onToggleItemSelect}
												/>
											)}
										</td>
									</tr>
								)}
							</Fragment>
						);
					})}
				</tbody>
			</table>
		</div>
	);
}

// ============================================================================
// CẤP 3: BẢNG CHẤT LƯỢNG (MONORAY) HOẶC ĐƠN VỊ (BĂNG TẢI & TRỤC)
// ============================================================================
interface L3TableProps {
	group: ProcessGroup;
	l2: L2Group;
	openedL3Keys: Set<string>;
	selectedRowIds: Set<string>;
	onToggleL3: (key: string) => void;
	onToggleItemSelect: (id: string) => void;
}

function L3Table({
	group,
	l2,
	openedL3Keys,
	selectedRowIds,
	onToggleL3,
	onToggleItemSelect,
}: L3TableProps) {
	const isConveyor = group.category === 'conveyor_shaft';

	return (
		<div className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow'>
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>{isConveyor ? 'Đơn vị' : 'Chất lượng'}</th>
						<th className='w-48 px-4 py-2.5 text-right'>Sản lượng kế hoạch ban đầu</th>
						<th className='w-56 px-4 py-2.5 text-right'>Doanh thu kế hoạch ban đầu (đ)</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{l2.items.map((item) => {
						const isOpenedL3 = openedL3Keys.has(item.id);
						const isSelected = selectedRowIds.has(item.id);

						// Giá trị Đơn vị là "Mã - Tên" (nếu có mã và tên)
						const label = isConveyor
							? item.routeDepartmentCode &&
								item.routeDepartmentCode !== '-' &&
								item.routeDepartmentCode !== item.routeDepartmentName
								? `${item.routeDepartmentCode} - ${item.routeDepartmentName || '-'}`
								: item.routeDepartmentName || '-'
							: item.equipmentQuality
								? item.equipmentQuality.startsWith('Loại')
									? item.equipmentQuality
									: `Loại ${item.equipmentQuality}`
								: '-';

						return (
							<Fragment key={item.id}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isSelected && 'bg-blue-50/30',
										isOpenedL3 && 'bg-neutral-50/50',
									)}
								>
									{/* Checkbox Cấp 3 */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={isSelected}
											onCheckedChange={() => onToggleItemSelect(item.id)}
										/>
									</td>

									{/* Cột Chất lượng hoặc Đơn vị */}
									<td className='px-4 py-2.5 align-middle font-medium text-black'>
										{label}
									</td>

									{/* Sản lượng kế hoạch ban đầu */}
									<td className='px-4 py-2.5 text-right align-middle text-black'>
										{formatNumber(item.totalProductionMeters)}
									</td>

									{/* Doanh thu kế hoạch ban đầu (đ) */}
									<td className='px-4 py-2.5 text-right align-middle font-semibold text-black'>
										{formatNumber(item.plannedTotalCost)}
									</td>

									{/* Nút Xem mở Cấp 4 */}
									<td className='px-3 py-2.5 text-center align-middle'>
										<Button
											variant='ghost'
											size='icon'
											className='size-7 rounded-full text-[#6e6e6e] hover:bg-[#f0f0f0] hover:text-[#333]'
											onClick={() => onToggleL3(item.id)}
											aria-label={isOpenedL3 ? 'Thu gọn' : 'Xem chi tiết'}
										>
											{isOpenedL3 ? (
												<VisibilityOffIcon fontSize='small' />
											) : (
												<VisibilityIcon fontSize='small' />
											)}
										</Button>
									</td>
								</tr>

								{/* CẤP 4: FORM CHI TIẾT DOANH THU KẾ HOẠCH BAN ĐẦU */}
								{isOpenedL3 && (
									<tr className='border-b border-neutral-200 bg-neutral-50/20'>
										<td colSpan={5} className='p-3 pl-8'>
											<L4DetailPanel product={item} />
										</td>
									</tr>
								)}
							</Fragment>
						);
					})}
				</tbody>
			</table>
		</div>
	);
}

// ============================================================================
// CẤP 4: FORM CHI TIẾT DOANH THU KẾ HOẠCH BAN ĐẦU (ĐÚNG CHUẨN ẢNH 2 CỦA USER)
// ============================================================================
interface L4DetailPanelProps {
	product: CostProduct;
}

function L4DetailPanel({ product }: L4DetailPanelProps) {
	const material = product.material;
	const maintenance = product.maintenance;
	const power = product.power;

	const renderPrice = (val?: number | null) => {
		if (val === null || val === undefined || val === 0) return '-';
		return formatNumber(val);
	};

	const renderFactor = (coeff?: number | null) => {
		if (coeff === null || coeff === undefined) return '-';
		return formatNumber(coeff);
	};

	return (
		<div className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow'>
			{/* Thanh tiêu đề Cấp 4 đồng bộ hệ thống */}
			<div className='flex items-center justify-between border-b border-neutral-200 bg-[#f5f5f5] px-4 py-2.5 text-sm font-normal'>
				<div className='font-medium text-black'>
					Doanh thu kế hoạch ban đầu
				</div>

				<div className='flex items-center gap-10 text-sm font-semibold text-black'>
					<div>{formatNumber(product.totalProductionMeters)}</div>
					<div>{formatNumber(product.plannedTotalCost)}</div>
				</div>
			</div>

			{/* Bảng chi tiết đơn giá gốc K1 K2 đơn giá */}
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='px-4 py-2.5'>Thành phần chi phí</th>
						<th className='px-4 py-2.5 text-right'>Đơn giá gốc (đ)</th>
						<th className='w-24 px-3 py-2.5 text-center'>K1</th>
						<th className='w-24 px-3 py-2.5 text-center'>K2</th>
						<th className='px-4 py-2.5 text-right'>Đơn giá (đ)</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{/* Vật liệu */}
					<tr>
						<td className='px-4 py-2.5 font-medium text-black'>Vật liệu</td>
						<td className='px-4 py-2.5 text-right'>
							{renderPrice(material?.baseUnitPrice ?? material?.effectiveUnitPrice)}
						</td>
						<td className='px-3 py-2.5 text-center text-neutral-400'>-</td>
						<td className='px-3 py-2.5 text-center text-neutral-400'>-</td>
						<td className='px-4 py-2.5 text-right font-medium text-black'>
							{renderPrice(material?.effectiveUnitPrice)}
						</td>
					</tr>

					{/* Sửa chữa thường xuyên (SCTX) */}
					<tr>
						<td className='px-4 py-2.5 font-medium text-black'>
							Sửa chữa thường xuyên (SCTX)
						</td>
						<td className='px-4 py-2.5 text-right'>
							{renderPrice(maintenance?.baseUnitPrice)}
						</td>
						<td className='px-3 py-2.5 text-center'>
							{renderFactor(maintenance?.k1Coefficient)}
						</td>
						<td className='px-3 py-2.5 text-center'>
							{renderFactor(maintenance?.k2Coefficient)}
						</td>
						<td className='px-4 py-2.5 text-right font-medium text-black'>
							{renderPrice(maintenance?.effectiveUnitPrice)}
						</td>
					</tr>

					{/* Động lực (Điện năng / Nhiên liệu) */}
					<tr>
						<td className='px-4 py-2.5 font-medium text-black'>
							Động lực (Điện năng / Nhiên liệu)
						</td>
						<td className='px-4 py-2.5 text-right'>
							{renderPrice(power?.baseUnitPrice)}
						</td>
						<td className='px-3 py-2.5 text-center'>
							{renderFactor(power?.k1Coefficient)}
						</td>
						<td className='px-3 py-2.5 text-center'>
							{renderFactor(power?.k2Coefficient)}
						</td>
						<td className='px-4 py-2.5 text-right font-medium text-black'>
							{renderPrice(power?.effectiveUnitPrice)}
						</td>
					</tr>
				</tbody>
			</table>
		</div>
	);
}
