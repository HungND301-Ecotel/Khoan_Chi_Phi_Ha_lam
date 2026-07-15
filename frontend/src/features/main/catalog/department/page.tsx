import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { DepartmentForm } from '@/features/main/catalog/department/actions';
import {
	CATALOG_DEPARTMENT_COLUMNS,
	Department,
} from '@/features/main/catalog/department/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogDepartmentPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const { hasPermission } = usePermission();

	const handleDelete = async ({ data }: ActionDialogProps<Department>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.DEPARTMENT.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.DEPARTMENT.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Department>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.DEPARTMENT.IMPORT, file);
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
			url={API.CATALOG.DEPARTMENT.LIST}
			columns={CATALOG_DEPARTMENT_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã đơn vị' },
				{ key: 'name', label: 'Tên đơn vị' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.CREATE) ? (props) => <DepartmentForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.CREATE) ? (props) => <DepartmentForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.UPDATE) ? (props) => <DepartmentForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.DEPARTMENT.IMPORT) ? handleImport : undefined}
		/>
	);
}
