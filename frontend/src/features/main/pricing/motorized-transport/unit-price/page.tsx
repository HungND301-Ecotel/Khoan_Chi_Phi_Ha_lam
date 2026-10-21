import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { useMemo, useState } from 'react';
import { MotorizedSectionExpand } from './expand-columns';
import type {
	ExpandPriceRow,
	MechanizedTransportAssignmentGroup,
	MechanizedTransportUnitPriceGroupDto,
	MotorizedC2Item,
} from './types';
import {
	VEHICLE_TYPE_LABELS,
	VEHICLE_TYPE_OPTIONS,
	getVehicleApi,
} from './types';
import { UNIFIED_MOTORIZED_TRANSPORT_COLUMNS } from './unified-columns';
import UnifiedMotorizedTransportForm from './unified-form';

const ALL_VEHICLE_TYPES = 'all';

export function MotorizedFuelPowerMaintenancePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const [selectedC3Ids, setSelectedC3Ids] = useState<string[]>([]);
	// Lọc theo nhóm vận tải cơ giới (vehicleType backend: 1=Scania, 2=Hút bùn, 3=Phục vụ, 4=Xúc gạt)
	const [vehicleTypeFilter, setVehicleTypeFilter] = useState<string>(ALL_VEHICLE_TYPES);
	const query = useMemo(
		() =>
			vehicleTypeFilter === ALL_VEHICLE_TYPES
				? undefined
				: { vehicleType: Number(vehicleTypeFilter) },
		[vehicleTypeFilter],
	);

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

	// Cấp 1 = Nhóm vật tư, tài sản + Chất lượng thiết bị (mỗi chất lượng 1 dòng); các cấp
	// dưới chỉ chứa đơn giá của đúng chất lượng đó nên bảng chi tiết không cần cột Chất lượng.
	const transformData = (
		rows: MechanizedTransportUnitPriceGroupDto[],
	): MechanizedTransportAssignmentGroup[] => {
		const groupMap = new Map<string, MechanizedTransportAssignmentGroup>();
		// Thứ tự xuất hiện của từng Nhóm vật tư — giữ các chất lượng của cùng 1 nhóm đứng liền nhau
		const assignmentOrder = new Map<string, number>();

		rows.forEach((row) => {
			const assignmentKey =
				row.assignmentCodeId ||
				row.assignmentCodeName ||
				`${row.startMonth}_${row.endMonth}`;
			if (!assignmentOrder.has(assignmentKey)) {
				assignmentOrder.set(assignmentKey, assignmentOrder.size);
			}

			(row.sections || []).forEach((section) => {
				// Tách dòng đơn giá của section theo Chất lượng thiết bị
				const rowsByQuality = new Map<string, typeof section.rows>();
				(section.rows || []).forEach((r) => {
					const quality = r.equipmentQuality || '';
					if (!rowsByQuality.has(quality)) rowsByQuality.set(quality, []);
					rowsByQuality.get(quality)!.push(r);
				});

				rowsByQuality.forEach((qualityRows, quality) => {
					const qualitySection = { ...section, rows: qualityRows };
					const groupKey = `${assignmentKey}__${quality}`;

					let assignmentGroup = groupMap.get(groupKey);
					if (!assignmentGroup) {
						assignmentGroup = {
							...row,
							id: groupKey,
							equipmentQuality: quality,
							sections: [],
							c2Items: [],
							rawGroups: [],
						};
						groupMap.set(groupKey, assignmentGroup);
					}
					if (!assignmentGroup.rawGroups!.includes(row)) {
						assignmentGroup.rawGroups!.push(row);
					}
					assignmentGroup.sections.push(qualitySection);

					// Gom section theo loại xe thành C2
					const c2Id = `${groupKey}_${row.startMonth}_${row.endMonth}_${section.vehicleType}`;
					let c2Item = assignmentGroup.c2Items.find((c2) => c2.id === c2Id);
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
						assignmentGroup.c2Items.push(c2Item);
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
						new Set(qualityRows.map((r) => r.headerId).filter(Boolean)),
					);

					const priceRows: ExpandPriceRow[] = qualityRows.map((r) => ({
						headerId: r.headerId,
						detailId: r.detailId,
						equipmentQuality: r.equipmentQuality,
						haulDistanceValue: r.haulDistanceValue,
						fuelUnitPrice: r.fuelUnitPrice,
						powerUnitPrice: r.powerUnitPrice,
						maintenanceUnitPrice: r.maintenanceUnitPrice,
					}));

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
							if (!existingC3.headerIds.includes(hid))
								existingC3.headerIds.push(hid);
						});
						existingC3.rows.push(...priceRows);
					}
				});
			});
		});

		const groups = Array.from(groupMap.values()).map((group) => {
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

		const orderOf = (group: MechanizedTransportAssignmentGroup) =>
			assignmentOrder.get(group.id!.split('__')[0]) ?? 0;
		return groups.sort(
			(a, b) =>
				orderOf(a) - orderOf(b) ||
				(a.equipmentQuality || '').localeCompare(b.equipmentQuality || ''),
		);
	};

	return (
		<DataTable
			url={API.PRICING.MOTORIZED_TRANSPORT.ALL.LIST}
			query={query}
			toolbarFilters={
				<Select
					value={vehicleTypeFilter}
					onValueChange={(value) => {
						setVehicleTypeFilter(value);
						setSelectedC3Ids([]);
					}}
				>
					<SelectTrigger className='w-64 bg-white'>
						<SelectValue placeholder='Chọn nhóm' />
					</SelectTrigger>
					<SelectContent>
						<SelectItem value={ALL_VEHICLE_TYPES}>Tất cả nhóm</SelectItem>
						{VEHICLE_TYPE_OPTIONS.map((opt) => (
							<SelectItem key={opt.value} value={String(opt.apiType)}>
								{opt.label}
							</SelectItem>
						))}
					</SelectContent>
				</Select>
			}
			columns={UNIFIED_MOTORIZED_TRANSPORT_COLUMNS}
			transformData={transformData}
			getRowId={(row) => row.id || row.assignmentCodeId || ''}
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


