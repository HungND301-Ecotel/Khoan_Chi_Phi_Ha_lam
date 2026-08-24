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
	MOCK_UNITS_OF_MEASURE,
} from '../utils';

type ServiceCraneMonthSectionProps = {
	form: UseFormReturn<DepartmentPlanFormSchema>;
	monthIndex: number;
	assignmentCodes: any[];
	processes: any[];
	distances: any[];
};

export function ServiceCraneMonthSection({
	form,
	monthIndex,
	assignmentCodes,
	processes,
	distances,
}: ServiceCraneMonthSectionProps) {
	const { units: uomList } = useUnitsOfMeasure();
	const unitOptions = uomList.length > 0 ? uomList : MOCK_UNITS_OF_MEASURE;
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

				const procText =
					`${proc?.name || ''} ${proc?.label || ''} ${proc?.code || ''}`.toLowerCase();
				const isWatering =
					procText.includes('tưới đường') ||
					procText.includes('tuoi_duong') ||
					procText.includes('tưới mỏ') ||
					procText.includes('tuoi_mo');

				const applicableDists =
					isWatering && dists.length > 0 && dists[0] !== '' ? dists : [''];

				procQualities.forEach((quality) => {
					applicableDists.forEach((distId) => {
						const dist = distances.find(
							(d) => d.id === distId || d.value === distId,
						);
						const existing = items.find(
							(it) =>
								it.equipmentId === eqId &&
								it.equipmentQuality === quality &&
								it.productionProcessId === procId &&
								(it.haulDistanceId || '') === (distId || ''),
						);

						const suggestedFactor = getSuggestedFuelAdjustmentFactor(
							'service_crane',
							proc?.name || proc?.label,
							eq?.code,
							isWatering ? dist?.value : undefined,
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
							haulDistanceId: isWatering ? distId || undefined : undefined,
							haulDistanceValue: isWatering
								? dist?.value || dist?.name || dist?.label
								: undefined,
							productionMeters: existing?.productionMeters ?? Number.NaN,
							fuelAdjustmentFactor:
								existing?.fuelAdjustmentFactor ?? suggestedFactor,
							unitName: getUnitForMotorizedProcess(
								proc?.name || proc?.label,
								'service_crane',
							),
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
	]);

	return (
		<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
			<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
				<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
				<span>Xe cẩu tự hành, xe dịch vụ</span>
			</div>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={formControl}
				name={`${monthPath}.assignmentCodeIds` as any}
				label='1. Nhóm vật tư, tài sản (Xe cẩu / Xe dịch vụ / Xe nâng)'
				placeholder='Chọn nhóm xe cẩu, xe dịch vụ'
				options={assignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* LẶP QUA TỪNG NHÓM XE */}
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
						{/* HEADER XE */}
						<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
							<span className='text-sm font-bold text-gray-900'>
								{eq?.code
									? `${eq.code} - ${eq.name || eq.label}`
									: eq?.name || eq?.label || eqId}
							</span>
						</div>

						{/* CẤP 2: CHỌN CÔNG ĐOẠN CHO XE NÀY */}
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

						{/* LẶP QUA TỪNG CÔNG ĐOẠN */}
						{selectedProcs.map((procId) => {
							const proc = processes.find(
								(p) => p.id === procId || p.value === procId,
							);
							const scopeKey = `${eqId}_${procId}`;
							const procItems = items.filter(
								(it) =>
									it.equipmentId === eqId && it.productionProcessId === procId,
							);

							const procText =
								`${proc?.name || ''} ${proc?.label || ''} ${proc?.code || ''}`.toLowerCase();
							const isWatering =
								procText.includes('tưới đường') ||
								procText.includes('tuoi_duong') ||
								procText.includes('tưới mỏ') ||
								procText.includes('tuoi_mo');

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

									{/* CHỌN CHẤT LƯỢNG & (NẾU LÀ TƯỚI ĐƯỜNG MỎ THÌ HIỂN THỊ CUNG ĐỘ) */}
									<div
										className={
											isWatering
												? 'grid grid-cols-1 gap-3 md:grid-cols-2'
												: 'w-full'
										}
									>
										<FormMultiSelect
											control={formControl}
											name={
												`${monthPath}.equipmentQualities.${scopeKey}` as any
											}
											label='Chất lượng thiết bị'
											placeholder='Chọn chất lượng thiết bị'
											options={[
												{ value: 'A', label: 'Thiết bị loại A' },
												{ value: 'B', label: 'Thiết bị loại B' },
												{ value: 'C', label: 'Thiết bị loại C' },
											]}
										/>

										{isWatering && (
											<FormMultiSelect
												control={formControl}
												name={
													`${monthPath}.equipmentDistances.${scopeKey}` as any
												}
												label='Cung độ vận tải (Tưới đường mỏ)'
												placeholder='Chọn cung độ (km)'
												options={distances.map((d) => ({
													value: d.id || d.value,
													label: d.value ? `${d.value} km` : d.name || d.label,
												}))}
											/>
										)}
									</div>

									{/* BẢNG NHẬP SẢN LƯỢNG CHO CỤM CÔNG ĐOẠN NÀY */}
									{procItems.length > 0 && (
										<div className='space-y-2 pt-2'>
											<div className='text-xs font-semibold text-gray-700'>
												Bảng kế hoạch sản lượng ({procItems.length} tổ hợp)
											</div>

											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-black uppercase'>
														<tr>
															<th
																className={
																	isWatering
																		? 'w-[25%] px-3 py-2'
																		: 'w-[30%] px-3 py-2'
																}
															>
																Chất lượng
															</th>
															{isWatering && (
																<th className='w-[20%] px-3 py-2'>Cung độ</th>
															)}
															<th
																className={
																	isWatering
																		? 'w-[18%] px-3 py-2 text-center'
																		: 'w-[20%] px-3 py-2 text-center'
																}
															>
																Đơn vị tính
															</th>
															<th
																className={
																	isWatering
																		? 'w-[15%] px-3 py-2'
																		: 'w-[20%] px-3 py-2'
																}
															>
																Hệ số điều chỉnh
															</th>
															<th
																className={
																	isWatering
																		? 'w-[22%] px-3 py-2'
																		: 'w-[30%] px-3 py-2'
																}
															>
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
																	key={`${row.equipmentId}-${row.equipmentQuality}-${row.productionProcessId}-${row.haulDistanceId || ''}`}
																	className='hover:bg-gray-50/50'
																>
																	<td className='px-3 py-2'>
																		<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																			{row.equipmentQuality
																				? `Loại ${row.equipmentQuality}`
																				: '-'}
																		</div>
																	</td>
																	{isWatering && (
																		<td className='px-3 py-2'>
																			<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																				{row.haulDistanceValue
																					? `L ≤ ${row.haulDistanceValue} km`
																					: '-'}
																			</div>
																		</td>
																	)}
																	<td className='px-3 py-2'>
																		<div className='min-w-[110px]'>
																			<FormComboBox
																				value={
																					row.unitName ||
																					(isWatering ? 'tkm' : 'giờ')
																				}
																				onValueChange={(val) => {
																					const next = [...items];
																					if (globalIdx >= 0) {
																						next[globalIdx] = {
																							...next[globalIdx],
																							unitName: val,
																							unitOfMeasureId: val,
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
																			value={row.fuelAdjustmentFactor ?? 1.0}
																			onValueChange={(val) => {
																				const next = [...items];
																				if (globalIdx >= 0) {
																					next[globalIdx] = {
																						...next[globalIdx],
																						fuelAdjustmentFactor: val ?? 1.0,
																					};
																					setItems(next);
																				}
																			}}
																			placeholder='1.0'
																		/>
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
