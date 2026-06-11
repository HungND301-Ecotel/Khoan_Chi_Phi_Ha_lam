import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { EquipmentForm } from '@/features/main/catalog/equipment/actions';
import {
	CATALOG_EQUIPMENT_COLUMNS,
	CATALOG_EQUIPMENT_EXPAND_COLUMNS,
	Equipment,
	EquipmentPartDetail,
} from '@/features/main/catalog/equipment/columns';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';

function MainCatalogEquipmentPage() {
	const popup = usePopup();

	const handleDelete = async ({ data }: ActionDialogProps<Equipment>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);
			const res = await api.delete(API.CATALOG.EQUIPMENT.DELETES, rows);
			if (!res.success) throw new Error(res.message);

			popup.success(`Đã xoá thành công ${rows.length} thiết bị.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.EQUIPMENT.EXPORT);
			popup.success(`Đã Tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Equipment>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.EQUIPMENT.IMPORT, file);
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
			url={`${API.CATALOG.EQUIPMENT.LIST}`}
			columns={CATALOG_EQUIPMENT_COLUMNS}
			filters={[
				{ key: 'name', label: 'Tên thiết bị' },
				{ key: 'code', label: 'Mã thiết bị' },
				{ key: 'unitOfMeasureName', label: 'Đơn vị tính' },
			]}
			onCreate={(props) => <EquipmentForm {...props} />}
			onDuplicate={(props) => <EquipmentForm {...props} isDuplicate />}
			onUpdate={(props) => <EquipmentForm {...props} />}
			onExpand={(props) => <EquipmentExpand {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

function EquipmentExpand({ row }: ActionDialogProps<Equipment>) {
	const [parts, setParts] = useState<EquipmentPartDetail[]>([]);

	useEffect(() => {
		if (!row) return;

		api
			.get<EquipmentPartDetail[]>(API.CATALOG.EQUIPMENT.EQUIPMENT_PARTS(row.id))
			.then((res) => setParts(res.result ?? []))
			.catch(() => setParts([]));
	}, [row]);

	if (!row) return null;

	return (
		<div className='mx-32'>
			<DataTable
				columns={CATALOG_EQUIPMENT_EXPAND_COLUMNS}
				items={parts}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={true}
				compact={true}
			/>
		</div>
	);
}

export default MainCatalogEquipmentPage;
