import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumberInput } from '@/components/form/form-number';
import { useEffect } from 'react';
import { useWatch, type UseFormReturn } from 'react-hook-form';
import type { ProductionFormSchema } from '../../production-form-schema';
import { getUnitForProcess } from '../utils';

type ExcavatorFormSectionProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	assignmentCodes: any[];
	processes: any[];
};

export function ExcavatorFormSection({
	form,
	groupIndex,
	assignmentCodes,
	processes,
}: ExcavatorFormSectionProps) {
	const groupPath = `groups.${groupIndex}` as const;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const formControl = form.control as any;

	const watchedGroup = useWatch({
		control: form.control,
		name: groupPath,
	}) as any;

	const assignmentCodeIds: string[] =
		watchedGroup?.excavatorAssignmentCodeIds || [];
	const equipmentProcesses: Record<string, string[]> =
		watchedGroup?.equipmentProcesses || {};
	const equipmentQualities: Record<string, string[]> =
		watchedGroup?.equipmentQualities || {};
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
		JSON.stringify(equipmentQualities),
	]);

	return (
		<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
			<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
				<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
				<span>Máy xúc, máy gạt </span>
			</div>

			{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
			<FormMultiSelect
				control={formControl}
				name={`${groupPath}.excavatorAssignmentCodeIds` as any}
				label='1. Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((a) => ({
					value: a.id || a.value,
					label: a.code
						? `${a.code} - ${a.name || a.label}`
						: a.name || a.label,
				}))}
			/>

			{/* LẶP QUA TỪNG NHÓM THIẾT BỊ */}
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
						{/* HEADER THIẾT BỊ */}
						<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
							<span className='text-sm font-bold text-gray-900'>
								{eq?.code
									? `${eq.code} - ${eq.name || eq.label}`
									: eq?.name || eq?.label || eqId}
							</span>
						</div>

						{/* CẤP 2: CHỌN CÔNG ĐOẠN CHO THIẾT BỊ NÀY */}
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
									<div className='flex items-center gap-2 border-b border-gray-200 pb-2'>
										<span className='h-2 w-2 rounded-full bg-blue-500' />
										<span className='text-xs font-semibold text-blue-800 uppercase'>
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

											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-black'>
														<tr>
															<th className='w-[50%] px-3 py-2'>Chất lượng</th>
															<th className='w-[50%] px-3 py-2'>
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
