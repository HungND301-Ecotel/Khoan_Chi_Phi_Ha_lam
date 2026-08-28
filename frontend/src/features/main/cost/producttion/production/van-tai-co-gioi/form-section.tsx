import { FormMultiSelect } from '@/components/form/form-multi-select';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../production-form-schema';
import { ExcavatorFormSection } from './components/excavator-form-section';
import { ScaniaFormSection } from './components/scania-form-section';
import { ServiceCraneFormSection } from './components/service-crane-form-section';
import { VacuumTruckFormSection } from './components/vacuum-truck-form-section';
import { MOTORIZED_CATEGORIES } from './types';

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

	const selectedCategories: string[] =
		watchedGroup?.motorizedCategories ||
		(watchedGroup?.motorizedCategory
			? [watchedGroup.motorizedCategory]
			: ['scania']);

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
			{/* CẤP 1: CHỌN NHÓM VẬN TẢI CƠ GIỚI (MULTI-SELECT) */}
			<FormMultiSelect
				control={form.control as any}
				name={`${groupPath}.motorizedCategories` as any}
				label='Nhóm Vận tải cơ giới'
				placeholder='Chọn nhóm vận tải cơ giới'
				options={MOTORIZED_CATEGORIES.map((cat) => ({
					value: cat.id,
					label: cat.name,
				}))}
			/>

			{/* RENDER DÃY FORM THEO TỪNG NHÓM ĐƯỢC CHỌN */}
			{selectedCategories.includes('scania') && (
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

			{selectedCategories.includes('excavator_dozer') && (
				<ExcavatorFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			)}

			{selectedCategories.includes('service_crane') && (
				<ServiceCraneFormSection
					form={form}
					groupIndex={groupIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}

			{selectedCategories.includes('vacuum_truck') && (
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
