import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { API } from '@/constants/api-enpoint';
import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { Technology } from '@/features/main/catalog/parameter/technology/columns';
import { MaterialForm } from '@/features/main/pricing/tunneling/material/form';
import {
	ExpandMaterialCostRow,
	ExpandMaterialDetail,
	MAIN_PRICING_MATERIAL_COLUMNS,
	MAIN_PRICING_MATERIAL_DETAIL_COLUMNS,
	MAIN_PRICING_MATERIAL_EXPAND_COLUMNS,
} from '@/features/main/pricing/tunneling/material/columns';
import {
	ExpandSupportAndDrillingDetail,
	MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS,
	MAIN_PRICING_SUPPORT_AND_DRILLING_DETAIL_COLUMNS,
} from '@/features/main/pricing/tunneling/material/support-and-drilling-columns';
import { SupportAndDrillingForm } from '@/features/main/pricing/tunneling/material/support-and-drilling-form';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Fragment, useEffect, useMemo, useState } from 'react';
import {
	Material,
	MaterialDetail,
	MaterialDetailCost,
	MaterialPeriod,
	SupportAndDrillingMaterial,
	SupportAndDrillingMaterialDetail,
} from './type';

function buildGroupedExpandRows(
	costItems: MaterialDetailCost[],
	otherMaterialValue?: number | null,
): ExpandMaterialCostRow[] {
	const groupedRows = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentCode: string;
			assignmentCodeName: string;
			items: ExpandMaterialCostRow[];
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
			unitPrice: item.unitPrice,
			norm: item.norm,
			totalPrice: item.totalPrice,
		});
	});

	const rows = Array.from(groupedRows.values()).flatMap((group) => {
		const groupTotal = group.items.reduce(
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

	const baseTotal = costItems.reduce((sum, item) => sum + item.totalPrice, 0);
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
			unitPrice: null,
			norm: '',
			totalPrice: otherMaterialTotal,
		},
	];
}

