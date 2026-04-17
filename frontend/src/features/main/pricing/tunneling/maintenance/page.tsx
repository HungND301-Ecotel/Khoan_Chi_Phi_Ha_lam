import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_PRICING_TUNNELING_COLUMNS,
	MAIN_PRICING_TUNNELING_EXPAND_COLUMNS,
	Tunneling,
} from '@/features/main/pricing/tunneling/maintenance/columns';
import { TunnelingForm } from '@/features/main/pricing/tunneling/maintenance/form';
import { api } from '@/lib/api';
import { useEffect, useMemo, useState } from 'react';

export type MaintainUnitPriceEquipment = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	partId: string;
	partCode: string;
	partName: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	partCost: number;
	replacementTimeStandard: number;
	averageMonthlyTunnelProduction: number;
	quantity: number;
	materialRatePerMetres: number;
	materialCostPerMetres: number;
};

export type TunnelingDetail = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	startMonth: string;
	endMonth: string;
	otherMaterialValue?: number;
	totalPrice: number;
	maintainUnitPriceEquipment: MaintainUnitPriceEquipment[];
};

export function MainPricingMaintenanceTunnelingPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const query = useMemo(
		() => ({ maintainType: 1, ignorePagination: true }),
		[],
	);

	const handleDelete = async ({ data }: ActionDialogProps<Tunneling>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.MAINTENANCE.DELETES, ids);

			popup.success(`Đã xoá thành công ${ids.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MAINTENANCE.TUNNEL_EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Tunneling>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MAINTENANCE.TUNNEL_IMPORT,
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
		<>
			<DataTable
				columns={MAIN_PRICING_TUNNELING_COLUMNS}
				url={API.PRICING.MAINTENANCE.LIST}
				query={query}
				getRowId={(row) => row.id}
				filters={[{ key: 'equipmentCode', label: 'Mã thiết bị' }]}
				onCreate={(props) => <TunnelingForm {...props} />}
				onUpdate={(props) => <TunnelingForm {...props} />}
				onExpand={(props) => <TunnelingExpand {...props} />}
				onDelete={handleDelete}
				onExport={handleExport}
				onImport={handleImport}
			/>
		</>
	);
}

export function TunnelingExpand({ row }: ActionDialogProps<Tunneling>) {
	const [detail, setDetail] = useState<TunnelingDetail>();

	useEffect(() => {
		if (!row) return;
		api
			.get<TunnelingDetail>(API.PRICING.MAINTENANCE.DETAIL(row.id))
			.then((res) => {
				setDetail(res.result);
			});
	}, [row]);

	const items = detail?.maintainUnitPriceEquipment || [];

	// Tính tổng chi phí của tất cả phụ tùng
	const totalPartsCost = items.reduce((sum, item) => {
		return sum + (item.materialCostPerMetres || 0);
	}, 0);

	// Thêm dòng VTK nếu có otherMaterialValue
	const itemsWithOther: MaintainUnitPriceEquipment[] = [...items];

	if (detail?.otherMaterialValue) {
		const otherCost = (totalPartsCost * detail.otherMaterialValue) / 100;

		itemsWithOther.push({
			id: 'vtk-other',
			equipmentId: detail.equipmentId,
			equipmentCode: detail.equipmentCode,
			partId: 'vtk',
			partCode: 'VTK',
			partName: 'Vật tư khác',
			unitOfMeasureId: '',
			unitOfMeasureName: '',
			partCost: 0,
			replacementTimeStandard: 0,
			averageMonthlyTunnelProduction: 0,
			quantity: 0,
			materialRatePerMetres: detail.otherMaterialValue,
			materialCostPerMetres: otherCost,
		});
	}

	return (
		<div className='mx-10 flex flex-col gap-4'>
			<DataTable
				columns={MAIN_PRICING_TUNNELING_EXPAND_COLUMNS}
				items={itemsWithOther}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				compact={true}
			/>
		</div>
	);
}
