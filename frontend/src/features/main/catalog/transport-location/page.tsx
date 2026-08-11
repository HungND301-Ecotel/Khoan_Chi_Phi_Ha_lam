import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { PERMISSIONS } from '@/constants/permissions';
import { useMeta } from '@/data/meta/meta-hook';
import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api';
import { CATALOG_TRANSPORT_LOCATION_COLUMNS, TransportLocation } from './columns';
import { TransportLocationForm } from './form';

export function MainCatalogTransportLocationPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<TransportLocation>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.TRANSPORT_LOCATION.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.TRANSPORT_LOCATION.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<TransportLocation>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.TRANSPORT_LOCATION.IMPORT, file);
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
			url={API.CATALOG.TRANSPORT_LOCATION.LIST}
			columns={CATALOG_TRANSPORT_LOCATION_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên vị trí nhận, đổ' },
				{ key: 'code', label: 'Mã vị trí' },
			]}
			onCreate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.CREATE)
					? (props) => <TransportLocationForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.CREATE)
					? (props) => <TransportLocationForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.UPDATE)
					? (props) => <TransportLocationForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.DELETE)
					? handleDelete
					: undefined
			}
			onExport={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.EXPORT)
					? handleExport
					: undefined
			}
			onImport={
				hasPermission(PERMISSIONS.CATALOG.TRANSPORT_LOCATION.IMPORT)
					? handleImport
					: undefined
			}
		/>
	);
}

export default MainCatalogTransportLocationPage;
