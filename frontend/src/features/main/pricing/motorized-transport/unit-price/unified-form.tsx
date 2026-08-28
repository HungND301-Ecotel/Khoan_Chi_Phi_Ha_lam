import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { usePopup } from '@/components/popup';
import { useDialog } from '@/data/dialog/dialog.hook';
import { InfoIcon } from 'lucide-react';
import { useMemo, useRef } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import {
	VEHICLE_TYPE_OPTIONS,
	MechanizedTransportUnitPriceGroupDto,
} from './types';
import {
	MotorizedScaniaForm,
	MotorizedSubFormHandle,
} from '../scania/form';
import { MotorizedVacuumTruckForm } from '../vacuum-truck/form';
import { MotorizedServiceCraneForm } from '../service-crane/form';
import { MotorizedExcavatorDozerForm } from '../excavator-dozer/form';

type UnifiedMotorizedTransportFormProps =
	ActionDialogProps<MechanizedTransportUnitPriceGroupDto> & {
		isDuplicate?: boolean;
		defaultVehicleType?: string;
	};

// Helper: map sections for a specific vehicle type
const getMappedRowForVehicleType = (
	row: MechanizedTransportUnitPriceGroupDto | undefined,
	vt: number,
) => {
	if (!row) return undefined;
	const filteredSections = (row.sections || []).filter(
		(s: any) => s.vehicleType === vt || (!s.vehicleType && vt === 1),
	);
	if (filteredSections.length === 0) return undefined;

	return {
		...row,
		sections: filteredSections,
		allProcesses: filteredSections.flatMap((section: any) =>
			(section.rows || []).map((r: any) => ({
				id: r.headerId,
				vehicleType: section.vehicleType,
				productionProcessId: section.productionProcessId,
				productionProcessName: section.productionProcessName,
				cargoTypeId: section.cargoTypeId,
				cargoTypeName: section.cargoTypeName,
				receivingLocationId: section.receivingLocationId,
				receivingLocationName: section.receivingLocationName,
				dumpingLocationId: section.dumpingLocationId,
				dumpingLocationName: section.dumpingLocationName,
				equipmentQuality: r.equipmentQuality,
				details: [
					{
						id: r.detailId,
						haulDistanceId: r.haulDistanceId,
						haulDistanceValue: r.haulDistanceValue,
						fuelUnitPrice: r.fuelUnitPrice,
						powerUnitPrice: r.powerUnitPrice,
						maintenanceUnitPrice: r.maintenanceUnitPrice,
					},
				],
			})),
		),
	};
};

