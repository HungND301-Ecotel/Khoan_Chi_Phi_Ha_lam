import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { FieldError } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { Department } from '@/features/main/catalog/department/columns';
import type { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import {
	detectTransportMode,
	isConveyorMode,
	isMonorailMode,
	isOtherMode,
	isShaftMode,
	isRouteOnlyProcess,
	normalizeRouteOnlyItems,
} from '@/features/main/cost/plan/van-tai-lo/utils';
import {
	TRANSPORT_PROCESS_ENTRY_DEFAULT,
	type ProductionFormSchema,
	type ProductionGroupSchema,
	type TransportProcessEntrySchema,
} from '@/features/main/cost/producttion/production/production-form-schema';
import { formatNumber } from '@/lib/utils';
import { useEffect, useRef } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';

export type ProductionProcessOption = {
	id: string;
	code?: string;
	name: string;
	processGroupName?: string;
};

type ContractCodeOption = { id: string; code: string; name: string };

function toMeters(value: unknown) {
	return typeof value === 'number' && !Number.isNaN(value) ? value : 0;
}

// Tổng sản lượng kế hoạch của 1 công đoạn = cộng kế hoạch các Tuyến/Nhóm VTTS đang chọn
function sumProcessPlanMeters(entry?: TransportProcessEntrySchema) {
	const planMeters: Record<string, number> = entry?.planMeters || {};
	return [...(entry?.routeIds || []), ...(entry?.equipmentIds || [])].reduce(
		(sum, key) => sum + toMeters(planMeters[key]),
		0,
	);
}

type PlanActualFieldsProps = {
	planValue?: number;
	actualValue: number;
	onPlanChange?: (value: number | undefined) => void;
};

// Cặp Sản lượng kế hoạch/thực tế: có onPlanChange thì kế hoạch là ô nhập (cấp Tuyến/Nhóm VTTS),
// không thì chỉ hiển thị tổng (cấp Công đoạn). Thực tế luôn là tổng các dòng con.
function PlanActualFields({
	planValue,
	actualValue,
	onPlanChange,
}: PlanActualFieldsProps) {
	return (
		<>
			<div className='flex-1 space-y-2'>
				<Label>Sản lượng kế hoạch</Label>
				{onPlanChange ? (
					<FormNumberInput
						value={planValue}
						onValueChange={onPlanChange}
						placeholder='Nhập sản lượng kế hoạch'
					/>
				) : (
					<Input
						readOnly
						className='read-only:bg-[#f5f5f5] read-only:text-gray-600'
						value={formatNumber(planValue || 0)}
					/>
				)}
			</div>
			<div className='flex-1 space-y-2'>
				<Label>Sản lượng thực tế</Label>
				<Input
					readOnly
					className='read-only:bg-[#f5f5f5] read-only:text-gray-600'
					value={formatNumber(actualValue)}
				/>
			</div>
		</>
	);
}

type VanTaiLoProcessEntryCardProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	processIndex: number;
	process: ProductionProcessOption;
	transportRoutes: TransportRoute[];
	contractCodes: ContractCodeOption[];
	departments: Department[];
};

