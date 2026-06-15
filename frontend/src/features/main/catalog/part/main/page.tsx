import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	CATALOG_PART_COLUMNS,
	Part,
} from '@/features/main/catalog/part/main/columns';
import { api } from '@/lib/api';
import { PartForm } from './actions';

export function MainCatalogPartPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Part>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = Array.from(
				new Set(selected.rows.map((row) => row.original.id)),
			);

			const res = await api.delete(API.CATALOG.PART.DELETES, rows);

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
			const filename = await api.export(API.CATALOG.PART.EXPORT, {
				query: { partType: '1' },
			});
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Part>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PART.IMPORT, file, {
				partType: 1,
			});
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
			url={API.CATALOG.PART.LIST}
			columns={CATALOG_PART_COLUMNS}
			query={{ ignorePagination: true, partType: 1 }}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã phụ tùng' },
				{ key: 'name', label: 'Tên phụ tùng' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <PartForm {...props} />}
			onDuplicate={(props) => <PartForm {...props} isDuplicate />}
			onUpdate={(props) => <PartForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
