import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { PositionForm } from '@/features/main/catalog/position/actions';
import {
	CATALOG_POSITION_COLUMNS,
	Position,
} from '@/features/main/catalog/position/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogPositionPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const { hasPermission } = usePermission();

	const handleDelete = async ({ data }: ActionDialogProps<Position>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.POSITION.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.POSITION.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Position>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.POSITION.IMPORT, file);
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
			url={API.CATALOG.POSITION.LIST}
			columns={CATALOG_POSITION_COLUMNS}
			filters={[{ key: 'name', label: 'Tên chức vụ' }]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.POSITION.CREATE) ? (props) => <PositionForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.POSITION.CREATE) ? (props) => <PositionForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.POSITION.UPDATE) ? (props) => <PositionForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.POSITION.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.POSITION.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.POSITION.IMPORT) ? handleImport : undefined}
		/>
	);
}