export function UnifiedMotorizedTransportForm(
	props: UnifiedMotorizedTransportFormProps,
) {
	const { row, isDuplicate = false, data } = props;
	const popup = usePopup();
	const { setOpen } = useDialog();

	// In Edit/Duplicate mode, find all vehicle types present in row.sections
	const editVehicleKeys = useMemo(() => {
		if (!row?.sections || row.sections.length === 0) return ['scania'];
		const types = Array.from(
			new Set(row.sections.map((s: any) => s.vehicleType || 1)),
		);
		const keys = types.map((vt) =>
			vt === 1
				? 'scania'
				: vt === 2
					? 'vacuum-truck'
					: vt === 3
						? 'service-crane'
						: vt === 4
							? 'excavator-dozer'
							: 'scania',
		);
		return keys.length > 0 ? keys : ['scania'];
	}, [row]);

	// Master form for common startMonth, endMonth, vehicleTypes
	const masterForm = useForm<{
		startMonth: string;
		endMonth: string;
		vehicleTypes: string[];
	}>({
		defaultValues: {
			startMonth: row?.startMonth?.substring(0, 7) || '',
			endMonth: row?.endMonth?.substring(0, 7) || '',
			vehicleTypes: row ? editVehicleKeys : ['scania'],
		},
	});

	const watchedStartMonth = useWatch({
		control: masterForm.control,
		name: 'startMonth',
	});
	const watchedEndMonth = useWatch({
		control: masterForm.control,
		name: 'endMonth',
	});
	const watchedVehicleTypes =
		useWatch({
			control: masterForm.control,
			name: 'vehicleTypes',
		}) || [];

	const selectedVehicleKeys = row ? editVehicleKeys : watchedVehicleTypes;

	// Individual sub-form refs for imperative submit
	const scaniaRef = useRef<MotorizedSubFormHandle>(null);
	const vacuumTruckRef = useRef<MotorizedSubFormHandle>(null);
	const serviceCraneRef = useRef<MotorizedSubFormHandle>(null);
	const excavatorDozerRef = useRef<MotorizedSubFormHandle>(null);

	// Map separate row data per vehicle type
	const mappedScaniaRow = useMemo(
		() => getMappedRowForVehicleType(row, 1),
		[row],
	);
	const mappedVacuumRow = useMemo(
		() => getMappedRowForVehicleType(row, 2),
		[row],
	);
	const mappedCraneRow = useMemo(
		() => getMappedRowForVehicleType(row, 3),
		[row],
	);
	const mappedExcavatorRow = useMemo(
		() => getMappedRowForVehicleType(row, 4),
		[row],
	);

	const handleMasterSubmit = async () => {
		const { startMonth, endMonth } = masterForm.getValues();
		if (!startMonth) {
			popup.error('Vui lòng chọn Thời gian bắt đầu');
			return;
		}

		const promises: Promise<boolean>[] = [];
		if (selectedVehicleKeys.includes('scania') && scaniaRef.current) {
			promises.push(scaniaRef.current.submit(startMonth, endMonth));
		}
		if (
			selectedVehicleKeys.includes('excavator-dozer') &&
			excavatorDozerRef.current
		) {
			promises.push(excavatorDozerRef.current.submit(startMonth, endMonth));
		}
		if (
			selectedVehicleKeys.includes('service-crane') &&
			serviceCraneRef.current
		) {
			promises.push(serviceCraneRef.current.submit(startMonth, endMonth));
		}
		if (
			selectedVehicleKeys.includes('vacuum-truck') &&
			vacuumTruckRef.current
		) {
			promises.push(vacuumTruckRef.current.submit(startMonth, endMonth));
		}

		if (promises.length === 0) {
			popup.error('Vui lòng chọn ít nhất một nhóm vận tải cơ giới');
			return;
		}

		const results = await Promise.all(promises);
		const allSuccess = results.every(Boolean);

		if (allSuccess) {
			popup.success(
				row && !isDuplicate
					? 'Cập nhật đơn giá định mức thành công'
					: 'Thêm mới đơn giá định mức thành công',
			);
			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		}
	};

	return (
		<FormProvider
			context={masterForm as any}
			onSubmit={handleMasterSubmit}
			onInvalid={(errors) => {
				const firstErr = Object.values(errors)[0];
				popup.error(
					(firstErr as any)?.message ||
						'Vui lòng điền đầy đủ các thông tin bắt buộc',
				);
			}}
		>
			<div className='space-y-4'>
				{/* 1. THỜI GIAN CHUNG Ở ĐẦU FORM */}
				<FormRow>
					<FormMonthYear
						control={masterForm.control as any}
						name='startMonth'
						label='Thời gian bắt đầu'
						className='flex-1'
					/>
					<FormMonthYear
						control={masterForm.control as any}
						name='endMonth'
						label='Thời gian kết thúc'
						className='flex-1'
					/>
				</FormRow>

				{/* 2. CHỌN NHÓM VẬN TẢI CƠ GIỚI (MULTI-SELECT CHO TẠO MỚI) */}
				{!row && (
					<FormMultiSelect
						control={masterForm.control as any}
						name='vehicleTypes'
						label='Nhóm Vận tải cơ giới'
						placeholder='Chọn một hoặc nhiều nhóm vận tải cơ giới'
						options={VEHICLE_TYPE_OPTIONS}
					/>
				)}

				{/* 3. DÃY KHỐI FORM NHẬP LIỆU NỐI TIẾP THEO TỪNG NHÓM XE */}
				{selectedVehicleKeys.includes('scania') && (
					<MotorizedScaniaForm
						{...(props as any)}
						ref={scaniaRef}
						row={mappedScaniaRow}
						hideTimeRow={true}
						hideConfirmButton={true}
						sharedStartMonth={watchedStartMonth}
						sharedEndMonth={watchedEndMonth}
					/>
				)}

				{selectedVehicleKeys.includes('excavator-dozer') && (
					<MotorizedExcavatorDozerForm
						{...(props as any)}
						ref={excavatorDozerRef}
						row={mappedExcavatorRow}
						hideTimeRow={true}
						hideConfirmButton={true}
						sharedStartMonth={watchedStartMonth}
						sharedEndMonth={watchedEndMonth}
					/>
				)}

				{selectedVehicleKeys.includes('service-crane') && (
					<MotorizedServiceCraneForm
						{...(props as any)}
						ref={serviceCraneRef}
						row={mappedCraneRow}
						hideTimeRow={true}
						hideConfirmButton={true}
						sharedStartMonth={watchedStartMonth}
						sharedEndMonth={watchedEndMonth}
					/>
				)}

				{selectedVehicleKeys.includes('vacuum-truck') && (
					<MotorizedVacuumTruckForm
						{...(props as any)}
						ref={vacuumTruckRef}
						row={mappedVacuumRow}
						hideTimeRow={true}
						hideConfirmButton={true}
						sharedStartMonth={watchedStartMonth}
						sharedEndMonth={watchedEndMonth}
					/>
				)}

				{/* 4. KHỐI LƯU Ý CHUNG Ở CUỐI FORM */}
				<div className='mt-3 rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-300'>
					<div className='flex items-center gap-1.5 font-semibold text-blue-900 dark:text-blue-200'>
						<InfoIcon className='size-4 text-blue-600 dark:text-blue-400' />
						Lưu ý về Hệ số điều chỉnh đơn giá định mức (Cấu hình ở Danh mục):
					</div>
					<ul className='mt-1 list-disc space-y-0.5 pl-5 text-slate-700 dark:text-slate-300'>
						<li>
							Đơn giá nhiên liệu, SCTX tăng 5% theo công đoạn sản xuất, mùa mưa
							và loại hàng.
						</li>
						<li>
							Áp dụng hệ số điều chỉnh khi sản phẩm là Than, bùn, bã sàng, đá
							sàng đổ tại Kho 5 (Kho BHN) & Kho 6 (mức +75):
							<span className='font-medium'> Mức ≤ +65 (K = 1)</span>;
							<span className='font-medium'>
								{' '}
								+65 &lt; Mức ≤ +90 (K = 1,03)
							</span>
							;
							<span className='font-medium'> Mức &gt; +90 (K = 1,06)</span>.
						</li>
					</ul>
				</div>

				{/* 5. NÚT XÁC NHẬN / HUỶ DUY NHẤT Ở CUỐI FORM */}
				<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
			</div>
		</FormProvider>
	);
}

export default UnifiedMotorizedTransportForm;
