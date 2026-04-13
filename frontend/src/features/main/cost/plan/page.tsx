import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	DepartmentPlanGroup,
	MAIN_COST_PLAN_COLUMNS,
	PLAN_DEPARTMENT_COLUMNS,
} from '@/features/main/cost/plan/columns';
import { PlanExpand } from '@/features/main/cost/plan/expand';
import { PlanForm } from '@/features/main/cost/plan/form';
import { CostProduct } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { useCallback, useMemo, useState } from 'react';

type DepartmentPlanProductsTableProps = {
	departmentId: string;
	reloadKey: number;
	selectAllRows: boolean;
	onSelectedRowsChange: (departmentId: string, rows: CostProduct[]) => void;
};

function DepartmentPlanProductsTable({
	departmentId,
	reloadKey,
	selectAllRows,
	onSelectedRowsChange,
}: DepartmentPlanProductsTableProps) {
	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 1,
			departmentId,
		}),
		[departmentId, reloadKey],
	);

	return (
		<DataTable
			columns={MAIN_COST_PLAN_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={query}
			getRowId={(item) => item.id}
			filters={[
				{ key: 'productCode', label: 'Mã sản phẩm' },
				{ key: 'productName', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onExpand={(props) => <PlanExpand {...props} />}
			onUpdate={(props) => <PlanForm {...props} />}
			onDelete={async () => undefined}
			showCreateAction={false}
			showFilterAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
			onSelectedRowsChange={(rows) =>
				onSelectedRowsChange(departmentId, rows as CostProduct[])
			}
			selectAllPageRows={selectAllRows}
			hasPagination={false}
		/>
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
				(existedEnd === undefined || (itemEnd !== undefined && itemEnd > existedEnd))
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
	const [reloadKey, setReloadKey] = useState(0);
	const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>(
		[],
	);
	const [selectedProductIdsByDepartment, setSelectedProductIdsByDepartment] =
		useState<Record<string, string[]>>({});
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
	const transformDepartmentRows = useCallback(
		(rows: DepartmentPlanGroup[]) =>
			groupByDepartment(
				rows as unknown as CostProduct[],
			) as unknown as DepartmentPlanGroup[],
		[],
	);

	const handleDeleteDepartment = async ({
		data,
	}: ActionDialogProps<DepartmentPlanGroup>) => {
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

	const handleDepartmentSelectionChange = (rows: DepartmentPlanGroup[]) => {
		const departmentIds = rows.map((row) => row.id);
		setSelectedDepartmentIds(departmentIds);
		setSelectedProductIdsByDepartment((prev) => {
			const next: Record<string, string[]> = {};

			Object.keys(prev).forEach((departmentId) => {
				if (departmentIds.includes(departmentId)) {
					next[departmentId] = prev[departmentId];
				}
			});

			rows.forEach((row) => {
				next[row.id] = row.productUnitPriceIds;
			});

			return next;
		});
	};

	const handleProductSelectionChange = (
		departmentId: string,
		rows: CostProduct[],
	) => {
		setSelectedProductIdsByDepartment((prev) => ({
			...prev,
			[departmentId]: rows.map((row) => row.id),
		}));
	};

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
			onCreate={(props) => (
				<PlanForm
					{...(props as unknown as ActionDialogProps<CostProduct>)}
					onSuccess={() => setReloadKey((prev) => prev + 1)}
				/>
			)}
			onDelete={handleDeleteDepartment}
			deleteCountOverride={selectedProductIds.length}
			deleteDisabledOverride={!selectedProductIds.length}
			onSelectedRowsChange={(rows) =>
				handleDepartmentSelectionChange(rows as DepartmentPlanGroup[])
			}
			hasPagination={false}
			onExpand={({ row }) => (
				<DepartmentPlanProductsTable
					departmentId={row?.id ?? ''}
					reloadKey={reloadKey}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedRowsChange={handleProductSelectionChange}
				/>
			)}
		/>
	);
}
