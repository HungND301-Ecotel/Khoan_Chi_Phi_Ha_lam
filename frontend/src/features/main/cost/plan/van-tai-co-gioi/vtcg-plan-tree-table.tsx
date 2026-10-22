import { Fragment, useCallback, useMemo, useState } from 'react';
import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate, formatNumber, cn } from '@/lib/utils';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { VtcgPlanExpand } from './expand';

// Cấp 2: Công đoạn sản xuất trong 1 Nhóm vật tư
interface L2Group {
	key: string;
	displayName: string;
	unitOfMeasureName: string;
	startMonth: string;
	totalProduction: number;
	totalCost: number;
	items: CostProduct[];
}

// Cấp 1: Nhóm vật tư, tài sản (chủ đạo — đúng thứ tự nhập trên form)
interface AssetGroup {
	id: string;
	assetCode: string;
	displayName: string;
	items: CostProduct[];
	l2Groups: L2Group[];
}

interface VtcgPlanTreeTableProps {
	items: CostProduct[];
	selectAllRows?: boolean;
	onSelectedRowsChange?: (rows: CostProduct[]) => void;
}

const codeName = (code?: string | null, name?: string | null) =>
	code && code !== '-' && code !== name
		? `${code} - ${name || '-'}`
		: name || code || '-';

const toggleKey = (prev: Set<string>, key: string) => {
	const next = new Set(prev);
	if (next.has(key)) {
		next.delete(key);
	} else {
		next.add(key);
	}
	return next;
};

function ViewButton({
	opened,
	onClick,
}: {
	opened: boolean;
	onClick: () => void;
}) {
	return (
		<Button
			variant='ghost'
			size='icon'
			className='size-7 rounded-full text-[#6e6e6e] hover:bg-[#f0f0f0] hover:text-[#333]'
			onClick={onClick}
			aria-label={opened ? 'Thu gọn' : 'Mở rộng'}
		>
			{opened ? (
				<VisibilityOffIcon fontSize='small' />
			) : (
				<VisibilityIcon fontSize='small' />
			)}
		</Button>
	);
}