// 1 khối ứng với 1 Công đoạn sản xuất cụ thể đã chọn trong nhóm VTL/VTCG: tự hiện đúng field
// (Tuyến vận tải / Nhóm vật tư) theo loại vận tải của công đoạn đó — không có K1/K2, không có
// ĐVT, vì ProductionOutputTransportLine chỉ ghi nhận sản lượng thực tế, không cần đủ field để dò
// đơn giá như Kế hoạch. Riêng 2 chiều khoán chi tiết thật sự vẫn giữ nguyên vì Kế hoạch định giá
// riêng theo đó: công đoạn theo Tuyến (Băng tải, Trục tải/Tời cáp) giữ "Đơn vị áp dụng cho
// tuyến này", công đoạn Monoray giữ "Chất lượng thiết bị" (A/B/C) — để sản lượng thực tế đối
// chiếu/quyết toán đúng theo từng đơn vị/chất lượng thiết bị.
function VanTaiLoProcessEntryCard({
	form,
	groupIndex,
	processIndex,
	process,
	transportRoutes,
	contractCodes,
	departments,
}: VanTaiLoProcessEntryCardProps) {
	const entryPath =
		`groups.${groupIndex}.transportProcesses.${processIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedEntry = useWatch({
		control: form.control,
		name: entryPath as any,
	}) as TransportProcessEntrySchema;

	const mode = detectTransportMode(process.code, process.name);
	const isConveyor = isConveyorMode(mode);
	const isShaft = isShaftMode(mode);
	const isMonorail = isMonorailMode(mode);
	const isOther = isOtherMode(mode);
	// Vận tải trục: chỉ chọn Tuyến, không có Đơn vị — mỗi tuyến 1 dòng sản lượng
	const routeOnly = isRouteOnlyProcess(process.code, process.name);

	const routeIds = watchedEntry?.routeIds || [];
	const routeDepartmentIds: Record<string, string[]> =
		watchedEntry?.routeDepartmentIds || {};
	const equipmentIds = watchedEntry?.equipmentIds || [];
	const equipmentQualities = watchedEntry?.equipmentQualities || [];
	const equipmentQualitiesMap: Record<string, string[]> =
		watchedEntry?.equipmentQualitiesMap || {};
	const items = watchedEntry?.items || [];
	const planMeters: Record<string, number> = watchedEntry?.planMeters || {};

	// Kế hoạch khoán theo từng Tuyến/Nhóm VTTS — key là routeId hoặc equipmentId
	const setPlan = (key: string, value: number | undefined) => {
		const next = { ...planMeters };
		if (value === undefined || Number.isNaN(value)) delete next[key];
		else next[key] = value;
		form.setValue(`${entryPath}.planMeters` as any, next);
	};
	const sumActual = (predicate: (item: (typeof items)[number]) => boolean) =>
		items
			.filter(predicate)
			.reduce((sum, item) => sum + toMeters(item.productionMeters), 0);
	const removePlanKeys = (removedIds: string[]) => {
		const current: Record<string, number> =
			form.getValues(`${entryPath}.planMeters` as any) || {};
		if (!removedIds.some((id) => id in current)) return;
		const next = { ...current };
		removedIds.forEach((id) => delete next[id]);
		form.setValue(`${entryPath}.planMeters` as any, next);
	};

	const setItems = (newItems: typeof items) => {
		form.setValue(
			`${entryPath}.items` as any,
			routeOnly
				? normalizeRouteOnlyItems(newItems, 'routeDepartmentId')
				: newItems,
		);
	};

	// VTT: cập nhật sản lượng thực tế của 1 tuyến (mỗi tuyến 1 dòng)
	const updateRouteOnlyItem = (routeId: string, productionMeters: number) => {
		const next = [...items];
		const idx = next.findIndex((it) => it.transportRouteId === routeId);
		if (idx >= 0) {
			next[idx] = { ...next[idx], productionMeters };
		} else {
			next.push({ transportRouteId: routeId, productionMeters });
		}
		setItems(next);
	};

	const prevRouteIdsRef = useRef<string[]>(routeIds);
	useEffect(() => {
		const prev = prevRouteIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(routeIds)) return;

		const removedIds = prev.filter((id) => !routeIds.includes(id));
		if (removedIds.length > 0) {
			removePlanKeys(removedIds);
			if (isConveyor || isShaft) {
				const nextRouteDept = { ...routeDepartmentIds };
				removedIds.forEach((id) => delete nextRouteDept[id]);
				form.setValue(`${entryPath}.routeDepartmentIds` as any, nextRouteDept);
			}
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as typeof items).filter(
				(item) => !removedIds.includes(item.transportRouteId || ''),
			);
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}
		prevRouteIdsRef.current = [...routeIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(routeIds)]);

	const prevRouteDepartmentIdsRef =
		useRef<Record<string, string[]>>(routeDepartmentIds);
	useEffect(() => {
		const prev = prevRouteDepartmentIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(routeDepartmentIds)) return;

		if ((isConveyor || isShaft) && !routeOnly) {
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as typeof items).filter((item) => {
				if (!item.transportRouteId) return true;
				const deptsForRoute = routeDepartmentIds[item.transportRouteId] || [];
				return deptsForRoute.includes(item.routeDepartmentId || '');
			});
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}
		prevRouteDepartmentIdsRef.current = routeDepartmentIds;
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(routeDepartmentIds)]);

	const prevEquipmentIdsRef = useRef<string[]>(equipmentIds);
	useEffect(() => {
		const prev = prevEquipmentIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(equipmentIds)) return;

		const removedIds = prev.filter((id) => !equipmentIds.includes(id));
		if (removedIds.length > 0) {
			removePlanKeys(removedIds);
			if (isMonorail) {
				const nextEqQualities = { ...equipmentQualitiesMap };
				removedIds.forEach((id) => delete nextEqQualities[id]);
				form.setValue(
					`${entryPath}.equipmentQualitiesMap` as any,
					nextEqQualities,
				);
			}
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as typeof items).filter(
				(item) => !removedIds.includes(item.equipmentId || ''),
			);
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}
		prevEquipmentIdsRef.current = [...equipmentIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(equipmentIds)]);

	const prevEquipmentQualitiesMapRef = useRef<Record<string, string[]>>(
		equipmentQualitiesMap,
	);
	useEffect(() => {
		const prev = prevEquipmentQualitiesMapRef.current;
		if (JSON.stringify(prev) === JSON.stringify(equipmentQualitiesMap)) return;

		if (isMonorail) {
			const currentItems = form.getValues(`${entryPath}.items` as any) || [];
			const filtered = (currentItems as typeof items).filter((item) => {
				if (!item.equipmentId) return true;
				const qualitiesForEq = equipmentQualitiesMap[item.equipmentId] || [];
				return qualitiesForEq.includes(item.equipmentQuality || '');
			});
			if (filtered.length !== currentItems.length) {
				form.setValue(`${entryPath}.items` as any, filtered);
			}
		}
		prevEquipmentQualitiesMapRef.current = equipmentQualitiesMap;
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(equipmentQualitiesMap)]);

	return (
		<div className='space-y-3 rounded-lg border border-gray-300 bg-gray-50/50 p-4'>
			<div className='text-sm font-semibold text-gray-800'>
				{process.code ? `${process.code} - ${process.name}` : process.name}
			</div>

			<div className='flex flex-col gap-3 md:flex-row'>
				<PlanActualFields
					planValue={sumProcessPlanMeters(watchedEntry)}
					actualValue={sumActual(() => true)}
				/>
			</div>

			{(isConveyor || isShaft) && (
				<>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.routeIds` as any}
						label='Tuyến vận tải'
						placeholder='Chọn Tuyến vận tải'
						options={transportRoutes.map((r) => ({
							value: r.id,
							label: `${r.code} - ${r.name}`,
						}))}
					/>

					{/* VTT: 1 bảng duy nhất, mỗi tuyến 1 dòng — không có Đơn vị */}
					{routeOnly && routeIds.length > 0 && (
						<div className='overflow-hidden rounded-md border border-gray-200'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
									<tr>
										<th className='min-w-[240px] px-4 py-3'>Tuyến vận tải</th>
										<th className='min-w-[160px] px-4 py-3'>
											Sản lượng kế hoạch
										</th>
										<th className='min-w-[160px] px-4 py-3'>
											Sản lượng thực tế
										</th>
									</tr>
								</thead>
								<tbody className='divide-y divide-gray-100'>
									{routeIds.map((routeId) => {
										const route = transportRoutes.find((r) => r.id === routeId);
										const currentItem = items.find(
											(it) => it.transportRouteId === routeId,
										);
										return (
											<tr key={routeId} className='hover:bg-gray-50/50'>
												<td className='px-4 py-2'>
													<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
														{route ? `${route.code} - ${route.name}` : routeId}
													</div>
												</td>
												<td className='px-4 py-2'>
													<FormNumberInput
														value={planMeters[routeId]}
														onValueChange={(value) => setPlan(routeId, value)}
														placeholder='Nhập sản lượng kế hoạch'
													/>
												</td>
												<td className='px-4 py-2'>
													<FormNumberInput
														value={currentItem?.productionMeters}
														onValueChange={(value) =>
															updateRouteOnlyItem(routeId, value ?? Number.NaN)
														}
														placeholder='Nhập sản lượng thực tế'
													/>
												</td>
											</tr>
										);
									})}
								</tbody>
							</table>
						</div>
					)}

					{!routeOnly &&
						routeIds.map((routeId) => {
							const route = transportRoutes.find((r) => r.id === routeId);
							const deptsForRoute = routeOnly
								? ['']
								: routeDepartmentIds[routeId] || [];

							return (
								<div
									key={routeId}
									className='flex flex-col gap-3 rounded-sm border border-gray-200 bg-white p-4'
								>
									<div className='text-xs font-semibold text-gray-800'>
										{route ? `${route.code} - ${route.name}` : routeId}
									</div>

									<div className='flex flex-col gap-3 md:flex-row md:items-end'>
										<div className='md:flex-[2]'>
											<FormMultiSelect
												control={formControl}
												name={
													`${entryPath}.routeDepartmentIds.${routeId}` as any
												}
												label='Đơn vị áp dụng cho tuyến này'
												placeholder='Chọn đơn vị'
												options={departments.map((d) => ({
													value: d.id,
													label: `${d.code} - ${d.name}`,
												}))}
											/>
										</div>
										<PlanActualFields
											planValue={planMeters[routeId]}
											actualValue={sumActual(
												(it) => it.transportRouteId === routeId,
											)}
											onPlanChange={(value) => setPlan(routeId, value)}
										/>
									</div>

									{deptsForRoute.length > 0 && (
										<div className='overflow-hidden rounded-md border border-gray-200'>
											<table className='w-full text-left text-sm'>
												<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
													<tr>
														{!routeOnly && (
															<th className='min-w-[220px] px-4 py-3'>
																Đơn vị
															</th>
														)}
														<th className='min-w-[140px] px-4 py-3'>
															Sản lượng thực tế
														</th>
													</tr>
												</thead>
												<tbody className='divide-y divide-gray-100'>
													{deptsForRoute.map((deptId) => {
														const dept = departments.find(
															(d) => d.id === deptId,
														);
														const itemIndex = items.findIndex(
															(it) =>
																it.transportRouteId === routeId &&
																(routeOnly || it.routeDepartmentId === deptId),
														);

														return (
															<tr key={deptId} className='hover:bg-gray-50/50'>
																{!routeOnly && (
																	<td className='px-4 py-2'>
																		<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
																			{dept
																				? `${dept.code} - ${dept.name}`
																				: deptId}
																		</div>
																	</td>
																)}
																<td className='px-4 py-2'>
																	<FormNumberInput
																		value={items[itemIndex]?.productionMeters}
																		onValueChange={(value) => {
																			const next = [...items];
																			if (itemIndex >= 0) {
																				next[itemIndex] = {
																					...next[itemIndex],
																					productionMeters: value ?? Number.NaN,
																				};
																			} else {
																				next.push({
																					transportRouteId: routeId,
																					routeDepartmentId: routeOnly
																						? undefined
																						: deptId,
																					productionMeters: value ?? Number.NaN,
																				});
																			}
																			setItems(next);
																		}}
																		placeholder='Nhập sản lượng thực tế'
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
				</>
			)}

			{isMonorail && (
				<>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.equipmentIds` as any}
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn Nhóm vật tư, tài sản'
						options={contractCodes.map((c) => ({
							value: c.id,
							label: `${c.code} - ${c.name}`,
						}))}
					/>

					{equipmentIds.map((equipmentId) => {
						const cc = contractCodes.find((c) => c.id === equipmentId);
						const qualitiesForEq =
							equipmentQualitiesMap[equipmentId] ||
							(equipmentQualities.length > 0 ? equipmentQualities : []);

						return (
							<div
								key={equipmentId}
								className='flex flex-col gap-3 rounded-sm border border-gray-200 bg-white p-4'
							>
								<div className='text-xs font-semibold text-gray-800'>
									{cc ? `${cc.code} - ${cc.name}` : equipmentId}
								</div>

								<div className='flex flex-col gap-3 md:flex-row md:items-end'>
									<div className='md:flex-[2]'>
										<FormMultiSelect
											control={formControl}
											name={
												`${entryPath}.equipmentQualitiesMap.${equipmentId}` as any
											}
											label='Chất lượng thiết bị'
											placeholder='Chọn chất lượng thiết bị'
											options={[
												{ value: 'A', label: 'Thiết bị loại A' },
												{ value: 'B', label: 'Thiết bị loại B' },
												{ value: 'C', label: 'Thiết bị loại C' },
											]}
										/>
									</div>
									<PlanActualFields
										planValue={planMeters[equipmentId]}
										actualValue={sumActual(
											(it) => it.equipmentId === equipmentId,
										)}
										onPlanChange={(value) => setPlan(equipmentId, value)}
									/>
								</div>

								{qualitiesForEq.length > 0 && (
									<div className='overflow-hidden rounded-md border border-gray-200'>
										<table className='w-full text-left text-sm'>
											<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
												<tr>
													<th className='min-w-[160px] px-4 py-3'>
														Chất lượng thiết bị
													</th>
													<th className='min-w-[140px] px-4 py-3'>
														Sản lượng thực tế
													</th>
												</tr>
											</thead>
											<tbody className='divide-y divide-gray-100'>
												{qualitiesForEq.map((quality) => {
													const itemIndex = items.findIndex(
														(it) =>
															it.equipmentId === equipmentId &&
															it.equipmentQuality === quality,
													);

													return (
														<tr key={quality} className='hover:bg-gray-50/50'>
															<td className='px-4 py-2'>
																<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
																	Thiết bị loại {quality}
																</div>
															</td>
															<td className='px-4 py-2'>
																<FormNumberInput
																	value={items[itemIndex]?.productionMeters}
																	onValueChange={(value) => {
																		const next = [...items];
																		if (itemIndex >= 0) {
																			next[itemIndex] = {
																				...next[itemIndex],
																				productionMeters: value ?? Number.NaN,
																			};
																		} else {
																			next.push({
																				equipmentId,
																				equipmentQuality: quality,
																				productionMeters: value ?? Number.NaN,
																			});
																		}
																		setItems(next);
																	}}
																	placeholder='Nhập sản lượng thực tế'
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
				</>
			)}

			{isOther && (
				<>
					<FormMultiSelect
						control={formControl}
						name={`${entryPath}.equipmentIds` as any}
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn Nhóm vật tư, tài sản'
						options={contractCodes.map((c) => ({
							value: c.id,
							label: `${c.code} - ${c.name}`,
						}))}
					/>

					{equipmentIds.length > 0 && (
						<div className='overflow-hidden rounded-md border border-gray-200'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
									<tr>
										<th className='min-w-[220px] px-4 py-3'>
											Nhóm vật tư, tài sản
										</th>
										<th className='min-w-[140px] px-4 py-3'>
											Sản lượng kế hoạch
										</th>
										<th className='min-w-[140px] px-4 py-3'>
											Sản lượng thực tế
										</th>
									</tr>
								</thead>
								<tbody className='divide-y divide-gray-100'>
									{equipmentIds.map((equipmentId) => {
										const cc = contractCodes.find((c) => c.id === equipmentId);
										const itemIndex = items.findIndex(
											(it) => it.equipmentId === equipmentId,
										);

										return (
											<tr key={equipmentId} className='hover:bg-gray-50/50'>
												<td className='px-4 py-2'>
													<div className='flex h-9 items-center rounded-sm border border-[#999999] bg-[#f5f5f5] px-3 text-xs font-semibold text-gray-800'>
														{cc ? `${cc.code} - ${cc.name}` : equipmentId}
													</div>
												</td>
												<td className='px-4 py-2'>
													<FormNumberInput
														value={planMeters[equipmentId]}
														onValueChange={(value) =>
															setPlan(equipmentId, value)
														}
														placeholder='Nhập sản lượng kế hoạch'
													/>
												</td>
												<td className='px-4 py-2'>
													<FormNumberInput
														value={items[itemIndex]?.productionMeters}
														onValueChange={(value) => {
															const next = [...items];
															if (itemIndex >= 0) {
																next[itemIndex] = {
																	...next[itemIndex],
																	productionMeters: value ?? Number.NaN,
																};
															} else {
																next.push({
																	equipmentId,
																	productionMeters: value ?? Number.NaN,
																});
															}
															setItems(next);
														}}
														placeholder='Nhập sản lượng thực tế'
													/>
												</td>
											</tr>
										);
									})}
								</tbody>
							</table>
						</div>
					)}
				</>
			)}
		</div>
	);
}

type VanTaiLoGroupFieldsProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	productionProcesses: ProductionProcessOption[];
	transportRoutes: TransportRoute[];
	contractCodes: ContractCodeOption[];
	departments: Department[];
};

// Khối "Vận hành sản xuất" cho 1 Nhóm công đoạn sản xuất kiểu VTL/VTCG — thay thế MultiSelect
// Sản phẩm (Khai thác) bằng chọn nhiều Công đoạn sản xuất, mỗi công đoạn render đúng field theo
// loại vận tải (VanTaiLoProcessEntryCard).
export function VanTaiLoGroupFields({
	form,
	groupIndex,
	productionProcesses,
	transportRoutes,
	contractCodes,
	departments,
}: VanTaiLoGroupFieldsProps) {
	const groupPath = `groups.${groupIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedGroup = useWatch({
		control: form.control,
		name: groupPath,
	}) as ProductionGroupSchema;

	const processIds = watchedGroup?.processIds || [];
	const transportProcesses = watchedGroup?.transportProcesses || [];

	// SL kế hoạch của Nhóm công đoạn VTL không nhập — luôn = tổng kế hoạch các Tuyến/Nhóm VTTS
	const groupPlanMeters = transportProcesses.reduce(
		(sum, entry) => sum + sumProcessPlanMeters(entry),
		0,
	);
	useEffect(() => {
		if ((watchedGroup?.planProductionMeters || 0) === groupPlanMeters) return;
		form.setValue(`groups.${groupIndex}.planProductionMeters`, groupPlanMeters);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [groupPlanMeters, groupIndex]);

	const prevProcessIdsRef = useRef<string[]>(processIds);
	useEffect(() => {
		const prev = prevProcessIdsRef.current;
		if (JSON.stringify(prev) === JSON.stringify(processIds)) return;

		const currentProcesses =
			form.getValues(`groups.${groupIndex}.transportProcesses`) || [];
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

		form.setValue(`groups.${groupIndex}.transportProcesses`, nextProcesses);
		prevProcessIdsRef.current = [...processIds];
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [JSON.stringify(processIds), groupIndex]);

	return (
		<div className='space-y-3'>
			<FormMultiSelect
				control={formControl}
				name={`groups.${groupIndex}.processIds` as any}
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={productionProcesses.map((p) => ({
					value: p.id,
					label: p.code ? `${p.code} - ${p.name}` : p.name,
				}))}
			/>

			{typeof form.formState.errors.groups?.[groupIndex]?.processIds
				?.message === 'string' && (
				<FieldError
					errors={[form.formState.errors.groups?.[groupIndex]?.processIds]}
				/>
			)}

			{transportProcesses.length > 0 && (
				<div className='flex flex-col gap-3'>
					{transportProcesses.map((entry, processIndex) => {
						const process = productionProcesses.find(
							(p) => p.id === entry.productionProcessId,
						);
						if (!process) return null;

						return (
							<VanTaiLoProcessEntryCard
								key={entry.productionProcessId}
								form={form}
								groupIndex={groupIndex}
								processIndex={processIndex}
								process={process}
								transportRoutes={transportRoutes}
								contractCodes={contractCodes}
								departments={departments}
							/>
						);
					})}
				</div>
			)}
		</div>
	);
}
