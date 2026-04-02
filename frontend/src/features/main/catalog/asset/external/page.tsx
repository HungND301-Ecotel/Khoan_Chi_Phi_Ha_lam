import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { CATALOG_ASSET_EXTERNAL_COLUMNS } from '@/features/main/catalog/asset/external/columns';
import { AssetExternalForm } from '@/features/main/catalog/asset/external/form';
import { Asset } from '@/features/main/catalog/asset/types';
import { api } from '@/lib/api';

function MainCatalogAssetExternalPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Asset>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.ASSET.DELETES, rows);
			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				`${API.CATALOG.ASSET.EXPORT}?materialType=2`,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Asset>['data'],
	) => {
		try {
			const result = await api.import(
				API.CATALOG.ASSET.IMPORT,
				file,
				{ materialType: 2 },
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
			url={API.CATALOG.ASSET.LIST}
			columns={CATALOG_ASSET_EXTERNAL_COLUMNS}
			query={{ ignorePagination: true, materialType: 2 }}
			filters={[
				{ key: 'code', label: 'Mã vật tư, tài sản' },
				{ key: 'name', label: 'Tên vật tư, tài sản' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <AssetExternalForm {...props} />}
			onUpdate={(props) => <AssetExternalForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MainCatalogAssetExternalPage;

