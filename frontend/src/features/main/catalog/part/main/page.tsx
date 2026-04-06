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
			const rows = selected.rows.map((row) => row.original.equipmentPartId);

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
			const filename = await api.export(API.CATALOG.PART.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Part>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PART.IMPORT, file);
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
			query={{ ignorePagination: true }}
			getRowId={(row) => `${row.id}-${row.equipmentId}`}
			filters={[
				{ key: 'code', label: 'Mã phụ tùng' },
				{ key: 'equipmentCode', label: 'Mã thiết bị' },
				{ key: 'processGroupCodeText', label: 'Nhóm công đoạn' },
				{ key: 'name', label: 'Tên phụ tùng' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <PartForm {...props} />}
			onUpdate={(props) => <PartForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
