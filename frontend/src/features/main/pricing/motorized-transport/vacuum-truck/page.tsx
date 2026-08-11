import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { MOTORIZED_VACUUM_TRUCK_COLUMNS, MotorizedVacuumTruckUnitPrice } from './columns';
import { MotorizedVacuumTruckForm } from './form';
import { MotorizedVacuumTruckViewDialog } from './view-dialog';

export function MotorizedVacuumTruckPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	// Gom nhóm dữ liệu theo (Thời gian bắt đầu, Thời gian kết thúc, Nhóm vật tư, tài sản)
	const transformData = (rows: MotorizedVacuumTruckUnitPrice[]) => {
		if (!rows || rows.length === 0) return [];

		const groups: Record<
			string,
			MotorizedVacuumTruckUnitPrice & {
				allProcesses: MotorizedVacuumTruckUnitPrice[];
				allIds: string[];
			}
		> = {};

		rows.forEach((item) => {
			const start = item.startMonth?.substring(0, 7) || '';
			const end = item.endMonth?.substring(0, 7) || '';
			const acId = item.assignmentCodeId || item.assignmentCodeName || '';

			const groupKey = `${start}_${end}_${acId}`;

			if (!groups[groupKey]) {
				groups[groupKey] = {
					...item,
					allProcesses: [item],
					allIds: [item.id],
				};
			} else {
				groups[groupKey].allProcesses.push(item);
				groups[groupKey].allIds.push(item.id);
			}
		});

		return Object.values(groups);
	};

	const handleDelete = async ({ data }: ActionDialogProps<MotorizedVacuumTruckUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const allIds = selected.rows.flatMap((r) => r.original.allIds || [r.original.id]);
			await api.delete(API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.DELETES, allIds);

			popup.success(`Đã xoá thành công ${selected.rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedVacuumTruckUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.IMPORT, file);
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
			url={API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.LIST}
			columns={MOTORIZED_VACUUM_TRUCK_COLUMNS}
			transformData={transformData}
			filters={[
				{ key: 'assignmentCodeName', label: 'Nhóm vật tư, tài sản' },
				{ key: 'productionProcessName', label: 'Công đoạn sản xuất' },
			]}
			onExpand={({ row }) => <MotorizedVacuumTruckViewDialog row={row} />}
			onCreate={(props) => <MotorizedVacuumTruckForm {...props} />}
			onDuplicate={(props) => <MotorizedVacuumTruckForm {...props} isDuplicate />}
			onUpdate={(props) => <MotorizedVacuumTruckForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedVacuumTruckPage;
