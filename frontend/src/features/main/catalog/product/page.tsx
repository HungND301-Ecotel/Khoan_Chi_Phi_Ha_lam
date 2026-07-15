import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { ProductForm } from '@/features/main/catalog/product/actions';
import {
	CATALOG_PRODUCT_COLUMNS,
	Product,
} from '@/features/main/catalog/product/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogProductPage() {
	const { hasPermission } = usePermission();
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Product>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.PRODUCT.DELETES, ids);

			setOpen(false);
			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.PRODUCT.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Product>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.PRODUCT.IMPORT, file);
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
			url={API.CATALOG.PRODUCT.LIST}
			columns={CATALOG_PRODUCT_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã sản phẩm' },
				{ key: 'name', label: 'Tên sản phẩm' },
				{ key: 'processGroupCode', label: 'Mã nhóm CĐSX' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.PRODUCT.CREATE) ? (props) => <ProductForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.PRODUCT.CREATE) ? (props) => <ProductForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.PRODUCT.UPDATE) ? (props) => <ProductForm {...props} /> : undefined}
			onDelete={hasPermission(PERMISSIONS.CATALOG.PRODUCT.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.PRODUCT.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.PRODUCT.IMPORT) ? handleImport : undefined}
		/>
	);
}
