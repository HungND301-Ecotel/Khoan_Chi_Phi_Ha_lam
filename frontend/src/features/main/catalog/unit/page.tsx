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

export default function MainCatalogUnitPage() {
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
			popup.success(`Đã xuất file ${filename}`);
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
			onCreate={(props) => <UnitForm {...props} />}
			onDuplicate={(props) => <UnitForm {...props} isDuplicate />}
			onUpdate={(props) => <UnitForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
