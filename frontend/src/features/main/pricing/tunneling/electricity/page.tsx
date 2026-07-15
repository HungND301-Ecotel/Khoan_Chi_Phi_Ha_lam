import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	Electricity,
	MAIN_PRICING_ELECTRICITY_COLUMNS,
} from '@/features/main/pricing/tunneling/electricity/columns';
import { ElectricityForm } from '@/features/main/pricing/tunneling/electricity/form';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';

export function MainPricingElectricityPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Electricity>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.ELECTRICITY.TUNNELING.DELETES, ids);

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
				API.PRICING.ELECTRICITY.TUNNELING.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
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
				API.PRICING.ELECTRICITY.TUNNELING.IMPORT,
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
			url={API.PRICING.ELECTRICITY.TUNNELING.LIST}
			filters={[
				{ key: 'equipmentCode', label: 'Nhóm vật tư, tài sản' },
				{ key: 'equipmentName', label: 'Tên nhóm vật tư, tài sản' },
			]}
			onCreate={hasPermission('pricing.tunnerelectricityunitprice.create') ? (props) => <ElectricityForm {...props} /> : undefined}
			onDuplicate={hasPermission('pricing.tunnerelectricityunitprice.create') ? (props) => <ElectricityForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission('pricing.tunnerelectricityunitprice.update') ? (props) => <ElectricityForm {...props} /> : undefined}
			onDelete={hasPermission('pricing.tunnerelectricityunitprice.delete') ? handleDelete : undefined}
			onExport={hasPermission('pricing.tunnerelectricityunitprice.export') ? handleExport : undefined}
			onImport={hasPermission('pricing.tunnerelectricityunitprice.import') ? handleImport : undefined}
		/>
	);
}
