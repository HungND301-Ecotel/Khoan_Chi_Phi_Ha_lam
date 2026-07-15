import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { PassportForm } from '@/features/main/catalog/parameter/passport/actions';
import {
	CATALOG_PARAMETER_PASSPORT_COLUMNS,
	Passport,
} from '@/features/main/catalog/parameter/passport/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogParameterPassportPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Passport>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PARAMETER.PASSPORT.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PARAMETER.PASSPORT.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Passport>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.PARAMETER.PASSPORT.IMPORT,
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
			url={API.CATALOG.PARAMETER.PASSPORT.LIST}
			columns={CATALOG_PARAMETER_PASSPORT_COLUMNS}
			filters={[
				{ key: 'name', label: 'Hộ chiếu' },
				{ key: 'sd', label: 'Sđ' },
				{ key: 'sc', label: 'Sc' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.CREATE) ? (props) => <PassportForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.CREATE) ? (props) => <PassportForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.UPDATE) ? (props) => <PassportForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PARAMETER_PASSPORT.IMPORT) ? handleImport : undefined}
		/>
	);
}
