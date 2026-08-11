import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { TransportDistanceForm } from '@/features/main/catalog/parameter/transport-distance/actions';
import {
	CATALOG_TRANSPORT_DISTANCE_COLUMNS,
	TransportDistanceParameter,
} from '@/features/main/catalog/parameter/transport-distance/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogParameterTransportDistancePage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<TransportDistanceParameter>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<TransportDistanceParameter>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.IMPORT,
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
			url={API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.LIST}
			columns={CATALOG_TRANSPORT_DISTANCE_COLUMNS}
			filters={[{ key: 'value', label: 'Cung độ vận tải (L)' }]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.CREATE) ? (props) => <TransportDistanceForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.CREATE) ? (props) => <TransportDistanceForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.UPDATE) ? (props) => <TransportDistanceForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_TRANSPORT_DISTANCE.IMPORT) ? handleImport : undefined}
		/>
	);
}

export default MainCatalogParameterTransportDistancePage;
