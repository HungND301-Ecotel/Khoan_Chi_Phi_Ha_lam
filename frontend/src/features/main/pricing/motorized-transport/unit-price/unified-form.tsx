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
		allProcesses: (row.sections || []).flatMap((section: any) =>
			(section.rows || []).map((r: any) => ({
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
			{/* Chọn Nhóm Vận tải cơ giới (Tab Buttons như bên Kế hoạch) */}
			{!row && (
				<div className='space-y-1.5 rounded-lg border border-gray-200 bg-white p-3 shadow-2xs'>
					<div className='text-xs font-bold uppercase text-gray-700'>
						Nhóm Vận tải cơ giới
					</div>
					<div className='grid grid-cols-2 gap-2 sm:grid-cols-4'>
						{VEHICLE_TYPE_OPTIONS.map((cat) => {
							const isSelected = activeVehicleType === cat.value;
							return (
								<button
									key={cat.value}
									type='button'
									className={`flex h-auto min-h-[44px] items-center justify-between rounded-md border px-3 py-2 text-left text-xs font-semibold whitespace-normal transition-all ${
										isSelected
											? 'border-primary bg-primary text-white shadow-xs'
											: 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
									}`}
									onClick={() => setVehicleType(cat.value)}
								>
									<span>{cat.label}</span>
									{isSelected && (
										<span className='ml-2 h-2 w-2 shrink-0 rounded-full bg-white' />
									)}
								</button>
							);
						})}
					</div>
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
