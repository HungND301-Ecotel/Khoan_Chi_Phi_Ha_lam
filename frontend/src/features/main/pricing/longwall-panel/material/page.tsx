import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { API } from '@/constants/api-enpoint';
import { Seamface } from '@/features/main/catalog/parameter/seamface/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { Technology } from '@/features/main/catalog/parameter/technology/columns';
import { Power } from '@/features/main/catalog/parameter/power/columns';
import { LongwallMaterialForm } from '@/features/main/pricing/longwall-panel/material/form';
import {
	ExpandLongwallMaterialDetail,
	LONGWALL_MATERIAL_DETAIL_CGH_COLUMNS,
	LONGWALL_MATERIAL_DETAIL_NON_CGH_COLUMNS,
	LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS,
	LONGWALL_MATERIAL_COLUMNS,
	type ExpandLongwallMaterialCostRow,
	type LongwallMaterial,
} from '@/features/main/pricing/longwall-panel/material/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useEffect, useMemo, useState } from 'react';
import type {
	LongwallMaterialDetail,
	LongwallMaterialDetailCost,
	LongwallMaterialPeriod,
} from './type';

function buildGroupedExpandRows(
	costItems: LongwallMaterialDetailCost[],
	otherMaterialValue?: number | null,
): ExpandLongwallMaterialCostRow[] {
	const groupedRows = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentCode: string;
			assignmentCodeName: string;
			items: ExpandLongwallMaterialCostRow[];
		}
	>();

	costItems.forEach((item) => {
		const groupKey =
			item.assignmentCodeId ||
			`${item.assignmentCode}-${item.assignmentCodeName}`;

		if (!groupedRows.has(groupKey)) {
			groupedRows.set(groupKey, {
				assignmentCodeId: item.assignmentCodeId || groupKey,
				assignmentCode: item.assignmentCode,
				assignmentCodeName: item.assignmentCodeName,
				items: [],
			});
		}

		groupedRows.get(groupKey)?.items.push({
			rowType: 'material-item',
			assignmentCodeId: item.assignmentCodeId,
			assignmentCode: item.assignmentCode,
			assignmentCodeName: item.assignmentCodeName,
			materialId: item.materialId,
			materialCode: item.materialCode,
			materialName: item.materialName,
			unitOfMeasureName: item.unitOfMeasureName,
			unitPrice: item.unitPrice,
			norm: item.norm,
			totalPrice: item.totalPrice,
		});
	});

	const rows = Array.from(groupedRows.values()).flatMap((group) => {
		const groupTotal = group.items.reduce(
			(sum, item) => sum + Number(item.totalPrice || 0),
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
			...group.items,
		];
	});

	if (otherMaterialValue === undefined || otherMaterialValue === null) {
		return rows;
	}

	const baseTotal = costItems.reduce(
		(sum, item) => sum + Number(item.totalPrice || 0),
		0,
	);
	const otherMaterialTotal =
		(baseTotal * (Number(otherMaterialValue) || 0)) / 100;

	return [
		...rows,
		{
			rowType: 'group-summary',
			assignmentCodeId: 'VTK',
			assignmentCode: 'VTK',
			assignmentCodeName: 'Vật tư khác',
			materialId: 'group-VTK',
			materialCode: '',
			materialName: '',
			unitOfMeasureName: '',
			unitPrice: null,
			norm: '',
			totalPrice: otherMaterialTotal,
		},
	];
}

