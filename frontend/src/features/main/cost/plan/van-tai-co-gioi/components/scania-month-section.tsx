import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { useUnitsOfMeasure } from '@/hooks/use-units-of-measure';
import { useEffect, useRef } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { DepartmentPlanFormSchema } from '@/features/main/cost/plan/schema';
import {
	getSuggestedFuelAdjustmentFactor,
	getUnitForMotorizedProcess,
} from '../utils';

type ScaniaMonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	assignmentCodes: any[];
	processes: any[];
	cargoTypes?: any[];
	pickupLocations?: any[];
	dropoffLocations?: any[];
	distances: any[];
	locations?: any[];
};

export function ScaniaMonthSection({
	form,
	monthIndex,
	assignmentCodes,
	processes,
	cargoTypes = [],
	pickupLocations = [],
	dropoffLocations = [],
	distances,
	locations = [],
}: ScaniaMonthSectionProps) {
	const { units: uomList } = useUnitsOfMeasure();
	const unitOptions = uomList || [];
	const monthPath = `motorizedMonths.${monthIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedMonth = useWatch({
		control: form.control,
		name: monthPath,
	}) as any;

	const assignmentCodeIds: string[] =
		watchedMonth?.scaniaAssignmentCodeIds || [];
	const equipmentProcesses: Record<string, string[]> =
		watchedMonth?.equipmentProcesses || {};
	const equipmentQualities: Record<string, string[]> =
		watchedMonth?.equipmentQualities || {};
	const equipmentDistances: Record<string, string[]> =
		watchedMonth?.equipmentDistances || {};
	const processCargoTypes: Record<string, string[]> =
		watchedMonth?.processCargoTypes || {};
	const processPickupLocations: Record<string, string[]> =
		watchedMonth?.processPickupLocations || {};
	const processDropoffLocations: Record<string, string[]> =
		watchedMonth?.processDropoffLocations || {};
	const items: any[] = watchedMonth?.items || [];

	const prevEquipmentIdsRef = useRef<string[]>(assignmentCodeIds);

	const allLocations =
		locations && locations.length > 0
			? locations
			: [...pickupLocations, ...dropoffLocations];

	const setItems = (newRows: typeof items) => {
		const currentItems: any[] =
			form.getValues(`${monthPath}.items` as any) || [];
		const otherItems = currentItems.filter(
			(it) =>
				!assignmentCodeIds.includes(it.equipmentId) &&
				!prevEquipmentIdsRef.current.includes(it.equipmentId),
		);
		form.setValue(`${monthPath}.items` as any, [
			...otherItems,
			...newRows,
		]);
		prevEquipmentIdsRef.current = assignmentCodeIds;
	};

	// Đồng bộ danh sách dòng dữ liệu khi các tiêu chí lọc thay đổi
	useEffect(() => {
		if (assignmentCodeIds.length === 0) {
			setItems([]);
			return;
		}

		const newRows: any[] = [];

		assignmentCodeIds.forEach((acId) => {
			const eq = assignmentCodes.find(
				(a) => a.id === acId || a.value === acId,
			);

			const selectedProcs: string[] = equipmentProcesses[acId] || [];

			selectedProcs.forEach((procId) => {
				const scopeKey = `${acId}_${procId}`;
				const proc = processes.find(
					(p) => p.id === procId || p.value === procId,
				);
				const procName = proc
					? proc.name || proc.label
					: procId;

				const selectedQualities: string[] = [
					...(equipmentQualities[scopeKey] || []),
				].sort((a, b) => a.localeCompare(b));

				const selectedDists: string[] = equipmentDistances[scopeKey] || [];

				if (selectedQualities.length === 0 || selectedDists.length === 0) {
					return;
				}

				const procCargoIds: string[] = processCargoTypes[scopeKey] || [];
				const procPickupIds: string[] = processPickupLocations[scopeKey] || [];
				const procDropoffIds: string[] = processDropoffLocations[scopeKey] || [];

				const cargoList = procCargoIds.length > 0 ? procCargoIds : [''];
				const dropoffList = procDropoffIds.length > 0 ? procDropoffIds : [''];
				const distList = selectedDists;

				const procPickupNames = procPickupIds
					.map((id) => {
						const l = allLocations.find(
							(loc: any) =>
								loc.id === id || loc.value === id || loc.name === id,
						);
						return l ? (l as any).name || (l as any).label : '';
					})
					.filter(Boolean);
				const combinedPickupName = procPickupNames.join(', ');

				selectedQualities.forEach((qual: string) => {
					cargoList.forEach((cargoId: string) => {
						dropoffList.forEach((dropoffId: string) => {
							distList.forEach((distId: string) => {
								const cargoObj = cargoTypes.find(
									(c: any) =>
										c.id === cargoId ||
										c.value === cargoId ||
										c.name === cargoId,
								);
								const cargoName = cargoObj
									? (cargoObj as any).name || (cargoObj as any).label
									: '';

								const dropoffObj = allLocations.find(
									(l: any) =>
										l.id === dropoffId ||
										l.value === dropoffId ||
										l.name === dropoffId,
								);
								const dropoffName = dropoffObj
									? (dropoffObj as any).name || (dropoffObj as any).label
									: '';

								const distObj = distances.find(
									(d: any) => d.id === distId || d.value === distId,
								);
								const distValue = distObj
									? (distObj as any).value ??
										(distObj as any).name ??
										(distObj as any).label
									: '';

								const existing = items.find(
									(it: any) =>
										it.equipmentId === acId &&
										it.productionProcessId === procId &&
										it.equipmentQuality === qual &&
										(it.cargoTypeId || '') === (cargoId || '') &&
										(it.dumpingLocationId || '') === (dropoffId || '') &&
										(it.haulDistanceId || '') === (distId || ''),
								);

								const suggestedFactor = getSuggestedFuelAdjustmentFactor(
									'scania',
									procName,
									eq?.code,
									distValue,
								);

								const defaultUnitName =
									existing?.unitName ||
									getUnitForMotorizedProcess(
										procName,
										'scania',
									);

								const matchedUnit = unitOptions.find(
									(u: any) =>
										u.name?.toLowerCase() === defaultUnitName.toLowerCase() ||
										u.value?.toLowerCase() === defaultUnitName.toLowerCase() ||
										u.id === existing?.unitOfMeasureId,
								);

								newRows.push({
									id: existing?.id,
									equipmentId: acId,
									equipmentCode: eq?.code,
									equipmentName: eq?.name || eq?.label || acId,
									equipmentQuality: qual,
									productionProcessId: procId,
									productionProcessCode: proc?.code,
									productionProcessName: procName,
									cargoTypeId: cargoId || undefined,
									cargoTypeName: cargoName,
									receivingLocationIds: procPickupIds,
									receivingLocationNames: combinedPickupName,
									receivingLocationId: procPickupIds[0] || undefined,
									receivingLocationName: combinedPickupName,
									dumpingLocationId: dropoffId || undefined,
									dumpingLocationName: dropoffName,
									haulDistanceId: distId || undefined,
									haulDistanceValue: distValue,
									productionMeters: existing?.productionMeters ?? Number.NaN,
									fuelAdjustmentFactor:
										existing?.fuelAdjustmentFactor ?? suggestedFactor,
									unitName: defaultUnitName,
									unitOfMeasureId:
										existing?.unitOfMeasureId || matchedUnit?.id || undefined,
								});
							});
						});
					});
				});
			});
		});

		setItems(newRows);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [
		JSON.stringify(assignmentCodeIds),
		JSON.stringify(equipmentProcesses),
		JSON.stringify(equipmentQualities),
		JSON.stringify(equipmentDistances),
		JSON.stringify(processCargoTypes),
		JSON.stringify(processPickupLocations),
		JSON.stringify(processDropoffLocations),
		unitOptions.length,
	]);

	return (
		<div className='space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-2xs'>
			<div className='border-b border-neutral-100 pb-2 text-sm font-semibold text-black'>
				<span>Vận chuyển (Xe Scania)</span>
			</div>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={formControl}
				name={`${monthPath}.scaniaAssignmentCodeIds` as any}
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* LẶP QUA TỪNG XE SCANIA */}
			{assignmentCodeIds.map((acId) => {
				const eq = assignmentCodes.find(
					(a) => a.id === acId || a.value === acId,
				);
				const title = eq
					? eq.code
						? `${eq.code} - ${eq.name || eq.label}`
						: eq.name || eq.label || acId
					: acId;
				const selectedProcs = equipmentProcesses[acId] || [];

				return (
					<div
						key={acId}
						className='space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-xs'
					>
						{/* HEADER XE */}
						<div className='flex items-center justify-between border-b border-neutral-100 pb-2'>
							<span className='text-sm font-semibold text-black'>{title}</span>
						</div>

						{/* CẤP 2: CHỌN CÔNG ĐOẠN SẢN XUẤT CHO XE NÀY */}
						<FormMultiSelect
							control={formControl}
							name={`${monthPath}.equipmentProcesses.${acId}` as any}
							label='Công đoạn sản xuất'
							placeholder='Chọn các công đoạn sản xuất'
							options={processes.map((p) => ({
								value: p.id || p.value,
								label: p.code
									? `${p.code} - ${p.name || p.label}`
									: p.name || p.label,
							}))}
						/>

						{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
						{selectedProcs.map((procId) => {
							const proc = processes.find(
								(p) => p.id === procId || p.value === procId,
							);
							const procName = proc
								? proc.code
									? `${proc.code} - ${proc.name || proc.label}`
									: proc.name || proc.label
								: procId;
							const scopeKey = `${acId}_${procId}`;

							const currentQualities: string[] =
								equipmentQualities[scopeKey] || [];
							const currentDists: string[] =
								equipmentDistances[scopeKey] || [];
							const currentPickups: string[] =
								processPickupLocations[scopeKey] || [];

							const procPickupNames = currentPickups
								.map((id) => {
									const l = allLocations.find(
										(loc: any) =>
											loc.id === id || loc.value === id || loc.name === id,
									);
									return l ? (l as any).name || (l as any).label : '';
								})
								.filter(Boolean);
							const combinedPickupName = procPickupNames.join(', ');

							const filteredItems = items.filter(
								(it: any) =>
									it.equipmentId === acId && it.productionProcessId === procId,
							);

							const groupsMap = new Map<string, any>();
							filteredItems.forEach((item: any) => {
								const groupKey = `${item.equipmentQuality}_${item.cargoTypeId || ''}_${item.dumpingLocationId || ''}`;
								if (!groupsMap.has(groupKey)) {
									groupsMap.set(groupKey, {
										equipmentQuality: item.equipmentQuality,
										cargoTypeId: item.cargoTypeId,
										cargoTypeName: item.cargoTypeName,
										dumpingLocationId: item.dumpingLocationId,
										dumpingLocationName: item.dumpingLocationName,
										items: [],
									});
								}
								groupsMap.get(groupKey)!.items.push(item);
							});
							const groups = Array.from(groupsMap.values());

							return (
								<div
									key={procId}
									className='space-y-4 rounded-lg border border-neutral-200 bg-neutral-50/50 p-4 shadow-2xs'
								>
									{/* HEADER CỤM CÔNG ĐOẠN */}
									<div className='border-b border-neutral-200/80 pb-2'>
										<span className='text-sm font-semibold text-black'>
											{procName}
										</span>
									</div>

									{/* SELECTOR THÔNG SỐ ĐIỀU KIỆN */}
									<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.equipmentQualities.${scopeKey}` as any
											}
											label='Chất lượng thiết bị'
											placeholder='Chọn chất lượng thiết bị'
											options={[
												{ label: 'Thiết bị loại A', value: 'A' },
												{ label: 'Thiết bị loại B', value: 'B' },
												{ label: 'Thiết bị loại C', value: 'C' },
											]}
										/>

										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.equipmentDistances.${scopeKey}` as any
											}
											label='Cung độ vận tải'
											placeholder='Chọn cung độ vận tải'
											options={distances.map((d) => ({
												value: d.id || d.value,
												label: d.value
													? `${d.value} km`
													: d.name || d.label,
											}))}
										/>
									</div>

									<div className='grid grid-cols-1 gap-3 md:grid-cols-3'>
										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.processCargoTypes.${scopeKey}` as any
											}
											label='Chủng loại hàng'
											placeholder='Chọn chủng loại hàng'
											options={cargoTypes.map((c) => ({
												value: c.id || c.value,
												label: c.code
													? `${c.code} - ${c.name || c.label}`
													: c.name || c.label,
											}))}
										/>

										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.processPickupLocations.${scopeKey}` as any
											}
											label='Vị trí nhận (Không bắt buộc)'
											placeholder='Chọn vị trí nhận'
											options={pickupLocations.map((l) => ({
												value: l.id || l.value,
												label: l.name || l.label,
											}))}
										/>

										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.processDropoffLocations.${scopeKey}` as any
											}
											label='Vị trí đổ (Không bắt buộc)'
											placeholder='Chọn vị trí đổ'
											options={dropoffLocations.map((l) => ({
												value: l.id || l.value,
												label: l.name || l.label,
											}))}
										/>
									</div>

									{/* KHU VỰC BẢNG SẢN LƯỢNG THEO CUNG ĐỘ (CARDS VỚI BADGES) */}
									{currentDists.length > 0 &&
										currentQualities.length > 0 &&
										groups.length > 0 && (
											<div className='space-y-4 pt-2'>
												{groups.map((grp: any, gIdx: number) => {
													const summaryParts: string[] = [];
													if (grp.cargoTypeName) {
														summaryParts.push(grp.cargoTypeName);
													}
													if (combinedPickupName && grp.dumpingLocationName) {
														summaryParts.push(
															`từ ${combinedPickupName} đến ${grp.dumpingLocationName}`,
														);
													} else if (combinedPickupName) {
														summaryParts.push(`từ ${combinedPickupName}`);
													} else if (grp.dumpingLocationName) {
														summaryParts.push(
															`đến ${grp.dumpingLocationName}`,
														);
													}
													const paramSummary = summaryParts.join(' ');

													return (
														<div
															key={gIdx}
															className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow-xs'
														>
															<div className='flex flex-wrap items-center justify-between gap-3 border-b border-neutral-200 bg-neutral-100 px-3.5 py-2.5'>
																<div className='flex flex-wrap items-center gap-2.5 text-sm'>
																	<span className='rounded-md border border-neutral-300 bg-white px-3 py-1 font-semibold text-black shadow-2xs'>
																		{title} (Loại {grp.equipmentQuality})
																	</span>
																	{paramSummary && (
																		<span className='rounded-md border border-neutral-300 bg-white px-3 py-1 font-medium text-black shadow-2xs'>
																			<span className='font-bold text-black'>
																				Thông số:
																			</span>{' '}
																			{paramSummary}
																		</span>
																	)}
																</div>
															</div>

															<div className='w-full overflow-x-auto'>
																<table className='w-full min-w-[500px] text-left text-sm'>
																	<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
																		<tr>
																			<th className='px-4 py-2.5 whitespace-nowrap'>
																				Cung độ vận tải
																			</th>
																			<th className='w-36 min-w-[120px] px-4 py-2.5 whitespace-nowrap text-center'>
																				Đơn vị tính
																			</th>
																			<th className='w-52 min-w-[160px] px-4 py-2.5 whitespace-nowrap'>
																				Sản lượng kế hoạch
																			</th>
																		</tr>
																	</thead>
																	<tbody className='divide-y divide-neutral-100'>
																		{grp.items.map((item: any) => {
																			const globalIdx = items.findIndex(
																				(it) => it === item,
																			);
																			return (
																				<tr
																					key={`${item.equipmentId}-${item.equipmentQuality}-${item.productionProcessId}-${item.cargoTypeId || ''}-${item.dumpingLocationId || ''}-${item.haulDistanceId || ''}`}
																					className='transition-colors hover:bg-neutral-50/50'
																				>
																					<td className='px-4 py-2 whitespace-nowrap'>
																						<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-neutral-300 bg-white px-3 text-xs font-medium text-black'>
																							{item.haulDistanceValue
																								? `${item.haulDistanceValue} km`
																								: '-'}
																						</div>
																					</td>
																					<td className='w-36 min-w-[120px] px-4 py-2'>
																						<FormComboBox
																							value={item.unitName || 'tấn'}
																							onValueChange={(val) => {
																								const next = [...items];
																								if (globalIdx >= 0) {
																									const matched = unitOptions.find(
																										(u: any) =>
																											u.value === val ||
																											u.name === val ||
																											u.id === val,
																									);
																									next[globalIdx] = {
																										...next[globalIdx],
																										unitName: matched?.name || val,
																										unitOfMeasureId:
																											matched?.id || val,
																									};
																									setItems(next);
																								}
																							}}
																							options={unitOptions}
																							placeholder='Chọn ĐVT'
																						/>
																					</td>
																					<td className='w-52 min-w-[160px] px-4 py-2'>
																						<FormNumberInput
																							value={item.productionMeters}
																							onValueChange={(val) => {
																								const next = [...items];
																								if (globalIdx >= 0) {
																									next[globalIdx] = {
																										...next[globalIdx],
																										productionMeters:
																											val ?? Number.NaN,
																									};
																									setItems(next);
																								}
																							}}
																							placeholder='Nhập sản lượng KH'
																						/>
																					</td>
																				</tr>
																			);
																		})}
																	</tbody>
																</table>
															</div>
														</div>
													);
												})}
											</div>
										)}
								</div>
							);
						})}
					</div>
				);
			})}
		</div>
	);
}
