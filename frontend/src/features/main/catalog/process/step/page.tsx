import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ProcessStepForm } from '@/features/main/catalog/process/step/action';
import {
	CATALOG_PROCESS_STEP_COLUMNS,
	ProcessStep,
} from '@/features/main/catalog/process/step/columns';
import { api } from '@/lib/api';

export default function MainCatalogProcessStepPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<ProcessStep>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PROCESS.STEP.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PROCESS.STEP.EXPORT);
			popup.success(`Đã Tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<ProcessStep>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PROCESS.STEP.IMPORT, file);
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
			url={`${API.CATALOG.PROCESS.STEP.LIST}`}
			columns={CATALOG_PROCESS_STEP_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên công đoạn sản xuất' },
				{ key: 'code', label: 'Mã công đoạn sản xuất' },
				{ key: 'processGroupName', label: 'Nhóm công đoạn sản xuất' },
			]}
			onCreate={(props) => <ProcessStepForm {...props} />}
			onDuplicate={(props) => <ProcessStepForm {...props} isDuplicate />}
			onUpdate={(props) => <ProcessStepForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