export function MainPricingMaterialPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const isRowSelected = (row: Material): boolean | 'indeterminate' => {
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

	const handleRowSelectChange = (row: Material, checked: boolean) => {
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

	const handleSelectAllRowsChange = (checked: boolean, rows: Material[]) => {
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

	const handleDelete = async ({ data }: ActionDialogProps<Material>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.MATERIAL.TUNNELING.DELETES,
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
			const filename = await api.export(API.PRICING.MATERIAL.TUNNELING.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Material>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.TUNNELING.IMPORT,
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

	const [selectedSupportPeriodIds, setSelectedSupportPeriodIds] = useState<
		string[]
	>([]);

	const isSupportRowSelected = (
		row: SupportAndDrillingMaterial,
	): boolean | 'indeterminate' => {
		const periods = row.periods ?? [];
		if (periods.length === 0) {
			return selectedSupportPeriodIds.includes(row.id);
		}
		const selectedCount = periods.filter((p) =>
			selectedSupportPeriodIds.includes(p.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === periods.length) return true;
		return 'indeterminate';
	};

	const handleSupportRowSelectChange = (
		row: SupportAndDrillingMaterial,
		checked: boolean,
	) => {
		const periodIds =
			row.periods && row.periods.length > 0
				? row.periods.map((p) => p.id)
				: [row.id];

		setSelectedSupportPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...periodIds]));
			} else {
				return prev.filter((id) => !periodIds.includes(id));
			}
		});
	};

	const handleSelectAllSupportRowsChange = (
		checked: boolean,
		rows: SupportAndDrillingMaterial[],
	) => {
		const allPeriodIds = rows.flatMap((r) =>
			r.periods && r.periods.length > 0 ? r.periods.map((p) => p.id) : [r.id],
		);
		setSelectedSupportPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allPeriodIds]));
			} else {
				return prev.filter((id) => !allPeriodIds.includes(id));
			}
		});
	};

	const handleToggleSupportPeriodSelect = (periodId: string) => {
		setSelectedSupportPeriodIds((prev) => {
			if (prev.includes(periodId)) {
				return prev.filter((id) => id !== periodId);
			} else {
				return [...prev, periodId];
			}
		});
	};

	const handleDeleteSupportAndDrilling = async ({
		data,
	}: ActionDialogProps<SupportAndDrillingMaterial>) => {
		if (selectedSupportPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DELETES,
				selectedSupportPeriodIds,
			);

			popup.success(
				`Đã xoá thành công ${selectedSupportPeriodIds.length} khoảng thời gian định mức.`,
			);
			setSelectedSupportPeriodIds([]);
			await data.refresh();
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExportSupportAndDrilling = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImportSupportAndDrilling = async (
		file: File,
		data?: ActionDialogProps<SupportAndDrillingMaterial>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.IMPORT,
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

	const canReadMaterial = hasPermission('pricing.materialunitprice.read');
	const canReadSupportAndDrilling = hasPermission(
		'pricing.tunnelsupportanddrillingmaterialunitprice.read',
	);
	const defaultTab = canReadMaterial
		? 'material'
		: canReadSupportAndDrilling
			? 'support-and-drilling'
			: '';

	if (!defaultTab) return null;

	return (
		<Tabs defaultValue={defaultTab} className='w-full'>
			<TabsList className='mb-4'>
				{canReadMaterial && (
					<TabsTrigger value='material'>
						Đơn giá và định mức vật liệu đào lò
					</TabsTrigger>
				)}
				{canReadSupportAndDrilling && (
					<TabsTrigger value='support-and-drilling'>
						Đơn giá và định mức lò neo bê tông phun
					</TabsTrigger>
				)}
			</TabsList>

			{canReadMaterial && (
				<TabsContent value='material' className='mt-0'>
					<DataTable
						url={API.PRICING.MATERIAL.TUNNELING.GROUPED_LIST}
						columns={MAIN_PRICING_MATERIAL_COLUMNS}
						filters={[
							{ key: 'code', label: 'Mã định mức vật liệu' },
							{ key: 'processName', label: 'Công đoạn sản xuất' },
							{ key: 'materialDetail', label: 'Thông số' },
						]}
						onCreate={
							hasPermission('pricing.materialunitprice.create')
								? (props) => <MaterialForm {...props} />
								: undefined
						}
						onDuplicate={
							hasPermission('pricing.materialunitprice.create')
								? (props) => <MaterialForm {...props} isDuplicate />
								: undefined
						}
						onUpdate={
							hasPermission('pricing.materialunitprice.update')
								? (props) => <MaterialForm {...props} />
								: undefined
						}
						onDelete={
							hasPermission('pricing.materialunitprice.delete')
								? handleDelete
								: undefined
						}
						deleteCountOverride={selectedPeriodIds.length}
						deleteDisabledOverride={selectedPeriodIds.length === 0}
						isRowSelected={isRowSelected}
						onRowSelectChange={handleRowSelectChange}
						onSelectAllRowsChange={handleSelectAllRowsChange}
						onExport={
							hasPermission('pricing.materialunitprice.export')
								? handleExport
								: undefined
						}
						onImport={
							hasPermission('pricing.materialunitprice.import')
								? handleImport
								: undefined
						}
						onExpand={(props) => (
							<MaterialDetailExpand
								{...props}
								selectedPeriodIds={selectedPeriodIds}
								onTogglePeriodSelect={handleTogglePeriodSelect}
							/>
						)}
					/>
				</TabsContent>
			)}

			{canReadSupportAndDrilling && (
				<TabsContent value='support-and-drilling' className='mt-0'>
					<DataTable
						url={API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.GROUPED_LIST}
						columns={MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS}
						filters={[
							{ key: 'code', label: 'Mã đơn giá' },
							{ key: 'processName', label: 'Công đoạn sản xuất' },
							{ key: 'technologyName', label: 'Công nghệ' },
							{ key: 'passportName', label: 'Hộ chiếu' },
							{ key: 'hardnessName', label: 'Độ kiên cố than đá' },
						]}
						onCreate={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.create',
							)
								? (props) => <SupportAndDrillingForm {...props} />
								: undefined
						}
						onDuplicate={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.create',
							)
								? (props) => <SupportAndDrillingForm {...props} isDuplicate />
								: undefined
						}
						onUpdate={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.update',
							)
								? (props) => <SupportAndDrillingForm {...props} />
								: undefined
						}
						onDelete={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.delete',
							)
								? handleDeleteSupportAndDrilling
								: undefined
						}
						deleteCountOverride={selectedSupportPeriodIds.length}
						deleteDisabledOverride={selectedSupportPeriodIds.length === 0}
						isRowSelected={isSupportRowSelected}
						onRowSelectChange={handleSupportRowSelectChange}
						onSelectAllRowsChange={handleSelectAllSupportRowsChange}
						onExport={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.export',
							)
								? handleExportSupportAndDrilling
								: undefined
						}
						onImport={
							hasPermission(
								'pricing.tunnelsupportanddrillingmaterialunitprice.import',
							)
								? handleImportSupportAndDrilling
								: undefined
						}
						onExpand={(props) => (
							<SupportAndDrillingDetailExpand
								{...props}
								selectedPeriodIds={selectedSupportPeriodIds}
								onTogglePeriodSelect={handleToggleSupportPeriodSelect}
							/>
						)}
					/>
				</TabsContent>
			)}
		</Tabs>
	);
}

