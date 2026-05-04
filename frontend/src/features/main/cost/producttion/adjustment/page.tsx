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
import { useMeta } from '@/data/meta/meta-hook';
import { AdjustmentExpand } from '@/features/main/cost/producttion/adjustment/adjustment-expand';
import {
	ADJUSTMENT_DEPARTMENT_COLUMNS,
	DepartmentAdjustmentGroup,
	MAIN_COST_ADJUSTMENT_COLUMNS,
	ProductionAdjustment,
} from '@/features/main/cost/producttion/adjustment/columns';
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
			onExpand={(props) => <AdjustmentExpand {...props} />}
			showCreateAction={false}
			showFilterAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
			onDelete={async () => undefined}
			onSelectedRowsChange={(rows) =>
				onSelectedRowsChange(monthId, rows as ProductionAdjustment[])
			}
			selectAllPageRows={selectAllRows}
			hasPagination={false}
		/>
	);
}

type DepartmentAdjustmentMonthsTableProps = {
	departmentId: string;
	reloadKey: number;
	selectAllRows: boolean;
	onSelectedProductIdsChange: (
		departmentId: string,
		productIds: string[],
	) => void;
};

function groupByMonth(
	products: ProductionAdjustment[],
): DepartmentAdjustmentMonthGroup[] {
	const groups = new Map<string, DepartmentAdjustmentMonthGroup>();

	products.forEach((item) => {
		if (!item.startMonth) return;

		const existed = groups.get(item.startMonth);
		if (existed) {
			existed.productUnitPriceIds.push(item.id);
			existed.products.push(item);
			return;
		}

		groups.set(item.startMonth, {
			id: item.startMonth,
			time: item.startMonth,
			productUnitPriceIds: [item.id],
			products: [item],
		});
	});

	return Array.from(groups.values()).sort((a, b) =>
		a.time.localeCompare(b.time),
	);
}

function DepartmentAdjustmentMonthsTable({
	departmentId,
	reloadKey,
	selectAllRows,
	onSelectedProductIdsChange,
}: DepartmentAdjustmentMonthsTableProps) {
	const [products, setProducts] = useState<ProductionAdjustment[]>([]);
	const [selectedProductIdsByMonth, setSelectedProductIdsByMonth] = useState<
		Record<string, string[]>
	>({});
	const [openedMonthIds, setOpenedMonthIds] = useState<string[]>([]);

	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 2,
			departmentId,
		}),
		[departmentId],
	);
	const monthGroups = useMemo(() => groupByMonth(products), [products]);
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

		const loadProducts = async () => {
			const response = await api.pagging<ProductionAdjustment>(
				API.COST.PRODUCT.LIST,
				query,
			);
			if (!mounted) return;
			setProducts(response.result.data ?? []);
		};

		loadProducts();

		return () => {
			mounted = false;
		};
	}, [query, reloadKey]);

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
	const transformDepartmentRows = useCallback(
		(rows: DepartmentAdjustmentGroup[]) =>
			groupByDepartment(
				rows as unknown as ProductionAdjustment[],
			) as unknown as DepartmentAdjustmentGroup[],
		[],
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
					reloadKey={reloadKey}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedProductIdsChange={handleProductSelectionChange}
				/>
			)}
		/>
	);
}
