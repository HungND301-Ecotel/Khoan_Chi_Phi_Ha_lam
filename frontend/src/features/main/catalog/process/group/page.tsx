import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessGroupForm } from '@/features/main/catalog/process/group/action';
import {
	CATALOG_PROCESS_GROUP_COLUMNS,
	ProcessGroup,
} from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';

export default function MainCatalogProcessGroupPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<ProcessGroup>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			const res = await api.delete(API.CATALOG.PROCESS.GROUP.DELETES, rows);

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
			const filename = await api.export(API.CATALOG.PROCESS.GROUP.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<ProcessGroup>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PROCESS.GROUP.IMPORT, file);
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
			url={API.CATALOG.PROCESS.GROUP.LIST}
			columns={CATALOG_PROCESS_GROUP_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên nhóm công đoạn sản xuất' },
				{ key: 'code', label: 'Mã nhóm công đoạn sản xuất' },
			]}
			onCreate={(props) => <ProcessGroupForm {...props} />}
			onUpdate={(props) => <ProcessGroupForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
