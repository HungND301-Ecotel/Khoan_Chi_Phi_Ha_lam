import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { EmployeeForm } from '@/features/main/catalog/employee/actions';
import {
	CATALOG_EMPLOYEE_COLUMNS,
	Employee,
} from '@/features/main/catalog/employee/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogEmployeePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const { hasPermission } = usePermission();

	const handleDelete = async ({ data }: ActionDialogProps<Employee>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.EMPLOYEE.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.EMPLOYEE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Employee>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.EMPLOYEE.IMPORT, file);
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
			url={API.CATALOG.EMPLOYEE.LIST}
			columns={CATALOG_EMPLOYEE_COLUMNS}
			filters={[
				{ key: 'fullName', label: 'Họ và tên' },
				{ key: 'userName', label: 'Tên đăng nhập' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.CREATE) ? (props) => <EmployeeForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.CREATE) ? (props) => <EmployeeForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.UPDATE) ? (props) => <EmployeeForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.IMPORT) ? handleImport : undefined}
		/>
	);
}
