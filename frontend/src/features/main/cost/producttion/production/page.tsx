import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	DepartmentProductionGroup,
	MAIN_COST_PRODUCTION_COLUMNS,
	Production,
	PRODUCTION_DEPARTMENT_COLUMNS,
} from '@/features/main/cost/producttion/production/columns';
import { ProductionExpand } from '@/features/main/cost/producttion/production/production-expand';
import { ProductionForm } from '@/features/main/cost/producttion/production/production-form';
import { api } from '@/lib/api';
import { useEffect, useMemo, useState } from 'react';

type DepartmentProductionOutputsTableProps = {
	departmentId: string;
	productions: Production[];
	selectAllRows: boolean;
	onSelectedRowsChange: (departmentId: string, rows: Production[]) => void;
	onRefresh: () => void;
};

function DepartmentProductionOutputsTable({
	departmentId,
	productions,
	selectAllRows,
	onSelectedRowsChange,
	onRefresh,
}: DepartmentProductionOutputsTableProps) {
	const departmentProductions = useMemo(
		() => productions.filter((item) => item.departmentId === departmentId),
		[productions, departmentId],
	);

	return (
		<DataTable
			columns={MAIN_COST_PRODUCTION_COLUMNS}
			items={departmentProductions}
			getRowId={(item) => item.id}
			onExpand={(props) => <ProductionExpand {...props} />}
			onUpdate={(props) => (
				<ProductionForm
					{...(props as unknown as ActionDialogProps<Production>)}
					onSuccess={onRefresh}
				/>
			)}
			onDelete={async () => undefined}
			showCreateAction={false}
			showFilterAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
			onSelectedRowsChange={(rows) =>
				onSelectedRowsChange(departmentId, rows as Production[])
			}
			selectAllPageRows={selectAllRows}
			hasPagination={false}
		/>
	);
}

function groupByDepartment(
	productions: Production[],
): DepartmentProductionGroup[] {
	const groups = new Map<string, DepartmentProductionGroup>();
	const toTimestamp = (value?: string) => {
		if (!value) return undefined;
		const parsed = Date.parse(value);
		return Number.isNaN(parsed) ? undefined : parsed;
	};

	productions.forEach((item) => {
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

			existed.productionOutputIds.push(item.id);
			return;
		}

		groups.set(item.departmentId, {
			id: item.departmentId,
			code: item.departmentCode ?? '',
			name: item.departmentName ?? '',
			startMonth: item.startMonth,
			endMonth: item.endMonth,
			productionOutputIds: [item.id],
		});
	});

	return Array.from(groups.values()).sort((a, b) =>
		a.code.localeCompare(b.code),
	);
}

export function MainCostProductionCostPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [allProductions, setAllProductions] = useState<Production[]>([]);
	const [departmentGroups, setDepartmentGroups] = useState<
		DepartmentProductionGroup[]
	>([]);
	const [reloadKey, setReloadKey] = useState(0);
	const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>(
		[],
	);
	const [selectedOutputIdsByDepartment, setSelectedOutputIdsByDepartment] =
		useState<Record<string, string[]>>({});

	const selectedOutputIds = useMemo(
		() => [...new Set(Object.values(selectedOutputIdsByDepartment).flat())],
		[selectedOutputIdsByDepartment],
	);

	const refreshDepartmentGroups = async () => {
		try {
			const productionsRes = await api.pagging<Production>(
				API.PRODUCTION.PRODUCTION_OUTPUT.LIST,
				{
					ignorePagination: true,
				},
			);

			const productions = productionsRes.result.data ?? [];
			setAllProductions(productions);
			setDepartmentGroups(groupByDepartment(productions));
		} catch (err) {
			error(err);
		}
	};

	useEffect(() => {
		refreshDepartmentGroups();
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [reloadKey]);

	const handleDeleteProduction = async ({
		data,
	}: ActionDialogProps<DepartmentProductionGroup>) => {
		try {
			const ids = selectedOutputIds;
			if (!ids.length) return;

			await api.delete(API.PRODUCTION.PRODUCTION_OUTPUT.DELETES, ids);

			success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
			setSelectedDepartmentIds([]);
			setSelectedOutputIdsByDepartment({});
			setReloadKey((prev) => prev + 1);
		} catch (err) {
			error(err);
		}
	};

	const handleDepartmentSelectionChange = (
		rows: DepartmentProductionGroup[],
	) => {
		const departmentIds = rows.map((row) => row.id);
		setSelectedDepartmentIds(departmentIds);
		setSelectedOutputIdsByDepartment((prev) => {
			const next: Record<string, string[]> = {};

			Object.keys(prev).forEach((departmentId) => {
				if (departmentIds.includes(departmentId)) {
					next[departmentId] = prev[departmentId];
				}
			});

			rows.forEach((row) => {
				next[row.id] = row.productionOutputIds;
			});

			return next;
		});
	};

	const handleOutputSelectionChange = (
		departmentId: string,
		rows: Production[],
	) => {
		setSelectedOutputIdsByDepartment((prev) => ({
			...prev,
			[departmentId]: rows.map((row) => row.id),
		}));
	};

	return (
		<DataTable
			columns={PRODUCTION_DEPARTMENT_COLUMNS}
			items={departmentGroups}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã đơn vị' },
				{ key: 'name', label: 'Tên đơn vị' },
			]}
			onCreate={(props) => (
				<ProductionForm
					{...(props as unknown as ActionDialogProps<Production>)}
					onSuccess={() => setReloadKey((prev) => prev + 1)}
				/>
			)}
			onDelete={handleDeleteProduction}
			deleteCountOverride={selectedOutputIds.length}
			deleteDisabledOverride={!selectedOutputIds.length}
			onSelectedRowsChange={(rows) =>
				handleDepartmentSelectionChange(rows as DepartmentProductionGroup[])
			}
			hasPagination={false}
			onExpand={({ row }) => (
				<DepartmentProductionOutputsTable
					departmentId={row?.id ?? ''}
					productions={allProductions}
					selectAllRows={selectedDepartmentIds.includes(row?.id ?? '')}
					onSelectedRowsChange={handleOutputSelectionChange}
					onRefresh={() => setReloadKey((prev) => prev + 1)}
				/>
			)}
		/>
	);
}
