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
import {
	DepartmentPlanGroup,
	DepartmentPlanMonthGroup,
	MAIN_COST_PLAN_COLUMNS,
	PLAN_DEPARTMENT_COLUMNS,
	VTL_COST_PLAN_COLUMNS,
	VTCG_COST_PLAN_COLUMNS,
} from '@/features/main/cost/plan/columns';
import { PlanExpand } from '@/features/main/cost/plan/expand';
import { PlanForm } from '@/features/main/cost/plan/form';
import {
	CostProduct,
	type DepartmentPlannedDetail,
	type TransportCostComponent,
	mapDepartmentPlannedDetail,
} from '@/features/main/cost/plan/types';
import { usePermission } from '@/hooks/use-permission';
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

type DepartmentPlanProductsTableProps = {
	monthId: string;
	items: CostProduct[];
	selectAllRows: boolean;
	onSelectedRowsChange: (monthId: string, rows: CostProduct[]) => void;
	reloadKey: number;
};

function DepartmentPlanProductsTable({
	monthId,
	items,
	selectAllRows,
	onSelectedRowsChange,
	reloadKey,
}: DepartmentPlanProductsTableProps) {
	const handleSelectedRowsChange = useCallback(
		(rows: unknown[]) => {
			onSelectedRowsChange(monthId, rows as CostProduct[]);
		},
		[monthId, onSelectedRowsChange],
	);

	const vtcgItems = useMemo(
		() =>
			items.filter(
				(item) =>
					item.fixedKeyType === ProcessGroupType.VTCG ||
					(item.fixedKeyType as any) === 13 ||
					(item.fixedKeyType as any) === '13' ||
					item.processGroupCode === 'VTCG' ||
					(item.processGroupName || '').toLowerCase().includes('cơ giới') ||
					item.haulDistanceId ||
					item.haulDistanceValue,
			),
		[items],
	);

	const vtlItems = useMemo(
		() =>
			items.filter(
				(item) =>
					!vtcgItems.includes(item) &&
					(item.fixedKeyType === ProcessGroupType.VTL ||
						item.processGroupCode === 'VTL' ||
						(item.processGroupName || '').toLowerCase().includes('vận tải lò')),
			),
		[items, vtcgItems],
	);

	const khaiThacItems = useMemo(
		() =>
			items.filter(
				(item) => !vtcgItems.includes(item) && !vtlItems.includes(item),
			),
		[items, vtcgItems, vtlItems],
	);

	return (
		<div className='space-y-4'>
			{khaiThacItems.length > 0 && (
				<div>
					{(vtlItems.length > 0 || vtcgItems.length > 0) && (
						<div className='mb-2 px-1 text-xs font-bold text-gray-700 uppercase'>
							1. Khai thác
						</div>
					)}
					<DataTable
						columns={MAIN_COST_PLAN_COLUMNS}
						items={khaiThacItems}
						getRowId={(item) => item.id}
						filters={[
							{ key: 'productCode', label: 'Mã sản phẩm' },
							{ key: 'productName', label: 'Tên sản phẩm' },
							{ key: 'processGroupCode', label: 'Mã nhóm CĐSX' },
						]}
						onExpand={(props) => (
							<PlanExpand
								{...props}
								monthId={monthId}
								data={{
									...props.data,
									refresh: async () => {
										await props.data.refresh();
									},
								}}
								key={`${monthId}-${reloadKey}-${props.row?.id ?? ''}`}
							/>
						)}
						onDelete={async () => undefined}
						showCreateAction={false}
						showFilterAction={false}
						showDeleteAction={false}
						showUtilityActions={false}
						onSelectedRowsChange={handleSelectedRowsChange}
						selectAllPageRows={selectAllRows}
						hasPagination={false}
					/>
				</div>
			)}

			{vtlItems.length > 0 && (
				<div>
					{(khaiThacItems.length > 0 || vtcgItems.length > 0) && (
						<div className='mb-2 px-1 text-xs font-bold text-gray-700 uppercase'>
							{khaiThacItems.length > 0 ? '2. Vận tải lò' : 'Vận tải lò'}
						</div>
					)}
					<DataTable
						columns={VTL_COST_PLAN_COLUMNS}
						items={vtlItems}
						getRowId={(item) => item.id}
						filters={[
							{ key: 'productionProcessCode', label: 'Mã CĐSX' },
							{ key: 'productionProcessName', label: 'Tên CĐSX' },
							{ key: 'contractCodeCode', label: 'Mã nhóm VTTS' },
							{ key: 'contractCodeName', label: 'Tên nhóm VTTS' },
							{ key: 'routeDepartmentCode', label: 'Mã đơn vị' },
							{ key: 'routeDepartmentName', label: 'Tên đơn vị' },
						]}
						onExpand={(props) => (
							<PlanExpand
								{...props}
								monthId={monthId}
								data={{
									...props.data,
									refresh: async () => {
										await props.data.refresh();
									},
								}}
								key={`${monthId}-${reloadKey}-${props.row?.id ?? ''}`}
							/>
						)}
						onDelete={async () => undefined}
						showCreateAction={false}
						showFilterAction={false}
						showDeleteAction={false}
						showUtilityActions={false}
						onSelectedRowsChange={handleSelectedRowsChange}
						selectAllPageRows={selectAllRows}
						hasPagination={false}
					/>
				</div>
			)}

			{vtcgItems.length > 0 && (
				<div>
					{(khaiThacItems.length > 0 || vtlItems.length > 0) && (
						<div className='mb-2 px-1 text-xs font-bold text-gray-700 uppercase'>
							{khaiThacItems.length > 0 && vtlItems.length > 0
								? '3. Vận tải cơ giới'
								: khaiThacItems.length > 0
									? '2. Vận tải cơ giới'
									: 'Vận tải cơ giới'}
						</div>
					)}
					<DataTable
						columns={VTCG_COST_PLAN_COLUMNS}
						items={vtcgItems}
						getRowId={(item) => item.id}
						filters={[
							{ key: 'productionProcessCode', label: 'Mã CĐSX' },
							{ key: 'productionProcessName', label: 'Tên CĐSX' },
							{ key: 'contractCodeCode', label: 'Mã nhóm xe/VTTS' },
							{ key: 'contractCodeName', label: 'Tên nhóm xe/VTTS' },
						]}
						onExpand={(props) => (
							<PlanExpand
								{...props}
								monthId={monthId}
								data={{
									...props.data,
									refresh: async () => {
										await props.data.refresh();
									},
								}}
								key={`${monthId}-${reloadKey}-${props.row?.id ?? ''}`}
							/>
						)}
						onDelete={async () => undefined}
						showCreateAction={false}
						showFilterAction={false}
						showDeleteAction={false}
						showUtilityActions={false}
						onSelectedRowsChange={handleSelectedRowsChange}
						selectAllPageRows={selectAllRows}
						hasPagination={false}
					/>
				</div>
			)}
		</div>
	);
}