export function LongwallPanelMaterialPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (row: LongwallMaterial): boolean | 'indeterminate' => {
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

	const handleRowSelectChange = (row: LongwallMaterial, checked: boolean) => {
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
		rows: LongwallMaterial[],
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
	}: ActionDialogProps<LongwallMaterial>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.MATERIAL.LONGWALL_PANEL.DELETES,
				selectedPeriodIds,
			);

			popup.success(
				`Đã xoá thành công ${selectedPeriodIds.length} khoảng thời gian định mức.`,
			);
			setSelectedPeriodIds([]);
			await data.refresh();
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MATERIAL.LONGWALL_PANEL.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallMaterial>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.LONGWALL_PANEL.IMPORT,
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

	return (
		<DataTable
			url={API.PRICING.MATERIAL.LONGWALL_PANEL.GROUPED_LIST}
			columns={LONGWALL_MATERIAL_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã định mức vật liệu' },
				{ key: 'processName', label: 'Công đoạn sản xuất' },
				{ key: 'materialDetail', label: 'Thông số' },
			]}
			onCreate={
				hasPermission('pricing.longwallmaterialunitprice.create')
					? (props) => <LongwallMaterialForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission('pricing.longwallmaterialunitprice.create')
					? (props) => <LongwallMaterialForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission('pricing.longwallmaterialunitprice.update')
					? (props) => <LongwallMaterialForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission('pricing.longwallmaterialunitprice.delete')
					? handleDelete
					: undefined
			}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={
				hasPermission('pricing.longwallmaterialunitprice.export')
					? handleExport
					: undefined
			}
			onImport={
				hasPermission('pricing.longwallmaterialunitprice.import')
					? handleImport
					: undefined
			}
			onExpand={(props) => (
				<LongwallMaterialExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

interface LongwallMaterialExpandProps extends ActionDialogProps<LongwallMaterial> {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}

function LongwallMaterialExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: LongwallMaterialExpandProps) {
	const popup = usePopup();
	const [detail, setDetail] = useState<ExpandLongwallMaterialDetail>();
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const [periodCostsMap, setPeriodCostsMap] = useState<
		Record<string, ExpandLongwallMaterialCostRow[]>
	>({});
	const [loadingPeriodMap, setLoadingPeriodMap] = useState<
		Record<string, boolean>
	>({});

	useEffect(() => {
		if (!row) return;

		const load = async () => {
			const [technologyRes, powerRes, hardnessRes, seamFaceRes] =
				await Promise.all([
					row.technologyId
						? api.get<Technology>(
								API.CATALOG.PARAMETER.TECHNOLOGY.DETAIL(row.technologyId),
							)
						: Promise.resolve(undefined),
					row.powerId
						? api.get<Power>(API.CATALOG.PARAMETER.POWER.DETAIL(row.powerId))
						: Promise.resolve(undefined),
					row.hardnessId
						? api.get<Strength>(
								API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId),
							)
						: Promise.resolve(undefined),
					row.seamFaceId
						? api.get<Seamface>(
								API.CATALOG.PARAMETER.SEAMFACE.DETAIL(row.seamFaceId),
							)
						: Promise.resolve(undefined),
				]);

			const longwallParametersValue = row.longwallParameters
				? `Llc ${row.longwallParameters.llc}; Lkc ${row.longwallParameters.lkc}; Mk ${row.longwallParameters.mk}`
				: '';

			const cuttingThicknessValue =
				row.cuttingThickness?.value ??
				(row.cuttingthicknessId ? String(row.cuttingthicknessId) : '');

			const isCGH = !!row.isLongwallMaterialUnitPriceCGH;

			setDetail({
				technologyName:
					technologyRes?.result?.value ?? row.technologyName ?? '',
				powerOrHardnessValue: isCGH
					? (powerRes?.result?.value ?? row.powerName ?? '')
					: (hardnessRes?.result?.value ?? row.hardnessName ?? ''),
				longwallParametersValue,
				cuttingThicknessValue,
				seamFaceValue: seamFaceRes?.result?.value ?? row.seamFaceName ?? '',
			});
		};

		load();
	}, [row]);

	const handleTogglePeriodDetails = async (period: LongwallMaterialPeriod) => {
		const isCurrentlyExpanded = expandedPeriodIds.includes(period.id);

		if (isCurrentlyExpanded) {
			setExpandedPeriodIds((prev) => prev.filter((id) => id !== period.id));
			return;
		}

		setExpandedPeriodIds((prev) => [...prev, period.id]);

		if (!periodCostsMap[period.id]) {
			setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: true }));
			try {
				const res = await api.get<LongwallMaterialDetail>(
					API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(period.id),
				);
				const rows = buildGroupedExpandRows(
					res.result.costs ?? [],
					res.result.otherMaterialValue,
				);
				setPeriodCostsMap((prev) => ({ ...prev, [period.id]: rows }));
			} catch (error) {
				popup.error(error);
			} finally {
				setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: false }));
			}
		}
	};

	const detailColumns = useMemo(
		() =>
			row?.isLongwallMaterialUnitPriceCGH
				? LONGWALL_MATERIAL_DETAIL_CGH_COLUMNS
				: LONGWALL_MATERIAL_DETAIL_NON_CGH_COLUMNS,
		[row?.isLongwallMaterialUnitPriceCGH],
	);

	const detailItems = useMemo(() => [detail ?? {}], [detail]);
	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 flex flex-col gap-3 text-black'>
			{/* TẦNG 2: 1. BẢNG NHỎ THÔNG SỐ KỸ THUẬT */}
			<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
				<DataTable
					columns={detailColumns}
					items={detailItems}
					hasActions={false}
					hasPagination={false}
					hasSort={false}
					hasIndex={false}
					compact={true}
				/>
			</div>

			{/* TẦNG 2: 2. BẢNG THỜI GIAN VÀ ĐƠN GIÁ VẬT LIỆU */}
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
								Đơn giá vật liệu (đ/1000 tấn)
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
																columns={
																	LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS
																}
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
