import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	AdjustmentExpand,
	ProductionExpand,
} from '@/features/main/cost/producttion/production/production-expand';
import { api } from '@/lib/api';
import {
	MAIN_COST_ADJUSTMENT_COLUMNS,
	ProductionAdjustment,
} from './adjustment/columns';
import { MAIN_COST_PRODUCTION_COLUMNS, Production } from './production/columns';
import { ProductionForm } from './production/production-form';
import { AdjustmentForm } from './adjustment/form';
import { CostProduct } from '../plan/types';

export function MainCostProductionPage() {
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
		<Tabs defaultValue='production'>
			<TabsList>
				<TabsTrigger value='production'>Chi phí</TabsTrigger>
				<TabsTrigger value='adjustment'>Doanh thu điều chỉnh</TabsTrigger>
			</TabsList>

			<TabsContent value='production' className='mt-4'>
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
			</TabsContent>

			<TabsContent value='adjustment' className='mt-4'>
				<DataTable
					columns={MAIN_COST_ADJUSTMENT_COLUMNS}
					url={API.COST.PRODUCT.LIST}
					query={{
						ignorePagination: true,
						outputType: 2,
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
					onUpdate={(props) => (
						<AdjustmentForm
							{...(props as unknown as ActionDialogProps<CostProduct>)}
						/>
					)}
					onDelete={handleDeleteAdjustment}
				/>
			</TabsContent>
		</Tabs>
	);
}
