import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ClampForm } from '@/features/main/catalog/parameter/clamp/actions';
import {
	CATALOG_PARAMETER_CLAMP_COLUMNS,
	Clamp,
} from '@/features/main/catalog/parameter/clamp/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterClampPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Clamp>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.CLAMP.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.CLAMP.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Clamp>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PARAMETER.CLAMP.IMPORT, file);
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
			url={API.CATALOG.PARAMETER.CLAMP.LIST}
			columns={CATALOG_PARAMETER_CLAMP_COLUMNS}
			filters={[{ key: 'value', label: 'Tỷ lệ đá kẹp' }]}
			onCreate={(props) => <ClampForm {...props} />}
			onDuplicate={(props) => <ClampForm {...props} isDuplicate />}
			onUpdate={(props) => <ClampForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
