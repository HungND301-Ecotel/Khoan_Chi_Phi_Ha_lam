import { FormMultiSelect } from '@/components/form/form-multi-select';
import {
	getProcessCategories,
	MOTORIZED_CATEGORY_ORDER,
	useMotorizedPricingIndex,
	withCategoryOverrides,
} from '@/features/main/cost/plan/van-tai-co-gioi/pricing-index';
import { AssetProcessSelect } from '@/features/main/cost/plan/van-tai-co-gioi/components/asset-process-select';
import { useEffect, useMemo } from 'react';
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
	const categoryOverrides: Record<string, string> =
		watchedGroup?.assetCategoryOverrides || {};
	const equipmentProcesses: Record<string, string[]> =
		watchedGroup?.equipmentProcesses || {};
	const processCategories: Record<string, string> =
		watchedGroup?.processCategories || {};

	const assignmentCodeKey = JSON.stringify(assignmentCodeIds);
	const categoryOverridesKey = JSON.stringify(categoryOverrides);
	const equipmentProcessesKey = JSON.stringify(equipmentProcesses);
	const processCategoriesKey = JSON.stringify(processCategories);

	// Nhóm vật tư chưa có đơn giá vẫn chọn được: gắn loại xe người dùng chọn (mặc định đoán theo tên);
	// Công đoạn có đơn giá ở nhiều loại xe chỉ thuộc loại xe người dùng chọn
	const effectiveIndex = useMemo(
		() =>
			pricingIndex
				? withCategoryOverrides(
						pricingIndex,
						assignmentCodeIds,
						categoryOverrides,
						assignmentCodes,
						processCategories,
					)
				: null,
		// eslint-disable-next-line react-hooks/exhaustive-deps
		[
			pricingIndex,
			assignmentCodeKey,
			categoryOverridesKey,
			processCategoriesKey,
			assignmentCodes,
		],
	);

	// Chọn Nhóm vật tư + Công đoạn — khối loại xe chỉ hiện khi có Công đoạn (có đơn giá ở loại xe đó) được chọn
	useEffect(() => {
		if (!effectiveIndex) return;

		const idsByCategory = Object.fromEntries(
			MOTORIZED_CATEGORY_ORDER.map((category) => [category, [] as string[]]),
		) as Record<MotorizedCategory, string[]>;
		assignmentCodeIds.forEach((acId) => {
			getProcessCategories(
				effectiveIndex,
				acId,
				equipmentProcesses[acId] || [],
			).forEach((category) => idsByCategory[category].push(acId));
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
	}, [effectiveIndex, assignmentCodeKey, equipmentProcessesKey, groupPath]);

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

	// Nhập chi tiết ngay trong thẻ Nhóm vật tư, theo loại xe của các Công đoạn đã chọn
	const renderDetails = (acId: string) => {
		if (!effectiveIndex) return null;
		const categories = getProcessCategories(
			effectiveIndex,
			acId,
			equipmentProcesses[acId] || [],
		);
		return (
			<>
				{categories.includes('scania') && (
					<ScaniaFormSection
						form={form}
						groupIndex={groupIndex}
						pricingIndex={effectiveIndex}
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
						assignmentCodeId={acId}
					/>
				)}

				{categories.includes('excavator_dozer') && (
					<ExcavatorFormSection
						form={form}
						groupIndex={groupIndex}
						pricingIndex={effectiveIndex}
						assignmentCodes={currentEquipments}
						processes={currentProcesses}
						assignmentCodeId={acId}
					/>
				)}

				{categories.includes('service_crane') && (
					<ServiceCraneFormSection
						form={form}
						groupIndex={groupIndex}
						pricingIndex={effectiveIndex}
						assignmentCodes={currentEquipments}
						processes={currentProcesses}
						distances={currentDistances}
						assignmentCodeId={acId}
					/>
				)}

				{categories.includes('vacuum_truck') && (
					<VacuumTruckFormSection
						form={form}
						groupIndex={groupIndex}
						pricingIndex={effectiveIndex}
						assignmentCodes={currentEquipments}
						processes={currentProcesses}
						distances={currentDistances}
						assignmentCodeId={acId}
					/>
				)}
			</>
		);
	};

	return (
		<div className='space-y-4'>
			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={form.control as any}
				name={`${groupPath}.assignmentCodeIds` as any}
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* CẤP 2: CÔNG ĐOẠN THEO TỪNG NHÓM VẬT TƯ — biết dòng thuộc đơn giá loại xe nào */}
			{pricingIndex && effectiveIndex && (
				<AssetProcessSelect
					form={form}
					basePath={groupPath}
					assignmentCodeIds={assignmentCodeIds}
					pricingIndex={pricingIndex}
					effectiveIndex={effectiveIndex}
					assignmentCodes={assignmentCodes}
					processes={productionProcesses}
					renderDetails={renderDetails}
				/>
			)}
		</div>
	);
}
