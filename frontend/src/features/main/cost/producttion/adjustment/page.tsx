import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { useMeta } from '@/data/meta/meta-hook';
import { AdjustmentExpand } from '@/features/main/cost/producttion/adjustment/adjustment-expand';
import {
	ADJUSTMENT_DEPARTMENT_COLUMNS,
	DepartmentAdjustmentGroup,
	MAIN_COST_ADJUSTMENT_COLUMNS,
	ProductionAdjustment,
	VTL_COST_ADJUSTMENT_COLUMNS,
} from '@/features/main/cost/producttion/adjustment/columns';
import {
	type DepartmentAdjustmentDetail,
	type DepartmentAdjustmentMonth,
	mapDepartmentAdjustmentDetail,
} from '@/features/main/cost/producttion/adjustment/type';
import { VtlAdjustmentExpand } from '@/features/main/cost/producttion/adjustment/van-tai-lo/expand';
import { TransportCostComponent } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useCallback, useEffect, useMemo, useState } from 'react';

function areStringArraysEqual(a: string[], b: string[]) {
	return a.length === b.length && a.every((value, index) => value === b[index]);
}

function areStringRecordsEqual(
	a: Record<string, string[]>,
	b: Record<string, string[]>,
) {
	const aKeys = Object.keys(a);
	const bKeys = Object.keys(b);

	if (aKeys.length !== bKeys.length) return false;

	return aKeys.every((key) => {
		const aValue = a[key] ?? [];
		const bValue = b[key] ?? [];
		return areStringArraysEqual(aValue, bValue);
	});
}

type DepartmentAdjustmentMonthGroup = {
	id: string;
	time: string;
	productUnitPriceIds: string[];
	products: ProductionAdjustment[];
};

type DepartmentAdjustmentProductsTableProps = {
	monthId: string;
	items: ProductionAdjustment[];
	selectAllRows: boolean;
	onSelectedRowsChange: (monthId: string, rows: ProductionAdjustment[]) => void;
};

