import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import {
	MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS,
	TransportPeriod,
	TransportUnitPrice,
} from '@/features/main/pricing/transport/unit-price/columns';
import { TransportProcessPeriodExpand } from '@/features/main/pricing/transport/unit-price/expand';
import { TransportUnitPriceForm } from '@/features/main/pricing/transport/unit-price/form';
import { api } from '@/lib/api';
import { useState } from 'react';

export function MainPricingTransportUnitPricePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const handleTogglePeriodSelect = (periodId: string) => {
		setSelectedPeriodIds((prev) =>
			prev.includes(periodId)
				? prev.filter((id) => id !== periodId)
				: [...prev, periodId],
		);
	};

	const isRowSelected = (row: TransportUnitPrice) => {
		const periods = row.periods ?? [];
		if (periods.length === 0) return selectedPeriodIds.includes(row.id);
		const selectedCount = periods.filter((p) =>
			selectedPeriodIds.includes(p.id),
		).length;
		if (selectedCount === periods.length) return true;
		if (selectedCount > 0) return 'indeterminate';
		return false;
	};

	const handleRowSelectChange = (row: TransportUnitPrice, checked: boolean) => {
		const periods = row.periods ?? [];
		const periodIds =
			periods.length > 0 ? periods.map((p) => p.id) : [row.id];
		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...periodIds]));
			} else {
				return prev.filter((id) => !periodIds.includes(id));
			}
		});
	};

	const handleSelectAllRowsChange = (
		checked: boolean,
		rows: TransportUnitPrice[],
	) => {
		const allPeriodIds = rows.flatMap((r) =>
			r.periods && r.periods.length > 0 ? r.periods.map((p) => p.id) : [r.id],
		);
		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allPeriodIds]));
			} else {
				return prev.filter((id) => !allPeriodIds.includes(id));
			}
		});
	};

	const handleDelete = async ({
		data,
	}: ActionDialogProps<TransportUnitPrice>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			const allPeriods = data.table
				.getRowModel()
				.rows.flatMap((r) => r.original.periods ?? []);
			const selectedPeriods = allPeriods.filter((p) =>
				selectedPeriodIds.includes(p.id),
			);
			const ids = Array.from(
				new Set(
					selectedPeriods.flatMap((p) =>
						p.items && p.items.length > 0 ? p.items.map((i) => i.id) : [p.id],
					),
				),
			);

			if (ids.length === 1) {
				await api.delete(API.PRICING.TRANSPORT.DELETE(ids[0]));
			} else if (ids.length > 1) {
				await api.delete(API.PRICING.TRANSPORT.DELETES, ids);
			}

			popup.success(
				`Đã xoá thành công ${selectedPeriodIds.length} khoảng thời gian ${breadcrumb}.`,
			);
			setSelectedPeriodIds([]);
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
		data?: ActionDialogProps<TransportUnitPrice>['data'],
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

	const transformData = (rows: TransportUnitPrice[]): TransportUnitPrice[] => {
		const groupMap = new Map<string, TransportUnitPrice>();

		rows.forEach((row) => {
			const groupKey =
				row.productionProcessId ||
				row.productionProcessCode ||
				row.productionProcessName ||
				row.id;
			const existing = groupMap.get(groupKey);

			const period: TransportPeriod = {
				...row,
				id: row.id,
				productionProcessId: row.productionProcessId,
				productionProcessCode: row.productionProcessCode,
				productionProcessName: row.productionProcessName,
				startMonth: row.startMonth,
				endMonth: row.endMonth,
				itemCount: row.itemCount,
				items: row.items && row.items.length > 0 ? row.items : [row],
			};

			if (!existing) {
				groupMap.set(groupKey, {
					...row,
					id: row.productionProcessId || row.id,
					periods: [period],
				});
			} else {
				existing.periods = existing.periods ?? [];
				if (!existing.periods.some((p) => p.id === period.id)) {
					existing.periods.push(period);
				}
			}
		});

		return Array.from(groupMap.values()).map((item) => {
			const sortedPeriods = (item.periods ?? []).sort((a, b) =>
				(b.startMonth || '').localeCompare(a.startMonth || ''),
			);
			const latestPeriod = sortedPeriods[0];
			return {
				...item,
				...(latestPeriod
					? {
							id: latestPeriod.id,
							startMonth: latestPeriod.startMonth,
							endMonth: latestPeriod.endMonth,
							items: latestPeriod.items,
						}
					: {}),
				periods: sortedPeriods,
			};
		});
	};

	return (
		<DataTable
			url={API.PRICING.TRANSPORT.LIST}
			columns={MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS}
			transformData={transformData}
			getRowId={(row) => row.productionProcessId || row.id}
			filters={[
				{ key: 'productionProcessName', label: 'Công đoạn sản xuất' },
			]}
			onCreate={(props) => <TransportUnitPriceForm {...props} />}
			onDuplicate={(props) => (
				<TransportUnitPriceForm {...props} isDuplicate />
			)}
			onUpdate={(props) => <TransportUnitPriceForm {...props} />}
			onDelete={handleDelete}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={handleExport}
			onImport={handleImport}
			onExpand={({ row }) => (
				<TransportProcessPeriodExpand
					row={row}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

export default MainPricingTransportUnitPricePage;
