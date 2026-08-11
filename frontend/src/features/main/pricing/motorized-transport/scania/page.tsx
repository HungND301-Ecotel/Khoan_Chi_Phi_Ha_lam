import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { MOTORIZED_SCANIA_COLUMNS, MotorizedScaniaUnitPrice } from './columns';
import { MotorizedScaniaForm } from './form';
import UnifiedMotorizedTransportForm from '../unit-price/unified-form';

export function MotorizedScaniaPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<MotorizedScaniaUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedScaniaUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.IMPORT, file);
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
			url={API.PRICING.MOTORIZED_TRANSPORT.SCANIA.LIST}
			columns={MOTORIZED_SCANIA_COLUMNS}
			filters={[
				{ key: 'equipmentName', label: 'Nhóm vật tư, tài sản' },
				{ key: 'productionProcess', label: 'Công đoạn sản xuất' },
				{ key: 'cargoTypeName', label: 'Chủng loại hàng' },
			]}
			onCreate={(props) => <UnifiedMotorizedTransportForm {...props} defaultVehicleType='scania' />}
			onDuplicate={(props) => <MotorizedScaniaForm {...props} isDuplicate />}
			onUpdate={(props) => <MotorizedScaniaForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedScaniaPage;
