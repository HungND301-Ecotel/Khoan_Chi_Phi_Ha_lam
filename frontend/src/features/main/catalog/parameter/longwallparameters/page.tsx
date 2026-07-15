import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { LongwallparametersForm } from '@/features/main/catalog/parameter/longwallparameters/actions';
import {
	CATALOG_PARAMETER_LONGWALLPARAMETERS_COLUMNS,
	Longwallparameters,
} from '@/features/main/catalog/parameter/longwallparameters/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogParameterLongwallparametersPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<Longwallparameters>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.LONGWALLPARAMETERS.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.CATALOG.PARAMETER.LONGWALLPARAMETERS.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Longwallparameters>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.LONGWALLPARAMETERS.IMPORT,
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
			url={API.CATALOG.PARAMETER.LONGWALLPARAMETERS.LIST}
			columns={CATALOG_PARAMETER_LONGWALLPARAMETERS_COLUMNS}
			filters={[
				{ key: 'llc', label: 'LLC' },
				{ key: 'lkc', label: 'LKC' },
				{ key: 'mk', label: 'MK' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.CREATE) ? (props) => <LongwallparametersForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.CREATE) ? (props) => <LongwallparametersForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.UPDATE) ? (props) => <LongwallparametersForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_LONGWALL.IMPORT) ? handleImport : undefined}
		/>
	);
}
