import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { useState } from 'react';
import { MotorizedSectionExpand } from './expand-columns';
import type {
	ExpandPriceRow,
	MechanizedTransportAssignmentGroup,
	MechanizedTransportUnitPriceGroupDto,
	MotorizedC2Item,
} from './types';
import { VEHICLE_TYPE_LABELS, getVehicleApi } from './types';
import { UNIFIED_MOTORIZED_TRANSPORT_COLUMNS } from './unified-columns';
import UnifiedMotorizedTransportForm from './unified-form';

export function MotorizedFuelPowerMaintenancePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const [selectedC3Ids, setSelectedC3Ids] = useState<string[]>([]);

	const handleToggleC3Select = (c3Id: string) => {
		setSelectedC3Ids((prev) =>
			prev.includes(c3Id)
				? prev.filter((id) => id !== c3Id)
				: [...prev, c3Id],
		);
	};

	const handleToggleC2Select = (c2Item: MotorizedC2Item, checked: boolean) => {
		const c3Ids = (c2Item.c3Items || []).map((c3) => c3.id);
		setSelectedC3Ids((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...c3Ids]));
			} else {
				return prev.filter((id) => !c3Ids.includes(id));
			}
		});
	};

	const isRowSelected = (
		row: MechanizedTransportAssignmentGroup,
	): boolean | 'indeterminate' => {
		const allC3Items = (row.c2Items ?? []).flatMap((c2) => c2.c3Items ?? []);
		if (allC3Items.length === 0) return false;
		const selectedCount = allC3Items.filter((item) =>
			selectedC3Ids.includes(item.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === allC3Items.length) return true;
		return 'indeterminate';
	};

	const handleRowSelectChange = (
		row: MechanizedTransportAssignmentGroup,
		checked: boolean,
	) => {
		const allC3Ids = (row.c2Items ?? []).flatMap((c2) =>
			(c2.c3Items ?? []).map((c3) => c3.id),
		);

		setSelectedC3Ids((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allC3Ids]));
			} else {
				return prev.filter((id) => !allC3Ids.includes(id));
			}
		});
	};

	const handleSelectAllRowsChange = (
		checked: boolean,
		rows: MechanizedTransportAssignmentGroup[],
	) => {
		const allC3Ids = rows.flatMap((r) =>
			(r.c2Items ?? []).flatMap((c2) => (c2.c3Items ?? []).map((c3) => c3.id)),
		);
		setSelectedC3Ids((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allC3Ids]));
			} else {
				return prev.filter((id) => !allC3Ids.includes(id));
			}
		});
	};

	const handleDelete = async ({
		data,
	}: ActionDialogProps<MechanizedTransportAssignmentGroup>) => {
		if (selectedC3Ids.length === 0) return;
		try {
			const allC3Items = data.table
				.getRowModel()
				.rows.flatMap((r) =>
					(r.original.c2Items ?? []).flatMap((c2) => c2.c3Items ?? []),
				);
			const selectedItems = allC3Items.filter((item) =>
				selectedC3Ids.includes(item.id),
			);

			const idsToDelete: Record<number, string[]> = {};
			selectedItems.forEach((item) => {
				const vt = item.vehicleType;
				if (!idsToDelete[vt]) idsToDelete[vt] = [];
				item.headerIds.forEach((hid) => {
					if (!idsToDelete[vt].includes(hid)) {
						idsToDelete[vt].push(hid);
					}
				});
			});

			const promises = Object.entries(idsToDelete).map(([vt, ids]) => {
				const apiEndpoints = getVehicleApi(Number(vt), API);
				return api.delete(apiEndpoints.DELETES, ids);
			});

			await Promise.all(promises);

			popup.success(`Đã xoá thành công ${selectedC3Ids.length} mục ${breadcrumb}`);
			setSelectedC3Ids([]);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MOTORIZED_TRANSPORT.ALL.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MechanizedTransportAssignmentGroup>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MOTORIZED_TRANSPORT.ALL.IMPORT,
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
		rows: MechanizedTransportUnitPriceGroupDto[],
	): MechanizedTransportAssignmentGroup[] => {
		const groupMap = new Map<string, MechanizedTransportAssignmentGroup>();

		rows.forEach((row) => {
			const groupKey =
				row.assignmentCodeId ||
				row.assignmentCodeName ||
				`${row.startMonth}_${row.endMonth}`;

			let assignmentGroup = groupMap.get(groupKey);
			if (!assignmentGroup) {
				assignmentGroup = {
					...row,
					id: groupKey,
					c2Items: [],
					rawGroups: [row],
				};
				groupMap.set(groupKey, assignmentGroup);
			} else {
				assignmentGroup.rawGroups?.push(row);
				assignmentGroup.sections = [
					...(assignmentGroup.sections || []),
					...(row.sections || []),
				];
			}

			// Group sections of this row by vehicleType into C2 items
			const sections = row.sections || [];
			sections.forEach((section) => {
				const c2Id = `${row.assignmentCodeId}_${row.startMonth}_${row.endMonth}_${section.vehicleType}`;
				let c2Item = assignmentGroup!.c2Items.find((c2) => c2.id === c2Id);
				if (!c2Item) {
					c2Item = {
						id: c2Id,
						parentAssignmentCodeId: row.assignmentCodeId,
						startMonth: row.startMonth,
						endMonth: row.endMonth,
						vehicleType: section.vehicleType,
						vehicleLabel:
							VEHICLE_TYPE_LABELS[section.vehicleType] || 'Phương tiện',
						c3Items: [],
					};
					assignmentGroup!.c2Items.push(c2Item);
				}

				const params = [
					section.cargoTypeName,
					section.receivingLocationName,
					section.dumpingLocationName,
				]
					.filter(Boolean)
					.join(' - ');

				const c3Id = `${c2Id}_${section.productionProcessId}_${section.cargoTypeId || ''}_${section.receivingLocationId || ''}_${section.dumpingLocationId || ''}`;

				const headerIds = Array.from(
					new Set(
						(section.rows || []).map((r) => r.headerId).filter(Boolean),
					),
				);

				const priceRows: ExpandPriceRow[] = (section.rows || [])
					.map((r) => ({
						headerId: r.headerId,
						detailId: r.detailId,
						equipmentQuality: r.equipmentQuality,
						haulDistanceValue: r.haulDistanceValue,
						fuelUnitPrice: r.fuelUnitPrice,
						powerUnitPrice: r.powerUnitPrice,
						maintenanceUnitPrice: r.maintenanceUnitPrice,
					}))
					.sort((a, b) =>
						(a.equipmentQuality || '').localeCompare(
							b.equipmentQuality || '',
						),
					);

				// Check if c3Item already exists
				const existingC3 = c2Item.c3Items.find((c3) => c3.id === c3Id);
				if (!existingC3) {
					c2Item.c3Items.push({
						id: c3Id,
						c2Id: c2Id,
						vehicleType: section.vehicleType,
						productionProcessId: section.productionProcessId,
						productionProcessName: section.productionProcessName || '-',
						cargoTypeName: section.cargoTypeName,
						receivingLocationName: section.receivingLocationName,
						dumpingLocationName: section.dumpingLocationName,
						params: params || '-',
						headerIds,
						rows: priceRows,
					});
				} else {
					headerIds.forEach((hid) => {
						if (!existingC3.headerIds.includes(hid)) existingC3.headerIds.push(hid);
					});
					existingC3.rows.push(...priceRows);
				}
			});
		});

		return Array.from(groupMap.values()).map((group) => {
			const sortedRaw = (group.rawGroups || []).sort((a, b) =>
				(b.startMonth || '').localeCompare(a.startMonth || ''),
			);
			if (sortedRaw.length > 0) {
				group.startMonth = sortedRaw[0].startMonth;
				group.endMonth = sortedRaw[0].endMonth;
			}
			// Sort C2 items
			group.c2Items.sort((a, b) => {
				const timeCmp = (b.startMonth || '').localeCompare(a.startMonth || '');
				if (timeCmp !== 0) return timeCmp;
				return a.vehicleType - b.vehicleType;
			});
			// Sort C3 items inside each C2
			group.c2Items.forEach((c2) => {
				c2.c3Items.sort((a, b) =>
					a.productionProcessName.localeCompare(b.productionProcessName),
				);
			});
			return group;
		});
	};

	return (
		<DataTable
			url={API.PRICING.MOTORIZED_TRANSPORT.ALL.LIST}
			columns={UNIFIED_MOTORIZED_TRANSPORT_COLUMNS}
			transformData={transformData}
			getRowId={(row) => row.assignmentCodeId || row.assignmentCodeName || row.id || ''}
			filters={[{ key: 'assignmentCodeName', label: 'Nhóm vật tư, tài sản' }]}
			onExpand={({ row }) => (
				<MotorizedSectionExpand
					row={row}
					selectedC3Ids={selectedC3Ids}
					onToggleC2Select={handleToggleC2Select}
					onToggleC3Select={handleToggleC3Select}
				/>
			)}
			onCreate={(props) => <UnifiedMotorizedTransportForm {...(props as any)} />}
			onDuplicate={(props) => (
				<UnifiedMotorizedTransportForm {...(props as any)} isDuplicate />
			)}
			onUpdate={(props) => <UnifiedMotorizedTransportForm {...(props as any)} />}
			onDelete={handleDelete}
			deleteCountOverride={selectedC3Ids.length}
			deleteDisabledOverride={selectedC3Ids.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedFuelPowerMaintenancePage;


