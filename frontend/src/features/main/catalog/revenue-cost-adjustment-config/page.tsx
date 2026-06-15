import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { RevenueCostAdjustmentConfigForm } from './actions';
import {
	CATALOG_REVENUE_COST_ADJUSTMENT_CONFIG_COLUMNS,
	RevenueCostAdjustmentConfig,
} from './columns';

export function MainCatalogRevenueCostAdjustmentConfigPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<RevenueCostAdjustmentConfig>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<RevenueCostAdjustmentConfig>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.IMPORT,
				file,
			);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<DataTable
			url={API.CATALOG.REVENUE_COST_ADJUSTMENT_CONFIG.LIST}
			columns={CATALOG_REVENUE_COST_ADJUSTMENT_CONFIG_COLUMNS}
			filters={[
				{ key: 'profitConditionDisplay', label: 'Điều kiện lợi nhuận' },
				{ key: 'rateDisplay', label: 'Tỷ lệ điều chỉnh' },
				{ key: 'description', label: 'Mô tả' },
			]}
			onCreate={(props) => <RevenueCostAdjustmentConfigForm {...props} />}
			onDuplicate={(props) => (
				<RevenueCostAdjustmentConfigForm {...props} isDuplicate />
			)}
			onUpdate={(props) => <RevenueCostAdjustmentConfigForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
