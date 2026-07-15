import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { UnitForm } from '@/features/main/catalog/unit/actions';
import {
	CATALOG_UNIT_COLUMNS,
	Unit,
} from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export default function MainCatalogUnitPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Unit>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.UNIT.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.UNIT.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Unit>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.UNIT.IMPORT, file);
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
			url={API.CATALOG.UNIT.LIST}
			columns={CATALOG_UNIT_COLUMNS}
			filters={[{ key: 'name', label: 'Đơn vị tính' }]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.UNIT.CREATE) ? (props) => <UnitForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.UNIT.CREATE) ? (props) => <UnitForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.UNIT.UPDATE) ? (props) => <UnitForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.UNIT.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.UNIT.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.UNIT.IMPORT) ? handleImport : undefined}
		/>
	);
}