interface MaterialDetailExpandProps extends ActionDialogProps<Material> {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}

function MaterialDetailExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: MaterialDetailExpandProps) {
	const popup = usePopup();
	const [detail, setDetail] = useState<ExpandMaterialDetail>();
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const [periodCostsMap, setPeriodCostsMap] = useState<
		Record<string, ExpandMaterialCostRow[]>
	>({});
	const [loadingPeriodMap, setLoadingPeriodMap] = useState<
		Record<string, boolean>
	>({});

	useEffect(() => {
		if (!row) return;

		const promises = Promise.all([
			api.get<Passport>(API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId)),
			api.get<Strength>(API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId)),
			api.get<Insert>(API.CATALOG.PARAMETER.INSERT.DETAIL(row.insertItemId)),
			api.get<Step>(API.CATALOG.PARAMETER.STEP.DETAIL(row.supportStepId)),
		]);

		promises.then(([passport, strength, insert, step]) => {
			setDetail({
				passport: passport.result,
				strength: strength.result,
				insert: insert.result,
				step: step.result,
			});
		});
	}, [row]);

	const handleTogglePeriodDetails = async (period: MaterialPeriod) => {
		const isCurrentlyExpanded = expandedPeriodIds.includes(period.id);

		if (isCurrentlyExpanded) {
			setExpandedPeriodIds((prev) => prev.filter((id) => id !== period.id));
			return;
		}

		setExpandedPeriodIds((prev) => [...prev, period.id]);

		if (!periodCostsMap[period.id]) {
			setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: true }));
			try {
				const res = await api.get<MaterialDetail>(
					API.PRICING.MATERIAL.TUNNELING.DETAIL(period.id),
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

	const detailItems = useMemo(() => [detail ?? {}], [detail]);
	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 flex flex-col gap-3 text-black'>
			{/* TẦNG 2: 1. BẢNG NHỎ THÔNG SỐ KỸ THUẬT */}
			<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
				<DataTable
					columns={MAIN_PRICING_MATERIAL_DETAIL_COLUMNS}
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
								Đơn giá vật liệu (đ/m)
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
													{/* Nút Xem chi tiết: VisibilityIcon / VisibilityOffIcon */}
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
																columns={MAIN_PRICING_MATERIAL_EXPAND_COLUMNS}
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

interface SupportAndDrillingDetailExpandProps extends ActionDialogProps<SupportAndDrillingMaterial> {
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}

function SupportAndDrillingDetailExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: SupportAndDrillingDetailExpandProps) {
	const popup = usePopup();
	const [detail, setDetail] = useState<ExpandSupportAndDrillingDetail>();
	const [expandedPeriodIds, setExpandedPeriodIds] = useState<string[]>([]);
	const [periodCostsMap, setPeriodCostsMap] = useState<
		Record<string, ExpandMaterialCostRow[]>
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
			row.technologyId
				? api.get<Technology>(
						API.CATALOG.PARAMETER.TECHNOLOGY.DETAIL(row.technologyId),
					)
				: Promise.resolve(undefined),
		]);

		promises.then(([passport, strength, tech]) => {
			const p = passport?.result;
			setDetail({
				passportText: p
					? `H/c ${p.name}; ${p.sd}; ${p.sc}`
					: (row.passportName ?? ''),
				hardnessName: strength?.result?.value ?? row.hardnessName ?? '',
				technologyName: tech?.result?.value ?? row.technologyName ?? '',
			});
		});
	}, [row]);

	const handleTogglePeriodDetails = async (period: MaterialPeriod) => {
		const isCurrentlyExpanded = expandedPeriodIds.includes(period.id);

		if (isCurrentlyExpanded) {
			setExpandedPeriodIds((prev) => prev.filter((id) => id !== period.id));
			return;
		}

		setExpandedPeriodIds((prev) => [...prev, period.id]);

		if (!periodCostsMap[period.id]) {
			setLoadingPeriodMap((prev) => ({ ...prev, [period.id]: true }));
			try {
				const res = await api.get<SupportAndDrillingMaterialDetail>(
					API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DETAIL(period.id),
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

	const detailItems = useMemo(() => [detail ?? {}], [detail]);
	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 flex flex-col gap-3 text-black'>
			{/* TẦNG 2: 1. BẢNG NHỎ THÔNG SỐ KỸ THUẬT */}
			<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
				<DataTable
					columns={MAIN_PRICING_SUPPORT_AND_DRILLING_DETAIL_COLUMNS}
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
								Đơn giá vật liệu (đ/m)
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
																columns={MAIN_PRICING_MATERIAL_EXPAND_COLUMNS}
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
