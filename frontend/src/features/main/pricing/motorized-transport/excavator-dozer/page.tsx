import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { MOTORIZED_EXCAVATOR_DOZER_COLUMNS, MotorizedExcavatorDozerUnitPrice } from './columns';
import { MotorizedExcavatorDozerForm } from './form';
import { MotorizedExcavatorDozerViewDialog } from './view-dialog';
import UnifiedMotorizedTransportForm from '../unit-price/unified-form';

export function MotorizedExcavatorDozerPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	// Gom nhóm dữ liệu theo (Thời gian bắt đầu, Thời gian kết thúc, Nhóm vật tư, tài sản)
	const transformData = (rows: MotorizedExcavatorDozerUnitPrice[]) => {
		if (!rows || rows.length === 0) return [];

		const groups: Record<
			string,
			MotorizedExcavatorDozerUnitPrice & {
				allProcesses: MotorizedExcavatorDozerUnitPrice[];
				allIds: string[];
			}
		> = {};

		rows.forEach((item) => {
			const start = item.startMonth?.substring(0, 7) || '';
			const end = item.endMonth?.substring(0, 7) || '';
			const ccId = item.assignmentCodeId || item.assignmentCodeName || item.equipmentId || item.equipmentName || '';

			const groupKey = `${start}_${end}_${ccId}`;

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

	const handleDelete = async ({ data }: ActionDialogProps<MotorizedExcavatorDozerUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const allIds = selected.rows.flatMap((r) => r.original.allIds || [r.original.id]);
			await api.delete(API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.DELETES, allIds);

			popup.success(`Đã xoá thành công ${selected.rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedExcavatorDozerUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.IMPORT, file);
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
			url={API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.LIST}
			columns={MOTORIZED_EXCAVATOR_DOZER_COLUMNS}
			transformData={transformData}
			filters={[
				{ key: 'assignmentCodeName', label: 'Nhóm vật tư, tài sản' },
				{ key: 'productionProcessName', label: 'Công đoạn sản xuất' },
			]}
			onExpand={({ row }) => <MotorizedExcavatorDozerViewDialog row={row} />}
			onCreate={(props) => <UnifiedMotorizedTransportForm {...props} defaultVehicleType='excavator-dozer' />}
			onDuplicate={(props) => <MotorizedExcavatorDozerForm {...props} isDuplicate />}
			onUpdate={(props) => <MotorizedExcavatorDozerForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedExcavatorDozerPage;
