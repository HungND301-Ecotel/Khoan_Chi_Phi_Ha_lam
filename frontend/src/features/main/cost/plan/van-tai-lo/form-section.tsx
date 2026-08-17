import { CostPlanAdjustmentFactorInput } from '@/features/main/cost/plan/components/cost-plan-adjustment-factor-input';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import {
	DepartmentPlanFormSchema,
	TRANSPORT_PROCESS_ENTRY_DEFAULT,
} from '@/features/main/cost/plan/schema';
import { AdjustmentDetail } from '@/features/main/cost/plan/planed-maintain-cost/types';
import { CostPlanAdjustmentSelection } from '@/features/main/cost/plan/types';
import {
	detectTransportMode,
	formatAdjustmentOptionLabel,
	isConveyorMode,
	isMonorailMode,
	isOtherMode,
	isShaftMode,
} from './utils';
import { Department } from '@/features/main/catalog/department/columns';
import { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { XCircleIcon } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { UseFormReturn, useWatch } from 'react-hook-form';
import { NumericFormat } from 'react-number-format';

type FormSelectInputProps = {
	label?: string;
	value?: string;
	onValueChange: (value: string) => void;
	placeholder?: string;
	options: { value: string; label: string }[];
};

function FormSelectInput({
	label,
	value,
	onValueChange,
	placeholder,
	options,
}: FormSelectInputProps) {
	return (
		<div className='flex flex-col gap-1'>
			{label && <label className='text-xs font-medium text-gray-700'>{label}</label>}
			<Select value={value || ''} onValueChange={onValueChange}>
				<SelectTrigger className='h-9 w-full rounded-sm border border-[#999999] px-3 text-sm focus:border-primary focus:outline-none'>
					<SelectValue placeholder={placeholder} />
				</SelectTrigger>
				<SelectContent className='max-h-54'>
					{options.map((option) => (
						<SelectItem key={option.value} value={option.value}>
							{option.label}
						</SelectItem>
					))}
				</SelectContent>
			</Select>
		</div>
	);
}

export type ProductionProcessOption = {
	id: string;
	code?: string;
	name: string;
	processGroupId?: string;
	processGroupName?: string;
};

export type TransportProcessEntryCardProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	processIndex: number;
	process: ProductionProcessOption;
	transportRoutes: TransportRoute[];
	departments: Department[];
	contractCodes: {
		id: string;
		code: string;
		name: string;
		unitOfMeasureId?: string;
	}[];
	units: Unit[];
	adjustments?: AdjustmentDetail[];
	k1Adjustment?: AdjustmentDetail | null;
	k2Adjustment?: AdjustmentDetail | null;
};

function isK1Factor(factor: AdjustmentDetail) {
	const code = (factor.fixedKeyKey || factor.code || '').toUpperCase();
	const name = (factor.name || '').toUpperCase();
	const type = (factor as any).fixedKeyType ?? (factor as any).type;
	return (
		code === 'K1' ||
		code.startsWith('K1') ||
		name.includes('K1') ||
		name.includes('CHẤT LƯỢNG') ||
		type === 1
	);
}

function isK2Factor(factor: AdjustmentDetail) {
	const code = (factor.fixedKeyKey || factor.code || '').toUpperCase();
	const name = (factor.name || '').toUpperCase();
	const type = (factor as any).fixedKeyType ?? (factor as any).type;
	return (
		code === 'K2' ||
		code.startsWith('K2') ||
		name.includes('K2') ||
		name.includes('MÔI TRƯỜNG') ||
		name.includes('ĐIỀU KIỆN') ||
		type === 2
	);
}

