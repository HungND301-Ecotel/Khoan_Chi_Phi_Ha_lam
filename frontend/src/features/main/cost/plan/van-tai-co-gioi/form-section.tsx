import { FormCheckBox } from '@/components/form/form-check-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { XCircleIcon } from 'lucide-react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import { ExcavatorMonthSection } from './components/excavator-month-section';
import { ScaniaMonthSection } from './components/scania-month-section';
import { ServiceCraneMonthSection } from './components/service-crane-month-section';
import { VacuumTruckMonthSection } from './components/vacuum-truck-month-section';
import { MOTORIZED_PLAN_CATEGORIES } from './types';

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

	const selectedCategories: string[] =
		watchedMonth?.motorizedCategories ||
		(watchedMonth?.motorizedCategory
			? [watchedMonth.motorizedCategory]
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

			{/* CẤP 1: CHỌN NHÓM VẬN TẢI CƠ GIỚI (MULTI-SELECT) */}
			<FormMultiSelect
				control={form.control as any}
				name={`${monthPath}.motorizedCategories` as any}
				label='Nhóm Vận tải cơ giới'
				placeholder='Chọn nhóm vận tải cơ giới'
				options={MOTORIZED_PLAN_CATEGORIES.map((cat) => ({
					value: cat.id,
					label: cat.name,
				}))}
			/>

			{/* RENDER DÃY FORM THEO TỪNG NHÓM ĐƯỢC CHỌN */}
			{selectedCategories.includes('scania') && (
				<ScaniaMonthSection
					form={form}
					monthIndex={monthIndex}
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
				<ExcavatorMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			)}

			{selectedCategories.includes('service_crane') && (
				<ServiceCraneMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}

			{selectedCategories.includes('vacuum_truck') && (
				<VacuumTruckMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			)}
		</div>
	);
}
