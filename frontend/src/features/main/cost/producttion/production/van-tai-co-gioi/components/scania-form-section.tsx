import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { useEffect } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../../production-form-schema';
import { getUnitForProcess } from '../utils';

type ScaniaFormSectionProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	assignmentCodes: any[];
	processes: any[];
	cargoTypes: any[];
	pickupLocations: any[];
	dropoffLocations: any[];
	distances: any[];
};

export function ScaniaFormSection({
	form,
	groupIndex,
	assignmentCodes,
	processes,
	cargoTypes,
	pickupLocations,
	dropoffLocations,
	distances,
}: ScaniaFormSectionProps) {
	const groupPath = `groups.${groupIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedGroup = useWatch({
		control: form.control,
		name: groupPath,
	}) as any;

	const assignmentCodeIds: string[] =
		watchedGroup?.scaniaAssignmentCodeIds ||
		watchedGroup?.assignmentCodeIds ||
		[];
	const equipmentProcesses: Record<string, string[]> =
		watchedGroup?.equipmentProcesses || {};
	const equipmentQualities: Record<string, string[]> =
		watchedGroup?.equipmentQualities || {};
	const equipmentDistances: Record<string, string[]> =
		watchedGroup?.equipmentDistances || {};
	const processCargoTypes: Record<string, string[]> =
		watchedGroup?.processCargoTypes || {};
	const processPickupLocations: Record<string, string[]> =
		watchedGroup?.processPickupLocations || {};
	const processDropoffLocations: Record<string, string[]> =
		watchedGroup?.processDropoffLocations || {};
	const items: any[] = watchedGroup?.motorizedItems || [];

	const setItems = (newRows: typeof items) => {
		const currentItems: any[] =
			form.getValues(`${groupPath}.motorizedItems` as any) || [];
		const otherItems = currentItems.filter(
			(it) => !assignmentCodeIds.includes(it.equipmentId),
		);
		form.setValue(`${groupPath}.motorizedItems` as any, [
			...otherItems,
			...newRows,
		]);
	};

	// Synchronize item rows when filter selections change
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

				const procQualities = [
					...(equipmentQualities[scopeKey] ||
						equipmentQualities[eqId] ||
						[]),
				].sort((a, b) => a.localeCompare(b));
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

									newRows.push({
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
										unitName: getUnitForProcess(
											proc?.name || proc?.label,
											'scania',
										),
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
	]);

	return (
		<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
			<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
				<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
				<span>Xe ô tô Scania</span>
			</div>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={formControl}
				name={`${groupPath}.scaniaAssignmentCodeIds` as any}
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
							name={`${groupPath}.equipmentProcesses.${eqId}` as any}
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
													`${groupPath}.equipmentQualities.${scopeKey}` as any
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
													`${groupPath}.equipmentDistances.${scopeKey}` as any
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
													`${groupPath}.processCargoTypes.${scopeKey}` as any
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
													`${groupPath}.processPickupLocations.${scopeKey}` as any
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
													`${groupPath}.processDropoffLocations.${scopeKey}` as any
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

									{/* BẢNG NHẬP SẢN LƯỢNG THỰC TẾ CHO RIÊNG CỤM NÀY */}
									{procItems.length > 0 && (
										<div className='space-y-2 pt-2'>
											<div className='text-xs font-semibold text-gray-700'>
												Bảng kế hoạch sản lượng thực tế ({procItems.length} tổ
												hợp)
											</div>

											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-black'>
														<tr>
															<th className='w-[15%] px-3 py-2'>Chất lượng</th>
															<th className='w-[22%] px-3 py-2'>Loại hàng</th>
															<th className='w-[28%] px-3 py-2'>
																Vị trí nhận → Đổ
															</th>
															<th className='w-[15%] px-3 py-2'>Cung độ</th>
															<th className='w-[20%] px-3 py-2'>
																Sản lượng thực tế
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
																			placeholder='Nhập sản lượng'
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
