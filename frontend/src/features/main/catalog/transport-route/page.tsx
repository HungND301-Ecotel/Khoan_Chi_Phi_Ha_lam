import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { PERMISSIONS } from '@/constants/permissions';
import { useMeta } from '@/data/meta/meta-hook';
import { TransportRouteForm } from '@/features/main/catalog/transport-route/action';
import {
	CATALOG_TRANSPORT_ROUTE_COLUMNS,
	TransportRoute,
} from '@/features/main/catalog/transport-route/columns';
import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api';

export function MainCatalogTransportRoutePage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<TransportRoute>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.TRANSPORT_ROUTE.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.TRANSPORT_ROUTE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<TransportRoute>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.TRANSPORT_ROUTE.IMPORT, file);
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
			url={API.CATALOG.TRANSPORT_ROUTE.LIST}
			columns={CATALOG_TRANSPORT_ROUTE_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên tuyến vận tải' },
				{ key: 'code', label: 'Mã tuyến vận tải' },
			]}
			onCreate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.CREATE)
					? (props) => <TransportRouteForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.CREATE)
					? (props) => <TransportRouteForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.UPDATE)
					? (props) => <TransportRouteForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.DELETE)
					? handleDelete
					: undefined
			}
			onExport={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.EXPORT)
					? handleExport
					: undefined
			}
			onImport={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_ROUTE.IMPORT)
					? handleImport
					: undefined
			}
		/>
	);
}

export default MainCatalogTransportRoutePage;
