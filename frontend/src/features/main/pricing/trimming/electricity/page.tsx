import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	Electricity,
	MAIN_PRICING_ELECTRICITY_COLUMNS,
} from '@/features/main/pricing/trimming/electricity/columns';
import { ElectricityForm } from '@/features/main/pricing/trimming/electricity/form';
import { api } from '@/lib/api';

export function MainPricingTrimmingElectricityPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Electricity>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.ELECTRICITY.TRIMMING.DELETES, ids);

			popup.success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.ELECTRICITY.TRIMMING.EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Electricity>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.ELECTRICITY.TRIMMING.IMPORT,
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
			columns={MAIN_PRICING_ELECTRICITY_COLUMNS}
			url={API.PRICING.ELECTRICITY.TRIMMING.LIST}
			filters={[
				{ key: 'equipmentCode', label: 'Mã thiết bị' },
				{ key: 'equipmentName', label: 'Tên thiết bị' },
			]}
			onCreate={(props) => <ElectricityForm {...props} />}
			onDuplicate={(props) => <ElectricityForm {...props} isDuplicate />}
			onUpdate={(props) => <ElectricityForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
