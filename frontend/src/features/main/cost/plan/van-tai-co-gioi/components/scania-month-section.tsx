import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { useUnitsOfMeasure } from '@/hooks/use-units-of-measure';
import { useEffect } from 'react';
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

	const assignmentCodeIds: string[] = watchedMonth?.assignmentCodeIds || [];
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

	const setItems = (newItems: typeof items) => {
		form.setValue(`${monthPath}.items` as any, newItems);
	};

	useEffect(() => {
		if (assignmentCodeIds.length === 0) {
			setItems([]);
			return;
		}

		const newRows: any[] = [];

		assignmentCodeIds.forEach((eqId) => {
			const eq = assignmentCodes.find((a) => a.id === eqId || a.value === eqId);
			const eqProcs = equipmentProcesses[eqId] || [];

			eqProcs.forEach((procId) => {
				const proc = processes.find(
					(p) => p.id === procId || p.value === procId,
				);
				const scopeKey = `${eqId}_${procId}`;

				const procQualities =
					equipmentQualities[scopeKey] || equipmentQualities[eqId] || [];
				const dists = equipmentDistances[scopeKey] ||
					equipmentDistances[eqId] || [''];
				const cTypes = processCargoTypes[scopeKey] ||
					processCargoTypes[procId] || [''];
				const pickups = processPickupLocations[scopeKey] ||
					processPickupLocations[procId] || [''];
				const dropoffs = processDropoffLocations[scopeKey] ||
					processDropoffLocations[procId] || [''];

				procQualities.forEach((quality) => {
					cTypes.forEach((cargoId) => {
						pickups.forEach((pickupId) => {
							dropoffs.forEach((dropoffId) => {
								dists.forEach((distId) => {
									const cargo = cargoTypes.find(
										(c) => c.id === cargoId || c.value === cargoId,
									);
									const pickup = pickupLocations.find(
										(l) => l.id === pickupId || l.value === pickupId,
									);
									const dropoff = dropoffLocations.find(
										(l) => l.id === dropoffId || l.value === dropoffId,
									);
									const dist = distances.find(
										(d) => d.id === distId || d.value === distId,
									);

									const existing = items.find(
										(it) =>
											it.equipmentId === eqId &&
											it.equipmentQuality === quality &&
											it.productionProcessId === procId &&
											(it.cargoTypeId || '') === (cargoId || '') &&
											(it.receivingLocationId || '') === (pickupId || '') &&
											(it.dumpingLocationId || '') === (dropoffId || '') &&
											(it.haulDistanceId || '') === (distId || ''),
									);

									const suggestedFactor = getSuggestedFuelAdjustmentFactor(
										'scania',
										proc?.name || proc?.label,
										eq?.code,
										dist?.value,
									);

									const defaultUnitName =
										existing?.unitName ||
										getUnitForMotorizedProcess(
											proc?.name || proc?.label,
											'scania',
										);

									const matchedUnit = unitOptions.find(
										(u) =>
											u.name?.toLowerCase() === defaultUnitName.toLowerCase() ||
											u.value?.toLowerCase() === defaultUnitName.toLowerCase() ||
											u.id === existing?.unitOfMeasureId,
									);

									newRows.push({
										id: existing?.id,
										equipmentId: eqId,
										equipmentCode: eq?.code,
										equipmentName: eq?.name || eq?.label,
										equipmentQuality: quality,
										productionProcessId: procId,
										productionProcessCode: proc?.code,
										productionProcessName: proc?.name || proc?.label,
										cargoTypeId: cargoId || undefined,
										cargoTypeName: cargo?.name || cargo?.label,
										receivingLocationId: pickupId || undefined,
										receivingLocationName: pickup?.name || pickup?.label,
										dumpingLocationId: dropoffId || undefined,
										dumpingLocationName: dropoff?.name || dropoff?.label,
										haulDistanceId: distId || undefined,
										haulDistanceValue: dist?.value || dist?.name || dist?.label,
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
		<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
			<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
				<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
				<span>Vận chuyển (Xe Scania)</span>
			</div>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={formControl}
				name={`${monthPath}.assignmentCodeIds` as any}
				label='1. Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* LẶP QUA TỪNG NHÓM VẬT TƯ ĐƯỢC CHỌN */}
			{assignmentCodeIds.map((eqId) => {
				const eq = assignmentCodes.find(
					(a) => a.id === eqId || a.value === eqId,
				);
				const selectedProcs = equipmentProcesses[eqId] || [];

				return (
					<div
						key={eqId}
						className='space-y-4 rounded-lg border border-gray-300 bg-white p-4 shadow-2xs'
					>
						{/* HEADER NHÓM XE */}
						<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
							<span className='text-sm font-bold text-gray-900'>
								{eq?.code
									? `${eq.code} - ${eq.name || eq.label}`
									: eq?.name || eq?.label || eqId}
							</span>
						</div>

						{/* CẤP 2: CHỌN CÔNG ĐOẠN SẢN XUẤT CHO XE NÀY */}
						<FormMultiSelect
							control={formControl}
							name={`${monthPath}.equipmentProcesses.${eqId}` as any}
							label={`2. Công đoạn sản xuất áp dụng cho [${eq?.code || eq?.name || eq?.label}]`}
							placeholder='Chọn công đoạn sản xuất'
							options={processes.map((p) => ({
								value: p.id || p.value,
								label: p.code
									? `${p.code} - ${p.name || p.label}`
									: p.name || p.label,
							}))}
						/>

						{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT (FORM LỒNG FORM) */}
						{selectedProcs.map((procId) => {
							const proc = processes.find(
								(p) => p.id === procId || p.value === procId,
							);
							const scopeKey = `${eqId}_${procId}`;
							const procItems = items.filter(
								(it) =>
									it.equipmentId === eqId && it.productionProcessId === procId,
							);

							return (
								<div
									key={procId}
									className='space-y-4 rounded-lg border border-gray-300 bg-gray-50/40 p-4 shadow-xs'
								>
									{/* HEADER CỤM CÔNG ĐOẠN */}
									<div className='flex items-center gap-2 border-b border-gray-200 pb-2'>
										<span className='h-2 w-2 rounded-full bg-blue-500' />
										<span className='text-xs font-semibold text-blue-800 uppercase'>
											{proc?.code
												? `${proc.code} - ${proc.name || proc.label}`
												: proc?.name || proc?.label || procId}
										</span>
									</div>

									{/* THÔNG SỐ ĐIỀU KIỆN CHO RIÊNG CÔNG ĐOẠN NÀY */}
									<div className='space-y-3'>
										{/* HÀNG 1: Chất lượng & Cung độ */}
										<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
											<FormMultiSelect
												control={formControl}
												name={
													`${monthPath}.equipmentQualities.${scopeKey}` as any
												}
												label='Chất lượng thiết bị'
												placeholder='Chọn chất lượng xe'
												options={[
													{ value: 'A', label: 'Thiết bị loại A' },
													{ value: 'B', label: 'Thiết bị loại B' },
													{ value: 'C', label: 'Thiết bị loại C' },
												]}
											/>

											<FormMultiSelect
												control={formControl}
												name={
													`${monthPath}.equipmentDistances.${scopeKey}` as any
												}
												label='Cung độ vận tải (km)'
												placeholder='Chọn cung độ'
												options={distances.map((d) => ({
													value: d.id || d.value,
													label: d.value ? `${d.value} km` : d.name || d.label,
												}))}
											/>
										</div>

										{/* HÀNG 2: Cụm Loại hàng, Vị trí nhận, Vị trí đổ */}
										<div className='grid grid-cols-1 gap-3 md:grid-cols-3'>
											<FormMultiSelect
												control={formControl}
												name={
													`${monthPath}.processCargoTypes.${scopeKey}` as any
												}
												label='Loại hàng'
												placeholder='Chọn loại hàng'
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
												label='Vị trí nhận'
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
												label='Vị trí đổ'
												placeholder='Chọn vị trí đổ'
												options={dropoffLocations.map((l) => ({
													value: l.id || l.value,
													label: l.name || l.label,
												}))}
											/>
										</div>
									</div>

									{/* BẢNG NHẬP SẢN LƯỢNG CỦA RIÊNG CỤM CÔNG ĐOẠN NÀY */}
									{procItems.length > 0 && (
										<div className='space-y-2 pt-2'>
											<div className='text-xs font-semibold text-gray-700'>
												Bảng kế hoạch sản lượng ({procItems.length} tổ hợp)
											</div>

											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-black uppercase'>
														<tr>
															<th className='w-[14%] px-3 py-2'>Chất lượng</th>
															<th className='w-[20%] px-3 py-2'>Loại hàng</th>
															<th className='w-[26%] px-3 py-2'>
																Vị trí nhận → Đổ
															</th>
															<th className='w-[14%] px-3 py-2'>Cung độ</th>
															<th className='w-[14%] px-3 py-2 text-center'>
																Đơn vị tính
															</th>
															<th className='w-[12%] px-3 py-2'>
																Sản lượng kế hoạch
															</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-gray-100'>
														{procItems.map((row) => {
															const globalIdx = items.findIndex(
																(it) => it === row,
															);
															return (
																<tr
																	key={`${row.equipmentId}-${row.equipmentQuality}-${row.productionProcessId}-${row.cargoTypeId || ''}-${row.receivingLocationId || ''}-${row.dumpingLocationId || ''}-${row.haulDistanceId || ''}`}
																	className='hover:bg-gray-50/50'
																>
																	<td className='px-3 py-2'>
																		<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																			{row.equipmentQuality
																				? `Loại ${row.equipmentQuality}`
																				: '-'}
																		</div>
																	</td>
																	<td className='px-3 py-2'>
																		<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																			{row.cargoTypeName || '-'}
																		</div>
																	</td>
																	<td className='px-3 py-2'>
																		<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																			{row.receivingLocationName ||
																			row.dumpingLocationName
																				? `${row.receivingLocationName || '...'} → ${row.dumpingLocationName || '...'}`
																				: '-'}
																		</div>
																	</td>
																	<td className='px-3 py-2'>
																		<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																			{row.haulDistanceValue
																				? `${row.haulDistanceValue} km`
																				: '-'}
																		</div>
																	</td>
																	<td className='px-3 py-2'>
																		<div className='min-w-[110px]'>
																			<FormComboBox
																				value={row.unitName || 'tấn'}
																				onValueChange={(val) => {
																					const next = [...items];
																					if (globalIdx >= 0) {
																						const matched = unitOptions.find(
																							(u) =>
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
																		</div>
																	</td>
																	<td className='px-3 py-2'>
																		<FormNumberInput
																			value={row.productionMeters}
																			onValueChange={(val) => {
																				const next = [...items];
																				if (globalIdx >= 0) {
																					next[globalIdx] = {
																						...next[globalIdx],
																						productionMeters: val ?? Number.NaN,
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
