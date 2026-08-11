import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { PERMISSIONS } from '@/constants/permissions';
import { useMeta } from '@/data/meta/meta-hook';
import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api';
import { CargoType, CATALOG_CARGO_TYPE_COLUMNS } from './columns';
import { CargoTypeForm } from './form';

export function MainCatalogCargoTypePage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<CargoType>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.CARGO_TYPE.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.CARGO_TYPE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<CargoType>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.CARGO_TYPE.IMPORT, file);
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
			url={API.CATALOG.CARGO_TYPE.LIST}
			columns={CATALOG_CARGO_TYPE_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên chủng loại hàng' },
				{ key: 'code', label: 'Mã chủng loại hàng' },
			]}
			onCreate={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.CREATE)
					? (props) => <CargoTypeForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.CREATE)
					? (props) => <CargoTypeForm {...props} isDuplicate />
					: undefined
			}
			onUpdate={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.UPDATE)
					? (props) => <CargoTypeForm {...props} />
					: undefined
			}
			onDelete={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.DELETE)
					? handleDelete
					: undefined
			}
			onExport={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.EXPORT)
					? handleExport
					: undefined
			}
			onImport={
				hasPermission(PERMISSIONS.CATALOG.CARGO_TYPE.IMPORT)
					? handleImport
					: undefined
			}
		/>
	);
}

export default MainCatalogCargoTypePage;
