import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { CuttingthicknessForm } from '@/features/main/catalog/parameter/cuttingthickness/actions';
import {
	CATALOG_PARAMETER_CUTTINGTHICKNESS_COLUMNS,
	Cuttingthickness,
} from '@/features/main/catalog/parameter/cuttingthickness/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterCuttingthicknessPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<Cuttingthickness>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.CUTTINGTHICKNESS.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.CATALOG.PARAMETER.CUTTINGTHICKNESS.EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Cuttingthickness>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.CUTTINGTHICKNESS.IMPORT,
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
			url={API.CATALOG.PARAMETER.CUTTINGTHICKNESS.LIST}
			columns={CATALOG_PARAMETER_CUTTINGTHICKNESS_COLUMNS}
			filters={[{ key: 'value', label: 'Chiều dày lớp khấu' }]}
			onCreate={(props) => <CuttingthicknessForm {...props} />}
			onUpdate={(props) => <CuttingthicknessForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
