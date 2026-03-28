import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_PRICING_LONGWALL_PANEL_COLUMNS,
	MAIN_PRICING_LONGWALL_PANEL_EXPAND_COLUMNS,
	LongwallPanel,
} from '@/features/main/pricing/longwall-panel/maintenance/columns';
import { LongwallPanelForm } from '@/features/main/pricing/longwall-panel/maintenance/form';
import { api } from '@/lib/api';
import { useEffect, useMemo, useState } from 'react';

// Mock data for detail expand
export type MaintainUnitPriceLongwallPanel = {
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

export type LongwallPanelDetail = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	startMonth: string;
	endMonth: string;
	otherMaterialValue?: number;
	totalPrice: number;
	maintainUnitPriceEquipment: MaintainUnitPriceLongwallPanel[];
};

export function MainPricingMaintenanceLongwallPanelPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const query = useMemo(() => ({ maintainType: 2 }), []);

	const handleDelete = async ({ data }: ActionDialogProps<LongwallPanel>) => {
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
			const filename = await api.export(
				API.PRICING.MAINTENANCE.LONGWALL_EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallPanel>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MAINTENANCE.LONGWALL_IMPORT,
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
				columns={MAIN_PRICING_LONGWALL_PANEL_COLUMNS}
				url={API.PRICING.MAINTENANCE.LIST}
				query={query}
				getRowId={(row) => row.id}
				filters={[{ key: 'equipmentCode', label: 'Mã thiết bị' }]}
				onCreate={(props) => <LongwallPanelForm {...props} />}
				onUpdate={(props) => <LongwallPanelForm {...props} />}
				onExpand={(props) => <LongwallPanelExpand {...props} />}
				onDelete={handleDelete}
				onExport={handleExport}
				onImport={handleImport}
			/>
		</>
	);
}

export function LongwallPanelExpand({ row }: ActionDialogProps<LongwallPanel>) {
	const [detail, setDetail] = useState<LongwallPanelDetail>();

	useEffect(() => {
		if (!row) return;
		api
			.get<LongwallPanelDetail>(API.PRICING.MAINTENANCE.DETAIL(row.id))
			.then((res) => {
				setDetail(res.result);
			});
	}, [row]);

	const items = detail?.maintainUnitPriceEquipment || [];

	// Tính tổng chi phí của tất cả phụ tùng
	const totalPartsCost = items.reduce(
		(sum: number, item: MaintainUnitPriceLongwallPanel) => {
			return sum + (item.materialCostPerMetres || 0);
		},
		0,
	);

	// Thêm dòng VTK nếu có otherMaterialValue
	const itemsWithOther: MaintainUnitPriceLongwallPanel[] = [...items];

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
		<div className='mx-32 flex flex-col gap-4'>
			<DataTable
				columns={MAIN_PRICING_LONGWALL_PANEL_EXPAND_COLUMNS}
				items={itemsWithOther}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				compact={true}
			/>
		</div>
	);
}
