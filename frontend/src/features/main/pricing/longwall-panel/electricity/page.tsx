import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { useMeta } from '@/data/meta/meta-hook';
import { API } from '@/constants/api-enpoint';
import {
	LongwallElectricity,
	LONGWALL_ELECTRICITY_COLUMNS,
} from '@/features/main/pricing/longwall-panel/electricity/columns';
import { ElectricityForm } from '@/features/main/pricing/longwall-panel/electricity/form';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';

export function MainPricingLongwallElectricityPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const [items, setItems] = useState<LongwallElectricity[]>([]);

	const fetchData = async () => {
		try {
			const response = await api.pagging<LongwallElectricity>(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.LIST,
			);
			setItems(response.result.data || []);
		} catch (error) {
			popup.error(error);
		}
	};

	useEffect(() => {
		fetchData();
	}, []);

	const handleDelete = async ({
		data,
	}: ActionDialogProps<LongwallElectricity>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const deleteIds = selected.rows.map((row) => row.original.id);

			await api.delete(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.DELETES,
				deleteIds,
			);

			popup.success(`Đã xoá thành công ${deleteIds.length} ${breadcrumb}.`);
			await fetchData();
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallElectricity>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.ELECTRICITY.LONGWALL_PANEL.IMPORT,
				file,
			);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
			}
			await data?.refresh();
			await fetchData();
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<DataTable
			columns={LONGWALL_ELECTRICITY_COLUMNS}
			items={items}
			filters={[
				{ key: 'equipmentCode', label: 'Mã thiết bị' },
				{ key: 'equipmentName', label: 'Tên thiết bị' },
			]}
			onCreate={(props) => <ElectricityForm {...props} onSuccess={fetchData} />}
			onDuplicate={(props) => (
				<ElectricityForm {...props} onSuccess={fetchData} isDuplicate />
			)}
			onUpdate={(props) => <ElectricityForm {...props} onSuccess={fetchData} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
