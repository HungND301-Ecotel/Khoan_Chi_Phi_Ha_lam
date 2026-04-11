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

export function MainCatalogDepartmentPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

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

	return (
		<DataTable
			url={API.CATALOG.DEPARTMENT.LIST}
			columns={CATALOG_DEPARTMENT_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã đơn vị' },
				{ key: 'name', label: 'Tên đơn vị' },
			]}
			onCreate={(props) => <DepartmentForm {...props} />}
			onUpdate={(props) => <DepartmentForm {...props} />}
			onDelete={handleDelete}
		/>
	);
}
