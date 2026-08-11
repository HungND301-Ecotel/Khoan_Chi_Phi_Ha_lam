import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { PERMISSIONS } from '@/constants/permissions';
import { useMeta } from '@/data/meta/meta-hook';
import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api';
import {
	MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS,
	MotorizedLowValueSupplyElectricityUnitPrice,
} from './columns';
import { MotorizedLowValueSupplyElectricityForm } from './form';

export function MotorizedLowValueSupplyElectricityPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<MotorizedLowValueSupplyElectricityUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.DELETES,
				rows,
			);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedLowValueSupplyElectricityUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.IMPORT,
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

	const perm = PERMISSIONS.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY;

	return (
		<DataTable
			url={API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.LIST}
			columns={MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS}
			filters={[{ key: 'processGroupName', label: 'Nhóm công đoạn sản xuất' }]}
			onCreate={
				hasPermission(perm.CREATE)
					? (props) => <MotorizedLowValueSupplyElectricityForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission(perm.CREATE)
					? (props) => (
							<MotorizedLowValueSupplyElectricityForm {...props} isDuplicate />
						)
					: undefined
			}
			onUpdate={
				hasPermission(perm.UPDATE)
					? (props) => <MotorizedLowValueSupplyElectricityForm {...props} />
					: undefined
			}
			onDelete={hasPermission(perm.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(perm.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(perm.IMPORT) ? handleImport : undefined}
		/>
	);
}

export default MotorizedLowValueSupplyElectricityPage;
