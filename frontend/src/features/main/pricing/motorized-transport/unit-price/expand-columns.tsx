import { ColumnDef } from '@tanstack/react-table';
import {
	MechanizedTransportUnitPriceGroupDto,
	VEHICLE_TYPE_LABELS,
} from './types';

// ====== Types cho expand data ======

export type ExpandPriceRow = {
	headerId: string;
	detailId: string;
	equipmentQuality: string;
	haulDistanceValue?: string;
	fuelUnitPrice?: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice?: number;
};

// ====== Columns cho bảng đơn giá ======

export const EXPAND_PRICE_COLUMNS: ColumnDef<ExpandPriceRow>[] = [
	{
		accessorKey: 'equipmentQuality',
		header: 'Chất lượng',
		cell: ({ row }) => (
			<span className='font-medium text-gray-700'>
				Thiết bị loại {row.original.equipmentQuality}
			</span>
		),
	},
	{
		accessorKey: 'haulDistanceValue',
		header: 'Cung độ vận tải',
		cell: ({ row }) => row.original.haulDistanceValue || '-',
	},
	{
		accessorKey: 'fuelUnitPrice',
		header: 'Đơn giá Nhiên liệu (đ)',
		cell: ({ row }) => row.original.fuelUnitPrice?.toLocaleString('vi-VN') ?? 0,
	},
	{
		accessorKey: 'powerUnitPrice',
		header: 'Đơn giá Động lực (đ)',
		cell: ({ row }) =>
			row.original.powerUnitPrice?.toLocaleString('vi-VN') ?? '-',
	},
	{
		accessorKey: 'maintenanceUnitPrice',
		header: 'Đơn giá SCTX (đ)',
		cell: ({ row }) =>
			row.original.maintenanceUnitPrice?.toLocaleString('vi-VN') ?? 0,
	},
];

// ====== Helper functions ======

export function buildExpandData(row: MechanizedTransportUnitPriceGroupDto) {
	const sections = row.sections || [];

	// Group sections by vehicleType + productionProcessId
	const groupedByProcess: Record<
		string,
		{
			vehicleType: number;
			vehicleLabel: string;
			productionProcessName: string;
			cargoTypeName?: string;
			receivingLocationName?: string;
			dumpingLocationName?: string;
			rows: ExpandPriceRow[];
		}
	> = {};

	sections.forEach((section) => {
		const key = `${section.vehicleType}_${section.productionProcessId}_${section.cargoTypeId || ''}_${section.receivingLocationId || ''}_${section.dumpingLocationId || ''}`;
		if (!groupedByProcess[key]) {
			groupedByProcess[key] = {
				vehicleType: section.vehicleType,
				vehicleLabel: VEHICLE_TYPE_LABELS[section.vehicleType] || 'Phương tiện',
				productionProcessName: section.productionProcessName || '-',
				cargoTypeName: section.cargoTypeName,
				receivingLocationName: section.receivingLocationName,
				dumpingLocationName: section.dumpingLocationName,
				rows: [],
			};
		}

		// Map rows
		section.rows.forEach((row) => {
			groupedByProcess[key].rows.push({
				headerId: row.headerId,
				detailId: row.detailId,
				equipmentQuality: row.equipmentQuality,
				haulDistanceValue: row.haulDistanceValue,
				fuelUnitPrice: row.fuelUnitPrice,
				powerUnitPrice: row.powerUnitPrice,
				maintenanceUnitPrice: row.maintenanceUnitPrice,
			});
		});
	});

	// Sort rows by equipmentQuality (A → B → C)
	Object.values(groupedByProcess).forEach((group) => {
		group.rows.sort((a, b) => {
			const qA = a.equipmentQuality || '';
			const qB = b.equipmentQuality || '';
			return qA.localeCompare(qB);
		});
	});

	return groupedByProcess;
}
