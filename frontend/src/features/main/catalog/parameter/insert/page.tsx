import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { InsertForm } from '@/features/main/catalog/parameter/insert/actions';
import {
	CATALOG_PARAMETER_INSERT_COLUMNS,
	Insert,
} from '@/features/main/catalog/parameter/insert/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterInsertPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Insert>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.INSERT.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.INSERT.EXPORT);
			popup.success(`Đã Tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Insert>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.INSERT.IMPORT,
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
			url={API.CATALOG.PARAMETER.INSERT.LIST}
			columns={CATALOG_PARAMETER_INSERT_COLUMNS}
			filters={[{ key: 'value', label: 'Chèn' }]}
			onCreate={(props) => <InsertForm {...props} />}
			onDuplicate={(props) => <InsertForm {...props} isDuplicate />}
			onUpdate={(props) => <InsertForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
