import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { useMeta } from '@/data/meta/meta-hook';
import {
	LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS,
	LowValuePerishableSupplyUnitPrice,
} from '@/features/main/pricing/low-value-perishable-supply/columns';
import { LowValuePerishableSupplyForm } from '@/features/main/pricing/low-value-perishable-supply/form';
import { api } from '@/lib/api';

type LowValuePerishableSupplyPageProps = {
	type: LowValuePerishableSupplyType;
};

export function LowValuePerishableSupplyPage({
	type,
}: LowValuePerishableSupplyPageProps) {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const apiConfig =
		type === LowValuePerishableSupplyType.TunnelExcavation
			? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TUNNELING
			: API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.LONGWALL;

	const handleDelete = async ({
		data,
	}: ActionDialogProps<LowValuePerishableSupplyUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.DELETES, ids);

			popup.success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(apiConfig.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LowValuePerishableSupplyUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(apiConfig.IMPORT, file);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success('Nhập dữ liệu thành công');
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<DataTable
			url={apiConfig.LIST}
			columns={LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS}
			filters={[
				{ key: 'departmentCode', label: 'Mã đơn vị' },
				{ key: 'departmentName', label: 'Tên đơn vị' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn' },
				{ key: 'processGroupName', label: 'Tên nhóm công đoạn' },
			]}
			onCreate={(props) => (
				<LowValuePerishableSupplyForm {...props} type={type} />
			)}
			onDuplicate={(props) => (
				<LowValuePerishableSupplyForm {...props} type={type} isDuplicate />
			)}
			onUpdate={(props) => (
				<LowValuePerishableSupplyForm {...props} type={type} />
			)}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
