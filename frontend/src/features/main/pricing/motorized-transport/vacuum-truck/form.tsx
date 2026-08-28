import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { MotorizedVacuumTruckUnitPrice } from './columns';
import {
	MOTORIZED_VACUUM_TRUCK_FORM_DEFAULT,
	motorizedVacuumTruckFormSchema,
	MotorizedVacuumTruckFormSchema,
} from './schema';
import { MotorizedSubFormHandle } from '../scania/form';

export type MotorizedVacuumTruckFormProps =
	ActionDialogProps<MotorizedVacuumTruckUnitPrice> & {
		isDuplicate?: boolean;
		hideTimeRow?: boolean;
		hideConfirmButton?: boolean;
		sharedStartMonth?: string;
		sharedEndMonth?: string;
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

export const MotorizedVacuumTruckForm = forwardRef<
	MotorizedSubFormHandle,
	MotorizedVacuumTruckFormProps
>(function MotorizedVacuumTruckForm(
	{
		data,
		row,
		isDuplicate = false,
		hideTimeRow = false,
		hideConfirmButton = false,
		sharedStartMonth,
		sharedEndMonth,
	}: MotorizedVacuumTruckFormProps,
	ref,
) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();

	const [assignmentCodes, setAssignmentCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>([]);
	const [distanceOptions, setDistanceOptions] = useState<any[]>([]);

	const form = useForm<MotorizedVacuumTruckFormSchema>({
		resolver: zodResolver(motorizedVacuumTruckFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_VACUUM_TRUCK_FORM_DEFAULT,
	});

	useEffect(() => {
		if (sharedStartMonth !== undefined) {
			form.setValue('startMonth', sharedStartMonth);
		}
	}, [sharedStartMonth, form]);

	useEffect(() => {
		if (sharedEndMonth !== undefined) {
			form.setValue('endMonth', sharedEndMonth);
		}
	}, [sharedEndMonth, form]);

	const selectedAssignmentCodeIds =
		useWatch({
			control: form.control as any,
			name: 'assignmentCodeIds',
		}) || [];

	const watchedEquipmentQualities =
		useWatch({
			control: form.control as any,
			name: 'equipmentQualities',
		}) || {};

	const watchedEquipmentProcesses =
		useWatch({
			control: form.control as any,
			name: 'equipmentProcesses',
		}) || {};

	const watchedEquipmentDistances =
		useWatch({
			control: form.control as any,
			name: 'equipmentDistances',
		}) || {};

	const items =
		useWatch({
			control: form.control as any,
			name: 'items',
		}) || [];

	useEffect(() => {
		Promise.all([
			fetchCatalogList(API.CATALOG.CONTRACT_CODE.LIST),
			fetchCatalogList(API.CATALOG.PROCESS.STEP.LIST),
			fetchCatalogList(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.LIST),
		])
			.then(([acList, procList, distList]) => {
				// 1. Nhóm vật tư, tài sản (AssignmentCode)
				setAssignmentCodes(acList);

				// 2. Công đoạn sản xuất (ProductionProcess)
				const procOpts = procList.map((item: any) => ({
					label: item.name || item.code,
					value: item.id,
					name: item.name || item.code,
				}));
				setProcessOptions(procOpts);

				// 3. Cung độ vận tải (HaulDistance)
				const distOpts = distList.map((item: any) => ({
					label: item.value || item.name || item.distanceRange || item.code,
					value: item.id,
					name: item.value || item.name || item.distanceRange || item.code,
				}));
				setDistanceOptions(distOpts);

				if (!row) return;

				const allProcs: MotorizedVacuumTruckUnitPrice[] = (row as any)
					.allProcesses || [row];
				const initialAcId = row.assignmentCodeId || '';

				const initialProcsList: string[] = [];
				const initialQualitiesMap: Record<string, string[]> = {};
				const initialDistsMap: Record<string, string[]> = {};
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

					if (!initialDistsMap[scopeKey]) {
						initialDistsMap[scopeKey] = [];
					}

					(p.details || []).forEach((d) => {
						if (
							d.haulDistanceId &&
							!initialDistsMap[scopeKey].includes(d.haulDistanceId)
						) {
							initialDistsMap[scopeKey].push(d.haulDistanceId);
						}

						initialItems.push({
							id: p.id, // Header Id
							detailId: d.id,
							assignmentCodeId: initialAcId,
							equipmentQuality: q,
							productionProcessId: p.productionProcessId,
							productionProcessName: p.productionProcessName,
							haulDistanceId: d.haulDistanceId || null,
							haulDistanceValue: d.haulDistanceValue || '',
							title: p.assignmentCodeName || initialAcId,
							fuelUnitPrice: d.fuelUnitPrice ?? null,
							maintenanceUnitPrice: d.maintenanceUnitPrice ?? null,
						});
					});
				});

				form.reset({
					startMonth: row.startMonth?.substring(0, 7),
					endMonth: row.endMonth?.substring(0, 7),
					assignmentCodeIds: initialAcId ? [initialAcId] : [],
					equipmentQualities: initialQualitiesMap,
					equipmentProcesses: initialAcId
						? { [initialAcId]: initialProcsList }
						: {},
					equipmentDistances: initialDistsMap,
					items: initialItems,
				});
			})
			.catch((err) => {
				console.error(err);
			});
	}, [form, row]);

	// Auto sync items matrix when inputs change
	useEffect(() => {
		if (row && !isDuplicate) return;

		if (selectedAssignmentCodeIds.length === 0) {
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

			selectedProcs.forEach((procId: string) => {
				const scopeKey = `${acId}_${procId}`;
				const procObj = processOptions.find((p) => p.value === procId);
				const procName = procObj ? procObj.label : procId;
				const isHourly =
					procName.toLowerCase().includes('phục vụ') ||
					procName.toLowerCase().includes('di chuyển');

				const selectedQualities: string[] =
					watchedEquipmentQualities[scopeKey] || [];
				const selectedDists: string[] =
					watchedEquipmentDistances[scopeKey] || [];

				selectedQualities.forEach((qual: string) => {
					if (isHourly || selectedDists.length === 0) {
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
							haulDistanceId: null,
							haulDistanceValue: '',
							title,
							fuelUnitPrice: existing ? existing.fuelUnitPrice : null,
							maintenanceUnitPrice: existing
								? existing.maintenanceUnitPrice
								: null,
						});
					} else {
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
								haulDistanceId: distId,
								haulDistanceValue: distValue,
								title,
								fuelUnitPrice: existing ? existing.fuelUnitPrice : null,
								maintenanceUnitPrice: existing
									? existing.maintenanceUnitPrice
									: null,
							});
						});
					}
				});
			});
		});

		form.setValue('items', newItems);
	}, [
		JSON.stringify(selectedAssignmentCodeIds),
		JSON.stringify(watchedEquipmentQualities),
		JSON.stringify(watchedEquipmentProcesses),
		JSON.stringify(watchedEquipmentDistances),
	]);

	const submitInternal = async (
		startM?: string,
		endM?: string,
	): Promise<boolean> => {
		try {
			const values = form.getValues();
			const itemsToSubmit = (values.items || []).filter(
				(item: any) =>
					item.assignmentCodeId &&
					(Number(item.fuelUnitPrice) > 0 ||
						Number(item.maintenanceUnitPrice) > 0),
			);

			if (itemsToSubmit.length === 0) {
				if (selectedAssignmentCodeIds.length > 0) {
					popup.error(
						'Vui lòng chọn công đoạn sản xuất và chất lượng thiết bị để nhập đơn giá Xe hút bùn',
					);
					return false;
				}
				return true;
			}

			const rawStart = startM || values.startMonth;
			const rawEnd = endM || values.endMonth;

			if (!rawStart) {
				popup.error('Vui lòng chọn Thời gian bắt đầu cho Xe hút bùn');
				return false;
			}

			const startMonth =
				rawStart.length === 7 ? `${rawStart}-01` : rawStart;
			const endMonth =
				rawEnd.length === 7 ? `${rawEnd}-01` : rawEnd;

			// Group items by (assignmentCodeId, equipmentQuality, productionProcessId)
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

			itemsToSubmit.forEach((item: any) => {
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
					fuelUnitPrice:
						item.fuelUnitPrice !== null && item.fuelUnitPrice !== undefined
							? Number(item.fuelUnitPrice)
							: 0,
					maintenanceUnitPrice:
						item.maintenanceUnitPrice !== null &&
						item.maintenanceUnitPrice !== undefined
							? Number(item.maintenanceUnitPrice)
							: 0,
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

				if (header.id && !isDuplicate) {
					return api.put(API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.UPDATE, {
						id: header.id,
						...payload,
					});
				} else {
					return api.post(
						API.PRICING.MOTORIZED_TRANSPORT.VACUUM_TRUCK.CREATE,
						payload,
					);
				}
			});

			await Promise.all(promises);
			return true;
		} catch (error) {
			popup.error(error);
			return false;
		}
	};

	useImperativeHandle(ref, () => ({
		submit: submitInternal,
	}));

	const handleSubmit = async () => {
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
		<FormProvider
			context={form as any}
			onSubmit={handleSubmit}
			onInvalid={(errors) => {
				console.error('Form Validation Errors:', errors);
				const firstErr = Object.values(errors)[0];
				const msg =
					(firstErr as any)?.message ||
					'Vui lòng điền đầy đủ các thông tin bắt buộc';
				popup.error(msg);
			}}
		>
			{/* FORM TRÊN */}
			{!hideTimeRow && (
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
			)}

			<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
				<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
					<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
					<span>Hút bùn, chất thải (Xe hút bùn, chất thải)</span>
				</div>

				{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
				<FormMultiSelect
					control={form.control as any}
					name='assignmentCodeIds'
					label='1. Nhóm vật tư, tài sản (Xe hút bùn, chất thải)'
					placeholder='Chọn nhóm xe hút bùn, chất thải'
					options={assignmentCodes.map((item) => ({
						label: item.code
							? `${item.code} - ${item.name}`
							: item.name || item.id,
						value: item.id,
					}))}
					disabled={!!row && !isDuplicate}
				/>

				{/* LẶP QUA TỪNG NHÓM XE ĐƯỢC CHỌN */}
				{selectedAssignmentCodeIds.map((acId: string) => {
					const acObj = assignmentCodes.find((a) => a.id === acId);
					const title = acObj
						? acObj.code
							? `${acObj.code} - ${acObj.name}`
							: acObj.name
						: acId;
					const selectedProcList: string[] =
						watchedEquipmentProcesses?.[acId] || [];

					return (
						<div
							key={acId}
							className='space-y-4 rounded-lg border border-gray-300 bg-white p-4 shadow-2xs'
						>
							{/* HEADER NHÓM XE */}
							<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
								<span className='text-sm font-bold text-gray-900'>{title}</span>
							</div>

							{/* CẤP 2: CHỌN CÔNG ĐOẠN SẢN XUẤT */}
							<FormMultiSelect
								control={form.control as any}
								name={`equipmentProcesses.${acId}`}
								label={`2. Công đoạn sản xuất áp dụng cho [${title}]`}
								placeholder='Chọn công đoạn sản xuất'
								options={processOptions}
							/>

							{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
							{selectedProcList.map((procId: string) => {
								const scopeKey = `${acId}_${procId}`;
								const procObj = processOptions.find((p) => p.value === procId);
								const procName = procObj ? procObj.label : procId;
								const isHourly =
									procName.toLowerCase().includes('phục vụ') ||
									procName.toLowerCase().includes('di chuyển');
								const itemUnitLabel = isHourly ? '(đ/h)' : '(đ/tkm)';

								const currentQualities: string[] =
									watchedEquipmentQualities[scopeKey] || [];

								const filteredItems = items.filter(
									(it: any) =>
										it.assignmentCodeId === acId &&
										it.productionProcessId === procId,
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
												{procName}
											</span>
										</div>

										{/* THÔNG SỐ ĐIỀU KIỆN TRONG CÔNG ĐOẠN */}
										{isHourly ? (
											<div className='w-full'>
												<FormMultiSelect
													control={form.control as any}
													name={`equipmentQualities.${scopeKey}`}
													label='Chất lượng thiết bị'
													placeholder='Chọn chất lượng thiết bị'
													options={[
														{ label: 'Thiết bị loại A', value: 'A' },
														{ label: 'Thiết bị loại B', value: 'B' },
														{ label: 'Thiết bị loại C', value: 'C' },
													]}
												/>
											</div>
										) : (
											<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
												<FormMultiSelect
													control={form.control as any}
													name={`equipmentQualities.${scopeKey}`}
													label='Chất lượng thiết bị'
													placeholder='Chọn chất lượng thiết bị'
													options={[
														{ label: 'Thiết bị loại A', value: 'A' },
														{ label: 'Thiết bị loại B', value: 'B' },
														{ label: 'Thiết bị loại C', value: 'C' },
													]}
												/>
												<FormMultiSelect
													control={form.control as any}
													name={`equipmentDistances.${scopeKey}`}
													label='Cung độ vận tải'
													placeholder='Chọn các cung độ vận tải'
													options={distanceOptions}
												/>
											</div>
										)}

										{currentQualities.length > 0 &&
											filteredItems.length > 0 && (
												<div className='space-y-2 pt-2'>
													<div className='text-xs font-semibold text-gray-700'>
														Bảng đơn giá ({filteredItems.length} tổ hợp)
													</div>

													<div className='w-full overflow-x-auto rounded-md border border-gray-200 bg-white'>
														<table className='w-full min-w-[700px] text-left text-sm'>
															<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-black'>
																<tr>
																	<th className='whitespace-nowrap px-3 py-2'>
																		Chất lượng
																	</th>
																	{!isHourly && (
																		<th className='whitespace-nowrap px-3 py-2'>
																			Cung độ
																		</th>
																	)}
																	<th className='w-40 min-w-[140px] whitespace-nowrap px-3 py-2'>
																		Đơn giá Nhiên liệu {itemUnitLabel}
																	</th>
																	<th className='w-40 min-w-[140px] whitespace-nowrap px-3 py-2'>
																		Đơn giá SCTX {itemUnitLabel}
																	</th>
																</tr>
															</thead>
															<tbody className='divide-y divide-gray-100'>
																{filteredItems.map((item: any, idx: number) => {
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

																	if (itemIndex === -1) return null;

																	return (
																		<tr
																			key={`${item.equipmentQuality}-${item.haulDistanceId || idx}`}
																			className='hover:bg-gray-50/50'
																		>
																			<td className='whitespace-nowrap px-3 py-2'>
																				<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																					Thiết bị loại {item.equipmentQuality}
																				</div>
																			</td>
																			{!isHourly && (
																				<td className='whitespace-nowrap px-3 py-2'>
																					<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																						{item.haulDistanceValue
																							? `${item.haulDistanceValue} km`
																							: '-'}
																					</div>
																				</td>
																			)}
																			<td className='w-40 min-w-[140px] px-3 py-2'>
																				<FormNumber
																					control={form.control as any}
																					name={`items.${itemIndex}.fuelUnitPrice`}
																					placeholder='Nhập đơn giá nhiên liệu'
																				/>
																			</td>
																			<td className='w-40 min-w-[140px] px-3 py-2'>
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
											)}
									</div>
								);
							})}
						</div>
					);
				})}
			</div>

			{!hideConfirmButton && (
				<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
			)}
		</FormProvider>
	);
});
