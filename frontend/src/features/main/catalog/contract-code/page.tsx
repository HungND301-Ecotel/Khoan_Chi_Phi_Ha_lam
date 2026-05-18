import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { ContractCodeForm } from '@/features/main/catalog/contract-code/actions';
import {
	CATALOG_CONTRACT_CODE_COLUMNS,
	CATALOG_CONTRACT_CODE_EXPAND_COLUMNS,
	ContractCode,
	ContractCodeMaterialDetail,
} from '@/features/main/catalog/contract-code/columns';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';

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
				{ key: 'currentPrice', label: 'Đơn giá điện năng' },
			]}
			onCreate={(props) => <ContractCodeForm {...props} />}
			onDuplicate={(props) => <ContractCodeForm {...props} isDuplicate />}
			onUpdate={(props) => <ContractCodeForm {...props} />}
			onExpand={(props) => <ContractCodeExpand {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

type ContractCodeDetail = {
	id: string;
	code: string;
	name: string;
	materials?: ContractCodeMaterialDetail[];
};

function ContractCodeExpand({ row }: ActionDialogProps<ContractCode>) {
	const [materials, setMaterials] = useState<ContractCodeMaterialDetail[]>([]);

	useEffect(() => {
		if (!row) return;

		api
			.get<ContractCodeDetail>(API.CATALOG.CONTRACT_CODE.DETAIL(row.id))
			.then((res) => setMaterials(res.result?.materials ?? []))
			.catch(() => setMaterials([]));
	}, [row]);

	if (!row) return null;

	return (
		<div className='mx-32'>
			<DataTable
				columns={CATALOG_CONTRACT_CODE_EXPAND_COLUMNS}
				items={materials}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={true}
				compact={true}
			/>
		</div>
	);
}

export default MainCatalogContractCodePage;
