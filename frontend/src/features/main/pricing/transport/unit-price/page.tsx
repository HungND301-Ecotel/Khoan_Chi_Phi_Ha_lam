import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS,
	TransportUnitPrice,
} from '@/features/main/pricing/transport/unit-price/columns';
import { TransportDetailExpand } from '@/features/main/pricing/transport/unit-price/expand';
import { TransportUnitPriceForm } from '@/features/main/pricing/transport/unit-price/form';
import { api } from '@/lib/api';

// Danh sách đã được BE gộp theo (Công đoạn sản xuất, Thời gian) — 1 hàng = 1 lần tạo mới trên
// form, `items` chứa toàn bộ dòng Tuyến/Đơn vị/Nhóm vật tư/Chất lượng thiết bị con.
type GroupedTransportUnitPrice = TransportUnitPrice;

export function MainPricingTransportUnitPricePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<GroupedTransportUnitPrice>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const ids = selected.rows.flatMap((row) => {
				const items = row.original.items;
				return items && items.length > 0
					? items.map((item) => item.id)
					: [row.original.id];
			});
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

export default MainPricingTransportUnitPricePage;
