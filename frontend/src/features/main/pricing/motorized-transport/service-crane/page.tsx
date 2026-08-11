import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { MOTORIZED_SERVICE_CRANE_COLUMNS, MotorizedServiceCraneUnitPrice } from './columns';
import { MotorizedServiceCraneForm } from './form';

export function MotorizedServiceCranePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<MotorizedServiceCraneUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.PRICING.MOTORIZED_TRANSPORT.SERVICE_CRANE.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MOTORIZED_TRANSPORT.SERVICE_CRANE.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedServiceCraneUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.MOTORIZED_TRANSPORT.SERVICE_CRANE.IMPORT, file);
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
			url={API.PRICING.MOTORIZED_TRANSPORT.SERVICE_CRANE.LIST}
			columns={MOTORIZED_SERVICE_CRANE_COLUMNS}
			filters={[
				{ key: 'equipmentName', label: 'Nhóm vật tư, tài sản' },
				{ key: 'productionProcess', label: 'Công đoạn sản xuất' },
			]}
			onCreate={(props) => <MotorizedServiceCraneForm {...props} />}
			onDuplicate={(props) => <MotorizedServiceCraneForm {...props} isDuplicate />}
			onUpdate={(props) => <MotorizedServiceCraneForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedServiceCranePage;
