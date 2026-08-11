import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { InfoIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { CargoType } from '@/features/main/catalog/cargo-type/columns';
import { LocationType, TransportLocation } from '@/features/main/catalog/transport-location/columns';
import { MotorizedScaniaUnitPrice } from './columns';
import {
	MOTORIZED_SCANIA_FORM_DEFAULT,
	motorizedScaniaFormSchema,
	MotorizedScaniaFormSchema,
} from './schema';

type MotorizedScaniaFormProps = ActionDialogProps<MotorizedScaniaUnitPrice> & {
	isDuplicate?: boolean;
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
		const res: any = await api.pagging(url, { ignorePagination: true, pageSize: 1000 });
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

export function MotorizedScaniaForm({
	data,
	row,
	isDuplicate = false,
}: MotorizedScaniaFormProps) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const [assignmentCodes, setAssignmentCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>([]);
	const [distanceOptions, setDistanceOptions] = useState<any[]>([]);
	const [cargoTypes, setCargoTypes] = useState<CargoType[]>([]);
	const [locations, setLocations] = useState<TransportLocation[]>([]);

	const form = useForm<MotorizedScaniaFormSchema>({
		resolver: zodResolver(motorizedScaniaFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_SCANIA_FORM_DEFAULT,
	});

	const selectedAssignmentCodeIds =
		useWatch({ control: form.control as any, name: 'assignmentCodeIds' }) || [];

	const selectedEquipmentQualities =
		useWatch({ control: form.control as any, name: 'equipmentQualities' }) || [];

	const watchedEquipmentProcesses =
		useWatch({ control: form.control as any, name: 'equipmentProcesses' }) || {};

	const watchedEquipmentDistances =
		useWatch({ control: form.control as any, name: 'equipmentDistances' }) || {};

	const watchedProcessCargoTypes =
		useWatch({ control: form.control as any, name: 'processCargoTypes' }) || {};

	const watchedProcessPickupLocations =
		useWatch({ control: form.control as any, name: 'processPickupLocations' }) || {};

	const watchedProcessDropoffLocations =
		useWatch({ control: form.control as any, name: 'processDropoffLocations' }) || {};

	const items =
		useWatch({ control: form.control as any, name: 'items' }) || [];

	const showBottomSection =
		selectedAssignmentCodeIds.length > 0 && selectedEquipmentQualities.length > 0;

	// Location options
	const pickupOptions = locations
		.filter((loc) => loc.locationType === LocationType.Receiving || Number(loc.locationType) === 1)
		.map((loc) => ({ label: loc.name, value: loc.id || loc.name }));

	const dropoffOptions = locations
		.filter((loc) => loc.locationType === LocationType.Dumping || Number(loc.locationType) === 2)
		.map((loc) => ({ label: loc.name, value: loc.id || loc.name }));

	// Cargo type options
	const cargoTypeOptions = cargoTypes.map((item) => ({
		label: `${item.code} - ${item.name}`,
		value: item.id,
	}));

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

				// Map existing row data to form format
				const allProcs: MotorizedScaniaUnitPrice[] = (row as any).allProcesses || [row];
				const initialAcId = row.assignmentCodeId || (row as any).equipmentId || '';

				const initialQualitiesList: string[] = [];
				const initialProcsList: string[] = [];
				const initialDistsList: string[] = [];
				const initialProcCargoTypes: Record<string, string[]> = {};
				const initialProcPickups: Record<string, string[]> = {};
				const initialProcDropoffs: Record<string, string[]> = {};
				const initialItems: any[] = [];

				allProcs.forEach((p) => {
					const q = (p.equipmentQuality || '')
						.replace(/^Thiết bị loại\s*/i, '')
						.replace(/^Loại\s*/i, '')
						.trim();
					if (q && !initialQualitiesList.includes(q)) {
						initialQualitiesList.push(q);
					}

					if (p.productionProcessId && !initialProcsList.includes(p.productionProcessId)) {
						initialProcsList.push(p.productionProcessId);
					}

					// Per-process cargo types
					if (p.productionProcessId) {
						if (!initialProcCargoTypes[p.productionProcessId]) {
							initialProcCargoTypes[p.productionProcessId] = [];
						}
						if (p.cargoTypeId && !initialProcCargoTypes[p.productionProcessId].includes(p.cargoTypeId)) {
							initialProcCargoTypes[p.productionProcessId].push(p.cargoTypeId);
						}

						if (!initialProcPickups[p.productionProcessId]) {
							initialProcPickups[p.productionProcessId] = [];
						}
						if (p.receivingLocationId && !initialProcPickups[p.productionProcessId].includes(p.receivingLocationId)) {
							initialProcPickups[p.productionProcessId].push(p.receivingLocationId);
						}

						if (!initialProcDropoffs[p.productionProcessId]) {
							initialProcDropoffs[p.productionProcessId] = [];
						}
						if (p.dumpingLocationId && !initialProcDropoffs[p.productionProcessId].includes(p.dumpingLocationId)) {
							initialProcDropoffs[p.productionProcessId].push(p.dumpingLocationId);
						}
					}

					(p.details || []).forEach((d) => {
						if (d.haulDistanceId && !initialDistsList.includes(d.haulDistanceId)) {
							initialDistsList.push(d.haulDistanceId);
						}

						initialItems.push({
							id: p.id,
							detailId: d.id,
							assignmentCodeId: initialAcId,
							equipmentQuality: q,
							productionProcessId: p.productionProcessId,
							productionProcessName: p.productionProcessName || p.productionProcess || '',
							cargoTypeId: p.cargoTypeId || null,
							cargoTypeName: p.cargoTypeName || '',
							receivingLocationId: p.receivingLocationId || null,
							receivingLocationName: p.receivingLocationName || '',
							dumpingLocationId: p.dumpingLocationId || null,
							dumpingLocationName: p.dumpingLocationName || '',
							haulDistanceId: d.haulDistanceId || null,
							haulDistanceValue: d.haulDistanceValue || '',
							fuelUnitPrice: d.fuelUnitPrice ?? 0,
							powerUnitPrice: d.powerUnitPrice ?? 0,
							maintenanceUnitPrice: d.maintenanceUnitPrice ?? 0,
						});
					});
				});

				form.reset({
					startMonth: row.startMonth?.substring(0, 7),
					endMonth: row.endMonth?.substring(0, 7),
					assignmentCodeIds: initialAcId ? [initialAcId] : [],
					equipmentQualities: initialQualitiesList,
					equipmentProcesses: initialAcId ? { [initialAcId]: initialProcsList } : {},
					equipmentDistances: initialAcId ? { [initialAcId]: initialDistsList } : {},
					processCargoTypes: initialProcCargoTypes,
					processPickupLocations: initialProcPickups,
					processDropoffLocations: initialProcDropoffs,
					items: initialItems,
				});
			})
			.catch((err) => {
				console.error(err);
			});
	}, [form, row]);

	// Auto sync items matrix when inputs change
	useEffect(() => {
		if (!showBottomSection) {
			form.setValue('items', []);
			return;
		}

		const newItems: any[] = [];

		selectedAssignmentCodeIds.forEach((acId: string) => {
			const acObj = assignmentCodes.find((a) => a.id === acId);
			const title = acObj
				? acObj.code
					? `${acObj.code} - ${acObj.name}`
					: acObj.name
				: acId;

			const selectedProcs: string[] = watchedEquipmentProcesses[acId] || [];
			const selectedDists: string[] = watchedEquipmentDistances[acId] || [];

			selectedProcs.forEach((procId: string) => {
				const procObj = processOptions.find((p) => p.value === procId);
				const procName = procObj ? procObj.label : procId;

				// Per-process cargo types and locations
				const procCargoIds: string[] = watchedProcessCargoTypes[procId] || [];
				const procPickupIds: string[] = watchedProcessPickupLocations[procId] || [];
				const procDropoffIds: string[] = watchedProcessDropoffLocations[procId] || [];

				selectedEquipmentQualities.forEach((qual: string) => {
					// Nếu có chọn cung độ → tạo item cho mỗi cung độ
					if (selectedDists.length > 0) {
						selectedDists.forEach((distId: string) => {
							const distObj = distanceOptions.find((d) => d.value === distId);
							const distValue = distObj ? distObj.label : distId;

							const existing = items.find(
								(it: any) =>
									it.assignmentCodeId === acId &&
									it.productionProcessId === procId &&
									it.equipmentQuality === qual &&
									it.haulDistanceId === distId,
							);

							newItems.push({
								id: existing?.id,
								detailId: existing?.detailId,
								assignmentCodeId: acId,
								equipmentQuality: qual,
								productionProcessId: procId,
								productionProcessName: procName,
								title,
								cargoTypeIds: procCargoIds,
								receivingLocationIds: procPickupIds,
								dumpingLocationIds: procDropoffIds,
								haulDistanceId: distId,
								haulDistanceValue: distValue,
								fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
								powerUnitPrice: existing ? existing.powerUnitPrice : 0,
								maintenanceUnitPrice: existing ? existing.maintenanceUnitPrice : 0,
							});
						});
					} else {
						const existing = items.find(
							(it: any) =>
								it.assignmentCodeId === acId &&
								it.productionProcessId === procId &&
								it.equipmentQuality === qual &&
								!it.haulDistanceId,
						);

						newItems.push({
							id: existing?.id,
							detailId: existing?.detailId,
							assignmentCodeId: acId,
							equipmentQuality: qual,
							productionProcessId: procId,
							productionProcessName: procName,
							title,
							cargoTypeIds: procCargoIds,
							receivingLocationIds: procPickupIds,
							dumpingLocationIds: procDropoffIds,
							haulDistanceId: null,
							haulDistanceValue: '',
							fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
							powerUnitPrice: existing ? existing.powerUnitPrice : 0,
							maintenanceUnitPrice: existing ? existing.maintenanceUnitPrice : 0,
						});
					}
				});
			});
		});

		const isSameLength = items.length === newItems.length;
		const isSameContent =
			isSameLength &&
			items.every((it: any, idx: number) => {
				const nIt = newItems[idx];
				return (
					it?.assignmentCodeId === nIt?.assignmentCodeId &&
					it?.equipmentQuality === nIt?.equipmentQuality &&
					it?.productionProcessId === nIt?.productionProcessId &&
					it?.haulDistanceId === nIt?.haulDistanceId
				);
			});

		if (!isSameContent) {
			form.setValue('items', newItems);
		}
	}, [
		showBottomSection,
		JSON.stringify(selectedAssignmentCodeIds),
		JSON.stringify(selectedEquipmentQualities),
		JSON.stringify(watchedEquipmentProcesses),
		JSON.stringify(watchedEquipmentDistances),
		JSON.stringify(watchedProcessCargoTypes),
		JSON.stringify(watchedProcessPickupLocations),
		JSON.stringify(watchedProcessDropoffLocations),
	]);

	const handleSubmit = async (values: MotorizedScaniaFormSchema) => {
		try {
			const itemsToSubmit = values.items || [];
			if (itemsToSubmit.length === 0) {
				popup.error('Vui lòng chọn công đoạn sản xuất và nhập đơn giá');
				return;
			}

			const startMonth =
				values.startMonth.length === 7 ? `${values.startMonth}-01` : values.startMonth;
			const endMonth =
				values.endMonth.length === 7 ? `${values.endMonth}-01` : values.endMonth;

			// Flatten items by (assignmentCodeId, equipmentQuality, productionProcessId, cargoTypeId, receivingLocationId, dumpingLocationId)
			const groupedHeaders: Record<
				string,
				{
					id?: string;
					assignmentCodeId: string;
					equipmentQuality: string;
					productionProcessId: string;
					cargoTypeId: string;
					receivingLocationId: string | null;
					dumpingLocationId: string | null;
					startMonth: string;
					endMonth: string;
					details: Array<{
						haulDistanceId: string | null;
						fuelUnitPrice: number;
						powerUnitPrice: number;
						maintenanceUnitPrice: number;
					}>;
				}
			> = {};

			itemsToSubmit.forEach((item: any) => {
				const cargoTypeIds: string[] = item.cargoTypeIds || [];
				const receivingLocationIds: string[] = item.receivingLocationIds || [];
				const dumpingLocationIds: string[] = item.dumpingLocationIds || [];

				// If no cargo types selected, still create one record with empty cargoTypeId
				const cargoTypesToProcess = cargoTypeIds.length > 0 ? cargoTypeIds : [''];
				const pickupsToProcess = receivingLocationIds.length > 0 ? receivingLocationIds : [null];
				const dropoffsToProcess = dumpingLocationIds.length > 0 ? dumpingLocationIds : [null];

				cargoTypesToProcess.forEach((cargoTypeId) => {
					pickupsToProcess.forEach((receivingLocationId) => {
						dropoffsToProcess.forEach((dumpingLocationId) => {
							const key = `${item.assignmentCodeId}_${item.equipmentQuality}_${item.productionProcessId}_${cargoTypeId}_${receivingLocationId || ''}_${dumpingLocationId || ''}`;
							if (!groupedHeaders[key]) {
								groupedHeaders[key] = {
									id: undefined,
									assignmentCodeId: item.assignmentCodeId,
									equipmentQuality: item.equipmentQuality,
									productionProcessId: item.productionProcessId,
									cargoTypeId: cargoTypeId || undefined,
									receivingLocationId: receivingLocationId || undefined,
									dumpingLocationId: dumpingLocationId || undefined,
									startMonth,
									endMonth,
									details: [],
								};
							}

							groupedHeaders[key].details.push({
								haulDistanceId: item.haulDistanceId || null,
								fuelUnitPrice: Number(item.fuelUnitPrice) || 0,
								powerUnitPrice: Number(item.powerUnitPrice) || 0,
								maintenanceUnitPrice: Number(item.maintenanceUnitPrice) || 0,
							});
						});
					});
				});
			});

			const promises = Object.values(groupedHeaders).map((header) => {
				const payload = {
					assignmentCodeId: header.assignmentCodeId,
					equipmentQuality: header.equipmentQuality,
					productionProcessId: header.productionProcessId,
					cargoTypeId: header.cargoTypeId,
					receivingLocationId: header.receivingLocationId,
					dumpingLocationId: header.dumpingLocationId,
					startMonth: header.startMonth,
					endMonth: header.endMonth,
					details: header.details,
				};

				if (header.id && !isDuplicate) {
					return api.put(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.UPDATE, {
						id: header.id,
						...payload,
					});
				} else {
					return api.post(API.PRICING.MOTORIZED_TRANSPORT.SCANIA.CREATE, payload);
				}
			});

			await Promise.all(promises);

			popup.success(
				row && !isDuplicate
					? 'Cập nhật đơn giá Xe Scania thành công'
					: 'Thêm mới đơn giá Xe Scania thành công',
			);

			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form as any} onSubmit={handleSubmit}>
			{/* FORM TRÊN */}
			<FormRow>
				<FormMonthYear
					control={form.control as any}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control as any}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormRow>
				<div className='flex-1'>
					<FormMultiSelect
						control={form.control as any}
						name='assignmentCodeIds'
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn nhóm vật tư, tài sản'
						options={assignmentCodes.map((item) => ({
							label: item.code ? `${item.code} - ${item.name}` : item.name || item.id,
							value: item.id,
						}))}
					/>
				</div>
				<div className='flex-1'>
					<FormMultiSelect
						control={form.control as any}
						name='equipmentQualities'
						label='Chất lượng thiết bị'
						placeholder='Chọn chất lượng thiết bị'
						options={[
							{ label: 'Thiết bị loại A', value: 'A' },
							{ label: 'Thiết bị loại B', value: 'B' },
							{ label: 'Thiết bị loại C', value: 'C' },
						]}
					/>
				</div>
			</FormRow>

			{showBottomSection && <FormSeparator />}

			{/* FORM DƯỚI - Matrix nhập đơn giá */}
			{showBottomSection && (
				<div className='space-y-4'>
					<div className='text-xs font-semibold uppercase text-gray-500'>
						Danh sách các mục đã chọn ( {selectedAssignmentCodeIds.length} nhóm vật tư )
					</div>

					{selectedAssignmentCodeIds.map((acId: string) => {
						const acObj = assignmentCodes.find((a) => a.id === acId);
						const title = acObj
							? acObj.code
								? `${acObj.code} - ${acObj.name}`
								: acObj.name
							: acId;
						const selectedProcList: string[] = watchedEquipmentProcesses?.[acId] || [];
						const selectedDistList: string[] = watchedEquipmentDistances?.[acId] || [];

						return (
							<div
								key={acId}
								className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-xs'
							>
								<div className='text-sm font-semibold text-gray-800'>{title}</div>

								<FormRow>
									<div className='flex-1'>
										<FormMultiSelect
											control={form.control as any}
											name={`equipmentProcesses.${acId}`}
											label='Công đoạn sản xuất'
											placeholder='Chọn các công đoạn sản xuất'
											options={processOptions}
										/>
									</div>
									<div className='flex-1'>
										<FormMultiSelect
											control={form.control as any}
											name={`equipmentDistances.${acId}`}
											label='Cung độ vận chuyển (km)'
											placeholder='Chọn cung độ vận chuyển'
											options={distanceOptions}
										/>
									</div>
								</FormRow>

								{/* VỚI MỖI CÔNG ĐOẠN ĐƯỢC CHỌN -> HIỂN THỊ FIELDS + BẢNG ĐƠN GIÁ */}
								{selectedProcList.map((procId: string) => {
									const procObj = processOptions.find((p) => p.value === procId);
									const procName = procObj ? procObj.label : procId;

									const filteredItems = items.filter(
										(it: any) =>
											it.assignmentCodeId === acId &&
											it.productionProcessId === procId,
									);

									if (filteredItems.length === 0) return null;

									return (
										<div
											key={procId}
											className='space-y-3 rounded-md border border-gray-100 bg-gray-50/50 p-3'
										>
											<div className='text-sm font-semibold text-primary'>
												{procName}
											</div>

											{/* Fields riêng cho từng công đoạn */}
											<FormRow>
												<div className='flex-1'>
													<FormMultiSelect
														control={form.control as any}
														name={`processCargoTypes.${procId}`}
														label='Chủng loại hàng'
														placeholder='Chọn chủng loại hàng'
														options={cargoTypeOptions}
													/>
												</div>
											</FormRow>
											<FormRow>
												<div className='flex-1'>
													<FormMultiSelect
														control={form.control as any}
														name={`processPickupLocations.${procId}`}
														label='Vị trí nhận (Không bắt buộc)'
														placeholder='Chọn vị trí nhận'
														options={pickupOptions}
													/>
												</div>
												<div className='flex-1'>
													<FormMultiSelect
														control={form.control as any}
														name={`processDropoffLocations.${procId}`}
														label='Vị trí đổ (Không bắt buộc)'
														placeholder='Chọn vị trí đổ'
														options={dropoffOptions}
													/>
												</div>
											</FormRow>

											{/* Bảng nhập đơn giá */}
											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-gray-600'>
														<tr>
															<th className='min-w-[140px] px-4 py-3'>
																Chất lượng
															</th>
															{selectedDistList.length > 0 && (
																<th className='min-w-[140px] px-4 py-3'>
																	Cung độ
																</th>
															)}
															<th className='px-4 py-3'>
																Đơn giá Nhiên liệu (đ/tkm)
															</th>
															<th className='px-4 py-3'>
																Đơn giá Động lực (đ/tkm)
															</th>
															<th className='px-4 py-3'>
																Đơn giá SCTX (đ/tkm)
															</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-gray-100'>
														{filteredItems.map((item: any) => {
															const itemIndex = items.findIndex(
																(it: any) =>
																	it.assignmentCodeId ===
																		item.assignmentCodeId &&
																	it.productionProcessId ===
																		item.productionProcessId &&
																	it.equipmentQuality ===
																		item.equipmentQuality &&
																	it.haulDistanceId === item.haulDistanceId,
															);

															const qText = `Thiết bị loại ${item.equipmentQuality}`;

															return (
																<tr
																	key={`${item.assignmentCodeId}-${item.productionProcessId}-${item.equipmentQuality}-${item.haulDistanceId || 'no-dist'}`}
																	className='hover:bg-gray-50/50'
																>
																	<td className='px-4 py-2 text-sm font-medium text-gray-700'>
																		{qText}
																	</td>
																	{selectedDistList.length > 0 && (
																		<td className='px-4 py-2 text-sm font-medium text-gray-700'>
																			{item.haulDistanceValue || '-'}
																		</td>
																	)}
																	<td className='px-4 py-2'>
																		<FormNumber
																			control={form.control as any}
																			name={`items.${itemIndex}.fuelUnitPrice`}
																			placeholder='Nhập đơn giá nhiên liệu'
																		/>
																	</td>
																	<td className='px-4 py-2'>
																		<FormNumber
																			control={form.control as any}
																			name={`items.${itemIndex}.powerUnitPrice`}
																			placeholder='Nhập đơn giá động lực'
																		/>
																	</td>
																	<td className='px-4 py-2'>
																		<FormNumber
																			control={form.control as any}
																			name={`items.${itemIndex}.maintenanceUnitPrice`}
																			placeholder='Nhập đơn giá SCTX'
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
						);
					})}
				</div>
			)}

			<div className='mt-3 rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-300'>
				<div className='flex items-center gap-1.5 font-semibold text-blue-900 dark:text-blue-200'>
					<InfoIcon className='size-4 text-blue-600 dark:text-blue-400' />
					Lưu ý về Hệ số điều chỉnh đơn giá định mức (Cấu hình ở Danh mục):
				</div>
				<ul className='mt-1 list-disc pl-5 space-y-0.5 text-slate-700 dark:text-slate-300'>
					<li>Đơn giá nhiên liệu, SCTX tăng 5% theo công đoạn sản xuất, mùa mưa và loại hàng.</li>
					<li>Áp dụng hệ số điều chỉnh khi sản phẩm là Than, bùn, bã sàng, đá sàng đổ tại Kho 5 (Kho BHN) & Kho 6 (mức +75):
						<span className='font-medium'> Mức ≤ +65 (K = 1)</span>;
						<span className='font-medium'> +65 &lt; Mức ≤ +90 (K = 1,03)</span>;
						<span className='font-medium'> Mức &gt; +90 (K = 1,06)</span>.
					</li>
				</ul>
			</div>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