export function TransportProcessEntryCard({
	form,
	monthIndex,
	processIndex,
	process,
	transportRoutes,
	departments,
	contractCodes,
	units,
	adjustments = [],
	k1Adjustment,
	k2Adjustment,
}: TransportProcessEntryCardProps) {
	const entryPath =
		`transportMonths.${monthIndex}.processes.${processIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedEntry: any = useWatch({
		control: form.control,
		name: entryPath as any,
	});

	const mode = detectTransportMode(process.code, process.name);
	const isConveyor = isConveyorMode(mode);
	const isShaft = isShaftMode(mode);
	const isMonorail = isMonorailMode(mode);
	const isOther = isOtherMode(mode);

	const [fetchedAdjustments, setFetchedAdjustments] = useState<
		AdjustmentDetail[]
	>([]);

	useEffect(() => {
		let mounted = true;
		if (process?.processGroupId) {
			api
				.get<AdjustmentDetail[]>(API.CATALOG.ADJUSTMENT.FACTOR.DETAILS, {
					processGroupId: process.processGroupId,
				})
				.then((res) => {
					if (!mounted) return;
					if (res.result?.length) {
						setFetchedAdjustments(res.result);
					}
				})
				.catch(() => {});
		}
		return () => {
			mounted = false;
		};
	}, [process?.processGroupId]);

	const availableAdjustments = useMemo(() => {
		if (fetchedAdjustments.length > 0) return fetchedAdjustments;
		if (process?.processGroupId && adjustments.length > 0) {
			const filtered = adjustments.filter(
				(a) => a.processGroupId === process.processGroupId,
			);
			if (filtered.length > 0) return filtered;
		}
		return adjustments;
	}, [fetchedAdjustments, adjustments, process?.processGroupId]);

	const computedK1Adjustment = useMemo(() => {
		return availableAdjustments.find(isK1Factor) || k1Adjustment || null;
	}, [availableAdjustments, k1Adjustment]);

	const computedK2Adjustment = useMemo(() => {
		return availableAdjustments.find(isK2Factor) || k2Adjustment || null;
	}, [availableAdjustments, k2Adjustment]);

	const routeIds: string[] = watchedEntry?.routeIds || [];
	const routeDepartmentIds: Record<string, string[]> =
		(watchedEntry?.routeDepartmentIds as any) || {};
	const contractCodeIds: string[] = watchedEntry?.contractCodeIds || [];
	const contractCodeQualityIds: Record<string, string[]> =
		(watchedEntry?.contractCodeQualityIds as any) || {};
	const items: any[] = watchedEntry?.items || [];

	const setItems = (newItems: typeof items) => {
		form.setValue(`${entryPath}.items` as any, newItems);
	};

	// Dọn dữ liệu con khi bỏ chọn Tuyến vận tải
	const prevRouteIdsRef = useRef<string[]>(routeIds);
	useEffect(() => {
		const prev = prevRouteIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(routeIds)) return;

		const removedIds = prev.filter((id) => !routeIds.includes(id));
		if (removedIds.length > 0) {
			if (isConveyor) {
				const newRouteDept = { ...routeDepartmentIds };
				removedIds.forEach((id) => delete newRouteDept[id]);
				form.setValue(`${entryPath}.routeDepartmentIds` as any, newRouteDept);
			}
			if (isShaft) {
				const currentItems = form.getValues(`${entryPath}.items` as any) || [];
				const filtered = (currentItems as any[]).filter(
					(item: any) => !removedIds.includes(item.transportRouteId || ''),
				);
				if (filtered.length !== currentItems.length) {
					form.setValue(`${entryPath}.items` as any, filtered);
				}
			}
		}
		prevRouteIdsRef.current = [...routeIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(routeIds)]);

	// Dọn dữ liệu con khi bỏ chọn Nhóm vật tư (áp dụng Monoray + Thiết bị khác)
	const prevContractIdsRef = useRef<string[]>(contractCodeIds);
	useEffect(() => {
		const prevContract = prevContractIdsRef.current;
		if (JSON.stringify(prevContract) === JSON.stringify(contractCodeIds)) return;

		const removedIds = prevContract.filter((id) => !contractCodeIds.includes(id));
		if (removedIds.length > 0) {
			if (isMonorail) {
				const nextQualityIds = { ...contractCodeQualityIds };
				removedIds.forEach((id) => delete nextQualityIds[id]);
				form.setValue(
					`${entryPath}.contractCodeQualityIds` as any,
					nextQualityIds,
				);
			}
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as any[]).filter((item: any) =>
				contractCodeIds.includes(item.contractCodeId || ''),
			);
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}

		prevContractIdsRef.current = [...contractCodeIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(contractCodeIds)]);

	// Dọn dữ liệu con khi bỏ chọn Chất lượng thiết bị của 1 Nhóm vật tư cụ thể (chỉ Monoray)
	const prevQualityIdsRef = useRef<Record<string, string[]>>(
		contractCodeQualityIds,
	);
	useEffect(() => {
		const prev = prevQualityIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(contractCodeQualityIds)) return;

		if (isMonorail) {
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as any[]).filter((item: any) => {
				if (!item.contractCodeId) return true;
				const qualitiesForCc = contractCodeQualityIds[item.contractCodeId] || [];
				return qualitiesForCc.includes(item.equipmentQuality || '');
			});
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}

		prevQualityIdsRef.current = contractCodeQualityIds;
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(contractCodeQualityIds)]);

	return (
		<div className='flex flex-col gap-3 rounded-sm border border-[#cccccc] bg-[#fafafa] p-4'>
			<div className='font-bold text-gray-800 text-sm'>
				{process.code ? `${process.code} - ${process.name}` : process.name}
			</div>

			{/* ================= MODE 1: CONVEYOR (Vận tải đá / than qua băng tải) ================= */}
			{isConveyor && (
				<div className='flex flex-col gap-4'>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.routeIds` as any}
						label='Tuyến vận tải'
						placeholder='Chọn tuyến vận tải'
						options={transportRoutes.map((r) => ({
							value: r.id,
							label: r.code ? `${r.code} - ${r.name}` : r.name,
						}))}
					/>

					{routeIds.map((routeId: string) => {
						const routeObj = transportRoutes.find((r) => r.id === routeId);
						const deptsForRoute = Array.isArray(routeDepartmentIds[routeId])
							? routeDepartmentIds[routeId]
							: typeof routeDepartmentIds[routeId] === 'string'
								? [routeDepartmentIds[routeId] as string]
								: [];

						return (
							<div
								key={routeId}
								className='flex flex-col gap-3 rounded-sm border border-[#e0e0e0] bg-white p-4'
							>
								<div className='font-semibold text-gray-800 text-xs'>
									{routeObj?.code ? `${routeObj.code} - ${routeObj.name}` : routeObj?.name}
								</div>

								<FormMultiSelect
									control={formControl}
									name={`${entryPath}.routeDepartmentIds.${routeId}` as any}
									label='Đơn vị áp dụng cho tuyến này'
									placeholder='Chọn đơn vị'
									options={departments.map((d) => ({
										value: d.id,
										label: d.code ? `${d.code} - ${d.name}` : d.name,
									}))}
								/>

								{deptsForRoute.length > 0 && (
									<div className='overflow-x-auto rounded-sm border border-[#e0e0e0]'>
										<table className='w-full text-left text-sm'>
											<thead className='border-b border-[#e0e0e0] bg-[#f5f5f5] text-xs font-semibold text-gray-700 uppercase'>
												<tr>
													<th className='min-w-[180px] px-3 py-2'>Đơn vị</th>
													<th className='min-w-[140px] px-3 py-2'>Sản lượng</th>
													<th className='min-w-[120px] px-3 py-2'>ĐVT</th>
													<th className='min-w-[150px] px-3 py-2'>K1</th>
													<th className='min-w-[150px] px-3 py-2'>K2</th>
												</tr>
											</thead>
											<tbody className='divide-y divide-[#e0e0e0] bg-white'>
												{deptsForRoute.map((deptId: string) => {
													const deptObj = departments.find((d) => d.id === deptId);
													const itemIndex = items.findIndex(
														(it: any) =>
															it.transportRouteId === routeId &&
															it.departmentId === deptId,
													);
													const currentItem: any =
														itemIndex >= 0
															? items[itemIndex]
															: { transportRouteId: routeId, departmentId: deptId };

													return (
														<tr key={deptId} className='hover:bg-gray-50'>
															<td className='px-3 py-2'>
																<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
																	{deptObj?.code ? `${deptObj.code} - ${deptObj.name}` : deptObj?.name || deptId}
																</div>
															</td>
															<td className='px-3 py-2'>
																<NumericFormat
																	className='h-9 w-full rounded-sm border border-[#999999] px-3 text-sm focus:border-primary focus:outline-none'
																	placeholder='Nhập sản lượng'
																	decimalSeparator=','
																	thousandSeparator='.'
																	value={currentItem.productionMeters ?? ''}
																	onValueChange={(values) => {
																		const meters = values.floatValue ?? 0;
																		const next = [...items];
																		const idx = next.findIndex(
																			(it) =>
																				it.transportRouteId === routeId &&
																				it.departmentId === deptId,
																		);
																		if (idx >= 0) {
																			next[idx] = { ...next[idx], productionMeters: meters };
																		} else {
																			next.push({
																				transportRouteId: routeId,
																				departmentId: deptId,
																				productionMeters: meters,
																			});
																		}
																		setItems(next);
																	}}
																/>
															</td>
															<td className='px-3 py-2'>
																<FormSelectInput
																	value={currentItem.unitOfMeasureId || ''}
																	onValueChange={(unitId: string) => {
																		const next = [...items];
																		const idx = next.findIndex(
																			(it) =>
																				it.transportRouteId === routeId &&
																				it.departmentId === deptId,
																		);
																		if (idx >= 0) {
																			next[idx] = { ...next[idx], unitOfMeasureId: unitId };
																		} else {
																			next.push({
																				transportRouteId: routeId,
																				departmentId: deptId,
																				unitOfMeasureId: unitId,
																			});
																		}
																		setItems(next);
																	}}
																	placeholder='Chọn ĐVT'
																	options={units.map((u) => ({
																		value: u.id,
																		label: u.name,
																	}))}
																/>
															</td>
															<td className='px-3 py-2'>
																<CostPlanAdjustmentFactorInput
																	label=''
																	placeholder='Nhập K1'
																	customPlaceholder='Nhập K1'
																	adjustmentFactorId={computedK1Adjustment?.id ?? ''}
																	value={currentItem.k1Factor}
																	options={(
																		computedK1Adjustment?.adjustmentFactorDescriptions ?? []
																	).map((ad: any) => {
																		const val =
																			ad.maintenanceAdjustmentValue ??
																			ad.electricityAdjustmentValue ??
																			ad.value;
																		return {
																			label: formatAdjustmentOptionLabel(ad.description, val),
																			sortValue: val ?? 0,
																			value: ad.id,
																		};
																	})}
																	onChange={(value: CostPlanAdjustmentSelection) => {
																		const next = [...items];
																		const idx = next.findIndex(
																			(it) =>
																				it.transportRouteId === routeId &&
																				it.departmentId === deptId,
																		);
																		if (idx >= 0) {
																			next[idx] = { ...next[idx], k1Factor: value };
																		} else {
																			next.push({
																				transportRouteId: routeId,
																				departmentId: deptId,
																				k1Factor: value,
																			});
																		}
																		setItems(next);
																	}}
																/>
															</td>
															<td className='px-3 py-2'>
																<CostPlanAdjustmentFactorInput
																	label=''
																	placeholder='Nhập K2'
																	customPlaceholder='Nhập K2'
																	adjustmentFactorId={computedK2Adjustment?.id ?? ''}
																	value={currentItem.k2Factor}
																	options={(
																		computedK2Adjustment?.adjustmentFactorDescriptions ?? []
																	).map((ad: any) => {
																		const val =
																			ad.maintenanceAdjustmentValue ??
																			ad.electricityAdjustmentValue ??
																			ad.value;
																		return {
																			label: formatAdjustmentOptionLabel(ad.description, val),
																			sortValue: val ?? 0,
																			value: ad.id,
																		};
																	})}
																	onChange={(value: CostPlanAdjustmentSelection) => {
																		const next = [...items];
																		const idx = next.findIndex(
																			(it) =>
																				it.transportRouteId === routeId &&
																				it.departmentId === deptId,
																		);
																		if (idx >= 0) {
																			next[idx] = { ...next[idx], k2Factor: value };
																		} else {
																			next.push({
																				transportRouteId: routeId,
																				departmentId: deptId,
																				k2Factor: value,
																			});
																		}
																		setItems(next);
																	}}
																/>
															</td>
														</tr>
													);
												})}
											</tbody>
										</table>
									</div>
								)}
							</div>
						);
					})}
				</div>
			)}

			{/* ================= MODE 2: SHAFT (Vận tải trục đá / đá lẫn than) ================= */}
			{isShaft && (
				<div className='flex flex-col gap-3'>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.routeIds` as any}
						label='Tuyến vận tải'
						placeholder='Chọn tuyến vận tải'
						options={transportRoutes.map((r) => ({
							value: r.id,
							label: r.code ? `${r.code} - ${r.name}` : r.name,
						}))}
					/>

					{routeIds.length > 0 && (
						<div className='overflow-x-auto rounded-sm border border-[#e0e0e0] bg-white'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-[#e0e0e0] bg-[#f5f5f5] text-xs font-semibold text-gray-700 uppercase'>
									<tr>
										<th className='px-4 py-2.5'>Tuyến vận tải</th>
										<th className='min-w-[180px] px-4 py-2.5'>Sản lượng</th>
										<th className='min-w-[140px] px-4 py-2.5'>ĐVT</th>
									</tr>
								</thead>
								<tbody className='divide-y divide-[#e0e0e0]'>
									{routeIds.map((routeId: string) => {
										const routeObj = transportRoutes.find((r: any) => r.id === routeId);
										const itemIndex = items.findIndex((it: any) => it.transportRouteId === routeId);
										const currentItem: any =
											itemIndex >= 0 ? items[itemIndex] : { transportRouteId: routeId };

										return (
											<tr key={routeId} className='hover:bg-gray-50'>
												<td className='px-4 py-2.5'>
													<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
														{routeObj?.code ? `${routeObj.code} - ${routeObj.name}` : routeObj?.name}
													</div>
												</td>
												<td className='px-4 py-2.5'>
													<NumericFormat
														className='h-9 w-full rounded-sm border border-[#999999] px-3 text-sm focus:border-primary focus:outline-none'
														placeholder='Nhập sản lượng'
														decimalSeparator=','
														thousandSeparator='.'
														value={currentItem.productionMeters ?? ''}
														onValueChange={(values) => {
															const meters = values.floatValue ?? 0;
															const next = [...items];
															if (itemIndex >= 0) {
																next[itemIndex] = { ...next[itemIndex], productionMeters: meters };
															} else {
																next.push({
																	transportRouteId: routeId,
																	productionMeters: meters,
																});
															}
															setItems(next);
														}}
													/>
												</td>
												<td className='px-4 py-2.5'>
													<FormSelectInput
														value={currentItem.unitOfMeasureId || ''}
														onValueChange={(unitId: string) => {
															const next = [...items];
															if (itemIndex >= 0) {
																next[itemIndex] = { ...next[itemIndex], unitOfMeasureId: unitId };
															} else {
																next.push({
																	transportRouteId: routeId,
																	unitOfMeasureId: unitId,
																});
															}
															setItems(next);
														}}
														placeholder='Chọn ĐVT'
														options={units.map((u: any) => ({
															value: u.id,
															label: u.name,
														}))}
													/>
												</td>
											</tr>
										);
									})}
								</tbody>
							</table>
						</div>
					)}
				</div>
			)}

			{/* ================= MODE 3: MONORAIL (Monoray vận chuyển người / vật liệu) ================= */}
			{isMonorail && (
				<div className='flex flex-col gap-4'>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.contractCodeIds` as any}
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn nhóm vật tư, tài sản'
						options={contractCodes.map((c: any) => ({
							value: c.id,
							label: c.code ? `${c.code} - ${c.name}` : c.name,
						}))}
					/>

					{contractCodeIds.map((ccId: string) => {
						const ccObj = contractCodes.find((c: any) => c.id === ccId);
						const qualitiesForCc = contractCodeQualityIds[ccId] || [];

						return (
							<div
								key={ccId}
								className='flex flex-col gap-3 rounded-sm border border-[#e0e0e0] bg-white p-4'
							>
								<div className='font-semibold text-gray-800 text-xs'>
									{ccObj?.code ? `${ccObj.code} - ${ccObj.name}` : ccObj?.name}
								</div>

								<FormMultiSelect
									control={formControl}
									name={`${entryPath}.contractCodeQualityIds.${ccId}` as any}
									label='Chất lượng thiết bị'
									placeholder='Chọn chất lượng thiết bị'
									options={[
										{ value: 'A', label: 'Thiết bị loại A' },
										{ value: 'B', label: 'Thiết bị loại B' },
										{ value: 'C', label: 'Thiết bị loại C' },
									]}
								/>

								{qualitiesForCc.length > 0 && (
									<div className='overflow-x-auto rounded-sm border border-[#e0e0e0]'>
										<table className='w-full text-left text-sm'>
											<thead className='border-b border-[#e0e0e0] bg-[#f5f5f5] text-xs font-semibold text-gray-700 uppercase'>
												<tr>
													<th className='min-w-[160px] px-3 py-2'>Chất lượng thiết bị</th>
													<th className='min-w-[140px] px-3 py-2'>Sản lượng</th>
													<th className='min-w-[120px] px-3 py-2'>ĐVT</th>
												</tr>
											</thead>
											<tbody className='divide-y divide-[#e0e0e0] bg-white'>
												{qualitiesForCc.map((quality: string) => {
													const itemIndex = items.findIndex(
														(it: any) =>
															it.contractCodeId === ccId &&
															it.equipmentQuality === quality,
													);
													const currentItem: any =
														itemIndex >= 0
															? items[itemIndex]
															: { contractCodeId: ccId, equipmentQuality: quality };

													const defaultUnitId = ccObj?.unitOfMeasureId || currentItem.unitOfMeasureId || '';

													return (
														<tr key={quality} className='hover:bg-gray-50'>
															<td className='px-3 py-2'>
																<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
																	Thiết bị loại {quality}
																</div>
															</td>
															<td className='px-3 py-2'>
																<NumericFormat
																	className='h-9 w-full rounded-sm border border-[#999999] px-3 text-sm focus:border-primary focus:outline-none'
																	placeholder='Nhập sản lượng'
																	decimalSeparator=','
																	thousandSeparator='.'
																	value={currentItem.productionMeters ?? ''}
																	onValueChange={(values) => {
																		const meters = values.floatValue ?? 0;
																		const next = [...items];
																		if (itemIndex >= 0) {
																			next[itemIndex] = {
																				...next[itemIndex],
																				productionMeters: meters,
																				unitOfMeasureId: next[itemIndex].unitOfMeasureId || defaultUnitId,
																			};
																		} else {
																			next.push({
																				contractCodeId: ccId,
																				equipmentQuality: quality,
																				productionMeters: meters,
																				unitOfMeasureId: defaultUnitId,
																			});
																		}
																		setItems(next);
																	}}
																/>
															</td>
															<td className='px-3 py-2'>
																<FormSelectInput
																	value={currentItem.unitOfMeasureId || defaultUnitId}
																	onValueChange={(unitId: string) => {
																		const next = [...items];
																		if (itemIndex >= 0) {
																			next[itemIndex] = {
																				...next[itemIndex],
																				unitOfMeasureId: unitId,
																			};
																		} else {
																			next.push({
																				contractCodeId: ccId,
																				equipmentQuality: quality,
																				unitOfMeasureId: unitId,
																			});
																		}
																		setItems(next);
																	}}
																	placeholder='Chọn ĐVT'
																	options={units.map((u) => ({
																		value: u.id,
																		label: u.name,
																	}))}
																/>
															</td>
														</tr>
													);
												})}
											</tbody>
										</table>
									</div>
								)}
							</div>
						);
					})}
				</div>
			)}

			{/* ================= MODE 4: OTHER (Thiết bị khác) ================= */}
			{isOther && (
				<div className='flex flex-col gap-4'>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.contractCodeIds` as any}
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn nhóm vật tư, tài sản'
						options={contractCodes.map((c) => ({
							value: c.id,
							label: c.code ? `${c.code} - ${c.name}` : c.name,
						}))}
					/>

					{contractCodeIds.length > 0 && (
						<div className='overflow-x-auto rounded-sm border border-[#e0e0e0] bg-white'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-[#e0e0e0] bg-[#f5f5f5] text-xs font-semibold text-gray-700 uppercase'>
									<tr>
										<th className='px-3 py-2'>Nhóm vật tư</th>
										<th className='min-w-[180px] px-3 py-2'>Sản lượng</th>
										<th className='min-w-[140px] px-3 py-2'>ĐVT</th>
									</tr>
								</thead>
								<tbody className='divide-y divide-[#e0e0e0] bg-white'>
									{contractCodeIds.map((ccId: string) => {
										const ccObj = contractCodes.find((c: any) => c.id === ccId);
										const itemIndex = items.findIndex((it: any) => it.contractCodeId === ccId);
										const currentItem: any =
											itemIndex >= 0 ? items[itemIndex] : { contractCodeId: ccId };
										const defaultUnitId = ccObj?.unitOfMeasureId || currentItem.unitOfMeasureId || '';

										return (
											<tr key={ccId} className='hover:bg-gray-50'>
												<td className='px-3 py-2'>
													<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
														{ccObj?.code ? `${ccObj.code} - ${ccObj.name}` : ccObj?.name}
													</div>
												</td>
												<td className='px-3 py-2'>
													<NumericFormat
														className='h-9 w-full rounded-sm border border-[#999999] px-3 text-sm focus:border-primary focus:outline-none'
														placeholder='Nhập sản lượng'
														decimalSeparator=','
														thousandSeparator='.'
														value={currentItem.productionMeters ?? ''}
														onValueChange={(values) => {
															const meters = values.floatValue ?? 0;
															const next = [...items];
															if (itemIndex >= 0) {
																next[itemIndex] = {
																	...next[itemIndex],
																	productionMeters: meters,
																	unitOfMeasureId: next[itemIndex].unitOfMeasureId || defaultUnitId,
																};
															} else {
																next.push({
																	contractCodeId: ccId,
																	productionMeters: meters,
																	unitOfMeasureId: defaultUnitId,
																});
															}
															setItems(next);
														}}
													/>
												</td>
												<td className='px-3 py-2'>
													<FormSelectInput
														value={currentItem.unitOfMeasureId || defaultUnitId}
														onValueChange={(unitId: string) => {
															const next = [...items];
															if (itemIndex >= 0) {
																next[itemIndex] = {
																	...next[itemIndex],
																	unitOfMeasureId: unitId,
																};
															} else {
																next.push({
																	contractCodeId: ccId,
																	unitOfMeasureId: unitId,
																});
															}
															setItems(next);
														}}
														placeholder='Chọn ĐVT'
														options={units.map((u) => ({
															value: u.id,
															label: u.name,
														}))}
													/>
												</td>
											</tr>
										);
									})}
								</tbody>
							</table>
						</div>
					)}
				</div>
			)}
		</div>
	);
}

export type TransportMonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	canRemove: boolean;
	onRemoveMonth: () => void;
	productionProcesses: ProductionProcessOption[];
	transportRoutes: TransportRoute[];
	departments: Department[];
	contractCodes: {
		id: string;
		code: string;
		name: string;
		unitOfMeasureId?: string;
	}[];
	units: Unit[];
	adjustments?: AdjustmentDetail[];
	k1Adjustment?: AdjustmentDetail | null;
	k2Adjustment?: AdjustmentDetail | null;
};

export function TransportMonthSection({
	form,
	monthIndex,
	canRemove,
	onRemoveMonth,
	productionProcesses,
	transportRoutes,
	departments,
	contractCodes,
	units,
	adjustments = [],
	k1Adjustment,
	k2Adjustment,
}: TransportMonthSectionProps) {
	const monthPath = `transportMonths.${monthIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;
	const watchedMonth = useWatch({
		control: form.control,
		name: monthPath,
	}) as DepartmentPlanFormSchema['transportMonths'][number];

	const processIds = watchedMonth?.processIds || [];
	const processes = watchedMonth?.processes || [];

	const prevProcessIdsRef = useRef<string[]>(processIds);
	useEffect(() => {
		const prev = prevProcessIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(processIds)) return;

		const currentProcesses =
			form.getValues(`transportMonths.${monthIndex}.processes`) || [];
		const addedIds = processIds.filter((id) => !prev.includes(id));
		const keptProcesses = currentProcesses.filter((entry) =>
			processIds.includes(entry.productionProcessId || ''),
		);
		const nextProcesses = [
			...keptProcesses,
			...addedIds.map((id) => ({
				...TRANSPORT_PROCESS_ENTRY_DEFAULT,
				productionProcessId: id,
			})),
		];

		form.setValue(`transportMonths.${monthIndex}.processes`, nextProcesses);
		prevProcessIdsRef.current = [...processIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(processIds), monthIndex]);

	return (
		<div className='flex flex-col gap-4 rounded-sm border border-[#999999] p-4'>
			<div className='flex items-center justify-between gap-4'>
				<FormMonthYear
					control={form.control}
					name={`transportMonths.${monthIndex}.month`}
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

			{typeof form.formState.errors.transportMonths?.[monthIndex]?.month
				?.message === 'string' && (
				<FieldError
					errors={[form.formState.errors.transportMonths?.[monthIndex]?.month]}
				/>
			)}

			<FormCheckBox
				control={form.control}
				name={`transportMonths.${monthIndex}.lowValuePerishableSupply`}
				label='Chi phí vật tư mau hỏng rẻ tiền (đồng/tháng)'
			/>

			<FormMultiSelect
				control={formControl}
				name={`transportMonths.${monthIndex}.processIds` as any}
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={productionProcesses.map((p) => ({
					value: p.id,
					label: p.code ? `${p.code} - ${p.name}` : p.name,
				}))}
			/>

			{typeof form.formState.errors.transportMonths?.[monthIndex]?.processIds
				?.message === 'string' && (
				<FieldError
					errors={[
						form.formState.errors.transportMonths?.[monthIndex]?.processIds,
					]}
				/>
			)}

			{processes.length > 0 && (
				<div className='flex flex-col gap-3'>
					{processes.map((entry, processIndex) => {
						const process = productionProcesses.find(
							(p) => p.id === entry.productionProcessId,
						);
						if (!process) return null;

						return (
							<TransportProcessEntryCard
								key={entry.productionProcessId}
								form={form}
								monthIndex={monthIndex}
								processIndex={processIndex}
								process={process}
								transportRoutes={transportRoutes}
								departments={departments}
								contractCodes={contractCodes}
								units={units}
								adjustments={adjustments}
								k1Adjustment={k1Adjustment}
								k2Adjustment={k2Adjustment}
							/>
						);
					})}
				</div>
			)}
		</div>
	);
}
