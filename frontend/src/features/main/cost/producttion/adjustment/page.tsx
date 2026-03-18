import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_COST_ADJUSTMENT_COLUMNS,
	ProductionAdjustment,
} from '@/features/main/cost/producttion/adjustment/columns';
import { AdjustmentExpand } from '@/features/main/cost/producttion/production/production-expand';
import { api } from '@/lib/api';

export function MainCostProductionRevenueAdjustmentPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const handleDeleteAdjustment = async ({
		data,
	}: ActionDialogProps<ProductionAdjustment>) => {
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
			columns={MAIN_COST_ADJUSTMENT_COLUMNS}
			url={API.COST.PRODUCT.LIST}
			query={{
				ignorePagination: true,
				scenarioType: 2,
			}}
			importCrumb='Doanh thu điều chỉnh'
			filters={[
				{ key: 'productCode', label: 'Mã sản phẩm' },
				{ key: 'productName', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onExpand={(props) => (
				<AdjustmentExpand
					{...(props as unknown as ActionDialogProps<ProductionAdjustment>)}
				/>
			)}
			onDelete={handleDeleteAdjustment}
		/>
	);
}