type DepartmentPlanMonthsTableProps = {
	departmentId: string;
	hasKhaiThac?: boolean;
	hasVanTaiLo?: boolean;
	hasVanTaiCoGioi?: boolean;
	reloadKey: number;
	selectAllRows: boolean;
	onSelectedProductIdsChange: (
		departmentId: string,
		productIds: string[],
	) => void;
};

function mapDepartmentDetailToMonthGroups(
	detail: DepartmentPlannedDetail,
): DepartmentPlanMonthGroup[] {
	return detail.months
		.map((month) => ({
			id: month.month,
			time: month.month,
			productUnitPriceIds: month.items
				.map((item) => item.productUnitPriceId)
				.filter((id): id is string => !!id),
			products: month.items.map((item) => ({
				id:
					item.productUnitPriceId ??
					item.outputId ??
					`${month.month}-${item.productId}`,
				productId: item.productId,
				productCode: item.productCode,
				productName: item.productName,
				processGroupId: item.processGroupId,
				processGroupCode: item.processGroupCode,
				fixedKeyType: item.fixedKeyType!,
				processGroupType: item.processGroupType,
				unitOfMeasureId: item.unitOfMeasureId,
				unitOfMeasureName: item.unitOfMeasureName,
				departmentId: detail.departmentId,
				departmentCode: detail.departmentCode,
				departmentName: detail.departmentName,
				totalProductionMeters: item.productionMeters,
				plannedTotalCost: item.plannedTotalCost,
				startMonth: month.month,
				endMonth: month.month,
			})),
		}))
		.sort((a, b) => a.time.localeCompare(b.time));
}

