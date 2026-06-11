import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { TechnologyForm } from '@/features/main/catalog/parameter/technology/actions';
import {
	CATALOG_PARAMETER_TECHNOLOGY_COLUMNS,
	Technology,
} from '@/features/main/catalog/parameter/technology/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterTechnologyPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Technology>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.TECHNOLOGY.DELETES, ids);

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
				API.CATALOG.PARAMETER.TECHNOLOGY.EXPORT,
			);
			popup.success(`Đã Tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Technology>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.TECHNOLOGY.IMPORT,
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
			url={API.CATALOG.PARAMETER.TECHNOLOGY.LIST}
			columns={CATALOG_PARAMETER_TECHNOLOGY_COLUMNS}
			filters={[{ key: 'value', label: 'Công nghệ khai thác' }]}
			onCreate={(props) => <TechnologyForm {...props} />}
			onDuplicate={(props) => <TechnologyForm {...props} isDuplicate />}
			onUpdate={(props) => <TechnologyForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
