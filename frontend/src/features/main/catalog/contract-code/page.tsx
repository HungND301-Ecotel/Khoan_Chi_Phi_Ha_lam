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

			popup.success(
				`Đã xoá thành công ${rows.length} nhóm vật tư, tài sản.`,
			);
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
				{ key: 'name', label: 'Tên nhóm vật tư, tài sản' },
				{ key: 'code', label: 'Nhóm vật tư, tài sản' },
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
	otherMaterials?: ContractCodeMaterialDetail[];
};

function ContractCodeExpand({ row }: ActionDialogProps<ContractCode>) {
	const [materials, setMaterials] = useState<ContractCodeMaterialDetail[]>([]);
	const [otherMaterials, setOtherMaterials] = useState<ContractCodeMaterialDetail[]>(
		[],
	);

	useEffect(() => {
		if (!row) return;

		api
			.get<ContractCodeDetail>(API.CATALOG.CONTRACT_CODE.DETAIL(row.id))
			.then((res) => {
				setMaterials(res.result?.materials ?? []);
				setOtherMaterials(res.result?.otherMaterials ?? []);
			})
			.catch(() => {
				setMaterials([]);
				setOtherMaterials([]);
			});
	}, [row]);

	if (!row) return null;

	return (
		<div className='mx-32 space-y-6'>
			<div className='space-y-2'>
				<p className='text-sm font-semibold'>Vật tư, tài sản</p>
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
			<div className='space-y-2'>
				<p className='text-sm font-semibold'>Vật tư, tài sản khác</p>
				<DataTable
					columns={CATALOG_CONTRACT_CODE_EXPAND_COLUMNS}
					items={otherMaterials}
					hasActions={false}
					hasPagination={false}
					hasSort={false}
					hasIndex={true}
					compact={true}
				/>
			</div>
		</div>
	);
}

export default MainCatalogContractCodePage;
