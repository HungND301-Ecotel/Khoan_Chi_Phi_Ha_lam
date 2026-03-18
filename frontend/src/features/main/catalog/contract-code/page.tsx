import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { ContractCodeForm } from '@/features/main/catalog/contract-code/actions';
import {
	CATALOG_CONTRACT_CODE_COLUMNS,
	ContractCode,
} from '@/features/main/catalog/contract-code/columns';
import { api } from '@/lib/api';

function MainCatalogContractCodePage() {
	const popup = usePopup();
	const handleDelete = async ({ data }: ActionDialogProps<ContractCode>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.CONTRACT_CODE.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} mã giao khoán.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.CONTRACT_CODE.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<ContractCode>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.CONTRACT_CODE.IMPORT, file);
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
			url={`${API.CATALOG.CONTRACT_CODE.LIST}`}
			columns={CATALOG_CONTRACT_CODE_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên giao khoán' },
				{ key: 'code', label: 'Mã giao khoán' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <ContractCodeForm {...props} />}
			onUpdate={(props) => <ContractCodeForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MainCatalogContractCodePage;