function DepartmentAdjustmentProductsTable({
	monthId,
	items,
	selectAllRows,
	onSelectedRowsChange,
}: DepartmentAdjustmentProductsTableProps) {
	const handleSelectedRowsChange = useCallback(
		(rows: unknown[]) => {
			onSelectedRowsChange(monthId, rows as ProductionAdjustment[]);
		},
		[monthId, onSelectedRowsChange],
	);

	const isVTL = items[0]?.fixedKeyType === ProcessGroupType.VTL;

	if (isVTL) {
		return (
			<DataTable
				columns={VTL_COST_ADJUSTMENT_COLUMNS}
				items={items}
				getRowId={(item) => item.id}
				importCrumb='Doanh thu điều chỉnh'
				filters={[
					{ key: 'productionProcessCode', label: 'Mã CĐSX' },
					{ key: 'productionProcessName', label: 'Tên CĐSX' },
					{ key: 'contractCodeCode', label: 'Mã nhóm VTTS' },
					{ key: 'contractCodeName', label: 'Tên nhóm VTTS' },
					{ key: 'routeDepartmentCode', label: 'Mã đơn vị' },
					{ key: 'routeDepartmentName', label: 'Tên đơn vị' },
				]}
				onExpand={(props) => (
					<VtlAdjustmentExpand {...props} monthId={monthId} />
				)}
				showCreateAction={false}
				showFilterAction={false}
				showDeleteAction={false}
				showUtilityActions={false}
				onDelete={async () => undefined}
				onSelectedRowsChange={handleSelectedRowsChange}
				selectAllPageRows={selectAllRows}
				hasPagination={false}
			/>
		);
	}

	return (
		<DataTable
			columns={MAIN_COST_ADJUSTMENT_COLUMNS}
			items={items}
			getRowId={(item) => item.id}
			importCrumb='Doanh thu điều chỉnh'
			filters={[
				{ key: 'productCode', label: 'Mã sản phẩm' },
				{ key: 'productName', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onExpand={(props) => <AdjustmentExpand {...props} monthId={monthId} />}
			showCreateAction={false}
			showFilterAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
			onDelete={async () => undefined}
			onSelectedRowsChange={handleSelectedRowsChange}
			selectAllPageRows={selectAllRows}
			hasPagination={false}
		/>
	);
}

type DepartmentAdjustmentMonthsTableProps = {
	departmentId: string;
	hasKhaiThac?: boolean;
	hasVanTaiLo?: boolean;
	reloadKey: number;
	selectAllRows: boolean;
	onSelectedProductIdsChange: (
		departmentId: string,
		productIds: string[],
	) => void;
};

function mapDepartmentDetailToMonthGroups(
	detail: DepartmentAdjustmentDetail,
): DepartmentAdjustmentMonthGroup[] {
	return detail.months
		.map((month: DepartmentAdjustmentMonth) => ({
			id: month.month,
			time: month.month,
			productUnitPriceIds: month.items.map((item) => item.productUnitPriceId),
			products: month.items.map((item) => ({
				id: item.productUnitPriceId,
				productUnitPriceId: item.productUnitPriceId,
				plannedOutputId: item.plannedOutputId,
				productionOutputId: item.productionOutputId,
				productId: item.productId,
				productCode: item.productCode,
				productName: item.productName,
				processGroupId: item.processGroupId,
				processGroupCode: item.processGroupCode,
				processGroupName: item.processGroupName,
				fixedKeyType: item.fixedKeyType,
				processGroupType: item.processGroupType,
				unitOfMeasureId: item.unitOfMeasureId,
				unitOfMeasureName: item.unitOfMeasureName,
				departmentId: detail.departmentId,
				departmentCode: detail.departmentCode,
				departmentName: detail.departmentName,
				totalProductionMeters: item.productionMeters,
				plannedTotalCost: 0,
				actualTotalCost: 0,
				adjustmentTotalCost: item.adjustmentTotalCost,
				startMonth: month.month,
				endMonth: month.month,
				standardProductionMeters: item.standardProductionMeters,
				actualAshContent: item.actualAshContent,
				akRate: item.akRate,
				akRatePercent: item.akRatePercent,
			})),
		}))
		.sort((a, b) => a.time.localeCompare(b.time));
}

function DepartmentAdjustmentMonthsTable({
	departmentId,
	hasKhaiThac,
	hasVanTaiLo,
	reloadKey,
	selectAllRows,
	onSelectedProductIdsChange,
}: DepartmentAdjustmentMonthsTableProps) {
	const [monthGroups, setMonthGroups] = useState<
		DepartmentAdjustmentMonthGroup[]
	>([]);
	const [selectedProductIdsByMonth, setSelectedProductIdsByMonth] = useState<
		Record<string, string[]>
	>({});
	const [openedMonthIds, setOpenedMonthIds] = useState<string[]>([]);
	const handleMonthProductSelectionChange = useCallback(
		(monthId: string, rows: ProductionAdjustment[]) => {
			const nextIds = rows.map((item) => item.id);
			setSelectedProductIdsByMonth((prev) => {
				const currentIds = prev[monthId] ?? [];
				if (areStringArraysEqual(currentIds, nextIds)) return prev;
				return {
					...prev,
					[monthId]: nextIds,
				};
			});
		},
		[],
	);

	useEffect(() => {
		let mounted = true;

		const loadDepartmentDetail = async () => {
			const khaiThacPromise =
				hasKhaiThac !== false
					? api
							.get<DepartmentAdjustmentDetail>(
								API.COST.PRODUCT.DETAIL_ADJUSTMENT_BY_DEPARTMENT(departmentId),
							)
							.catch(() => null)
					: Promise.resolve(null);

			const vanTaiLoPromise =
				hasVanTaiLo !== false
					? api
							.get<{
								departmentId: string;
								departmentCode: string;
								departmentName: string;
								months: {
									month: string;
									items: {
										id: string;
										productionProcessId?: string;
										productionProcessCode?: string;
										productionProcessName?: string;
										transportRouteId?: string;
										transportRouteCode?: string;
										transportRouteName?: string;
										routeDepartmentId?: string;
										routeDepartmentCode?: string;
										routeDepartmentName?: string;
										equipmentId?: string;
										equipmentCode?: string;
										equipmentName?: string;
										equipmentQuality?: string;
										actualProductionMeters?: number;
										unitOfMeasureId?: string;
										unitOfMeasureName?: string;
										material?: TransportCostComponent;
										maintenance?: TransportCostComponent;
										power?: TransportCostComponent;
										isLowVolumeCase?: boolean;
										adjustmentTotalCost?: number;
									}[];
								}[];
							}>(
								API.COST.TRANSPORT_PLAN_LINE.DETAIL_ADJUSTMENT_BY_DEPARTMENT(
									departmentId,
								),
							)
							.catch(() => null)
					: Promise.resolve(null);

			const [khaiThacRes, vanTaiLoRes] = await Promise.all([
				khaiThacPromise,
				vanTaiLoPromise,
			]);

			if (!mounted) return;

			const khaiThacDetail = khaiThacRes?.result
				? mapDepartmentAdjustmentDetail(khaiThacRes.result)
				: null;

			const vanTaiLoDetail = vanTaiLoRes?.result ? vanTaiLoRes.result : null;

			const mergedMap = new Map<string, DepartmentAdjustmentMonthGroup>();

			if (khaiThacDetail) {
				const ktGroups = mapDepartmentDetailToMonthGroups(khaiThacDetail);
				ktGroups.forEach((g) => mergedMap.set(g.id, g));
			}

			if (vanTaiLoDetail?.months) {
				vanTaiLoDetail.months.forEach((vtlMonth) => {
					const monthKey = vtlMonth.month.substring(0, 10);
					const vtlProducts: ProductionAdjustment[] = (
						vtlMonth.items || []
					).map((item) => {
						const nameParts = [
							item.productionProcessName,
							item.equipmentName && `TB: ${item.equipmentName}`,
							item.equipmentQuality && `Loại ${item.equipmentQuality}`,
							item.transportRouteName && `Tuyến: ${item.transportRouteName}`,
						].filter(Boolean);

						return {
							id: item.id,
							productId: item.productionProcessId || item.id,
							productCode:
								item.productionProcessCode ||
								item.equipmentCode ||
								item.transportRouteCode ||
								'VTL',
							productName: nameParts.join(' - ') || 'Vận tải lò',
							processGroupId: '',
							processGroupCode: 'VTL',
							fixedKeyType: ProcessGroupType.VTL,
							unitOfMeasureId: item.unitOfMeasureId || '',
							unitOfMeasureName: item.unitOfMeasureName || '-',
							departmentId: vanTaiLoDetail.departmentId,
							departmentCode: vanTaiLoDetail.departmentCode,
							departmentName: vanTaiLoDetail.departmentName,
							routeDepartmentId: item.routeDepartmentId,
							routeDepartmentCode: item.routeDepartmentCode || '-',
							routeDepartmentName: item.routeDepartmentName || '-',
							totalProductionMeters: item.actualProductionMeters ?? 0,
							plannedTotalCost: 0,
							actualTotalCost: 0,
							adjustmentTotalCost: item.adjustmentTotalCost ?? 0,
							startMonth: monthKey,
							endMonth: monthKey,
							productionProcessCode: item.productionProcessCode || '-',
							productionProcessName: item.productionProcessName || '-',
							contractCodeCode:
								item.equipmentCode || item.transportRouteCode || '-',
							contractCodeName:
								item.equipmentName || item.transportRouteName || '-',
							equipmentQuality: item.equipmentQuality || '-',
							material: item.material,
							maintenance: item.maintenance,
							power: item.power,
							isLowVolumeCase: item.isLowVolumeCase,
						};
					});

					const existing = mergedMap.get(monthKey);
					if (existing) {
						existing.products = [...existing.products, ...vtlProducts];
						existing.productUnitPriceIds = [
							...existing.productUnitPriceIds,
							...vtlMonth.items.map((it) => it.id),
						];
					} else {
						mergedMap.set(monthKey, {
							id: monthKey,
							time: monthKey,
							productUnitPriceIds: vtlMonth.items.map((it) => it.id),
							products: vtlProducts,
						});
					}
				});
			}

			const mergedMonthGroups = Array.from(mergedMap.values()).sort((a, b) =>
				a.time.localeCompare(b.time),
			);
			setMonthGroups(mergedMonthGroups);
		};

		loadDepartmentDetail();

		return () => {
			mounted = false;
		};
	}, [departmentId, hasKhaiThac, hasVanTaiLo, reloadKey]);

	useEffect(() => {
		setSelectedProductIdsByMonth((prev) => {
			const next = Object.fromEntries(
				Object.entries(prev).filter(([monthId]) =>
					monthGroups.some((group) => group.id === monthId),
				),
			);
			return areStringRecordsEqual(prev, next) ? prev : next;
		});
		setOpenedMonthIds((prev) => {
			const next = prev.filter((monthId) =>
				monthGroups.some((group) => group.id === monthId),
			);
			return areStringArraysEqual(prev, next) ? prev : next;
		});
	}, [monthGroups]);

	useEffect(() => {
		const selectedIds = monthGroups.flatMap((group) => {
			const overriddenIds = selectedProductIdsByMonth[group.id];
			return overriddenIds ?? [];
		});

		onSelectedProductIdsChange(departmentId, [...new Set(selectedIds)]);
	}, [
		departmentId,
		monthGroups,
		onSelectedProductIdsChange,
		selectedProductIdsByMonth,
	]);

	return (
		<Accordion
			type='multiple'
			className='mx-2 mr-4 flex w-auto min-w-0 flex-col gap-2'
			value={openedMonthIds}
			onValueChange={setOpenedMonthIds}
		>
			{monthGroups.map((group) => (
				<AccordionItem
					key={group.id}
					value={group.id}
					className='min-w-0 overflow-hidden border-none'
				>
					<Item
						variant='outline'
						className='w-full flex-1 rounded-sm py-3 pr-6'
					>
						<ItemContent>
							<ItemTitle className='text-sm font-semibold'>
								{formatDate(group.time)}
							</ItemTitle>
						</ItemContent>
						<ItemActions className='pr-2'>
							<AccordionTrigger className='group p-0 hover:no-underline'>
								<div className='group-data-[state=open]:hidden'>
									<VisibilityIcon />
								</div>
								<div className='hidden group-data-[state=open]:block'>
									<VisibilityOffIcon />
								</div>
							</AccordionTrigger>
						</ItemActions>
					</Item>

					<AccordionContent className='p-0 px-2 pt-2'>
						<div className='w-full min-w-0 overflow-x-auto'>
							<DepartmentAdjustmentProductsTable
								monthId={group.id}
								items={group.products}
								selectAllRows={selectAllRows}
								onSelectedRowsChange={handleMonthProductSelectionChange}
							/>
						</div>
					</AccordionContent>
				</AccordionItem>
			))}
		</Accordion>
	);
}

function groupByDepartment(
	products: ProductionAdjustment[],
): DepartmentAdjustmentGroup[] {
	const groups = new Map<string, DepartmentAdjustmentGroup>();
	const toTimestamp = (value?: string) => {
		if (!value) return undefined;
		const parsed = Date.parse(value);
		return Number.isNaN(parsed) ? undefined : parsed;
	};

	products.forEach((item) => {
		if (!item.departmentId) return;

		const existed = groups.get(item.departmentId);
		if (existed) {
			const existedStart = toTimestamp(existed.startMonth);
			const itemStart = toTimestamp(item.startMonth);
			if (
				item.startMonth &&
				(existedStart === undefined ||
					(itemStart !== undefined && itemStart < existedStart))
			) {
				existed.startMonth = item.startMonth;
			}

			const existedEnd = toTimestamp(existed.endMonth);
			const itemEnd = toTimestamp(item.endMonth);
			if (
				item.endMonth &&
				(existedEnd === undefined ||
					(itemEnd !== undefined && itemEnd > existedEnd))
			) {
				existed.endMonth = item.endMonth;
			}

			existed.productUnitPriceIds.push(item.id);
			return;
		}

		groups.set(item.departmentId, {
			id: item.departmentId,
			code: item.departmentCode ?? '',
			name: item.departmentName ?? '',
			startMonth: item.startMonth,
			endMonth: item.endMonth,
			productUnitPriceIds: [item.id],
		});
	});

	return Array.from(groups.values()).sort((a, b) =>
		a.code.localeCompare(b.code),
	);
}

export function MainCostProductionRevenueAdjustmentPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [reloadKey, setReloadKey] = useState(0);
	const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>(
		[],
	);
	const [selectedProductIdsByDepartment, setSelectedProductIdsByDepartment] =
		useState<Record<string, string[]>>({});
	const [vtlDepartmentGroups, setVtlDepartmentGroups] = useState<
		DepartmentAdjustmentGroup[]
	>([]);
	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 2,
		}),
		[reloadKey],
	);
	const selectedProductIds = useMemo(
		() => [...new Set(Object.values(selectedProductIdsByDepartment).flat())],
		[selectedProductIdsByDepartment],
	);

	// Doanh thu điều chỉnh VTL không có scenario riêng (không lưu thành bản ghi mới) — chỉ cần
	// biết đơn vị nào có Kế hoạch ban đầu VTL để gộp vào danh sách Đơn vị, dùng chung
	// TransportPlanLine/List đã có sẵn (giống cost/plan/page.tsx).
	useEffect(() => {
		let mounted = true;

		Promise.all([
			api.pagging<{ id: string; code: string; name: string }>(
				API.CATALOG.DEPARTMENT.LIST,
				{ ignorePagination: true },
			),
			api.get<
				{
					departmentId: string;
					departmentCode: string;
					departmentName: string;
					months: { month: string; items: { id: string }[] }[];
				}[]
			>(API.COST.TRANSPORT_PLAN_LINE.LIST),
		])
			.then(([deptsRes, vtlRes]) => {
				if (!mounted) return;
				const deptsMap = new Map(
					(deptsRes.result.data ?? []).map((d) => [d.id, d]),
				);
				const list = vtlRes.result || [];
				const vtlGroups: DepartmentAdjustmentGroup[] = [];

				list.forEach((detail) => {
					const months = detail.months || [];
					if (!months.length) return;

					const allItems = months.flatMap((m) => m.items || []);
					if (!allItems.length) return;

					const monthDates = months
						.map((m) => m.month.substring(0, 10))
						.sort();

					const deptInfo = deptsMap.get(detail.departmentId);

					vtlGroups.push({
						id: detail.departmentId,
						code: detail.departmentCode || deptInfo?.code || '',
						name: detail.departmentName || deptInfo?.name || '',
						startMonth: monthDates[0],
						endMonth: monthDates[monthDates.length - 1],
						productUnitPriceIds: allItems.map((it) => it.id),
					});
				});

				setVtlDepartmentGroups(vtlGroups);
			})
			.catch(() => {});

		return () => {
			mounted = false;
		};
	}, [reloadKey]);

	const transformDepartmentRows = useCallback(
		(rows: DepartmentAdjustmentGroup[]) => {
			const khaiThacGroups = groupByDepartment(
				rows as unknown as ProductionAdjustment[],
			);
			const map = new Map<string, DepartmentAdjustmentGroup>();

			khaiThacGroups.forEach((g) =>
				map.set(g.id, {
					...g,
					hasKhaiThac: true,
					hasVanTaiLo: false,
				}),
			);

			vtlDepartmentGroups.forEach((vtlGroup) => {
				const existed = map.get(vtlGroup.id);
				if (existed) {
					existed.hasVanTaiLo = true;
					// Không gộp productUnitPriceIds VTL vào tập xoá được — Doanh thu điều chỉnh VTL
					// chỉ là view tính toán (Kế hoạch + Sản lượng thực tế), không có bản ghi riêng để
					// xoá; gộp id TransportPlanLine vào đây sẽ bị gửi nhầm sang API xoá ProductUnitPrice.
					if (
						vtlGroup.startMonth &&
						(!existed.startMonth || vtlGroup.startMonth < existed.startMonth)
					) {
						existed.startMonth = vtlGroup.startMonth;
					}
					if (
						vtlGroup.endMonth &&
						(!existed.endMonth || vtlGroup.endMonth > existed.endMonth)
					) {
						existed.endMonth = vtlGroup.endMonth;
					}
				} else {
					map.set(vtlGroup.id, {
						...vtlGroup,
						hasKhaiThac: false,
						hasVanTaiLo: true,
						productUnitPriceIds: [],
					});
				}
			});

			return Array.from(map.values()).sort((a, b) =>
				a.code.localeCompare(b.code),
			);
		},
		[vtlDepartmentGroups],
	);

	const handleDeleteAdjustment = async ({
		data,
	}: ActionDialogProps<DepartmentAdjustmentGroup>) => {
		try {
			const ids = selectedProductIds;
			if (!ids.length) return;

			await api.delete(API.COST.PRODUCT.DELETES, ids);

			success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
			setSelectedDepartmentIds([]);
			setSelectedProductIdsByDepartment({});
			setReloadKey((prev) => prev + 1);
		} catch (err) {
			error(err);
		}
	};

	const handleDepartmentSelectionChange = useCallback((rows: unknown[]) => {
		const departmentRows = rows as DepartmentAdjustmentGroup[];
		const departmentIds = departmentRows.map((row) => row.id);
		setSelectedDepartmentIds((prev) =>
			areStringArraysEqual(prev, departmentIds) ? prev : departmentIds,
		);
		setSelectedProductIdsByDepartment((prev) => {
			const next: Record<string, string[]> = {};

			Object.keys(prev).forEach((departmentId) => {
				if (departmentIds.includes(departmentId)) {
					next[departmentId] = prev[departmentId];
				}
			});

			departmentRows.forEach((row) => {
				next[row.id] = row.productUnitPriceIds;
			});

			return areStringRecordsEqual(prev, next) ? prev : next;
		});
	}, []);

	const handleProductSelectionChange = useCallback(
		(departmentId: string, productIds: string[]) => {
			setSelectedProductIdsByDepartment((prev) => {
				const currentIds = prev[departmentId] ?? [];
				if (areStringArraysEqual(currentIds, productIds)) return prev;
				return {
					...prev,
					[departmentId]: productIds,
				};
			});
		},
		[],
	);

	return (
		<DataTable
			columns={ADJUSTMENT_DEPARTMENT_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={query}
			transformData={transformDepartmentRows}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã đơn vị' },
				{ key: 'name', label: 'Tên đơn vị' },
			]}
			onDelete={handleDeleteAdjustment}
			deleteCountOverride={selectedProductIds.length}
			deleteDisabledOverride={!selectedProductIds.length}
			onSelectedRowsChange={handleDepartmentSelectionChange}
			showCreateAction={false}
			hasPagination={false}
			onExpand={({ row }) => (
				<DepartmentAdjustmentMonthsTable
					departmentId={row?.id ?? ''}
					hasKhaiThac={row?.hasKhaiThac}
					hasVanTaiLo={row?.hasVanTaiLo}
					reloadKey={reloadKey}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedProductIdsChange={handleProductSelectionChange}
				/>
			)}
		/>
	);
}
