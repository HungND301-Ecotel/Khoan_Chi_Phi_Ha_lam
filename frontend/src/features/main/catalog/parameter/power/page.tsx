import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { PowerForm } from './actions';
import { CATALOG_PARAMETER_POWER_COLUMNS, Power } from './columns';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogParameterPowerPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Power>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.POWER.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.POWER.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Power>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PARAMETER.POWER.IMPORT, file);
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
			url={API.CATALOG.PARAMETER.POWER.LIST}
			columns={CATALOG_PARAMETER_POWER_COLUMNS}
			filters={[{ key: 'value', label: 'Công suất' }]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.CREATE) ? (props) => <PowerForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.CREATE) ? (props) => <PowerForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.UPDATE) ? (props) => <PowerForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_POWER.IMPORT) ? handleImport : undefined}
		/>
	);
}
