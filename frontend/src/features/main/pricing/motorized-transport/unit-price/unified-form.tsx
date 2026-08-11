import type { ActionDialogProps } from '@/components/datatable';
import { useState } from 'react';
import {
	VEHICLE_TYPE_OPTIONS,
	MechanizedTransportUnitPriceGroupDto,
} from './types';
import { MotorizedScaniaForm } from '../scania/form';
import { MotorizedVacuumTruckForm } from '../vacuum-truck/form';
import { MotorizedServiceCraneForm } from '../service-crane/form';
import { MotorizedExcavatorDozerForm } from '../excavator-dozer/form';

type UnifiedMotorizedTransportFormProps =
	ActionDialogProps<MechanizedTransportUnitPriceGroupDto> & {
		isDuplicate?: boolean;
		defaultVehicleType?: string;
	};

export function UnifiedMotorizedTransportForm(
	props: UnifiedMotorizedTransportFormProps,
) {
	const { row, defaultVehicleType } = props;

	// For create mode, show vehicle type selector
	const [vehicleType, setVehicleType] = useState<string>(
		defaultVehicleType || 'scania',
	);

	// For edit/duplicate, extract vehicleType from first section (if available)
	const editVehicleType = row?.sections?.[0]?.vehicleType;
	const editVehicleKey = editVehicleType === 1 ? 'scania'
		: editVehicleType === 2 ? 'vacuum-truck'
		: editVehicleType === 3 ? 'service-crane'
		: editVehicleType === 4 ? 'excavator-dozer'
		: 'scania';

	// Use editVehicleKey for edit/duplicate, or selected vehicleType for create
	const activeVehicleType = row ? editVehicleKey : vehicleType;

	// Map group row to format expected by individual forms
	const mappedRow = row ? {
		...row,
		allProcesses: (row.sections || []).flatMap((section) =>
			section.rows.map((r) => ({
				id: r.headerId,
				vehicleType: section.vehicleType,
				productionProcessId: section.productionProcessId,
				productionProcessName: section.productionProcessName,
				equipmentQuality: r.equipmentQuality,
				details: [{
					id: r.detailId,
					haulDistanceId: r.haulDistanceId,
					haulDistanceValue: r.haulDistanceValue,
					fuelUnitPrice: r.fuelUnitPrice,
					powerUnitPrice: r.powerUnitPrice,
					maintenanceUnitPrice: r.maintenanceUnitPrice,
				}],
			}))
		),
	} : undefined;

	return (
		<div className='space-y-4'>
			{/* Dropdown Chọn Loại phương tiện nếu tạo mới */}
			{!row && (
				<div className='border-primary/20 bg-primary/5 rounded-lg border p-4'>
					<label className='mb-2 block text-sm font-semibold text-gray-800'>
						Nhóm :
					</label>
					<select
						value={vehicleType}
						onChange={(e) => setVehicleType(e.target.value)}
						className='focus:border-primary focus:ring-primary w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-900 shadow-2xs focus:ring-1 focus:outline-hidden'
					>
						{VEHICLE_TYPE_OPTIONS.map((opt) => (
							<option key={opt.value} value={opt.value}>
								{opt.label}
							</option>
						))}
					</select>
				</div>
			)}

			{/* Hiển thị form tương ứng với phương tiện được chọn */}
			{activeVehicleType === 'scania' && (
				<MotorizedScaniaForm {...(props as any)} row={mappedRow} />
			)}
			{activeVehicleType === 'vacuum-truck' && (
				<MotorizedVacuumTruckForm {...(props as any)} row={mappedRow} />
			)}
			{activeVehicleType === 'service-crane' && (
				<MotorizedServiceCraneForm {...(props as any)} row={mappedRow} />
			)}
			{activeVehicleType === 'excavator-dozer' && (
				<MotorizedExcavatorDozerForm {...(props as any)} row={mappedRow} />
			)}
		</div>
	);
}

export default UnifiedMotorizedTransportForm;