function DepartmentPlanMonthsTable({
	departmentId,
	hasKhaiThac,
	hasVanTaiLo,
	hasVanTaiCoGioi,
	reloadKey,
	selectAllRows,
	onSelectedProductIdsChange,
}: DepartmentPlanMonthsTableProps) {
	const [monthGroups, setMonthGroups] = useState<DepartmentPlanMonthGroup[]>([]);
	const [selectedProductIdsByMonth, setSelectedProductIdsByMonth] = useState<
		Record<string, string[]>
	>({});
	const [openedMonthIds, setOpenedMonthIds] = useState<string[]>([]);
	const handleMonthProductSelectionChange = useCallback(
		(monthId: string, rows: CostProduct[]) => {
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
							.get<DepartmentPlannedDetail>(
								API.COST.PRODUCT.DETAIL_PLANNED_BY_DEPARTMENT(departmentId),
							)
							.catch(() => null)
					: Promise.resolve(null);

			const shouldFetchTransport =
				hasVanTaiLo !== false ||
				hasVanTaiCoGioi !== false ||
				hasVanTaiLo ||
				hasVanTaiCoGioi;

			const vanTaiLoPromise =
				shouldFetchTransport
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
										productionMeters?: number;
										unitOfMeasureId?: string;
										unitOfMeasureName?: string;
										material?: TransportCostComponent;
										maintenance?: TransportCostComponent;
										power?: TransportCostComponent;
										isLowVolumeCase?: boolean;
										plannedTotalCost?: number;
									}[];
								}[];
							}>(
								API.COST.TRANSPORT_PLAN_LINE.DETAIL_PLANNED_BY_DEPARTMENT(
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
				? mapDepartmentPlannedDetail(khaiThacRes.result)
				: null;

			const vanTaiLoDetail = vanTaiLoRes?.result ? vanTaiLoRes.result : null;

			const mergedMap = new Map<string, DepartmentPlanMonthGroup>();

			if (khaiThacDetail) {
				const ktGroups = mapDepartmentDetailToMonthGroups(khaiThacDetail);
				ktGroups.forEach((g) => mergedMap.set(g.id, g));
			}

			if (vanTaiLoDetail?.months) {
				vanTaiLoDetail.months.forEach((vtlMonth) => {
					const monthKey = vtlMonth.month.substring(0, 10);
					const vtlProducts: CostProduct[] = (vtlMonth.items || []).map(
						(item: any) => {
							const isVtcg =
								item.haulDistanceId ||
								item.haulDistanceValue ||
								item.processGroupCode === 'VTCG' ||
								(item.processGroupName || '').toLowerCase().includes('cơ giới') ||
								(item.productionProcessName || '').toLowerCase().includes('cơ giới') ||
								(item.productionProcessCode || '').startsWith('VC_') ||
								(item.equipmentCode || '').includes('SCANIA') ||
								(item.equipmentCode || '').includes('KOMATSU') ||
								(item.equipmentCode || '').includes('FORKLIFT');

							const defaultCode = isVtcg ? 'VTCG' : 'VTL';
							const defaultName = isVtcg ? 'Vận tải cơ giới' : 'Vận tải lò';
							const defaultGroupType = isVtcg ? ProcessGroupType.VTCG : ProcessGroupType.VTL;

							const nameParts = [
								item.productionProcessName,
								item.equipmentName && `TB: ${item.equipmentName}`,
								item.equipmentQuality && `Loại ${item.equipmentQuality}`,
								item.haulDistanceValue && `Cung độ: ${item.haulDistanceValue}`,
								item.transportRouteName &&
									`Tuyến: ${item.transportRouteName}`,
							].filter(Boolean);

							return {
								id: item.id,
								productId: item.productionProcessId || item.id,
								productCode:
									item.productionProcessCode ||
									item.equipmentCode ||
									item.transportRouteCode ||
									defaultCode,
								productName: nameParts.join(' - ') || defaultName,
								processGroupId: item.processGroupId || '',
								processGroupCode: item.processGroupCode || defaultCode,
								processGroupName: item.processGroupName || defaultName,
								fixedKeyType: defaultGroupType,
								unitOfMeasureId: item.unitOfMeasureId || '',
								unitOfMeasureName: item.unitOfMeasureName || '-',
								departmentId: vanTaiLoDetail.departmentId,
								departmentCode: vanTaiLoDetail.departmentCode,
								departmentName: vanTaiLoDetail.departmentName,
								routeDepartmentId: item.routeDepartmentId,
								routeDepartmentCode: item.routeDepartmentCode || '-',
								routeDepartmentName: item.routeDepartmentName || '-',
								haulDistanceId: item.haulDistanceId,
								haulDistanceValue: item.haulDistanceValue,
								fuelAdjustmentFactor:
									item.fuelAdjustmentFactor ??
									item.k1?.customValue ??
									item.k2?.customValue ??
									1.0,
								totalProductionMeters: item.productionMeters ?? 0,
								plannedTotalCost: item.plannedTotalCost ?? 0,
								startMonth: monthKey,
								endMonth: monthKey,
								productionProcessCode: item.productionProcessCode || '-',
								productionProcessName: item.productionProcessName || '-',
								contractCodeCode: item.equipmentCode || item.transportRouteCode || '-',
								contractCodeName: item.equipmentName || item.transportRouteName || '-',
								equipmentQuality: item.equipmentQuality || '-',
								cargoTypeId: item.cargoTypeId,
								cargoTypeName: item.cargoTypeName,
								receivingLocationId: item.receivingLocationId,
								receivingLocationName: item.receivingLocationName,
								dumpingLocationId: item.dumpingLocationId,
								dumpingLocationName: item.dumpingLocationName,
								material: item.material,
								maintenance: item.maintenance,
								power: item.power,
								isLowVolumeCase: item.isLowVolumeCase,
							};
						},
					);

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

			const sortProducts = (items: CostProduct[]) => {
				return [...items].sort((a, b) => {
					const procCompare = (a.productionProcessCode || '').localeCompare(
						b.productionProcessCode || '',
					);
					if (procCompare !== 0) return procCompare;
					const eqCompare = (a.contractCodeCode || '').localeCompare(
						b.contractCodeCode || '',
					);
					if (eqCompare !== 0) return eqCompare;
					const qualA = (a.equipmentQuality || '').replace(/^Loại\s*/i, '');
					const qualB = (b.equipmentQuality || '').replace(/^Loại\s*/i, '');
					const qualCompare = qualA.localeCompare(qualB);
					if (qualCompare !== 0) return qualCompare;
					return (a.productName || '').localeCompare(b.productName || '');
				});
			};

			const mergedMonthGroups = Array.from(mergedMap.values())
				.map((group) => ({
					...group,
					products: sortProducts(group.products),
				}))
				.sort((a, b) => a.time.localeCompare(b.time));
			setMonthGroups(mergedMonthGroups);
		};

		loadDepartmentDetail();

		return () => {
			mounted = false;
		};
	}, [departmentId, reloadKey]);

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
							<DepartmentPlanProductsTable
								monthId={group.id}
								items={group.products}
								selectAllRows={selectAllRows}
								onSelectedRowsChange={handleMonthProductSelectionChange}
								reloadKey={reloadKey}
							/>
						</div>
					</AccordionContent>
				</AccordionItem>
			))}
		</Accordion>
	);
}

