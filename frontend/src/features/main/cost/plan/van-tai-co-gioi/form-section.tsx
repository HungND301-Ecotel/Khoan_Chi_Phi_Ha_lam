import { FormCheckBox } from '@/components/form/form-check-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { XCircleIcon } from 'lucide-react';
import { useEffect } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import { ExcavatorMonthSection } from './components/excavator-month-section';
import { ScaniaMonthSection } from './components/scania-month-section';
import { ServiceCraneMonthSection } from './components/service-crane-month-section';
import { VacuumTruckMonthSection } from './components/vacuum-truck-month-section';
import {
	getAssetCategories,
	MOTORIZED_CATEGORY_ORDER,
	useMotorizedPricingIndex,
} from './pricing-index';
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
	const selectedCategories: string[] = watchedMonth?.motorizedCategories || [];

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
	}, [pricingIndex, JSON.stringify(assignmentCodeIds), monthPath]);

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

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN — khối loại xe tự hiện theo đơn giá */}
			<FormMultiSelect
				control={form.control as any}
				name={`${monthPath}.assignmentCodeIds` as any}
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
				<ScaniaMonthSection
					form={form}
					monthIndex={monthIndex}
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
				<ExcavatorMonthSection
					form={form}
					monthIndex={monthIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			)}

			{pricingIndex && selectedCategories.includes('service_crane') && (
				<ServiceCraneMonthSection
					form={form}
					monthIndex={monthIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}

			{pricingIndex && selectedCategories.includes('vacuum_truck') && (
				<VacuumTruckMonthSection
					form={form}
					monthIndex={monthIndex}
					pricingIndex={pricingIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}
		</div>
	);
}
