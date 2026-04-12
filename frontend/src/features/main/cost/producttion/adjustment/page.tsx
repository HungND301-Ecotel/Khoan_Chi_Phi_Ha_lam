import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_COST_ADJUSTMENT_COLUMNS,
	ProductionAdjustment,
} from '@/features/main/cost/producttion/adjustment/columns';
import { AdjustmentExpand } from '@/features/main/cost/producttion/adjustment/adjustment-expand';
import { api } from '@/lib/api';
import type { ColumnDef } from '@tanstack/react-table';
import { useCallback, useMemo, useState } from 'react';

type DepartmentAdjustmentGroup = {
	id: string;
	code: string;
	name: string;
	totalProducts: number;
	productUnitPriceIds: string[];
};

const ADJUSTMENT_DEPARTMENT_COLUMNS: ColumnDef<DepartmentAdjustmentGroup>[] = [
	{
		accessorKey: 'code',
		header: () => <span className='whitespace-normal'>Mã đơn vị</span>,
	},
	{
		accessorKey: 'name',
		header: () => <span className='whitespace-normal'>Tên đơn vị</span>,
	},
	{
		accessorKey: 'totalProducts',
		header: () => <span className='whitespace-normal'>Số sản phẩm</span>,
	},
];

type DepartmentAdjustmentProductsTableProps = {
	departmentId: string;
	selectAllRows: boolean;
	onSelectedRowsChange: (
		departmentId: string,
		rows: ProductionAdjustment[],
	) => void;
};

function DepartmentAdjustmentProductsTable({
	departmentId,
	selectAllRows,
	onSelectedRowsChange,
}: DepartmentAdjustmentProductsTableProps) {
	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 2,
			departmentId,
		}),
		[departmentId],
	);

	return (
		<DataTable
			columns={MAIN_COST_ADJUSTMENT_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={query}
			getRowId={(item) => item.id}
			importCrumb='Doanh thu điều chỉnh'
			filters={[
				{ key: 'productCode', label: 'Mã sản phẩm' },
				{ key: 'productName', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onExpand={(props) => (
				<AdjustmentExpand
					{...props}
				/>
			)}
			showCreateAction={false}
			showFilterAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
			onDelete={async () => undefined}
			onSelectedRowsChange={(rows) =>
				onSelectedRowsChange(departmentId, rows as ProductionAdjustment[])
			}
			selectAllPageRows={selectAllRows}
			hasPagination={false}
		/>
	);
}

function groupByDepartment(
	products: ProductionAdjustment[],
): DepartmentAdjustmentGroup[] {
	const groups = new Map<string, DepartmentAdjustmentGroup>();

	products.forEach((item) => {
		if (!item.departmentId) return;

		const existed = groups.get(item.departmentId);
		if (existed) {
			existed.totalProducts += 1;
			existed.productUnitPriceIds.push(item.id);
			return;
		}

		groups.set(item.departmentId, {
			id: item.departmentId,
			code: item.departmentCode ?? '',
			name: item.departmentName ?? '',
			totalProducts: 1,
			productUnitPriceIds: [item.id],
		});
	});

	return Array.from(groups.values()).sort((a, b) => a.code.localeCompare(b.code));
}

export function MainCostProductionRevenueAdjustmentPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [reloadKey, setReloadKey] = useState(0);
	const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>([]);
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

	const handleDepartmentSelectionChange = (rows: DepartmentAdjustmentGroup[]) => {
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
		rows: ProductionAdjustment[],
	) => {
		setSelectedProductIdsByDepartment((prev) => ({
			...prev,
			[departmentId]: rows.map((row) => row.id),
		}));
	};

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
			onSelectedRowsChange={(rows) =>
				handleDepartmentSelectionChange(rows as DepartmentAdjustmentGroup[])
			}
			showCreateAction={false}
			hasPagination={false}
			onExpand={({ row }) => (
				<DepartmentAdjustmentProductsTable
					departmentId={row?.id ?? ''}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedRowsChange={handleProductSelectionChange}
				/>
			)}
		/>
	);
}
