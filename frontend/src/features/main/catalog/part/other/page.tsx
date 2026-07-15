import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { CATALOG_OTHER_PART_COLUMNS, OtherPart } from './columns';
import { OtherPartForm } from './actions';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogOtherPartPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<OtherPart>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			const res = await api.delete(API.CATALOG.PART.DELETES, rows);

			if (!res.success) throw new Error(res.message);

			popup.success(`Đã xoá ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PART.EXPORT, {
				query: { partType: '2' },
			});
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<OtherPart>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PART.IMPORT, file, {
				partType: 2,
			});
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
			url={API.CATALOG.PART.LIST}
			columns={CATALOG_OTHER_PART_COLUMNS}
			query={{ ignorePagination: true, partType: 2 }}
			getRowId={(row) => `${row.id}`}
			filters={[
				{ key: 'code', label: 'Mã phụ tùng' },
				{ key: 'name', label: 'Tên phụ tùng' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.CREATE) ? (props) => <OtherPartForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.CREATE) ? (props) => <OtherPartForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.UPDATE) ? (props) => <OtherPartForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PART_OTHER.IMPORT) ? handleImport : undefined}
		/>
	);
}
