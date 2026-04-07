import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { SavingsRateConfigForm } from './actions';
import {
	CATALOG_SAVINGS_RATE_CONFIG_COLUMNS,
	SavingsRateConfig,
} from './columns';

export function MainCatalogSavingsRateConfigPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<SavingsRateConfig>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.SAVINGS_RATE_CONFIG.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.SAVINGS_RATE_CONFIG.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<SavingsRateConfig>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.SAVINGS_RATE_CONFIG.IMPORT,
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
			url={API.CATALOG.SAVINGS_RATE_CONFIG.LIST}
			columns={CATALOG_SAVINGS_RATE_CONFIG_COLUMNS}
			filters={[
				{ key: 'revenueDisplay', label: 'Tổng doanh thu 3 yếu tố' },
				{ key: 'savingsRateDisplay', label: 'Giá trị tiết kiệm' },
				{ key: 'description', label: 'Mô tả' },
			]}
			onCreate={(props) => <SavingsRateConfigForm {...props} />}
			onUpdate={(props) => <SavingsRateConfigForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
