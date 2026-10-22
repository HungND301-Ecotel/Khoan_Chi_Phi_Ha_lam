import { FormCheckBox } from '@/components/form/form-check-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { XCircleIcon } from 'lucide-react';
import { useEffect, useMemo } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import { ExcavatorMonthSection } from './components/excavator-month-section';
import { ScaniaMonthSection } from './components/scania-month-section';
import { ServiceCraneMonthSection } from './components/service-crane-month-section';
import { VacuumTruckMonthSection } from './components/vacuum-truck-month-section';
import {
	getProcessCategories,
	MOTORIZED_CATEGORY_ORDER,
	useMotorizedPricingIndex,
	withCategoryOverrides,
} from './pricing-index';
import { AssetProcessSelect } from './components/asset-process-select';
import type { MotorizedCategory } from './types';

const CATEGORY_ASSIGNMENT_FIELDS: Record<MotorizedCategory, string> = {
	scania: 'scaniaAssignmentCodeIds',
	excavator_dozer: 'excavatorAssignmentCodeIds',
	service_crane: 'serviceCraneAssignmentCodeIds',
	vacuum_truck: 'vacuumTruckAssignmentCodeIds',
};

const isSameIds = (a: string[] = [], b: string[] = []) =>
	a.length === b.length && a.every((id, index) => id === b[index]);

type MotorizedMonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	assignmentCodes?: any[];
	productionProcesses?: any[];
	distances?: any[];
	cargoTypes?: any[];
	locations?: any[];
	onRemoveMonth: () => void;
	canRemove: boolean;
};

export function MotorizedMonthSection({
	form,
	monthIndex,
	assignmentCodes = [],
	productionProcesses = [],
	distances = [],
	cargoTypes = [],
	locations = [],
	onRemoveMonth,
	canRemove,
}: MotorizedMonthSectionProps) {
	const monthPath = `motorizedMonths.${monthIndex}` as const;

	const watchedMonth = useWatch({
		control: form.control,
		name: monthPath,
	}) as any;

	const pricingIndex = useMotorizedPricingIndex();
	const assignmentCodeIds: string[] = watchedMonth?.assignmentCodeIds || [];
	const categoryOverrides: Record<string, string> =
		watchedMonth?.assetCategoryOverrides || {};
	const equipmentProcesses: Record<string, string[]> =
		watchedMonth?.equipmentProcesses || {};
	const processCategories: Record<string, string> =
		watchedMonth?.processCategories || {};

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

		const current = form.getValues(monthPath) as any;
		MOTORIZED_CATEGORY_ORDER.forEach((category) => {
			const field = CATEGORY_ASSIGNMENT_FIELDS[category];
			if (!isSameIds(current?.[field], idsByCategory[category])) {
				form.setValue(`${monthPath}.${field}` as any, idsByCategory[category]);
			}
		});

		const categories = MOTORIZED_CATEGORY_ORDER.filter(
			(category) => idsByCategory[category].length > 0,
		);
		if (!isSameIds(current?.motorizedCategories, categories)) {
			form.setValue(`${monthPath}.motorizedCategories` as any, categories);
			form.setValue(
				`${monthPath}.motorizedCategory` as any,
				categories[0] || 'scania',
			);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [effectiveIndex, assignmentCodeKey, equipmentProcessesKey, monthPath]);

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
					<ScaniaMonthSection
						form={form}
						monthIndex={monthIndex}
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
					<ExcavatorMonthSection
						form={form}
						monthIndex={monthIndex}
						pricingIndex={effectiveIndex}
						assignmentCodes={currentEquipments}
						processes={currentProcesses}
						assignmentCodeId={acId}
					/>
				)}

				{categories.includes('service_crane') && (
					<ServiceCraneMonthSection
						form={form}
						monthIndex={monthIndex}
						pricingIndex={effectiveIndex}
						assignmentCodes={currentEquipments}
						processes={currentProcesses}
						distances={currentDistances}
						assignmentCodeId={acId}
					/>
				)}

				{categories.includes('vacuum_truck') && (
					<VacuumTruckMonthSection
						form={form}
						monthIndex={monthIndex}
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
		<div className='flex flex-col gap-4 rounded-sm border border-[#999999] p-4'>
			<div className='flex items-center justify-between gap-4'>
				<FormMonthYear
					control={form.control}
					name={`motorizedMonths.${monthIndex}.month`}
					label='Thời gian'
					className='flex-1'
				/>
				<Button
					type='button'
					variant='ghost'
					size='sm'
					className='text-error hover:text-error-muted mt-7 bg-transparent'
					onClick={onRemoveMonth}
					disabled={!canRemove}
				>
					<XCircleIcon className='size-4' />
					<span>Xóa tháng</span>
				</Button>
			</div>

			{typeof (form.formState.errors as any).motorizedMonths?.[monthIndex]
				?.month?.message === 'string' && (
				<FieldError
					errors={[
						(form.formState.errors as any).motorizedMonths?.[monthIndex]?.month,
					]}
				/>
			)}

			<FormCheckBox
				control={form.control}
				name={`motorizedMonths.${monthIndex}.lowValuePerishableSupply`}
				label='Chi phí vật tư mau hỏng rẻ tiền (đồng/tháng)'
			/>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={form.control as any}
				name={`${monthPath}.assignmentCodeIds` as any}
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
					basePath={monthPath}
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
