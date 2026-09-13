import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { PERMISSIONS } from '@/constants/permissions';
import { useMeta } from '@/data/meta/meta-hook';
import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api';
import { useState } from 'react';
import {
	MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS,
	MotorizedLowValueSupplyElectricityUnitPrice,
} from './columns';
import { MotorizedLowValuePeriodExpand } from './expand';
import { MotorizedLowValueSupplyElectricityForm } from './form';

export function MotorizedLowValueSupplyElectricityPage() {
	const { hasPermission } = usePermission();
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

	const isRowSelected = (
		row: MotorizedLowValueSupplyElectricityUnitPrice,
	): boolean | 'indeterminate' => {
		const periods = row.periods || [];
		if (periods.length === 0) return false;
		const selectedCount = periods.filter((p) =>
			selectedPeriodIds.includes(p.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === periods.length) return true;
		return 'indeterminate';
	};

	const handleRowSelectChange = (
		row: MotorizedLowValueSupplyElectricityUnitPrice,
		checked: boolean,
	) => {
		const periodIds = (row.periods || []).map((p) => p.id);
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
		rows: MotorizedLowValueSupplyElectricityUnitPrice[],
	) => {
		const allPeriodIds = rows.flatMap((r) =>
			(r.periods || []).map((p) => p.id),
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
	}: ActionDialogProps<MotorizedLowValueSupplyElectricityUnitPrice>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.DELETES,
				selectedPeriodIds,
			);

			popup.success(
				`Đã xoá thành công ${selectedPeriodIds.length} mục ${breadcrumb}`,
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
			const filename = await api.export(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MotorizedLowValueSupplyElectricityUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.IMPORT,
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

	const transformData = (
		rows: MotorizedLowValueSupplyElectricityUnitPrice[],
	): MotorizedLowValueSupplyElectricityUnitPrice[] => {
		const groupMap = new Map<
			string,
			MotorizedLowValueSupplyElectricityUnitPrice
		>();

		rows.forEach((row) => {
			const groupKey = `${row.departmentId || row.departmentName || ''}__${row.processGroupId || row.processGroupName || ''}`;

			let group = groupMap.get(groupKey);
			if (!group) {
				group = {
					id: groupKey,
					departmentId: row.departmentId,
					departmentCode: row.departmentCode,
					departmentName: row.departmentName,
					processGroupId: row.processGroupId,
					processGroupCode: row.processGroupCode,
					processGroupName: row.processGroupName,
					startMonth: row.startMonth,
					endMonth: row.endMonth,
					lowValuePerishableSupplyUnitPrice:
						row.lowValuePerishableSupplyUnitPrice,
					lowValueSupplyUnitPrice: row.lowValueSupplyUnitPrice,
					electricityUnitPrice: row.electricityUnitPrice,
					periods: [],
				};
				groupMap.set(groupKey, group);
			}

			group.periods!.push({
				id: row.id,
				startMonth: row.startMonth || '',
				endMonth: row.endMonth || '',
				lowValueSupplyUnitPrice:
					row.lowValuePerishableSupplyUnitPrice ??
					row.lowValueSupplyUnitPrice ??
					0,
				lowValuePerishableSupplyUnitPrice:
					row.lowValuePerishableSupplyUnitPrice ??
					row.lowValueSupplyUnitPrice ??
					0,
				electricityUnitPrice: row.electricityUnitPrice ?? 0,
			});
		});

		return Array.from(groupMap.values()).map((group) => {
			group.periods!.sort((a, b) =>
				(b.startMonth || '').localeCompare(a.startMonth || ''),
			);
			if (group.periods!.length > 0) {
				group.startMonth = group.periods![0].startMonth;
				group.endMonth = group.periods![0].endMonth;
				group.lowValueSupplyUnitPrice =
					group.periods![0].lowValueSupplyUnitPrice;
				group.lowValuePerishableSupplyUnitPrice =
					group.periods![0].lowValuePerishableSupplyUnitPrice;
				group.electricityUnitPrice = group.periods![0].electricityUnitPrice;
			}
			return group;
		});
	};

	const perm =
		PERMISSIONS.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY;

	return (
		<DataTable
			url={API.PRICING.MOTORIZED_TRANSPORT.LOW_VALUE_SUPPLY_ELECTRICITY.LIST}
			columns={MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS}
			transformData={transformData}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'departmentName', label: 'Đơn vị' },
				{ key: 'processGroupName', label: 'Nhóm công đoạn sản xuất' },
			]}
			onExpand={({ row }) =>
				row ? (
					<MotorizedLowValuePeriodExpand
						row={row}
						selectedPeriodIds={selectedPeriodIds}
						onTogglePeriodSelect={handleTogglePeriodSelect}
					/>
				) : (
					<div />
				)
			}
			onCreate={
				hasPermission(perm.CREATE)
					? (props) => <MotorizedLowValueSupplyElectricityForm {...props} />
					: undefined
			}
			onDuplicate={
				hasPermission(perm.CREATE)
					? (props) => (
							<MotorizedLowValueSupplyElectricityForm
								{...props}
								isDuplicate
							/>
						)
					: undefined
			}
			onUpdate={
				hasPermission(perm.UPDATE)
					? (props) => <MotorizedLowValueSupplyElectricityForm {...props} />
					: undefined
			}
			onDelete={hasPermission(perm.DELETE) ? handleDelete : undefined}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={hasPermission(perm.EXPORT) ? handleExport : undefined}
			onImport={hasPermission(perm.IMPORT) ? handleImport : undefined}
		/>
	);
}

export default MotorizedLowValueSupplyElectricityPage;
