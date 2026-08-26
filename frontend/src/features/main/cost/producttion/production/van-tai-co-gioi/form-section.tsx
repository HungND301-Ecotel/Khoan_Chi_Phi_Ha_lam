import { Button } from '@/components/ui/button';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../production-form-schema';
import { ExcavatorFormSection } from './components/excavator-form-section';
import { ScaniaFormSection } from './components/scania-form-section';
import { ServiceCraneFormSection } from './components/service-crane-form-section';
import { VacuumTruckFormSection } from './components/vacuum-truck-form-section';
import { MOTORIZED_CATEGORIES, MotorizedCategory } from './types';

type VanTaiCoGioiGroupFieldsProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	assignmentCodes?: any[];
	productionProcesses?: any[];
	distances?: any[];
	cargoTypes?: any[];
	locations?: any[];
};

export function VanTaiCoGioiGroupFields({
	form,
	groupIndex,
	assignmentCodes = [],
	productionProcesses = [],
	distances = [],
	cargoTypes = [],
	locations = [],
}: VanTaiCoGioiGroupFieldsProps) {
	const groupPath = `groups.${groupIndex}` as const;

	const watchedGroup = useWatch({
		control: form.control,
		name: groupPath,
	}) as any;

	const activeCategory: MotorizedCategory =
		watchedGroup?.motorizedCategory || 'scania';

	const handleCategoryChange = (category: MotorizedCategory) => {
		form.setValue(`${groupPath}.motorizedCategory` as any, category);
		// Reset selections for clean UI
		form.setValue(`${groupPath}.assignmentCodeIds` as any, []);
		form.setValue(`${groupPath}.equipmentQualities` as any, {});
		form.setValue(`${groupPath}.equipmentProcesses` as any, {});
		form.setValue(`${groupPath}.equipmentDistances` as any, {});
		form.setValue(`${groupPath}.processCargoTypes` as any, {});
		form.setValue(`${groupPath}.processPickupLocations` as any, {});
		form.setValue(`${groupPath}.processDropoffLocations` as any, {});
		form.setValue(`${groupPath}.motorizedItems` as any, []);
	};

	const currentEquipments = assignmentCodes;
	const currentProcesses = productionProcesses;

	const currentDistances = distances;
	const currentCargoTypes = cargoTypes;
	const currentLocations = locations;

	const pickupLocations = currentLocations.filter(
		(l) =>
			l.locationType === 1 ||
			(l.name || '').toLowerCase().includes('khai trường') ||
			(l.name || '').toLowerCase().includes('máng') ||
			(l.name || '').toLowerCase().includes('xúc'),
	);
	const dropoffLocations = currentLocations.filter(
		(l) =>
			l.locationType === 2 ||
			(l.name || '').toLowerCase().includes('kho') ||
			(l.name || '').toLowerCase().includes('thải'),
	);

	return (
		<div className='space-y-4'>
			{/* CẤP 1: 4 LOẠI PHƯƠNG TIỆN VẬN TẢI CƠ GIỚI */}
			<div className='space-y-1.5'>
				<div className='text-xs font-bold text-gray-700 uppercase'>
					Nhóm Vận tải cơ giới
				</div>
				<div className='grid grid-cols-2 gap-2 sm:grid-cols-4'>
					{MOTORIZED_CATEGORIES.map((cat) => {
						const isSelected = activeCategory === cat.id;
						return (
							<Button
								key={cat.id}
								type='button'
								variant={isSelected ? 'default' : 'outline'}
								className={`h-auto flex-col items-start gap-1 p-3 text-left transition-all ${
									isSelected
										? 'border-blue-600 bg-blue-600 text-white shadow-sm'
										: 'border-gray-200 bg-white text-gray-800 hover:border-gray-300 hover:bg-gray-50'
								}`}
								onClick={() => handleCategoryChange(cat.id)}
							>
								<div className='text-xs font-bold'>{cat.name}</div>
								<div
									className={`line-clamp-1 text-[11px] ${
										isSelected ? 'text-blue-100' : 'text-gray-500'
									}`}
								>
									{cat.description}
								</div>
							</Button>
						);
					})}
				</div>
			</div>

			{/* RENDER FORM SECTION THEO TỪNG LOẠI PHƯƠNG TIỆN */}
			{activeCategory === 'scania' && (
				<ScaniaFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					cargoTypes={currentCargoTypes}
					pickupLocations={
						pickupLocations.length > 0 ? pickupLocations : currentLocations
					}
					dropoffLocations={
						dropoffLocations.length > 0 ? dropoffLocations : currentLocations
					}
					distances={currentDistances}
				/>
			)}

			{activeCategory === 'excavator_dozer' && (
				<ExcavatorFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			)}

			{activeCategory === 'service_crane' && (
				<ServiceCraneFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}

			{activeCategory === 'vacuum_truck' && (
				<VacuumTruckFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}
		</div>
	);
}
