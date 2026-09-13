import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { MultiSelect } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { Copy, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import {
	forwardRef,
	useEffect,
	useImperativeHandle,
	useState,
} from 'react';
import { MotorizedVacuumTruckUnitPrice } from './columns';
import { MotorizedSubFormHandle } from '../scania/form';
import {
	computeNextPeriodMonths,
	VacuumTruckPeriodData,
} from '../unit-price/types';

export type MotorizedVacuumTruckFormProps =
	ActionDialogProps<MotorizedVacuumTruckUnitPrice> & {
		isDuplicate?: boolean;
		hideConfirmButton?: boolean;
	};

const extractData = (res: any): any[] => {
	if (!res) return [];
	if (Array.isArray(res)) return res;
	if (Array.isArray(res.result)) return res.result;
	if (Array.isArray(res.result?.data)) return res.result.data;
	if (Array.isArray(res.data)) return res.data;
	return [];
};

const fetchCatalogList = async (url: string) => {
	try {
		const res: any = await api.pagging(url, {
			ignorePagination: true,
			pageSize: 1000,
		});
		const data = extractData(res);
		if (data.length > 0) return data;
	} catch (e) {
		// Fallback GET
	}
	try {
		const res: any = await api.get(url);
		return extractData(res);
	} catch (e) {
		return [];
	}
};

function computeItemsForVacuumTruckPeriod(
	assignmentCodeIds: string[],
	equipmentProcesses: Record<string, string[]>,
	equipmentQualities: Record<string, string[]>,
	equipmentDistances: Record<string, string[]>,
	assignmentCodes: any[],
	processOptions: any[],
	distanceOptions: any[],
	existingItems: any[] = [],
): any[] {
	const newItems: any[] = [];

	assignmentCodeIds.forEach((acId: string) => {
		const acObj = assignmentCodes.find((a) => a.id === acId);
		const title = acObj
			? acObj.code
				? `${acObj.code} - ${acObj.name}`
				: acObj.name
			: acId;
		const materialName = acObj ? acObj.name || acObj.code || acId : acId;
		const selectedProcs: string[] = equipmentProcesses[acId] || [];

		selectedProcs.forEach((procId: string) => {
			const scopeKey = `${acId}_${procId}`;
			const procObj = processOptions.find(
				(p) => p.value === procId || p.label === procId,
			);
			const procName = procObj ? procObj.label : procId;
			const isMoving =
				procName.toLowerCase().includes('di chuyển') ||
				procName.toLowerCase().includes('cung độ');

			const selectedQualities: string[] = equipmentQualities[scopeKey] || [];
			const selectedDists: string[] = equipmentDistances[scopeKey] || [];

			selectedQualities.forEach((qual: string) => {
				if (isMoving) {
					if (selectedDists.length > 0) {
						selectedDists.forEach((distId: string) => {
							const distObj = distanceOptions.find(
								(d: any) => d.value === distId || d.id === distId,
							);
							const distValue = distObj
								? (distObj as any).label || (distObj as any).value
								: '';

							const existing = existingItems.find(
								(it: any) =>
									it.assignmentCodeId === acId &&
									it.productionProcessId === procId &&
									it.equipmentQuality === qual &&
									it.haulDistanceId === distId,
							);

							newItems.push({
								tempId:
									existing?.tempId ||
									`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
								id: existing?.id,
								detailId: existing?.detailId,
								assignmentCodeId: acId,
								equipmentQuality: qual,
								productionProcessId: procId,
								productionProcessName: procName,
								title,
								materialName,
								haulDistanceId: distId,
								haulDistanceValue: distValue,
								fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
								maintenanceUnitPrice: existing
									? existing.maintenanceUnitPrice
									: 0,
							});
						});
					}
				} else {
					const existing = existingItems.find(
						(it: any) =>
							it.assignmentCodeId === acId &&
							it.productionProcessId === procId &&
							it.equipmentQuality === qual &&
							!it.haulDistanceId,
					);

					newItems.push({
						tempId:
							existing?.tempId ||
							`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: existing?.id,
						detailId: existing?.detailId,
						assignmentCodeId: acId,
						equipmentQuality: qual,
						productionProcessId: procId,
						productionProcessName: procName,
						title,
						materialName,
						haulDistanceId: null,
						haulDistanceValue: '',
						fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
						maintenanceUnitPrice: existing
							? existing.maintenanceUnitPrice
							: 0,
					});
				}
			});
		});
	});

	return newItems;
}

interface VacuumTruckPeriodSectionProps {
	period: VacuumTruckPeriodData;
	totalPeriods: number;
	assignmentCodes: any[];
	processOptions: any[];
	distanceOptions: any[];
	onUpdate: (updated: VacuumTruckPeriodData) => void;
	onCopy: () => void;
	onDelete: () => void;
}

function VacuumTruckPeriodSection({
	period,
	totalPeriods,
	assignmentCodes,
	processOptions,
	distanceOptions,
	onUpdate,
	onCopy,
	onDelete,
}: VacuumTruckPeriodSectionProps) {
	const handleAssignmentCodesChange = (newAcIds: string[]) => {
		const nextItems = computeItemsForVacuumTruckPeriod(
			newAcIds,
			period.equipmentProcesses,
			period.equipmentQualities,
			period.equipmentDistances,
			assignmentCodes,
			processOptions,
			distanceOptions,
			period.items,
		);
		onUpdate({
			...period,
			assignmentCodeIds: newAcIds,
			items: nextItems,
		});
	};

	const handleProcessesChange = (acId: string, newProcs: string[]) => {
		const nextProcs = { ...period.equipmentProcesses, [acId]: newProcs };
		const nextItems = computeItemsForVacuumTruckPeriod(
			period.assignmentCodeIds,
			nextProcs,
			period.equipmentQualities,
			period.equipmentDistances,
			assignmentCodes,
			processOptions,
			distanceOptions,
			period.items,
		);
		onUpdate({
			...period,
			equipmentProcesses: nextProcs,
			items: nextItems,
		});
	};

	const handleScopeFieldChange = (
		scopeKey: string,
		fieldName: 'equipmentQualities' | 'equipmentDistances',
		values: string[],
	) => {
		const nextField = { ...period[fieldName], [scopeKey]: values };
		const nextPeriod = { ...period, [fieldName]: nextField };
		const nextItems = computeItemsForVacuumTruckPeriod(
			nextPeriod.assignmentCodeIds,
			nextPeriod.equipmentProcesses,
			nextPeriod.equipmentQualities,
			nextPeriod.equipmentDistances,
			assignmentCodes,
			processOptions,
			distanceOptions,
			period.items,
		);
		onUpdate({
			...nextPeriod,
			items: nextItems,
		});
	};

	const handleItemValueChange = (
		tempId: string,
		field: 'fuelUnitPrice' | 'maintenanceUnitPrice',
		val: number | undefined,
	) => {
		const nextItems = period.items.map((it) => {
			if (it.tempId === tempId || it.id === tempId) {
				return { ...it, [field]: val ?? 0 };
			}
			return it;
		});
		onUpdate({
			...period,
			items: nextItems,
		});
	};

	return (
		<div className='flex flex-col gap-4 rounded-xl border border-neutral-300 bg-neutral-50/70 p-5 shadow-xs transition-all'>
			{/* Hàng 1: Thời gian bắt đầu, kết thúc, Copy, Delete */}
			<div className='flex items-end gap-3'>
				<MonthYearInput
					label='Thời gian bắt đầu'
					value={period.startMonth}
					onChange={(val) => onUpdate({ ...period, startMonth: val })}
					className='flex-1'
				/>
				<MonthYearInput
					label='Thời gian kết thúc'
					value={period.endMonth}
					onChange={(val) => onUpdate({ ...period, endMonth: val })}
					className='flex-1'
				/>
				<div className='mb-2 flex items-center gap-2'>
					<Button
						type='button'
						variant='ghost'
						size='icon'
						onClick={onCopy}
						className='hover:text-primary h-fit w-fit border-0 bg-transparent p-0 text-neutral-500 shadow-none hover:bg-transparent'
						title='Sao chép'
					>
						<Copy className='size-5' />
					</Button>
					{totalPeriods > 1 && (
						<Button
							type='button'
							variant='ghost'
							size='icon'
							onClick={onDelete}
							className='h-fit w-fit border-0 bg-transparent p-0 text-red-500 shadow-none hover:bg-transparent hover:text-red-700'
							title='Xóa'
						>
							<XCircleIcon className='size-5 text-red-500' />
						</Button>
					)}
				</div>
			</div>

			{/* Hàng 2: Nhóm vật tư, tài sản (Xe hút bùn, chất thải) */}
			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm xe hút bùn, chất thải'
				options={assignmentCodes.map((item) => ({
					label: item.code
						? `${item.code} - ${item.name}`
						: item.name || item.id,
					value: item.id,
				}))}
				values={(period.assignmentCodeIds || []).map((id) => {
					const item = assignmentCodes.find((a) => a.id === id);
					return {
						value: id,
						label: item
							? item.code
								? `${item.code} - ${item.name}`
								: item.name || item.id
							: id,
					};
				})}
				onValuesChange={(selected) =>
					handleAssignmentCodesChange(selected.map((s) => s.value))
				}
			/>

			{/* Form chi tiết từng xe */}
			{(period.assignmentCodeIds || []).map((acId: string) => {
				const acObj = assignmentCodes.find((a) => a.id === acId);
				const title = acObj
					? acObj.code
						? `${acObj.code} - ${acObj.name}`
						: acObj.name
					: acId;
				const materialName = acObj ? acObj.name || acObj.code || acId : acId;
				const selectedProcList: string[] =
					period.equipmentProcesses?.[acId] || [];

				return (
					<div
						key={acId}
						className='space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-xs'
					>
						<div className='flex items-center justify-between border-b border-neutral-100 pb-2'>
							<span className='text-sm font-semibold text-black'>{title}</span>
						</div>

						{/* CÔNG ĐOẠN SẢN XUẤT CHO XE NÀY */}
						<MultiSelect
							label='Công đoạn sản xuất'
							placeholder='Chọn các công đoạn sản xuất'
							options={processOptions.map((opt) => ({
								label: opt.label,
								value: opt.value,
							}))}
							values={selectedProcList.map((id) => {
								const p = processOptions.find((opt) => opt.value === id);
								return { value: id, label: p ? p.label : id };
							})}
							onValuesChange={(selected) =>
								handleProcessesChange(acId, selected.map((s) => s.value))
							}
						/>

						{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
						{selectedProcList.map((procId: string) => {
							const scopeKey = `${acId}_${procId}`;
							const procObj = processOptions.find((p) => p.value === procId);
							const procName = procObj ? procObj.label : procId;
							const isMoving =
								procName.toLowerCase().includes('di chuyển') ||
								procName.toLowerCase().includes('cung độ');

							const currentQualities: string[] =
								period.equipmentQualities?.[scopeKey] || [];
							const currentDists: string[] =
								period.equipmentDistances?.[scopeKey] || [];

							return (
								<div
									key={procId}
									className='space-y-4 rounded-lg border border-neutral-200 bg-neutral-50/50 p-4 shadow-2xs'
								>
									<div className='border-b border-neutral-200/80 pb-2'>
										<span className='text-sm font-semibold text-black'>
											{procName}
										</span>
									</div>

									<div
										className={`grid grid-cols-1 gap-3 ${
											isMoving ? 'md:grid-cols-2' : ''
										}`}
									>
										<MultiSelect
											label='Chất lượng thiết bị'
											placeholder='Chọn chất lượng thiết bị'
											options={[
												{ label: 'Thiết bị loại A', value: 'A' },
												{ label: 'Thiết bị loại B', value: 'B' },
												{ label: 'Thiết bị loại C', value: 'C' },
											]}
											values={currentQualities.map((q) => ({
												label: `Thiết bị loại ${q}`,
												value: q,
											}))}
											onValuesChange={(selected) =>
												handleScopeFieldChange(
													scopeKey,
													'equipmentQualities',
													selected.map((s) => s.value),
												)
											}
										/>

										{isMoving && (
											<MultiSelect
												label='Cung độ vận tải'
												placeholder='Chọn cung độ vận tải'
												options={distanceOptions.map((opt) => ({
													label: opt.label,
													value: opt.value,
												}))}
												values={currentDists.map((dId) => {
													const d = distanceOptions.find(
														(opt) => opt.value === dId || opt.id === dId,
													);
													return { value: dId, label: d ? d.label : dId };
												})}
												onValuesChange={(selected) =>
													handleScopeFieldChange(
														scopeKey,
														'equipmentDistances',
														selected.map((s) => s.value),
													)
												}
											/>
										)}
									</div>

									{/* BẢNG ĐƠN GIÁ */}
									{currentQualities.length > 0 &&
										(!isMoving || currentDists.length > 0) && (
											<div className='space-y-2 pt-2'>
												<div className='w-full overflow-x-auto rounded-md border border-neutral-200 bg-white shadow-xs'>
													<table className='w-full min-w-[600px] text-left text-sm'>
														<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
															<tr>
																<th className='whitespace-nowrap px-4 py-2.5'>
																	{isMoving
																		? 'Cung độ / Chất lượng thiết bị'
																		: 'Chất lượng thiết bị'}
																</th>
																<th className='w-40 min-w-[140px] whitespace-nowrap px-4 py-2.5'>
																	Đơn giá Nhiên liệu (đ/ca)
																</th>
																<th className='w-40 min-w-[140px] whitespace-nowrap px-4 py-2.5'>
																	Đơn giá SCTX (đ/ca)
																</th>
															</tr>
														</thead>
														<tbody className='divide-y divide-neutral-100'>
															{currentQualities.map((qual: string) => {
																if (isMoving) {
																	return currentDists.map((distId: string) => {
																		const item = period.items.find(
																			(it: any) =>
																				it.assignmentCodeId === acId &&
																				it.productionProcessId === procId &&
																				it.equipmentQuality === qual &&
																				it.haulDistanceId === distId,
																		);
																		if (!item) return null;

																		return (
																			<tr
																				key={`${qual}_${distId}`}
																				className='transition-colors hover:bg-neutral-50/50'
																			>
																				<td className='whitespace-nowrap px-4 py-2'>
																					<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-neutral-300 bg-white px-3 text-xs font-medium text-black'>
																						{item.haulDistanceValue
																							? `${materialName} (Loại ${qual}) - ${item.haulDistanceValue} km`
																							: `${materialName} (Loại ${qual})`}
																					</div>
																				</td>
																				<td className='w-40 min-w-[140px] px-4 py-2'>
																					<FormNumberInput
																						value={item.fuelUnitPrice ?? 0}
																						onValueChange={(val) =>
																							handleItemValueChange(
																								item.tempId || item.id,
																								'fuelUnitPrice',
																								val,
																							)
																						}
																						placeholder='Nhập đơn giá'
																					/>
																				</td>
																				<td className='w-40 min-w-[140px] px-4 py-2'>
																					<FormNumberInput
																						value={item.maintenanceUnitPrice ?? 0}
																						onValueChange={(val) =>
																							handleItemValueChange(
																								item.tempId || item.id,
																								'maintenanceUnitPrice',
																								val,
																							)
																						}
																						placeholder='Nhập đơn giá'
																					/>
																				</td>
																			</tr>
																		);
																	});
																} else {
																	const item = period.items.find(
																		(it: any) =>
																			it.assignmentCodeId === acId &&
																			it.productionProcessId === procId &&
																			it.equipmentQuality === qual &&
																			!it.haulDistanceId,
																	);
																	if (!item) return null;

																	return (
																		<tr key={qual} className='transition-colors hover:bg-neutral-50/50'>
																			<td className='whitespace-nowrap px-4 py-2'>
																				<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-neutral-300 bg-white px-3 text-xs font-medium text-black'>
																					{materialName} (Loại {qual})
																				</div>
																			</td>
																			<td className='w-40 min-w-[140px] px-4 py-2'>
																				<FormNumberInput
																					value={item.fuelUnitPrice ?? 0}
																					onValueChange={(val) =>
																						handleItemValueChange(
																							item.tempId || item.id,
																							'fuelUnitPrice',
																							val,
																						)
																					}
																					placeholder='Nhập đơn giá'
																				/>
																			</td>
																			<td className='w-40 min-w-[140px] px-4 py-2'>
																				<FormNumberInput
																					value={item.maintenanceUnitPrice ?? 0}
																					onValueChange={(val) =>
																						handleItemValueChange(
																							item.tempId || item.id,
																							'maintenanceUnitPrice',
																							val,
																						)
																					}
																					placeholder='Nhập đơn giá'
																				/>
																			</td>
																		</tr>
																	);
																}
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

export const MotorizedVacuumTruckForm = forwardRef<
	MotorizedSubFormHandle,
	MotorizedVacuumTruckFormProps
>(function MotorizedVacuumTruckForm(
	{
		data,
		row,
		isDuplicate = false,
		hideConfirmButton = false,
	}: MotorizedVacuumTruckFormProps,
	ref,
) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const [assignmentCodes, setAssignmentCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>([]);
	const [distanceOptions, setDistanceOptions] = useState<any[]>([]);

	const [periods, setPeriods] = useState<VacuumTruckPeriodData[]>([
		{
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: '',
			endMonth: '',
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			equipmentDistances: {},
			items: [],
		},
	]);

	useEffect(() => {
		Promise.all([
			fetchCatalogList(API.CATALOG.CONTRACT_CODE.LIST),
			fetchCatalogList(API.CATALOG.PROCESS.STEP.LIST),
			fetchCatalogList(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.LIST),
		])
			.then(([acList, procList, distList]) => {
				setAssignmentCodes(acList);

				const procOpts = procList.map((item: any) => ({
					label: item.name || item.code,
					value: item.id,
					name: item.name || item.code,
				}));
				setProcessOptions(procOpts);

				const distOpts = distList.map((item: any) => ({
					label: item.value || item.name || item.distanceRange || item.code,
					value: item.id,
					name: item.value || item.name || item.distanceRange || item.code,
				}));
				setDistanceOptions(distOpts);

				if (!row) return;

				const allProcs: any[] = (row as any).allProcesses || [row];
				const initialAcId =
					row.assignmentCodeId || (row as any).equipmentId || '';

				const initialProcsList: string[] = [];
				const initialQualitiesMap: Record<string, string[]> = {};
				const initialDistsMap: Record<string, string[]> = {};
				const initialItems: any[] = [];

				allProcs.forEach((p: any) => {
					const q = (p.equipmentQuality || 'A')
						.replace(/^Thiết bị loại\s*/i, '')
						.replace(/^Loại\s*/i, '')
						.trim();

					if (
						p.productionProcessId &&
						!initialProcsList.includes(p.productionProcessId)
					) {
						initialProcsList.push(p.productionProcessId);
					}

					const scopeKey = `${initialAcId}_${p.productionProcessId}`;
					if (!initialQualitiesMap[scopeKey]) {
						initialQualitiesMap[scopeKey] = [];
					}
					if (q && !initialQualitiesMap[scopeKey].includes(q)) {
						initialQualitiesMap[scopeKey].push(q);
					}

					if (!initialDistsMap[scopeKey]) {
						initialDistsMap[scopeKey] = [];
					}

					(p.details || []).forEach((d: any) => {
						if (
							d.haulDistanceId &&
							!initialDistsMap[scopeKey].includes(d.haulDistanceId)
						) {
							initialDistsMap[scopeKey].push(d.haulDistanceId);
						}

						initialItems.push({
							tempId: `item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
							id: isDuplicate ? undefined : p.id,
							detailId: isDuplicate ? undefined : d.id,
							assignmentCodeId: initialAcId,
							equipmentQuality: q,
							productionProcessId: p.productionProcessId,
							productionProcessName:
								p.productionProcessName || p.productionProcess || '',
							haulDistanceId: d.haulDistanceId || null,
							haulDistanceValue: d.haulDistanceValue || '',
							title: p.assignmentCodeName || initialAcId,
							fuelUnitPrice: d.fuelUnitPrice ?? 0,
							maintenanceUnitPrice: d.maintenanceUnitPrice ?? 0,
						});
					});
				});

				setPeriods([
					{
						tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: isDuplicate ? undefined : row.id,
						startMonth: row.startMonth?.substring(0, 7) || '',
						endMonth: row.endMonth?.substring(0, 7) || '',
						assignmentCodeIds: initialAcId ? [initialAcId] : [],
						equipmentProcesses: initialAcId
							? { [initialAcId]: initialProcsList }
							: {},
						equipmentQualities: initialQualitiesMap,
						equipmentDistances: initialDistsMap,
						items: initialItems,
					},
				]);
			})
			.catch((err) => {
				console.error(err);
			});
	}, [row, isDuplicate]);

	const handleAddPeriod = () => {
		const lastPeriod = periods[periods.length - 1];
		const nextMonths = computeNextPeriodMonths(lastPeriod?.endMonth);
		const newPeriod: VacuumTruckPeriodData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			equipmentDistances: {},
			items: [],
		};
		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;
		const nextMonths = computeNextPeriodMonths(target.endMonth);
		const copiedPeriod: VacuumTruckPeriodData = {
			...target,
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [...target.assignmentCodeIds],
			equipmentProcesses: JSON.parse(JSON.stringify(target.equipmentProcesses)),
			equipmentQualities: JSON.parse(JSON.stringify(target.equipmentQualities)),
			equipmentDistances: JSON.parse(JSON.stringify(target.equipmentDistances)),
			items: target.items.map((it) => ({
				...it,
				tempId: `item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: undefined,
				detailId: undefined,
			})),
		};
		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, copiedPeriod);
			return next;
		});
	};

	const handleDeletePeriod = (index: number) => {
		if (periods.length <= 1) return;
		setPeriods((prev) => prev.filter((_, i) => i !== index));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: VacuumTruckPeriodData,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const submitInternal = async (forceCreate?: boolean): Promise<boolean> => {
		try {
			if (periods.length === 0) {
				popup.error(
					'Cần có ít nhất một khoảng thời gian áp dụng cho Xe hút bùn',
				);
				return false;
			}

			for (let i = 0; i < periods.length; i++) {
				const p = periods[i];
				const order = i + 1;
				if (!p.startMonth || !p.startMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian bắt đầu ở khoảng thời gian thứ ${order} của Xe hút bùn`,
					);
					return false;
				}
				if (!p.endMonth || !p.endMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian kết thúc ở khoảng thời gian thứ ${order} của Xe hút bùn`,
					);
					return false;
				}
				if (p.startMonth > p.endMonth) {
					popup.error(
						`Thời gian bắt đầu không được lớn hơn thời gian kết thúc ở khoảng thời gian thứ ${order} của Xe hút bùn`,
					);
					return false;
				}
				if (p.assignmentCodeIds.length === 0) {
					popup.error(
						`Vui lòng chọn Nhóm xe hút bùn ở khoảng thời gian thứ ${order}`,
					);
					return false;
				}
			}

			for (let i = 0; i < periods.length; i++) {
				const p = periods[i];
				const isPeriodCreate =
					forceCreate !== undefined ? forceCreate : isDuplicate || !p.id;

				const startMonth =
					p.startMonth.length === 7 ? `${p.startMonth}-01` : p.startMonth;
				const endMonth =
					p.endMonth.length === 7 ? `${p.endMonth}-01` : p.endMonth;

				const groupedHeaders: Record<
					string,
					{
						id?: string;
						assignmentCodeId: string;
						equipmentQuality: string;
						productionProcessId: string;
						startMonth: string;
						endMonth: string;
						details: Array<{
							haulDistanceId: string | null;
							fuelUnitPrice: number;
							maintenanceUnitPrice: number;
						}>;
					}
				> = {};

				p.items.forEach((item: any) => {
					const key = `${item.assignmentCodeId}_${item.equipmentQuality}_${item.productionProcessId}`;
					if (!groupedHeaders[key]) {
						groupedHeaders[key] = {
							id: item.id,
							assignmentCodeId: item.assignmentCodeId,
							equipmentQuality: item.equipmentQuality,
							productionProcessId: item.productionProcessId,
							startMonth,
							endMonth,
							details: [],
						};
					}

					groupedHeaders[key].details.push({
						haulDistanceId: item.haulDistanceId || null,
						fuelUnitPrice: Number(item.fuelUnitPrice) || 0,
						maintenanceUnitPrice: Number(item.maintenanceUnitPrice) || 0,
					});
				});

				const promises = Object.values(groupedHeaders).map((header) => {
					const payload = {
						assignmentCodeId: header.assignmentCodeId,
						equipmentQuality: header.equipmentQuality,
						productionProcessId: header.productionProcessId,
						startMonth: header.startMonth,
						endMonth: header.endMonth,
						details: header.details,
					};

					if (header.id && !isDuplicate && !isPeriodCreate) {
						return api.put(
							API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.UPDATE,
							{
								id: header.id,
								...payload,
							},
						);
					} else {
						return api.post(
							API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.CREATE,
							payload,
						);
					}
				});

				await Promise.all(promises);
			}

			return true;
		} catch (error) {
			popup.error(error);
			return false;
		}
	};

	useImperativeHandle(ref, () => ({
		submit: submitInternal,
	}));

	const handleSubmit = async (e?: React.FormEvent) => {
		e?.preventDefault();
		const ok = await submitInternal();
		if (ok) {
			popup.success(
				row && !isDuplicate
					? 'Cập nhật đơn giá thành công'
					: 'Thêm mới đơn giá thành công',
			);
			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		}
	};

	return (
		<form onSubmit={handleSubmit}>
			<div className='space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-2xs'>
				<div className='border-b border-neutral-100 pb-2 text-sm font-semibold text-black'>
					<span>Hút bùn, chất thải (Xe hút bùn, chất thải)</span>
				</div>

				{/* DANH SÁCH CÁC KHOẢNG THỜI GIAN */}
				<div className='flex flex-col gap-5'>
					{periods.map((period, index) => (
						<VacuumTruckPeriodSection
							key={period.tempId}
							period={period}
							totalPeriods={periods.length}
							assignmentCodes={assignmentCodes}
							processOptions={processOptions}
							distanceOptions={distanceOptions}
							onUpdate={(updated) => handleUpdatePeriod(index, updated)}
							onDelete={() => handleDeletePeriod(index)}
							onCopy={() => handleCopyPeriod(index)}
						/>
					))}

					{/* NÚT THÊM THỜI GIAN */}
					<div className='flex justify-start pt-1'>
						<Button
							type='button'
							variant='ghost'
							size='sm'
							onClick={handleAddPeriod}
							className='flex h-fit w-fit items-center gap-1.5 border-0 bg-transparent p-0 shadow-none hover:bg-transparent'
						>
							<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
							<span className='text-sm text-black'>Thêm thời gian</span>
						</Button>
					</div>
				</div>

				{!hideConfirmButton && (
					<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
				)}
			</div>
		</form>
	);
});

export default MotorizedVacuumTruckForm;
