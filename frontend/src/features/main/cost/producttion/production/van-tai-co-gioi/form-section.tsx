import { Button } from '@/components/ui/button';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../production-form-schema';
import { ExcavatorFormSection } from './components/excavator-form-section';
import { ScaniaFormSection } from './components/scania-form-section';
import { ServiceCraneFormSection } from './components/service-crane-form-section';
import { VacuumTruckFormSection } from './components/vacuum-truck-form-section';
import { MOTORIZED_CATEGORIES, MotorizedCategory } from './types';
import {
	MOCK_CARGO_TYPES,
	MOCK_HAUL_DISTANCES,
	MOCK_LOCATIONS,
	MOCK_VTCG_EQUIPMENTS,
	MOCK_VTCG_PROCESSES,
} from './utils';

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

	// Use API list if available and matches category, or fallback to mock data
	const currentEquipments = (() => {
		const filtered = assignmentCodes.filter((a) => {
			const c = (a.code || '').toLowerCase();
			const n = (a.name || a.label || '').toLowerCase();
			if (activeCategory === 'scania')
				return c.includes('scania') || n.includes('scania');
			if (activeCategory === 'excavator_dozer')
				return c.includes('komatsu') || n.includes('xúc') || n.includes('gạt');
			if (activeCategory === 'service_crane')
				return (
					n.includes('cẩu') ||
					n.includes('tưới') ||
					n.includes('tải') ||
					n.includes('vụ')
				);
			if (activeCategory === 'vacuum_truck')
				return n.includes('hút') || n.includes('thải');
			return true;
		});
		return filtered.length > 0
			? filtered
			: MOCK_VTCG_EQUIPMENTS.filter((e) => e.category === activeCategory);
	})();

	const currentProcesses = (() => {
		const filtered = productionProcesses.filter((p) => {
			const n = (p.name || p.label || '').toLowerCase();
			if (activeCategory === 'scania')
				return (
					n.includes('vận chuyển') &&
					(n.includes('than') || n.includes('đất') || n.includes('bùn'))
				);
			if (activeCategory === 'excavator_dozer')
				return n.includes('xúc') || n.includes('gạt') || n.includes('giờ');
			if (activeCategory === 'service_crane')
				return n.includes('cẩu') || n.includes('tưới') || n.includes('giờ');
			if (activeCategory === 'vacuum_truck')
				return n.includes('hút') || n.includes('thải');
			return true;
		});
		return filtered.length > 0
			? filtered
			: MOCK_VTCG_PROCESSES.filter((p) => p.category === activeCategory);
	})();

	const currentDistances =
		distances.length > 0 ? distances : MOCK_HAUL_DISTANCES;
	const currentCargoTypes =
		cargoTypes.length > 0 ? cargoTypes : MOCK_CARGO_TYPES;
	const currentLocations = locations.length > 0 ? locations : MOCK_LOCATIONS;

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
