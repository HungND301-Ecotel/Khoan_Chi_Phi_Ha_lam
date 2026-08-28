import { FormCheckBox } from '@/components/form/form-check-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import { XCircleIcon } from 'lucide-react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import { ExcavatorMonthSection } from './components/excavator-month-section';
import { ScaniaMonthSection } from './components/scania-month-section';
import { ServiceCraneMonthSection } from './components/service-crane-month-section';
import { VacuumTruckMonthSection } from './components/vacuum-truck-month-section';
import { MOTORIZED_PLAN_CATEGORIES, MotorizedCategory } from './types';

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

	const activeCategory: MotorizedCategory =
		watchedMonth?.motorizedCategory || 'scania';

	const handleCategoryChange = (category: MotorizedCategory) => {
		form.setValue(`${monthPath}.motorizedCategory` as any, category);
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

			{/* CẤP 1: 4 LOẠI PHƯƠNG TIỆN VẬN TẢI CƠ GIỚI */}
			<div className='space-y-1.5'>
				<div className='text-xs font-bold text-gray-700 uppercase'>
					Nhóm Vận tải cơ giới
				</div>
				<div className='grid grid-cols-2 gap-2 sm:grid-cols-4'>
					{MOTORIZED_PLAN_CATEGORIES.map((cat) => {
						const isSelected = activeCategory === cat.id;
						return (
							<Button
								key={cat.id}
								type='button'
								variant={isSelected ? 'default' : 'outline'}
								className={`h-auto min-h-[44px] flex-col items-center justify-center gap-1 p-3 text-center transition-all ${
									isSelected
										? 'border-blue-600 bg-blue-600 text-white shadow-sm'
										: 'border-gray-200 bg-white text-gray-800 hover:border-gray-300 hover:bg-gray-50'
								}`}
								onClick={() => handleCategoryChange(cat.id)}
							>
								<div className='w-full text-center font-bold'>{cat.name}</div>
							</Button>
						);
					})}
				</div>
			</div>

			{/* RENDER FORM SECTION THEO TỪNG LOẠI PHƯƠNG TIỆN */}
			<div className={activeCategory === 'scania' ? 'block' : 'hidden'}>
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
			</div>

			<div className={activeCategory === 'excavator_dozer' ? 'block' : 'hidden'}>
				<ExcavatorMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
				/>
			</div>

			<div className={activeCategory === 'service_crane' ? 'block' : 'hidden'}>
				<ServiceCraneMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			</div>

			<div className={activeCategory === 'vacuum_truck' ? 'block' : 'hidden'}>
				<VacuumTruckMonthSection
					form={form}
					monthIndex={monthIndex}
					assignmentCodes={currentEquipments}
					processes={currentProcesses}
					distances={currentDistances}
				/>
			</div>
		</div>
	);
}