function groupByDepartment(products: CostProduct[]): DepartmentPlanGroup[] {
	const groups = new Map<string, DepartmentPlanGroup>();
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

export function MainCostPlanPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const { hasPermission } = usePermission();
	const [reloadKey, setReloadKey] = useState(0);
	const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>(
		[],
	);
	const [selectedProductIdsByDepartment, setSelectedProductIdsByDepartment] =
		useState<Record<string, string[]>>({});
	const [vtlDepartmentGroups, setVtlDepartmentGroups] = useState<
		DepartmentPlanGroup[]
	>([]);

	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 1,
		}),
		[reloadKey],
	);
	const selectedProductIds = useMemo(
		() => [...new Set(Object.values(selectedProductIdsByDepartment).flat())],
		[selectedProductIdsByDepartment],
	);

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
				const vtlGroups: DepartmentPlanGroup[] = [];

				list.forEach((detail) => {
					const months = detail.months || [];
					if (!months.length) return;

					const allItems = months.flatMap((m) => m.items || []);
					if (!allItems.length) return;

					const monthDates = months
						.map((m) => m.month.substring(0, 10))
						.sort();

					const deptInfo = deptsMap.get(detail.departmentId);

					const hasVtcg = allItems.some(
						(it: any) =>
							it.haulDistanceId ||
							it.haulDistanceValue ||
							it.processGroupCode === 'VTCG' ||
							(it.productionProcessName || '').toLowerCase().includes('cơ giới'),
					);
					const hasVtl = allItems.some(
						(it: any) =>
							!it.haulDistanceId &&
							!it.haulDistanceValue &&
							it.processGroupCode !== 'VTCG' &&
							!(it.productionProcessName || '').toLowerCase().includes('cơ giới'),
					);

					vtlGroups.push({
						id: detail.departmentId,
						code: detail.departmentCode || deptInfo?.code || '',
						name: detail.departmentName || deptInfo?.name || '',
						hasVanTaiLo: hasVtl || !hasVtcg,
						hasVanTaiCoGioi: hasVtcg,
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
		(rows: DepartmentPlanGroup[]) => {
			const khaiThacGroups = groupByDepartment(
				rows as unknown as CostProduct[],
			);
			const map = new Map<string, DepartmentPlanGroup>();

			khaiThacGroups.forEach((g) =>
				map.set(g.id, {
					...g,
					hasKhaiThac: true,
					hasVanTaiLo: false,
					hasVanTaiCoGioi: false,
				}),
			);

			vtlDepartmentGroups.forEach((vtlGroup) => {
				const existed = map.get(vtlGroup.id);
				if (existed) {
					existed.hasVanTaiLo = existed.hasVanTaiLo || vtlGroup.hasVanTaiLo;
					existed.hasVanTaiCoGioi =
						existed.hasVanTaiCoGioi || vtlGroup.hasVanTaiCoGioi;
					existed.productUnitPriceIds = [
						...new Set([
							...existed.productUnitPriceIds,
							...vtlGroup.productUnitPriceIds,
						]),
					];
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
						hasVanTaiLo: vtlGroup.hasVanTaiLo,
						hasVanTaiCoGioi: vtlGroup.hasVanTaiCoGioi,
					});
				}
			});

			return Array.from(map.values()).sort((a, b) =>
				a.code.localeCompare(b.code),
			);
		},
		[vtlDepartmentGroups],
	);

	const handleDeleteDepartment = async ({
		data,
	}: ActionDialogProps<DepartmentPlanGroup>) => {
		try {
			const ids = selectedProductIds;
			if (!ids.length) return;

			await Promise.allSettled([
				api.delete(API.COST.PRODUCT.DELETES, ids),
				api.delete(API.COST.TRANSPORT_PLAN_LINE.DELETES, ids),
			]);

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
		const departmentRows = rows as DepartmentPlanGroup[];
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
			columns={PLAN_DEPARTMENT_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={query}
			transformData={transformDepartmentRows}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã đơn vị' },
				{ key: 'name', label: 'Tên đơn vị' },
			]}
			onCreate={hasPermission('production.productunitprice.create') ? (props) => (
				<PlanForm
					{...(props as unknown as ActionDialogProps<DepartmentPlanGroup>)}
					onSuccess={() => setReloadKey((prev) => prev + 1)}
				/>
			) : undefined}
			onUpdate={hasPermission('production.productunitprice.update') ? (props) => (
				<PlanForm
					{...(props as unknown as ActionDialogProps<DepartmentPlanGroup>)}
					onSuccess={() => setReloadKey((prev) => prev + 1)}
				/>
			) : undefined}
			onDelete={handleDeleteDepartment}
			deleteCountOverride={selectedProductIds.length}
			deleteDisabledOverride={
				(!hasPermission('production.productunitprice.delete') &&
					!hasPermission('production.transportplanline.delete')) ||
				!selectedProductIds.length
			}
			onSelectedRowsChange={handleDepartmentSelectionChange}
			hasPagination={false}
			onExpand={({ row }) => (
				<DepartmentPlanMonthsTable
					departmentId={row?.id ?? ''}
					hasKhaiThac={row?.hasKhaiThac}
					hasVanTaiLo={row?.hasVanTaiLo}
					hasVanTaiCoGioi={row?.hasVanTaiCoGioi}
					reloadKey={reloadKey}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedProductIdsChange={handleProductSelectionChange}
				/>
			)}
		/>
	);
}
