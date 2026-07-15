import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import {
	CATALOG_PARAMETER_PRODUCTION_ORDER_COLUMNS,
	ProductionOrder,
} from './columns';
import { ProductionOrderForm } from './actions';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogParameterProductionOrderPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<ProductionOrder>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.PRODUCTION_ORDER.DELETES, ids);

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
				API.CATALOG.PARAMETER.PRODUCTION_ORDER.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<ProductionOrder>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.PRODUCTION_ORDER.IMPORT,
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
			url={API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST}
			columns={CATALOG_PARAMETER_PRODUCTION_ORDER_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã quyết định, lệnh sản xuất' },
				{ key: 'name', label: 'Tên quyết định, lệnh sản xuất' },
				{ key: 'startMonth', label: 'Thời gian' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.CREATE) ? (props) => <ProductionOrderForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.CREATE) ? (props) => <ProductionOrderForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.UPDATE) ? (props) => <ProductionOrderForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PRODUCTION_ORDER.IMPORT) ? handleImport : undefined}
		/>
	);
}
