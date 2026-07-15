import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { RevenueCostAdjustmentConfigForm } from './actions';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';
import {
	CATALOG_REVENUE_COST_ADJUSTMENT_CONFIG_COLUMNS,
	RevenueCostAdjustmentConfig,
} from './columns';

export function MainCatalogRevenueCostAdjustmentConfigPage() {
	const { hasPermission } = usePermission();
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
			onCreate={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.CREATE) ? (props) => <RevenueCostAdjustmentConfigForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.CREATE) ? (props) => (
				<RevenueCostAdjustmentConfigForm {...props} isDuplicate />
			) : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.UPDATE) ? (props) => <RevenueCostAdjustmentConfigForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.REVENUE_COST.IMPORT) ? handleImport : undefined}
		/>
	);
}
