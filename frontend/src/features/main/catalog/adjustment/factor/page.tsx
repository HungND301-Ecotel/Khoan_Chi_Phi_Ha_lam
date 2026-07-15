import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { FactorForm } from '@/features/main/catalog/adjustment/factor/actions';
import {
	CATALOG_ADJUSTMENT_FACTOR_COLUMNS,
	Factor,
} from '@/features/main/catalog/adjustment/factor/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogAdjustmentFactorPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Factor>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.ADJUSTMENT.FACTOR.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.ADJUSTMENT.FACTOR.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Factor>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.ADJUSTMENT.FACTOR.IMPORT,
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
			url={API.CATALOG.ADJUSTMENT.FACTOR.LIST}
			columns={CATALOG_ADJUSTMENT_FACTOR_COLUMNS}
			filters={[
				{ key: 'fixedKeyKey', label: 'Hệ số điều chỉnh' },
				{ key: 'name', label: 'Tên hệ số điều chỉnh' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.CREATE) ? (props) => <FactorForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.CREATE) ? (props) => <FactorForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.UPDATE) ? (props) => <FactorForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.IMPORT) ? handleImport : undefined}
		/>
	);
}
