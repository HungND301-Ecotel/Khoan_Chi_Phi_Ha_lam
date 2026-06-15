import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { AkFactorConfigForm } from './actions';
import { CATALOG_AK_FACTOR_CONFIG_COLUMNS, AkFactorConfig } from './columns';

export function MainCatalogAkFactorConfigPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<AkFactorConfig>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.AK_FACTOR_CONFIG.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.AK_FACTOR_CONFIG.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<AkFactorConfig>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.AK_FACTOR_CONFIG.IMPORT,
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
			url={API.CATALOG.AK_FACTOR_CONFIG.LIST}
			columns={CATALOG_AK_FACTOR_CONFIG_COLUMNS}
			filters={[
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn' },
				{ key: 'processGroupName', label: 'Tên nhóm công đoạn' },
				{ key: 'akDiffDisplay', label: 'Chênh lệch Ak' },
				{ key: 'adjustmentRateDisplay', label: 'Tỷ lệ điều chỉnh doanh thu' },
				{ key: 'description', label: 'Mô tả' },
			]}
			onCreate={(props) => <AkFactorConfigForm {...props} />}
			onDuplicate={(props) => <AkFactorConfigForm {...props} isDuplicate />}
			onUpdate={(props) => <AkFactorConfigForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}
