import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ProductionExpand } from '@/features/main/cost/producttion/production/production-expand';
import { api } from '@/lib/api';
import {
	MAIN_COST_PRODUCTION_COLUMNS,
	Production,
} from '@/features/main/cost/producttion/production/columns';
import { ProductionForm } from '@/features/main/cost/producttion/production/production-form';

export function MainCostProductionCostPage() {
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const handleDeleteProduction = async ({
		data,
	}: ActionDialogProps<Production>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRODUCTION.PRODUCTION_OUTPUT.DELETES, ids);

			success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<DataTable
			columns={MAIN_COST_PRODUCTION_COLUMNS}
			url={API.PRODUCTION.PRODUCTION_OUTPUT.LIST}
			onExpand={(props) => <ProductionExpand {...props} />}
			onDelete={handleDeleteProduction}
			importCrumb='Biên bản nghiệm thu'
			onCreate={(props) => (
				<ProductionForm
					{...(props as unknown as ActionDialogProps<Production>)}
				/>
			)}
			onUpdate={(props) => (
				<ProductionForm
					{...(props as unknown as ActionDialogProps<Production>)}
				/>
			)}
		/>
	);
}
