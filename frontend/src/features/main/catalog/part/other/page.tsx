import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { CATALOG_OTHER_PART_COLUMNS, OtherPart } from './columns';
import { OtherPartForm } from './actions';

export function MainCatalogOtherPartPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<OtherPart>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			const res = await api.delete(API.CATALOG.OTHER_PART.DELETES, rows);

			if (!res.success) throw new Error(res.message);

			popup.success(`Đã xoá ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.OTHER_PART.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<OtherPart>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.OTHER_PART.IMPORT, file);
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
			url={API.CATALOG.OTHER_PART.LIST}
			columns={CATALOG_OTHER_PART_COLUMNS}
			query={{ ignorePagination: true }}
			getRowId={(row) => `${row.id}`}
			filters={[
				{ key: 'code', label: 'Mã phụ tùng' },
				{ key: 'name', label: 'Tên phụ tùng' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <OtherPartForm {...props} />}
			onUpdate={(props) => <OtherPartForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
