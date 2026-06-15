import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { SeamfaceForm } from '@/features/main/catalog/parameter/seamface/actions';
import {
	CATALOG_PARAMETER_SEAMFACE_COLUMNS,
	Seamface,
} from '@/features/main/catalog/parameter/seamface/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterSeamfacePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Seamface>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.SEAMFACE.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.SEAMFACE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Seamface>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.SEAMFACE.IMPORT,
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
			url={API.CATALOG.PARAMETER.SEAMFACE.LIST}
			columns={CATALOG_PARAMETER_SEAMFACE_COLUMNS}
			filters={[{ key: 'value', label: 'Mặt vỉa (M)' }]}
			onCreate={(props) => <SeamfaceForm {...props} />}
			onDuplicate={(props) => <SeamfaceForm {...props} isDuplicate />}
			onUpdate={(props) => <SeamfaceForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
