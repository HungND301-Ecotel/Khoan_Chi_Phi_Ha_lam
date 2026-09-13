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
import type { CargoType } from '@/features/main/catalog/cargo-type/columns';
import {
	LocationType,
	type TransportLocation,
} from '@/features/main/catalog/transport-location/columns';
import { api } from '@/lib/api';
import { Copy, InfoIcon, PlusCircleIcon, XCircleIcon } from 'lucide-react';
import {
	forwardRef,
	useEffect,
	useImperativeHandle,
	useState,
} from 'react';
import { MotorizedScaniaUnitPrice } from './columns';
import {
	computeNextPeriodMonths,
	ScaniaPeriodData,
} from '../unit-price/types';

export type MotorizedSubFormHandle = {
	submit: (forceCreate?: boolean) => Promise<boolean>;
};

export type MotorizedScaniaFormProps =
	ActionDialogProps<MotorizedScaniaUnitPrice> & {
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

function computeItemsForScaniaPeriod(
	assignmentCodeIds: string[],
	equipmentProcesses: Record<string, string[]>,
	equipmentQualities: Record<string, string[]>,
	equipmentDistances: Record<string, string[]>,
	processCargoTypes: Record<string, string[]>,
	processPickupLocations: Record<string, string[]>,
	processDropoffLocations: Record<string, string[]>,
	assignmentCodes: any[],
	processOptions: any[],
	distanceOptions: any[],
	cargoTypes: any[],
	locations: any[],
	existingItems: any[] = [],
): any[] {
	const newItems: any[] = [];

	assignmentCodeIds.forEach((acId: string) => {
		const acObj = assignmentCodes.find((a) => a.id === acId);
		const title = acObj ? acObj.name || acObj.code || acId : acId;

		const selectedProcs: string[] = equipmentProcesses[acId] || [];

		selectedProcs.forEach((procId: string) => {
			const scopeKey = `${acId}_${procId}`;
			const procObj = processOptions.find((p) => p.value === procId);
			const procName = procObj ? procObj.label : procId;

			const selectedQualities: string[] = equipmentQualities[scopeKey] || [];
			const selectedDists: string[] = equipmentDistances[scopeKey] || [];

			const procCargoIds: string[] = processCargoTypes[scopeKey] || [];
			const procPickupIds: string[] = processPickupLocations[scopeKey] || [];
			const procDropoffIds: string[] = processDropoffLocations[scopeKey] || [];

			const cargoList = procCargoIds.length > 0 ? procCargoIds : [''];
			const dropoffList = procDropoffIds.length > 0 ? procDropoffIds : [''];
			const distList = selectedDists.length > 0 ? selectedDists : [''];

			const procPickupNames = procPickupIds
				.map((id) => {
					const l = locations.find(
						(loc: any) => loc.id === id || loc.value === id,
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
								(c: any) => c.id === cargoId || c.value === cargoId,
							);
							const cargoName = cargoObj
								? (cargoObj as any).name || (cargoObj as any).label
								: '';

							const dropoffObj = locations.find(
								(l: any) => l.id === dropoffId || l.value === dropoffId,
							);
							const dropoffName = dropoffObj
								? (dropoffObj as any).name || (dropoffObj as any).label
								: '';

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
									(it.cargoTypeId || '') === (cargoId || '') &&
									(it.dumpingLocationId || '') === (dropoffId || '') &&
									(it.haulDistanceId || '') === (distId || ''),
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
								cargoTypeId: cargoId || null,
								cargoTypeName: cargoName,
								receivingLocationIds: procPickupIds,
								receivingLocationNames: combinedPickupName,
								receivingLocationId: procPickupIds[0] || null,
								receivingLocationName: combinedPickupName,
								dumpingLocationId: dropoffId || null,
								dumpingLocationName: dropoffName,
								haulDistanceId: distId || null,
								haulDistanceValue: distValue,
								fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
								powerUnitPrice: existing ? existing.powerUnitPrice : 0,
								maintenanceUnitPrice: existing
									? existing.maintenanceUnitPrice
									: 0,
							});
						});
					});
				});
			});
		});
	});

	return newItems;
}