// Kế hoạch Vận tải cơ giới dạng cây như Vận tải lò, lấy Nhóm vật tư làm chủ đạo:
// Nhóm vật tư → Công đoạn → dòng (Chất lượng, Loại hàng, Vị trí, Cung độ) → chi tiết đơn giá
export function VtcgPlanTreeTable({
	items,
	selectAllRows = false,
	onSelectedRowsChange,
}: VtcgPlanTreeTableProps) {
	// Cấp 1 mặc định mở hết — chỉ lưu các Nhóm vật tư người dùng đã thu gọn
	const [closedAssetIds, setClosedAssetIds] = useState<Set<string>>(new Set());
	const [openedL2Keys, setOpenedL2Keys] = useState<Set<string>>(new Set());
	const [openedL3Keys, setOpenedL3Keys] = useState<Set<string>>(new Set());
	const [selectedRowIds, setSelectedRowIds] = useState<Set<string>>(new Set());

	const assetGroups = useMemo(() => {
		const groupMap = new Map<string, AssetGroup>();

		items.forEach((item) => {
			const assetCode = item.contractCodeCode || '';
			const groupId = `${assetCode}_${item.contractCodeName || ''}`;
			if (!groupMap.has(groupId)) {
				groupMap.set(groupId, {
					id: groupId,
					assetCode,
					displayName: codeName(assetCode, item.contractCodeName),
					items: [],
					l2Groups: [],
				});
			}
			groupMap.get(groupId)!.items.push(item);
		});

		groupMap.forEach((group) => {
			const l2Map = new Map<string, L2Group>();
			group.items.forEach((item) => {
				const l2Key = `${group.id}__${item.productionProcessCode || ''}_${item.productionProcessName || ''}`;
				if (!l2Map.has(l2Key)) {
					l2Map.set(l2Key, {
						key: l2Key,
						displayName: codeName(
							item.productionProcessCode,
							item.productionProcessName,
						),
						unitOfMeasureName: item.unitOfMeasureName || '-',
						startMonth: item.startMonth || '',
						totalProduction: 0,
						totalCost: 0,
						items: [],
					});
				}
				const l2 = l2Map.get(l2Key)!;
				l2.items.push(item);
				l2.totalProduction += item.totalProductionMeters || 0;
				l2.totalCost += item.plannedTotalCost || 0;
			});
			group.l2Groups = Array.from(l2Map.values());
		});

		return Array.from(groupMap.values()).sort((a, b) =>
			a.assetCode.localeCompare(b.assetCode),
		);
	}, [items]);

	// Chọn tất cả / đổi dữ liệu thì đặt lại các dòng đang chọn
	const [selectionSource, setSelectionSource] = useState({
		selectAllRows,
		items,
	});
	if (
		selectionSource.selectAllRows !== selectAllRows ||
		selectionSource.items !== items
	) {
		setSelectionSource({ selectAllRows, items });
		setSelectedRowIds(
			selectAllRows ? new Set(items.map((it) => it.id)) : new Set(),
		);
	}

	const notifySelectedChange = useCallback(
		(next: Set<string>) => {
			setSelectedRowIds(next);
			onSelectedRowsChange?.(items.filter((it) => next.has(it.id)));
		},
		[items, onSelectedRowsChange],
	);

	const toggleItemsSelect = (ids: string[]) => {
		const allSelected = ids.every((id) => selectedRowIds.has(id));
		const next = new Set(selectedRowIds);
		ids.forEach((id) => (allSelected ? next.delete(id) : next.add(id)));
		notifySelectedChange(next);
	};

	if (items.length === 0) return null;

	return (
		<div className='scrollbar-sm overflow-x-auto rounded-md border border-neutral-200 bg-white shadow'>
			{/* CẤP 1: NHÓM VẬT TƯ, TÀI SẢN */}
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>Nhóm vật tư, tài sản</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{assetGroups.map((group) => {
						const isOpened = !closedAssetIds.has(group.id);
						const ids = group.items.map((it) => it.id);
						return (
							<Fragment key={group.id}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isOpened && 'bg-neutral-50/50',
									)}
								>
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={ids.every((id) => selectedRowIds.has(id))}
											onCheckedChange={() => toggleItemsSelect(ids)}
											aria-label={`Chọn ${group.displayName}`}
										/>
									</td>
									<td className='px-4 py-2.5 align-middle font-medium text-black'>
										{group.displayName}
									</td>
									<td className='px-3 py-2.5 text-center align-middle'>
										<ViewButton
											opened={isOpened}
											onClick={() =>
												setClosedAssetIds((prev) => toggleKey(prev, group.id))
											}
										/>
									</td>
								</tr>

								{isOpened && (
									<tr className='border-b border-neutral-200 bg-neutral-50/30'>
										<td colSpan={3} className='p-3 pl-8'>
											<L2Table
												group={group}
												openedL2Keys={openedL2Keys}
												openedL3Keys={openedL3Keys}
												selectedRowIds={selectedRowIds}
												onToggleL2={(key) =>
													setOpenedL2Keys((prev) => toggleKey(prev, key))
												}
												onToggleL3={(key) =>
													setOpenedL3Keys((prev) => toggleKey(prev, key))
												}
												onToggleSelect={toggleItemsSelect}
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

// CẤP 2: CÔNG ĐOẠN SẢN XUẤT
interface L2TableProps {
	group: AssetGroup;
	openedL2Keys: Set<string>;
	openedL3Keys: Set<string>;
	selectedRowIds: Set<string>;
	onToggleL2: (key: string) => void;
	onToggleL3: (key: string) => void;
	onToggleSelect: (ids: string[]) => void;
}

function L2Table({
	group,
	openedL2Keys,
	openedL3Keys,
	selectedRowIds,
	onToggleL2,
	onToggleL3,
	onToggleSelect,
}: L2TableProps) {
	return (
		<div className='scrollbar-sm w-0 min-w-full overflow-x-auto rounded-md border border-neutral-200 bg-white shadow'>
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>Công đoạn sản xuất</th>
						<th className='w-28 px-3 py-2.5 text-center'>Đơn vị tính</th>
						<th className='w-32 px-3 py-2.5 text-center'>Thời gian</th>
						<th className='w-48 px-4 py-2.5 text-right'>
							Sản lượng kế hoạch ban đầu
						</th>
						<th className='w-56 px-4 py-2.5 text-right'>
							Doanh thu kế hoạch ban đầu (đ)
						</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{group.l2Groups.map((l2) => {
						const isOpened = openedL2Keys.has(l2.key);
						const ids = l2.items.map((it) => it.id);
						return (
							<Fragment key={l2.key}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isOpened && 'bg-neutral-50/50',
									)}
								>
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={ids.every((id) => selectedRowIds.has(id))}
											onCheckedChange={() => onToggleSelect(ids)}
											aria-label={`Chọn ${l2.displayName}`}
										/>
									</td>
									<td className='px-4 py-2.5 align-middle font-medium text-black'>
										{l2.displayName}
									</td>
									<td className='px-3 py-2.5 text-center align-middle'>
										{l2.unitOfMeasureName}
									</td>
									<td className='px-3 py-2.5 text-center align-middle'>
										{formatDate(l2.startMonth)}
									</td>
									<td className='px-4 py-2.5 text-right align-middle'>
										{formatNumber(l2.totalProduction)}
									</td>
									<td className='px-4 py-2.5 text-right align-middle font-semibold'>
										{formatNumber(l2.totalCost)}
									</td>
									<td className='px-3 py-2.5 text-center align-middle'>
										<ViewButton
											opened={isOpened}
											onClick={() => onToggleL2(l2.key)}
										/>
									</td>
								</tr>

								{isOpened && (
									<tr className='border-b border-neutral-200 bg-neutral-50/20'>
										<td colSpan={7} className='p-3 pl-8'>
											<L3Table
												l2={l2}
												openedL3Keys={openedL3Keys}
												selectedRowIds={selectedRowIds}
												onToggleL3={onToggleL3}
												onToggleSelect={onToggleSelect}
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

// CẤP 3: TỪNG DÒNG (CHẤT LƯỢNG, LOẠI HÀNG, VỊ TRÍ, CUNG ĐỘ) — CẤP 4 LÀ CHI TIẾT ĐƠN GIÁ.
// Chỉ hiện cột mà Công đoạn có dùng (vd. Giờ di chuyển không có Loại hàng / Vị trí / Cung độ)
interface L3TableProps {
	l2: L2Group;
	openedL3Keys: Set<string>;
	selectedRowIds: Set<string>;
	onToggleL3: (key: string) => void;
	onToggleSelect: (ids: string[]) => void;
}

function L3Table({
	l2,
	openedL3Keys,
	selectedRowIds,
	onToggleL3,
	onToggleSelect,
}: L3TableProps) {
	const showCargo = l2.items.some((it) => !!it.cargoTypeName);
	const showLocations = l2.items.some(
		(it) => !!it.receivingLocationName || !!it.dumpingLocationName,
	);
	const showDistance = l2.items.some((it) => !!it.haulDistanceValue);
	const columnCount =
		5 + Number(showCargo) + Number(showLocations) + Number(showDistance);

	return (
		<div className='scrollbar-sm w-0 min-w-full overflow-x-auto rounded-md border border-neutral-200 bg-white shadow'>
			<table className='w-full border-collapse text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-[#fafafa] text-sm font-semibold text-black'>
					<tr>
						<th className='w-10 px-3 py-2.5 text-center'>
							<span className='sr-only'>Chọn</span>
						</th>
						<th className='px-4 py-2.5'>Chất lượng</th>
						{showCargo && <th className='px-4 py-2.5'>Loại hàng</th>}
						{showLocations && <th className='px-4 py-2.5'>Vị trí nhận → Đổ</th>}
						{showDistance && <th className='px-4 py-2.5'>Cung độ</th>}
						<th className='w-48 px-4 py-2.5 text-right'>
							Sản lượng kế hoạch ban đầu
						</th>
						<th className='w-56 px-4 py-2.5 text-right'>
							Doanh thu kế hoạch ban đầu (đ)
						</th>
						<th className='w-14 px-3 py-2.5 text-center'>Xem</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{l2.items.map((item) => {
						const isOpened = openedL3Keys.has(item.id);
						const isSelected = selectedRowIds.has(item.id);
						const locations =
							item.receivingLocationName || item.dumpingLocationName
								? `${item.receivingLocationName || '...'} → ${item.dumpingLocationName || '...'}`
								: '-';
						return (
							<Fragment key={item.id}>
								<tr
									className={cn(
										'hover:bg-neutral-50/70',
										isSelected && 'bg-blue-50/30',
										isOpened && 'bg-neutral-50/50',
									)}
								>
									<td className='px-3 py-2.5 text-center align-middle'>
										<Checkbox
											checked={isSelected}
											onCheckedChange={() => onToggleSelect([item.id])}
										/>
									</td>
									<td className='px-4 py-2.5 align-middle font-medium'>
										{item.equipmentQuality
											? item.equipmentQuality.startsWith('Loại')
												? item.equipmentQuality
												: `Loại ${item.equipmentQuality}`
											: '-'}
									</td>
									{showCargo && (
										<td className='px-4 py-2.5 align-middle'>
											{item.cargoTypeName || '-'}
										</td>
									)}
									{showLocations && (
										<td className='px-4 py-2.5 align-middle'>{locations}</td>
									)}
									{showDistance && (
										<td className='px-4 py-2.5 align-middle'>
											{item.haulDistanceValue
												? `${item.haulDistanceValue} km`
												: '-'}
										</td>
									)}
									<td className='px-4 py-2.5 text-right align-middle'>
										{formatNumber(item.totalProductionMeters)}
									</td>
									<td className='px-4 py-2.5 text-right align-middle font-semibold'>
										{formatNumber(item.plannedTotalCost)}
									</td>
									<td className='px-3 py-2.5 text-center align-middle'>
										<ViewButton
											opened={isOpened}
											onClick={() => onToggleL3(item.id)}
										/>
									</td>
								</tr>

								{isOpened && (
									<tr className='border-b border-neutral-200 bg-neutral-50/20'>
										<td colSpan={columnCount} className='p-3 pl-8'>
											<VtcgPlanExpand row={item} data={undefined as never} />
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
