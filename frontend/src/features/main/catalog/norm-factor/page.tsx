import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';
import {
	CATALOG_NORM_FACTOR_COLUMNS,
	CATALOG_NORM_FACTOR_EXPAND_COLUMNS,
	NormFactor,
	NormFactorExpandItem,
} from './columns';
import { NormFactorForm } from './actions';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

export function MainCatalogNormFactorPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<NormFactor>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);
			await api.delete(API.CATALOG.NORM_FACTOR.DELETES, ids);

			popup.success(`Đã xoá ${ids.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.CATALOG.NORM_FACTOR.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<NormFactor>['data'],
	) => {
		try {
			const result = await api.import(API.CATALOG.NORM_FACTOR.IMPORT, file);
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
			url={API.CATALOG.NORM_FACTOR.LIST}
			columns={CATALOG_NORM_FACTOR_COLUMNS}
			filters={[
				{ key: 'productionProcessCode', label: 'Mã công đoạn sản xuất' },
				{ key: 'hardnessName', label: 'Độ kiên cố tham đá (f)' },
				{ key: 'stoneClampRatioName', label: 'Tỷ lệ đá kẹp (Ckẹp)' },
				{ key: 'value', label: 'Hệ số điều chỉnh định mức' },
			]}
			onCreate={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.CREATE) ? (props) => <NormFactorForm {...props} /> : undefined}
			onDuplicate={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.CREATE) ? (props) => <NormFactorForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.UPDATE) ? (props) => <NormFactorForm {...props} /> : undefined}
			onExpand={(props) => <NormFactorExpand {...props} />}
			onDelete={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.DELETE) ? handleDelete : undefined}
			onExport={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(PERMISSIONS.CATALOG.NORM_FACTOR.IMPORT) ? handleImport : undefined}
		/>
	);
}

function NormFactorExpand({ row }: ActionDialogProps<NormFactor>) {
	const [items, setItems] = useState<NormFactorExpandItem[]>([]);

	useEffect(() => {
		if (!row) return;

		api
			.get<NormFactor>(API.CATALOG.NORM_FACTOR.DETAIL(row.id))
			.then((res) => setItems(res.result.assignmentCodes ?? []))
			.catch(() => setItems([]));
	}, [row]);

	if (!row) return null;

	return (
		<div className='mx-32'>
			<DataTable
				columns={CATALOG_NORM_FACTOR_EXPAND_COLUMNS}
				items={items}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>
		</div>
	);
}
