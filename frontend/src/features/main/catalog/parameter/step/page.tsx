import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { StepForm } from '@/features/main/catalog/parameter/step/actions';
import {
	CATALOG_PARAMETER_STEP_COLUMNS,
	Step,
} from '@/features/main/catalog/parameter/step/columns';
import { api } from '@/lib/api';

export function MainCatalogParameterStepPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Step>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.STEP.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};
	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.STEP.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Step>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PARAMETER.STEP.IMPORT, file);
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
			url={API.CATALOG.PARAMETER.STEP.LIST}
			columns={CATALOG_PARAMETER_STEP_COLUMNS}
			filters={[{ key: 'value', label: 'Bước chống' }]}
			onCreate={(props) => <StepForm {...props} />}
			onDuplicate={(props) => <StepForm {...props} isDuplicate />}
			onUpdate={(props) => <StepForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
