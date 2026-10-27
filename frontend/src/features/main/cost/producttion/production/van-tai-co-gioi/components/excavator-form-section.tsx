import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { useEffect, useRef } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import {
	getAllowedProcessIds,
	type MotorizedPricingIndex,
} from '@/features/main/cost/plan/van-tai-co-gioi/pricing-index';
import type { ProductionFormSchema } from '../../production-form-schema';
import { getUnitForProcess } from '../utils';

type ExcavatorFormSectionProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	pricingIndex: MotorizedPricingIndex;
	assignmentCodes: any[];
	processes: any[];
	// Chỉ nhập cho 1 Nhóm vật tư, gọn ngay trong thẻ Nhóm vật tư (không khung loại xe / header nhóm)
	assignmentCodeId?: string;
};

export function ExcavatorFormSection({
	form,
	groupIndex,
	pricingIndex,
	assignmentCodes,
	processes,
	assignmentCodeId,
}: ExcavatorFormSectionProps) {
	const groupPath = `groups.${groupIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedGroup = useWatch({
		control: form.control,
		name: groupPath,
	}) as any;

	const categoryAssignmentCodeIds: string[] =
		watchedGroup?.excavatorAssignmentCodeIds || [];
	const assignmentCodeIds = assignmentCodeId
		? categoryAssignmentCodeIds.filter((id) => id === assignmentCodeId)
		: categoryAssignmentCodeIds;
	const equipmentProcesses: Record<string, string[]> =
		watchedGroup?.equipmentProcesses || {};
	// equipmentProcesses dùng chung cho mọi khối loại xe — khối này chỉ lấy Công đoạn có đơn giá ở
	// loại xe của mình, và chỉ thay các dòng thuộc loại xe này
	const getCategoryProcs = (assignmentCodeId: string) => {
		const all = equipmentProcesses[assignmentCodeId] || [];
		const allowed = getAllowedProcessIds(
			pricingIndex,
			'excavator_dozer',
			assignmentCodeId,
			all,
		);
		return all.filter((procId) => allowed.includes(procId));
	};
	const ownsItem = (it: any) =>
		getAllowedProcessIds(pricingIndex, 'excavator_dozer', it.equipmentId, [
			it.productionProcessId,
		]).includes(it.productionProcessId);
	const equipmentQualities: Record<string, string[]> =
		watchedGroup?.equipmentQualities || {};
	const items: any[] = watchedGroup?.motorizedItems || [];

	const prevEquipmentIdsRef = useRef<string[]>(assignmentCodeIds);

	const setItems = (newRows: typeof items) => {
		const currentItems: any[] =
			form.getValues(`${groupPath}.motorizedItems` as any) || [];
		const otherItems = currentItems.filter(
			(it) =>
				!(
					(assignmentCodeIds.includes(it.equipmentId) ||
						prevEquipmentIdsRef.current.includes(it.equipmentId)) &&
					ownsItem(it)
				),
		);
		form.setValue(`${groupPath}.motorizedItems` as any, [
			...otherItems,
			...newRows,
		]);
		prevEquipmentIdsRef.current = assignmentCodeIds;
	};

	useEffect(() => {
		if (assignmentCodeIds.length === 0) {
			setItems([]);
			return;
		}

		const newRows: any[] = [];

		assignmentCodeIds.forEach((eqId) => {
			const eq = assignmentCodes.find((a) => a.id === eqId || a.value === eqId);
			const eqProcs = getCategoryProcs(eqId);

			eqProcs.forEach((procId) => {
				const proc = processes.find(
					(p) => p.id === procId || p.value === procId,
				);
				const scopeKey = `${eqId}_${procId}`;
				const procQualities = [...(equipmentQualities[scopeKey] || [])].sort(
					(a, b) => a.localeCompare(b),
				);

				procQualities.forEach((quality) => {
					const existing = items.find(
						(it) =>
							it.equipmentId === eqId &&
							it.equipmentQuality === quality &&
							it.productionProcessId === procId,
					);

					newRows.push({
						equipmentId: eqId,
						equipmentCode: eq?.code,
						equipmentName: eq?.name || eq?.label,
						equipmentQuality: quality,
						productionProcessId: procId,
						productionProcessCode: proc?.code,
						productionProcessName: proc?.name || proc?.label,
						productionMeters: existing?.productionMeters ?? Number.NaN,
						fuelAdjustmentFactor: existing?.fuelAdjustmentFactor ?? 1,
						vehicleType: 4,
						unitName: getUnitForProcess(
							proc?.name || proc?.label,
							'excavator_dozer',
						),
					});
				});
			});
		});

		setItems(newRows);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [
		JSON.stringify(assignmentCodeIds),
		JSON.stringify(equipmentProcesses),
		pricingIndex,
		JSON.stringify(equipmentQualities),
	]);

	return (
		<div
			className={
				assignmentCodeId
					? 'space-y-4'
					: 'space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'
			}
		>
			{!assignmentCodeId && (
				<div className='border-b border-gray-100 pb-2 text-sm font-semibold text-black'>
					<span>Máy xúc, máy gạt </span>
				</div>
			)}

			{/* LẶP QUA TỪNG NHÓM THIẾT BỊ */}
			{assignmentCodeIds.map((eqId) => {
				const eq = assignmentCodes.find(
					(a) => a.id === eqId || a.value === eqId,
				);
				const selectedProcs = getCategoryProcs(eqId);

				return (
					<div
						key={eqId}
						className={
							assignmentCodeId
								? 'space-y-4'
								: 'space-y-4 rounded-lg border border-gray-300 bg-white p-4 shadow-2xs'
						}
					>
						{/* HEADER THIẾT BỊ */}
						{!assignmentCodeId && (
							<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
								<span className='text-sm font-bold text-gray-900'>
									{eq?.code
										? `${eq.code} - ${eq.name || eq.label}`
										: eq?.name || eq?.label || eqId}
								</span>
							</div>
						)}

						{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
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
									<div className='border-b border-gray-200 pb-2'>
										<span className='text-sm font-semibold text-black'>
											{proc?.code
												? `${proc.code} - ${proc.name || proc.label}`
												: proc?.name || proc?.label || procId}
										</span>
									</div>

									{/* CHỌN CHẤT LƯỢNG THIẾT BỊ CHO CÔNG ĐOẠN NÀY */}
									<div className='w-full'>
										<FormMultiSelect
											control={formControl}
											name={
												`${groupPath}.equipmentQualities.${scopeKey}` as any
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

									{/* BẢNG NHẬP SẢN LƯỢNG CHO CỤM CÔNG ĐOẠN NÀY */}
									{procItems.length > 0 && (
										<div className='space-y-2 pt-2'>
											<div className='text-xs font-semibold text-gray-700'>
												Bảng kế hoạch sản lượng thực tế ({procItems.length} tổ
												hợp)
											</div>

											<div className='scrollbar-sm overflow-x-auto rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-black'>
														<tr>
															<th className='w-[50%] px-3 py-2'>Chất lượng</th>
															<th className='w-[50%] px-3 py-2'>
																Sản lượng thực tế
															</th>
															<th className='w-32 px-3 py-2 whitespace-nowrap'>Hệ số</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-gray-100'>
														{procItems.map((row) => {
															const globalIdx = items.findIndex(
																(it) => it === row,
															);
															return (
																<tr
																	key={`${row.equipmentId}-${row.equipmentQuality}-${row.productionProcessId}`}
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
																	<td className='px-3 py-2'>
																		<FormNumberInput
																			className='min-w-[110px]'
																			value={row.fuelAdjustmentFactor}
																			onValueChange={(val) => {
																				const next = [...items];
																				if (globalIdx >= 0) {
																					next[globalIdx] = {
																						...next[globalIdx],
																						fuelAdjustmentFactor: val,
																					};
																					setItems(next);
																				}
																			}}
																			placeholder='1'
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
