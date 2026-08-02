import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	EXPAND_TRANSPORT_UNIT_PRICE_COLUMNS,
	MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS,
	TransportUnitPrice,
} from '@/features/main/pricing/transport/unit-price/columns';
import { TransportUnitPriceForm } from '@/features/main/pricing/transport/unit-price/form';
import { api } from '@/lib/api';

type GroupedTransportUnitPrice = TransportUnitPrice & {
	items?: TransportUnitPrice[];
	childIds?: string[];
};

function groupTransportUnitPrices(
	data: TransportUnitPrice[],
): GroupedTransportUnitPrice[] {
	if (!data || data.length === 0) return [];

	const map = new Map<string, TransportUnitPrice[]>();

	data.forEach((item) => {
		const key = [
			item.startMonth,
			item.endMonth,
			item.productionProcessId || '',
			item.transportRouteId || '',
			item.contractCodeId || item.equipmentId || item.materialId || '',
		].join('_');

		if (!map.has(key)) {
			map.set(key, []);
		}
		map.get(key)!.push(item);
	});

	const result: GroupedTransportUnitPrice[] = [];

	map.forEach((items) => {
		const sortedItems = [...items].sort((a, b) => {
			const qA = (a.equipmentQuality || '').toLowerCase();
			const qB = (b.equipmentQuality || '').toLowerCase();
			return qA.localeCompare(qB, 'vi', { numeric: true });
		});

		const first = sortedItems[0];
		result.push({
			...first,
			contractCodeName:
				first.contractCodeName || first.equipmentName || first.materialName,
			childIds: sortedItems.map((i) => i.id),
			items: sortedItems,
		});
	});

	return result;
}

export function MainPricingTransportUnitPricePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<GroupedTransportUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.flatMap(
				(row) => row.original.childIds || [row.original.id],
			);
			if (ids.length === 1) {
				await api.delete(API.PRICING.TRANSPORT.DELETE(ids[0]));
			} else if (ids.length > 1) {
				await api.delete(API.PRICING.TRANSPORT.DELETES, ids);
			}

			popup.success(`Đã xoá thành công ${selected.rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.TRANSPORT.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<GroupedTransportUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.TRANSPORT.IMPORT, file);
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
			url={API.PRICING.TRANSPORT.LIST}
			columns={MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS}
			filters={[
				{ key: 'displayCode', label: 'Mã định mức' },
				{ key: 'productionProcessName', label: 'Công đoạn sản xuất' },
				{ key: 'transportRouteName', label: 'Tuyến vận tải' },
			]}
			transformData={groupTransportUnitPrices as any}
			onCreate={(props) => <TransportUnitPriceForm {...props} />}
			onDuplicate={(props) => (
				<TransportUnitPriceForm {...props} isDuplicate />
			)}
			onUpdate={(props) => <TransportUnitPriceForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
			onExpand={({ row }) => <TransportDetailExpand row={row} />}
		/>
	);
}

/**
 * Bảng Chi Tiết Mở Rộng khi nhấn nút Xem (Mắt 👁️)
 */
function TransportDetailExpand({ row }: { row?: GroupedTransportUnitPrice }) {
	if (!row) return null;

	const expandItems = row.items && row.items.length > 0 ? row.items : [row];
	const sortedExpandItems = [...expandItems].sort((a, b) => {
		const qA = (a.equipmentQuality || '').toLowerCase();
		const qB = (b.equipmentQuality || '').toLowerCase();
		return qA.localeCompare(qB, 'vi', { numeric: true });
	});

	return (
		<div className='my-2 rounded-md border border-gray-200 bg-[#fbfbfb] p-3 shadow-inner'>
			<DataTable
				compact
				hasActions={false}
				hasIndex={false}
				hasPagination={false}
				hasSort={false}
				items={sortedExpandItems}
				columns={EXPAND_TRANSPORT_UNIT_PRICE_COLUMNS}
			/>
		</div>
	);
}

export default MainPricingTransportUnitPricePage;