interface ScaniaPeriodSectionProps {
	period: ScaniaPeriodData;
	totalPeriods: number;
	assignmentCodes: any[];
	processOptions: any[];
	distanceOptions: any[];
	cargoTypes: any[];
	locations: any[];
	pickupOptions: any[];
	dropoffOptions: any[];
	onUpdate: (updated: ScaniaPeriodData) => void;
	onCopy: () => void;
	onDelete: () => void;
}

function ScaniaPeriodSection({
	period,
	totalPeriods,
	assignmentCodes,
	processOptions,
	distanceOptions,
	cargoTypes,
	locations,
	pickupOptions,
	dropoffOptions,
	onUpdate,
	onCopy,
	onDelete,
}: ScaniaPeriodSectionProps) {
	const handleAssignmentCodesChange = (newAcIds: string[]) => {
		const nextItems = computeItemsForScaniaPeriod(
			newAcIds,
			period.equipmentProcesses,
			period.equipmentQualities,
			period.equipmentDistances,
			period.processCargoTypes,
			period.processPickupLocations,
			period.processDropoffLocations,
			assignmentCodes,
			processOptions,
			distanceOptions,
			cargoTypes,
			locations,
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
		const nextItems = computeItemsForScaniaPeriod(
			period.assignmentCodeIds,
			nextProcs,
			period.equipmentQualities,
			period.equipmentDistances,
			period.processCargoTypes,
			period.processPickupLocations,
			period.processDropoffLocations,
			assignmentCodes,
			processOptions,
			distanceOptions,
			cargoTypes,
			locations,
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
		fieldName:
			| 'equipmentQualities'
			| 'equipmentDistances'
			| 'processCargoTypes'
			| 'processPickupLocations'
			| 'processDropoffLocations',
		values: string[],
	) => {
		const nextField = { ...period[fieldName], [scopeKey]: values };
		const nextPeriod = { ...period, [fieldName]: nextField };
		const nextItems = computeItemsForScaniaPeriod(
			nextPeriod.assignmentCodeIds,
			nextPeriod.equipmentProcesses,
			nextPeriod.equipmentQualities,
			nextPeriod.equipmentDistances,
			nextPeriod.processCargoTypes,
			nextPeriod.processPickupLocations,
			nextPeriod.processDropoffLocations,
			assignmentCodes,
			processOptions,
			distanceOptions,
			cargoTypes,
			locations,
			period.items,
		);
		onUpdate({
			...nextPeriod,
			items: nextItems,
		});
	};

	const handleItemValueChange = (
		tempId: string,
		field: 'fuelUnitPrice' | 'powerUnitPrice' | 'maintenanceUnitPrice',
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
			{/* Hàng 1: Thời gian bắt đầu (50%), Thời gian kết thúc (50%), Action buttons */}
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

			{/* Hàng 2: Nhóm vật tư, tài sản (Xe Scania) */}
			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm vật tư, tài sản'
				options={assignmentCodes.map((item) => ({
					label: item.name || item.code || item.id,
					value: item.id,
				}))}
				values={(period.assignmentCodeIds || []).map((id) => {
					const item = assignmentCodes.find((a) => a.id === id);
					return {
						value: id,
						label: item ? item.name || item.code || item.id : id,
					};
				})}
				onValuesChange={(selected) =>
					handleAssignmentCodesChange(selected.map((s) => s.value))
				}
			/>

			{/* Form vật tư và các thuộc tính chi tiết bên trong */}
			{(period.assignmentCodeIds || []).map((acId: string) => {
				const acObj = assignmentCodes.find((a) => a.id === acId);
				const title = acObj ? acObj.name || acObj.code || acId : acId;
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

						{/* CHỌN CÔNG ĐOẠN CHO XE NÀY */}
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

							const currentQualities: string[] =
								period.equipmentQualities[scopeKey] || [];
							const currentDists: string[] =
								period.equipmentDistances[scopeKey] || [];
							const currentPickups: string[] =
								period.processPickupLocations[scopeKey] || [];
							const currentDropoffs: string[] =
								period.processDropoffLocations[scopeKey] || [];
							const currentCargos: string[] =
								period.processCargoTypes[scopeKey] || [];

							const procPickupNames = currentPickups
								.map((id) => {
									const l = locations.find(
										(loc: any) => loc.id === id || loc.value === id,
									);
									return l ? (l as any).name || (l as any).label : '';
								})
								.filter(Boolean);
							const combinedPickupName = procPickupNames.join(', ');

							const filteredItems = period.items.filter(
								(it: any) =>
									it.assignmentCodeId === acId &&
									it.productionProcessId === procId,
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
									<div className='border-b border-neutral-200/80 pb-2'>
										<span className='text-sm font-semibold text-black'>
											{procName}
										</span>
									</div>

									<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
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
									</div>

									<div className='grid grid-cols-1 gap-3 md:grid-cols-3'>
										<MultiSelect
											label='Chủng loại hàng'
											placeholder='Chọn chủng loại hàng'
											options={cargoTypes.map((item) => ({
												label: `${item.code} - ${item.name}`,
												value: item.id,
											}))}
											values={currentCargos.map((cId) => {
												const c = cargoTypes.find((item) => item.id === cId);
												return {
													value: cId,
													label: c ? `${c.code} - ${c.name}` : cId,
												};
											})}
											onValuesChange={(selected) =>
												handleScopeFieldChange(
													scopeKey,
													'processCargoTypes',
													selected.map((s) => s.value),
												)
											}
										/>

										<MultiSelect
											label='Vị trí nhận (Không bắt buộc)'
											placeholder='Chọn vị trí nhận'
											options={pickupOptions}
											values={currentPickups.map((pId) => {
												const l = locations.find(
													(loc) => loc.id === pId || loc.name === pId,
												);
												return { value: pId, label: l ? l.name : pId };
											})}
											onValuesChange={(selected) =>
												handleScopeFieldChange(
													scopeKey,
													'processPickupLocations',
													selected.map((s) => s.value),
												)
											}
										/>

										<MultiSelect
											label='Vị trí đổ (Không bắt buộc)'
											placeholder='Chọn vị trí đổ'
											options={dropoffOptions}
											values={currentDropoffs.map((dId) => {
												const l = locations.find(
													(loc) => loc.id === dId || loc.name === dId,
												);
												return { value: dId, label: l ? l.name : dId };
											})}
											onValuesChange={(selected) =>
												handleScopeFieldChange(
													scopeKey,
													'processDropoffLocations',
													selected.map((s) => s.value),
												)
											}
										/>
									</div>

									{/* KHU VỰC BẢNG ĐƠN GIÁ THEO CUNG ĐỘ */}
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
																<table className='w-full min-w-[650px] text-left text-sm'>
																	<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
																		<tr>
																			<th className='px-4 py-2.5 whitespace-nowrap'>
																				Cung độ vận tải
																			</th>
																			<th className='w-40 min-w-[140px] px-4 py-2.5 whitespace-nowrap'>
																				Đơn giá nhiên liệu (đ/tkm)
																			</th>
																			<th className='w-40 min-w-[140px] px-4 py-2.5 whitespace-nowrap'>
																				Đơn giá động lực (đ/tkm)
																			</th>
																			<th className='w-40 min-w-[140px] px-4 py-2.5 whitespace-nowrap'>
																				Đơn giá SCTX (đ/tkm)
																			</th>
																		</tr>
																	</thead>
																	<tbody className='divide-y divide-neutral-100'>
																		{grp.items.map((item: any) => (
																			<tr
																				key={`${item.tempId || item.id || item.haulDistanceId}`}
																				className='transition-colors hover:bg-neutral-50/50'
																			>
																				<td className='px-4 py-2 whitespace-nowrap'>
																					<div className='inline-flex h-9 items-center rounded-md border border-neutral-300 bg-white px-3 text-xs font-medium whitespace-nowrap text-black'>
																						{item.haulDistanceValue
																							? `${item.haulDistanceValue} km`
																							: '-'}
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
																						value={item.powerUnitPrice ?? 0}
																						onValueChange={(val) =>
																							handleItemValueChange(
																								item.tempId || item.id,
																								'powerUnitPrice',
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
																		))}
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

export const MotorizedScaniaForm = forwardRef<
	MotorizedSubFormHandle,
	MotorizedScaniaFormProps
>(function MotorizedScaniaForm(
	{
		data,
		row,
		isDuplicate = false,
		hideConfirmButton = false,
	}: MotorizedScaniaFormProps,
	ref,
) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const [assignmentCodes, setAssignmentCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>([]);
	const [distanceOptions, setDistanceOptions] = useState<any[]>([]);
	const [cargoTypes, setCargoTypes] = useState<CargoType[]>([]);
	const [locations, setLocations] = useState<TransportLocation[]>([]);

	const [periods, setPeriods] = useState<ScaniaPeriodData[]>([
		{
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: '',
			endMonth: '',
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			equipmentDistances: {},
			processCargoTypes: {},
			processPickupLocations: {},
			processDropoffLocations: {},
			items: [],
		},
	]);

	// Location options
	const pickupOptions = locations
		.filter(
			(loc) =>
				loc.locationType === LocationType.Receiving ||
				Number(loc.locationType) === 1,
		)
		.map((loc) => ({ label: loc.name, value: loc.id || loc.name }));

	const dropoffOptions = locations
		.filter(
			(loc) =>
				loc.locationType === LocationType.Dumping ||
				Number(loc.locationType) === 2,
		)
		.map((loc) => ({ label: loc.name, value: loc.id || loc.name }));

	useEffect(() => {
		Promise.all([
			fetchCatalogList(API.CATALOG.CONTRACT_CODE.LIST),
			fetchCatalogList(API.CATALOG.PROCESS.STEP.LIST),
			fetchCatalogList(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.LIST),
			fetchCatalogList(API.CATALOG.CARGO_TYPE.LIST),
			fetchCatalogList(API.CATALOG.TRANSPORT_LOCATION.LIST),
		])
			.then(([acList, procList, distList, cargoList, locList]) => {
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

				setCargoTypes(cargoList);
				setLocations(locList);

				if (!row) return;

				// Map existing row data to period format
				const allProcs: MotorizedScaniaUnitPrice[] = (row as any)
					.allProcesses || [row];
				const initialAcId =
					row.assignmentCodeId || (row as any).equipmentId || '';

				const initialProcsList: string[] = [];
				const initialQualitiesMap: Record<string, string[]> = {};
				const initialDistsMap: Record<string, string[]> = {};
				const initialProcCargoTypes: Record<string, string[]> = {};
				const initialProcPickups: Record<string, string[]> = {};
				const initialProcDropoffs: Record<string, string[]> = {};
				const initialItems: any[] = [];

				allProcs.forEach((p) => {
					const q = (p.equipmentQuality || '')
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

					if (p.cargoTypeId) {
						if (!initialProcCargoTypes[scopeKey]) {
							initialProcCargoTypes[scopeKey] = [];
						}
						if (!initialProcCargoTypes[scopeKey].includes(p.cargoTypeId)) {
							initialProcCargoTypes[scopeKey].push(p.cargoTypeId);
						}
					}

					const rIds =
						p.receivingLocationIds && p.receivingLocationIds.length > 0
							? p.receivingLocationIds
							: p.receivingLocationId
								? [p.receivingLocationId]
								: [];
					if (rIds.length > 0) {
						if (!initialProcPickups[scopeKey]) {
							initialProcPickups[scopeKey] = [];
						}
						rIds.forEach((rid) => {
							if (!initialProcPickups[scopeKey].includes(rid)) {
								initialProcPickups[scopeKey].push(rid);
							}
						});
					}

					if (p.dumpingLocationId) {
						if (!initialProcDropoffs[scopeKey]) {
							initialProcDropoffs[scopeKey] = [];
						}
						if (!initialProcDropoffs[scopeKey].includes(p.dumpingLocationId)) {
							initialProcDropoffs[scopeKey].push(p.dumpingLocationId);
						}
					}

					if (p.details && p.details.length > 0) {
						p.details.forEach((d: any) => {
							if (d.haulDistanceId) {
								if (!initialDistsMap[scopeKey]) {
									initialDistsMap[scopeKey] = [];
								}
								if (!initialDistsMap[scopeKey].includes(d.haulDistanceId)) {
									initialDistsMap[scopeKey].push(d.haulDistanceId);
								}
							}

							initialItems.push({
								tempId: `item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
								id: isDuplicate ? undefined : p.id,
								detailId: isDuplicate ? undefined : d.id,
								assignmentCodeId: initialAcId,
								equipmentQuality: q,
								productionProcessId: p.productionProcessId,
								productionProcessName: p.productionProcessName,
								cargoTypeId: p.cargoTypeId || null,
								cargoTypeName: p.cargoTypeName,
								receivingLocationIds: rIds,
								receivingLocationNames: p.receivingLocationNames || p.receivingLocationName,
								receivingLocationId: rIds[0] || null,
								receivingLocationName: p.receivingLocationName,
								dumpingLocationId: p.dumpingLocationId || null,
								dumpingLocationName: p.dumpingLocationName,
								haulDistanceId: d.haulDistanceId || null,
								haulDistanceValue: d.haulDistanceValue,
								fuelUnitPrice: d.fuelUnitPrice ?? 0,
								powerUnitPrice: d.powerUnitPrice ?? 0,
								maintenanceUnitPrice: d.maintenanceUnitPrice ?? 0,
							});
						});
					}
				});

				const initialProcessesMap: Record<string, string[]> = {};
				if (initialAcId) {
					initialProcessesMap[initialAcId] = initialProcsList;
				}

				setPeriods([
					{
						tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: isDuplicate ? undefined : row.id,
						startMonth: row.startMonth?.substring(0, 7) || '',
						endMonth: row.endMonth?.substring(0, 7) || '',
						assignmentCodeIds: initialAcId ? [initialAcId] : [],
						equipmentProcesses: initialProcessesMap,
						equipmentQualities: initialQualitiesMap,
						equipmentDistances: initialDistsMap,
						processCargoTypes: initialProcCargoTypes,
						processPickupLocations: initialProcPickups,
						processDropoffLocations: initialProcDropoffs,
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
		const newPeriod: ScaniaPeriodData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			equipmentDistances: {},
			processCargoTypes: {},
			processPickupLocations: {},
			processDropoffLocations: {},
			items: [],
		};
		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;
		const nextMonths = computeNextPeriodMonths(target.endMonth);
		const copiedPeriod: ScaniaPeriodData = {
			...target,
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [...target.assignmentCodeIds],
			equipmentProcesses: JSON.parse(JSON.stringify(target.equipmentProcesses)),
			equipmentQualities: JSON.parse(JSON.stringify(target.equipmentQualities)),
			equipmentDistances: JSON.parse(JSON.stringify(target.equipmentDistances)),
			processCargoTypes: JSON.parse(JSON.stringify(target.processCargoTypes)),
			processPickupLocations: JSON.parse(
				JSON.stringify(target.processPickupLocations),
			),
			processDropoffLocations: JSON.parse(
				JSON.stringify(target.processDropoffLocations),
			),
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

	const handleUpdatePeriod = (index: number, updated: ScaniaPeriodData) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const submitInternal = async (forceCreate?: boolean): Promise<boolean> => {
		try {
			if (periods.length === 0) {
				popup.error('Cần có ít nhất một khoảng thời gian áp dụng cho Xe Scania');
				return false;
			}

			// Validate periods
			for (let i = 0; i < periods.length; i++) {
				const p = periods[i];
				const order = i + 1;
				if (!p.startMonth || !p.startMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian bắt đầu ở khoảng thời gian thứ ${order} của Xe Scania`,
					);
					return false;
				}
				if (!p.endMonth || !p.endMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian kết thúc ở khoảng thời gian thứ ${order} của Xe Scania`,
					);
					return false;
				}
				if (p.startMonth > p.endMonth) {
					popup.error(
						`Thời gian bắt đầu không được lớn hơn thời gian kết thúc ở khoảng thời gian thứ ${order} của Xe Scania`,
					);
					return false;
				}
				if (p.assignmentCodeIds.length === 0) {
					popup.error(
						`Vui lòng chọn Nhóm vật tư, tài sản ở khoảng thời gian thứ ${order} của Xe Scania`,
					);
					return false;
				}
			}

			// Process each period
			for (let i = 0; i < periods.length; i++) {
				const p = periods[i];
				const isPeriodCreate =
					forceCreate !== undefined
						? forceCreate
						: isDuplicate || !p.id;

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
						cargoTypeId: string | null;
						receivingLocationIds: string[];
						receivingLocationId: string | null;
						dumpingLocationId: string | null;
						details: Array<{
							haulDistanceId: string | null;
							fuelUnitPrice: number;
							powerUnitPrice: number;
							maintenanceUnitPrice: number;
						}>;
					}
				> = {};

				for (const item of p.items) {
					const headerKey = `${item.assignmentCodeId}_${item.equipmentQuality}_${item.productionProcessId}_${item.cargoTypeId || ''}_${item.dumpingLocationId || ''}`;
					const recIds: string[] = Array.isArray(item.receivingLocationIds)
						? item.receivingLocationIds
						: item.receivingLocationId
							? [item.receivingLocationId]
							: [];

					if (!groupedHeaders[headerKey]) {
						groupedHeaders[headerKey] = {
							id: item.id,
							assignmentCodeId: item.assignmentCodeId,
							equipmentQuality: item.equipmentQuality,
							productionProcessId: item.productionProcessId,
							startMonth,
							endMonth,
							cargoTypeId: item.cargoTypeId || null,
							receivingLocationIds: recIds,
							receivingLocationId:
								item.receivingLocationId || recIds[0] || null,
							dumpingLocationId: item.dumpingLocationId || null,
							details: [],
						};
					}

					groupedHeaders[headerKey].details.push({
						haulDistanceId: item.haulDistanceId || null,
						fuelUnitPrice: Number(item.fuelUnitPrice) || 0,
						powerUnitPrice: Number(item.powerUnitPrice) || 0,
						maintenanceUnitPrice: Number(item.maintenanceUnitPrice) || 0,
					});
				}

				const promises = Object.values(groupedHeaders).map((header) => {
					const payload = {
						assignmentCodeId: header.assignmentCodeId,
						equipmentQuality: header.equipmentQuality,
						productionProcessId: header.productionProcessId,
						startMonth,
						endMonth,
						cargoTypeId: header.cargoTypeId,
						receivingLocationIds: header.receivingLocationIds,
						receivingLocationId: header.receivingLocationId,
						dumpingLocationId: header.dumpingLocationId,
						details: header.details,
					};

					if (header.id && !isDuplicate && !isPeriodCreate) {
						return api.put(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.UPDATE, {
							id: header.id,
							...payload,
						});
					} else {
						return api.post(
							API.PRICING.MOTORIZED_TRANSPORT.SCANIA.CREATE,
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
					? 'Cập nhật đơn giá Xe Scania thành công'
					: 'Thêm mới đơn giá Xe Scania thành công',
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
					<span>Vận chuyển (Xe Scania)</span>
				</div>

				{/* DANH SÁCH CÁC FORM KHOẢNG THỜI GIAN */}
				<div className='flex flex-col gap-5'>
					{periods.map((period, index) => (
						<ScaniaPeriodSection
							key={period.tempId}
							period={period}
							totalPeriods={periods.length}
							assignmentCodes={assignmentCodes}
							processOptions={processOptions}
							distanceOptions={distanceOptions}
							cargoTypes={cargoTypes}
							locations={locations}
							pickupOptions={pickupOptions}
							dropoffOptions={dropoffOptions}
							onUpdate={(updated) => handleUpdatePeriod(index, updated)}
							onDelete={() => handleDeletePeriod(index)}
							onCopy={() => handleCopyPeriod(index)}
						/>
					))}

					{/* NÚT THÊM THỜI GIAN Ở DƯỚI CÙNG */}
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
					<>
						<div className='mt-3 rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-300'>
							<div className='flex items-center gap-1.5 font-semibold text-blue-900 dark:text-blue-200'>
								<InfoIcon className='size-4 text-blue-600 dark:text-blue-400' />
								Lưu ý về Hệ số điều chỉnh đơn giá định mức (Cấu hình ở Danh mục):
							</div>
							<ul className='mt-1 list-disc space-y-0.5 pl-5 text-slate-700 dark:text-slate-300'>
								<li>
									Đơn giá nhiên liệu, SCTX tăng 5% theo công đoạn sản xuất, mùa
									mưa và loại hàng.
								</li>
								<li>
									Áp dụng hệ số điều chỉnh khi sản phẩm là Than, bùn, bã sàng, đá
									sàng đổ tại Kho 5 (Kho BHN) & Kho 6 (mức +75):
									<span className='font-medium'> Mức ≤ +65 (K = 1)</span>;
									<span className='font-medium'>
										{' '}
										+65 &lt; Mức ≤ +90 (K = 1,03)
									</span>
									;<span className='font-medium'> Mức &gt; +90 (K = 1,06)</span>.
								</li>
							</ul>
						</div>

						<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
					</>
				)}
			</div>
		</form>
	);
});

export default MotorizedScaniaForm;
