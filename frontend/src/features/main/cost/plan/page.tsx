import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { MAIN_COST_PLAN_COLUMNS } from '@/features/main/cost/plan/columns';
import { PlanExpand } from '@/features/main/cost/plan/expand';
import { PlanForm } from '@/features/main/cost/plan/form';
import { CostProduct } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { useMemo } from 'react';

export function MainCostPlanPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const query = useMemo(
		() => ({
			ignorePagination: true,
			scenarioType: 1,
		}),
		[],
	);

	const handleDelete = async ({ data }: ActionDialogProps<CostProduct>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.COST.PRODUCT.DELETES, ids);

			success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<DataTable
			columns={MAIN_COST_PLAN_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={query}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'productCode', label: 'Mã sản phẩm' },
				{ key: 'productName', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onExpand={(props) => <PlanExpand {...props} />}
			onCreate={(props) => <PlanForm {...props} />}
			onUpdate={(props) => <PlanForm {...props} />}
			onDelete={handleDelete}
		/>
	);
}
