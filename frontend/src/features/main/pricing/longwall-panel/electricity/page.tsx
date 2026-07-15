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
import { usePermission } from '@/hooks/use-permission';
import { useEffect, useState } from 'react';

export function MainPricingLongwallElectricityPage() {
	const { hasPermission } = usePermission();
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
			popup.success(`Đã tải xuống ${filename}`);
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
				{ key: 'equipmentCode', label: 'Nhóm vật tư, tài sản' },
				{ key: 'equipmentName', label: 'Tên nhóm vật tư, tài sản' },
			]}
			onCreate={hasPermission('pricing.longwallelectricityunitprice.create') ? (props) => <ElectricityForm {...props} onSuccess={fetchData} /> : undefined}
			onDuplicate={hasPermission('pricing.longwallelectricityunitprice.create') ? (props) => (
				<ElectricityForm {...props} onSuccess={fetchData} isDuplicate />
			) : undefined}
			onUpdate={hasPermission('pricing.longwallelectricityunitprice.update') ? (props) => <ElectricityForm {...props} onSuccess={fetchData} /> : undefined}
			onDelete={hasPermission('pricing.longwallelectricityunitprice.delete') ? handleDelete : undefined}
			onExport={hasPermission('pricing.longwallelectricityunitprice.export') ? handleExport : undefined}
			onImport={hasPermission('pricing.longwallelectricityunitprice.import') ? handleImport : undefined}
		/>
	);
}
