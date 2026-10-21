import { FormMultiSelect } from '@/components/form/form-multi-select';
import {
	getAssetCategories,
	MOTORIZED_CATEGORY_ORDER,
	useMotorizedPricingIndex,
} from '@/features/main/cost/plan/van-tai-co-gioi/pricing-index';
import { useEffect } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../production-form-schema';
import { ExcavatorFormSection } from './components/excavator-form-section';
import { ScaniaFormSection } from './components/scania-form-section';
import { ServiceCraneFormSection } from './components/service-crane-form-section';
import { VacuumTruckFormSection } from './components/vacuum-truck-form-section';
import type { MotorizedCategory } from './types';

const CATEGORY_ASSIGNMENT_FIELDS: Record<MotorizedCategory, string> = {
	scania: 'scaniaAssignmentCodeIds',
	excavator_dozer: 'excavatorAssignmentCodeIds',
	service_crane: 'serviceCraneAssignmentCodeIds',
	vacuum_truck: 'vacuumTruckAssignmentCodeIds',
};

const isSameIds = (a: string[] = [], b: string[] = []) =>
	a.length === b.length && a.every((id, index) => id === b[index]);

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

	const pricingIndex = useMotorizedPricingIndex();
	const assignmentCodeIds: string[] = watchedGroup?.assignmentCodeIds || [];
	const selectedCategories: string[] = watchedGroup?.motorizedCategories || [];

	// Chọn thẳng Nhóm vật tư — loại xe (khối nhập) suy ra từ Đơn giá VTCG của nhóm vật tư đó
	useEffect(() => {
		if (!pricingIndex) return;

		const idsByCategory = Object.fromEntries(
			MOTORIZED_CATEGORY_ORDER.map((category) => [category, [] as string[]]),
		) as Record<MotorizedCategory, string[]>;
		assignmentCodeIds.forEach((acId) => {
			getAssetCategories(pricingIndex, acId).forEach((category) =>
				idsByCategory[category].push(acId),
			);
		});

		const current = form.getValues(groupPath) as any;
		MOTORIZED_CATEGORY_ORDER.forEach((category) => {
			const field = CATEGORY_ASSIGNMENT_FIELDS[category];
			if (!isSameIds(current?.[field], idsByCategory[category])) {
				form.setValue(`${groupPath}.${field}` as any, idsByCategory[category]);
			}
		});

		const categories = MOTORIZED_CATEGORY_ORDER.filter(
			(category) => idsByCategory[category].length > 0,
		);
		if (!isSameIds(current?.motorizedCategories, categories)) {
			form.setValue(`${groupPath}.motorizedCategories` as any, categories);
			form.setValue(
				`${groupPath}.motorizedCategory` as any,
				categories[0] || 'scania',
			);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [pricingIndex, JSON.stringify(assignmentCodeIds), groupPath]);

	// Chỉ cho chọn Nhóm vật tư đã có đơn giá VTCG (giữ lại nhóm đã chọn trước đó để không mất dữ liệu)
	const pricedAssignmentCodes =
		pricingIndex && Object.keys(pricingIndex).length > 0
			? assignmentCodes.filter(
					(a) =>
						!!pricingIndex[a.id || a.value] ||
						assignmentCodeIds.includes(a.id || a.value),
				)
			: assignmentCodes;

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
			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN — khối loại xe tự hiện theo đơn giá */}
			<FormMultiSelect
				control={form.control as any}
				name={`${groupPath}.assignmentCodeIds` as any}
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={pricedAssignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* RENDER DÃY FORM THEO TỪNG NHÓM ĐƯỢC CHỌN */}
			{pricingIndex && selectedCategories.includes('scania') && (
				<ScaniaFormSection
					form={form}
					groupIndex={groupIndex}
					pricingIndex={pricingIndex}
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
					locations={currentLocations}
				/>
			)}

			{pricingIndex && selectedCategories.includes('excavator_dozer') && (
				<ExcavatorFormSection
					form={form}
					groupIndex={groupIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			)}

			{pricingIndex && selectedCategories.includes('service_crane') && (
				<ServiceCraneFormSection
					form={form}
					groupIndex={groupIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}

			{pricingIndex && selectedCategories.includes('vacuum_truck') && (
				<VacuumTruckFormSection
					form={form}
					groupIndex={groupIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}
		</div>
	);
}
