import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { StrengthForm } from '@/features/main/catalog/parameter/strength/actions';
import {
	CATALOG_PARAMETER_STRENGTH_COLUMNS,
	Strength,
} from '@/features/main/catalog/parameter/strength/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterStrengthPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Strength>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.STRENGTH.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.STRENGTH.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Strength>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.STRENGTH.IMPORT,
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
			url={API.CATALOG.PARAMETER.STRENGTH.LIST}
			columns={CATALOG_PARAMETER_STRENGTH_COLUMNS}
			filters={[{ key: 'value', label: 'Độ kiên cố than/đá' }]}
			onCreate={(props) => <StrengthForm {...props} />}
			onUpdate={(props) => <StrengthForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
