import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { InterpreterForm } from '@/features/main/catalog/adjustment/interpreter/actions';
import {
	CATALOG_ADJUSTMENT_INTERPRETER_COLUMNS,
	Interpreter,
} from '@/features/main/catalog/adjustment/interpreter/columns';
import { api } from '@/lib/api';

export function MainCatalogAdjustmentInterpreterPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Interpreter>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.ADJUSTMENT.INTERPRETER.DELETES, ids);

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
				API.CATALOG.ADJUSTMENT.INTERPRETER.EXPORT,
			);
			popup.success(`Đã Tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Interpreter>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.ADJUSTMENT.INTERPRETER.IMPORT,
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
			url={API.CATALOG.ADJUSTMENT.INTERPRETER.LIST}
			columns={CATALOG_ADJUSTMENT_INTERPRETER_COLUMNS}
			filters={[
				{ key: 'description', label: 'Diễn giải HSĐC' },
				{ key: 'maintenanceAdjustmentValue', label: 'Trị số điều chỉnh SCTX' },
				{
					key: 'electricityAdjustmentValue',
					label: 'Trị số điều chỉnh điện năng',
				},
				{
					key: 'adjustmentFactorCode',
					label: 'Mã hệ số điều chỉnh',
				},
			]}
			onCreate={(props) => <InterpreterForm {...props} />}
			onDuplicate={(props) => <InterpreterForm {...props} isDuplicate />}
			onUpdate={(props) => <InterpreterForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
