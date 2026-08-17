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
import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { MotorizedVacuumTruckUnitPrice } from './columns';
import {
	MOTORIZED_VACUUM_TRUCK_FORM_DEFAULT,
	motorizedVacuumTruckFormSchema,
	MotorizedVacuumTruckFormSchema,
} from './schema';

type MotorizedVacuumTruckFormProps =
	ActionDialogProps<MotorizedVacuumTruckUnitPrice> & {
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

export function MotorizedVacuumTruckForm({
	data,
	row,
	isDuplicate = false,
}: MotorizedVacuumTruckFormProps) {
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

	const selectedAssignmentCodeIds =
		useWatch({
			control: form.control as any,
			name: 'assignmentCodeIds',
		}) || [];

	const selectedEquipmentQualities =
		useWatch({
			control: form.control as any,
			name: 'equipmentQualities',
		}) || [];

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

	const showBottomSection =
		selectedAssignmentCodeIds.length > 0 &&
		selectedEquipmentQualities.length > 0;

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

				const initialQualitiesList: string[] = [];
				const initialProcsList: string[] = [];
				const initialDistsList: string[] = [];
				const initialItems: any[] = [];

				allProcs.forEach((p) => {
					const q = (p.equipmentQuality || '')
						.replace(/^Thiết bị loại\s*/i, '')
						.replace(/^Loại\s*/i, '')
						.trim();
					if (q && !initialQualitiesList.includes(q)) {
						initialQualitiesList.push(q);
					}

					if (
						p.productionProcessId &&
						!initialProcsList.includes(p.productionProcessId)
					) {
						initialProcsList.push(p.productionProcessId);
					}

					(p.details || []).forEach((d) => {
						if (
							d.haulDistanceId &&
							!initialDistsList.includes(d.haulDistanceId)
						) {
							initialDistsList.push(d.haulDistanceId);
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
							fuelUnitPrice: d.fuelUnitPrice ?? null,
							maintenanceUnitPrice: d.maintenanceUnitPrice ?? null,
						});
					});
				});

				form.reset({
					startMonth: row.startMonth?.substring(0, 7),
					endMonth: row.endMonth?.substring(0, 7),
					assignmentCodeIds: initialAcId ? [initialAcId] : [],
					equipmentQualities: initialQualitiesList,
					equipmentProcesses: initialAcId
						? { [initialAcId]: initialProcsList }
						: {},
					equipmentDistances: initialAcId
						? { [initialAcId]: initialDistsList }
						: {},
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
				const isHourly =
					procName.toLowerCase().includes('phục vụ') ||
					procName.toLowerCase().includes('di chuyển');

				selectedEquipmentQualities.forEach((qual: string) => {
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
	]);

	const handleSubmit = async (values: MotorizedVacuumTruckFormSchema) => {
		try {
			const itemsToSubmit = values.items || [];
			if (itemsToSubmit.length === 0) {
				popup.error('Vui lòng chọn công đoạn sản xuất và nhập đơn giá');
				return;
			}

			const startMonth =
				values.startMonth.length === 7
					? `${values.startMonth}-01`
					: values.startMonth;
			const endMonth =
				values.endMonth.length === 7
					? `${values.endMonth}-01`
					: values.endMonth;

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

			popup.success(
				row && !isDuplicate
					? 'Cập nhật đơn giá thành công'
					: 'Thêm mới đơn giá thành công',
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
							label: item.code
								? `${item.code} - ${item.name}`
								: item.name || item.id,
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

			{/* FORM DƯỚI */}
			{showBottomSection && (
				<div className='space-y-4'>
					<div className='text-xs font-semibold text-gray-500 uppercase'>
						Danh sách các mục đã chọn ( {selectedAssignmentCodeIds.length} nhóm
						vật tư )
					</div>

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
								className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-xs'
							>
								<div className='text-sm font-semibold text-gray-800'>
									{title}
								</div>

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
											label='Cung độ vận tải'
											placeholder='Chọn các cung độ vận tải'
											options={distanceOptions}
										/>
									</div>
								</FormRow>

								{/* VỚI MỖI CÔNG ĐOẠN ĐƯỢC CHỌN -> HIỂN THỊ BẢNG ĐƠN GIÁ TƯƠNG ỨNG */}
								{selectedProcList.map((procId: string) => {
									const procObj = processOptions.find(
										(p) => p.value === procId,
									);
									const procName = procObj ? procObj.label : procId;
									const isHourly =
										procName.toLowerCase().includes('phục vụ') ||
										procName.toLowerCase().includes('di chuyển');
									const itemUnitLabel = isHourly ? '(đ/h)' : '(đ/tkm)';

									const filteredItems = items.filter(
										(it: any) =>
											it.assignmentCodeId === acId &&
											it.productionProcessId === procId,
									);

									if (filteredItems.length === 0) return null;

									return (
										<div
											key={procId}
											className='space-y-2 rounded-md border border-gray-100 bg-gray-50/50 p-3'
										>
											<div className='text-primary text-sm font-semibold'>
												{procName}
											</div>

											<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
														<tr>
															<th className='min-w-[140px] px-4 py-3'>
																Chất lượng
															</th>
															{!isHourly && (
																<th className='min-w-[140px] px-4 py-3'>
																	Cung độ
																</th>
															)}
															<th className='px-4 py-3'>
																Đơn giá Nhiên liệu {itemUnitLabel}
															</th>
															<th className='px-4 py-3'>
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

															const qText = `Thiết bị loại ${item.equipmentQuality}`;

															return (
																<tr key={idx} className='hover:bg-gray-50/50'>
																	<td className='px-4 py-2 text-sm font-medium text-gray-700'>
																		{qText}
																	</td>
																	{!isHourly && (
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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
