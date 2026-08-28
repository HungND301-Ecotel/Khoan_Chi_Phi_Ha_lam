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
import { InfoIcon } from 'lucide-react';
import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { CargoType } from '@/features/main/catalog/cargo-type/columns';
import {
	LocationType,
	TransportLocation,
} from '@/features/main/catalog/transport-location/columns';
import { MotorizedScaniaUnitPrice } from './columns';
import {
	MOTORIZED_SCANIA_FORM_DEFAULT,
	motorizedScaniaFormSchema,
	MotorizedScaniaFormSchema,
} from './schema';

export type MotorizedSubFormHandle = {
	submit: (
		sharedStartMonth?: string,
		sharedEndMonth?: string,
	) => Promise<boolean>;
};

export type MotorizedScaniaFormProps =
	ActionDialogProps<MotorizedScaniaUnitPrice> & {
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

export const MotorizedScaniaForm = forwardRef<
	MotorizedSubFormHandle,
	MotorizedScaniaFormProps
>(function MotorizedScaniaForm(
	{
		data,
		row,
		isDuplicate = false,
		hideTimeRow = false,
		hideConfirmButton = false,
		sharedStartMonth,
		sharedEndMonth,
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

	const form = useForm<MotorizedScaniaFormSchema>({
		resolver: zodResolver(motorizedScaniaFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_SCANIA_FORM_DEFAULT,
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
		useWatch({ control: form.control as any, name: 'assignmentCodeIds' }) || [];

	const watchedEquipmentQualities =
		useWatch({ control: form.control as any, name: 'equipmentQualities' }) ||
		{};

	const watchedEquipmentProcesses =
		useWatch({ control: form.control as any, name: 'equipmentProcesses' }) ||
		{};

	const watchedEquipmentDistances =
		useWatch({ control: form.control as any, name: 'equipmentDistances' }) ||
		{};

	const watchedProcessCargoTypes =
		useWatch({ control: form.control as any, name: 'processCargoTypes' }) || {};

	const watchedProcessPickupLocations =
		useWatch({
			control: form.control as any,
			name: 'processPickupLocations',
		}) || {};

	const watchedProcessDropoffLocations =
		useWatch({
			control: form.control as any,
			name: 'processDropoffLocations',
		}) || {};

	const items = useWatch({ control: form.control as any, name: 'items' }) || [];

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

					if (!initialDistsMap[scopeKey]) {
						initialDistsMap[scopeKey] = [];
					}

					if (scopeKey) {
						if (!initialProcCargoTypes[scopeKey]) {
							initialProcCargoTypes[scopeKey] = [];
						}
						const resolvedCargoId =
							p.cargoTypeId ||
							cargoList.find(
								(c: any) =>
									c.name === p.cargoTypeName ||
									c.code === p.cargoTypeName,
							)?.id;
						if (
							resolvedCargoId &&
							!initialProcCargoTypes[scopeKey].includes(resolvedCargoId)
						) {
							initialProcCargoTypes[scopeKey].push(resolvedCargoId);
						}

						if (!initialProcPickups[scopeKey]) {
							initialProcPickups[scopeKey] = [];
						}
						const resolvedPickupId =
							p.receivingLocationId ||
							locList.find(
								(l: any) => l.name === p.receivingLocationName,
							)?.id ||
							p.receivingLocationName;
						if (
							resolvedPickupId &&
							!initialProcPickups[scopeKey].includes(resolvedPickupId)
						) {
							initialProcPickups[scopeKey].push(resolvedPickupId);
						}

						if (!initialProcDropoffs[scopeKey]) {
							initialProcDropoffs[scopeKey] = [];
						}
						const resolvedDropoffId =
							p.dumpingLocationId ||
							locList.find((l: any) => l.name === p.dumpingLocationName)
								?.id ||
							p.dumpingLocationName;
						if (
							resolvedDropoffId &&
							!initialProcDropoffs[scopeKey].includes(resolvedDropoffId)
						) {
							initialProcDropoffs[scopeKey].push(resolvedDropoffId);
						}
					}

					(p.details || []).forEach((d) => {
						if (
							d.haulDistanceId &&
							!initialDistsMap[scopeKey].includes(d.haulDistanceId)
						) {
							initialDistsMap[scopeKey].push(d.haulDistanceId);
						}

						initialItems.push({
							id: p.id,
							detailId: d.id,
							assignmentCodeId: initialAcId,
							equipmentQuality: q,
							productionProcessId: p.productionProcessId,
							productionProcessName:
								p.productionProcessName || p.productionProcess || '',
							cargoTypeId: p.cargoTypeId || null,
							cargoTypeName: p.cargoTypeName || '',
							receivingLocationId: p.receivingLocationId || null,
							receivingLocationName: p.receivingLocationName || '',
							dumpingLocationId: p.dumpingLocationId || null,
							dumpingLocationName: p.dumpingLocationName || '',
							haulDistanceId: d.haulDistanceId || null,
							haulDistanceValue: d.haulDistanceValue || '',
							title: p.assignmentCodeName || initialAcId,
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
					equipmentQualities: initialQualitiesMap,
					equipmentProcesses: initialAcId
						? { [initialAcId]: initialProcsList }
						: {},
					equipmentDistances: initialDistsMap,
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

				const selectedQualities: string[] =
					watchedEquipmentQualities[scopeKey] || [];
				const selectedDists: string[] =
					watchedEquipmentDistances[scopeKey] || [];

				const procCargoIds: string[] = watchedProcessCargoTypes[scopeKey] || [];
				const procPickupIds: string[] =
					watchedProcessPickupLocations[scopeKey] || [];
				const procDropoffIds: string[] =
					watchedProcessDropoffLocations[scopeKey] || [];

				const cargoList = procCargoIds.length > 0 ? procCargoIds : [''];
				const pickupList = procPickupIds.length > 0 ? procPickupIds : [''];
				const dropoffList = procDropoffIds.length > 0 ? procDropoffIds : [''];
				const distList = selectedDists.length > 0 ? selectedDists : [''];

				selectedQualities.forEach((qual: string) => {
					cargoList.forEach((cargoId: string) => {
						pickupList.forEach((pickupId: string) => {
							dropoffList.forEach((dropoffId: string) => {
								distList.forEach((distId: string) => {
									const cargoObj = cargoTypes.find(
										(c: any) => c.id === cargoId || c.value === cargoId,
									);
									const cargoName = cargoObj
										? (cargoObj as any).name || (cargoObj as any).label
										: '';

									const pickupObj = locations.find(
										(l: any) => l.id === pickupId || l.value === pickupId,
									);
									const pickupName = pickupObj
										? (pickupObj as any).name || (pickupObj as any).label
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

									const existing = items.find(
										(it: any) =>
											it.assignmentCodeId === acId &&
											it.productionProcessId === procId &&
											it.equipmentQuality === qual &&
											(it.cargoTypeId || '') === (cargoId || '') &&
											(it.receivingLocationId || '') === (pickupId || '') &&
											(it.dumpingLocationId || '') === (dropoffId || '') &&
											(it.haulDistanceId || '') === (distId || ''),
									);

									newItems.push({
										id: existing?.id,
										detailId: existing?.detailId,
										assignmentCodeId: acId,
										equipmentQuality: qual,
										productionProcessId: procId,
										productionProcessName: procName,
										title,
										cargoTypeId: cargoId || null,
										cargoTypeName: cargoName,
										receivingLocationId: pickupId || null,
										receivingLocationName: pickupName,
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
		});

		form.setValue('items', newItems);
	}, [
		JSON.stringify(selectedAssignmentCodeIds),
		JSON.stringify(watchedEquipmentQualities),
		JSON.stringify(watchedEquipmentProcesses),
		JSON.stringify(watchedEquipmentDistances),
		JSON.stringify(watchedProcessCargoTypes),
		JSON.stringify(watchedProcessPickupLocations),
		JSON.stringify(watchedProcessDropoffLocations),
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
						Number(item.powerUnitPrice) > 0 ||
						Number(item.maintenanceUnitPrice) > 0),
			);

			if (itemsToSubmit.length === 0) {
				if (selectedAssignmentCodeIds.length > 0) {
					popup.error(
						'Vui lòng chọn công đoạn sản xuất và chất lượng thiết bị để nhập đơn giá Xe Scania',
					);
					return false;
				}
				return true;
			}

			const rawStart = startM || values.startMonth;
			const rawEnd = endM || values.endMonth;

			if (!rawStart) {
				popup.error('Vui lòng chọn Thời gian bắt đầu cho Xe Scania');
				return false;
			}

			const startMonth =
				rawStart.length === 7 ? `${rawStart}-01` : rawStart;
			const endMonth =
				rawEnd.length === 7 ? `${rawEnd}-01` : rawEnd;

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

			itemsToSubmit.forEach((item: any) => {
				const key = `${item.assignmentCodeId}_${item.equipmentQuality}_${item.productionProcessId}_${item.cargoTypeId || ''}_${item.receivingLocationId || ''}_${item.dumpingLocationId || ''}`;
				if (!groupedHeaders[key]) {
					groupedHeaders[key] = {
						id: item.id,
						assignmentCodeId: item.assignmentCodeId,
						equipmentQuality: item.equipmentQuality,
						productionProcessId: item.productionProcessId,
						startMonth,
						endMonth,
						cargoTypeId: item.cargoTypeId || null,
						receivingLocationId: item.receivingLocationId || null,
						dumpingLocationId: item.dumpingLocationId || null,
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

			const promises = Object.values(groupedHeaders).map((header) => {
				const payload = {
					assignmentCodeId: header.assignmentCodeId,
					equipmentQuality: header.equipmentQuality,
					productionProcessId: header.productionProcessId,
					startMonth: header.startMonth,
					endMonth: header.endMonth,
					cargoTypeId: header.cargoTypeId,
					receivingLocationId: header.receivingLocationId,
					dumpingLocationId: header.dumpingLocationId,
					details: header.details,
				};

				if (header.id && !isDuplicate) {
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
					? 'Cập nhật đơn giá Xe Scania thành công'
					: 'Thêm mới đơn giá Xe Scania thành công',
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
					<span>Vận chuyển (Xe Scania)</span>
				</div>

				{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN (XE SCANIA) */}
				<FormMultiSelect
					control={form.control as any}
					name='assignmentCodeIds'
					label='1. Nhóm vật tư, tài sản (Xe Scania)'
					placeholder='Chọn nhóm xe Scania'
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

							{/* CẤP 2: CHỌN CÔNG ĐOẠN SẢN XUẤT CHO XE NÀY */}
							<FormMultiSelect
								control={form.control as any}
								name={`equipmentProcesses.${acId}`}
								label={`2. Công đoạn sản xuất áp dụng cho [${title}]`}
								placeholder='Chọn các công đoạn sản xuất'
								options={processOptions}
							/>

							{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
							{selectedProcList.map((procId: string) => {
								const scopeKey = `${acId}_${procId}`;
								const procObj = processOptions.find((p) => p.value === procId);
								const procName = procObj ? procObj.label : procId;

								const currentQualities: string[] =
									watchedEquipmentQualities[scopeKey] || [];
								const currentDists: string[] =
									watchedEquipmentDistances[scopeKey] || [];
								const currentCargoTypes: string[] =
									watchedProcessCargoTypes[scopeKey] || [];
								const currentPickups: string[] =
									watchedProcessPickupLocations[scopeKey] || [];
								const currentDropoffs: string[] =
									watchedProcessDropoffLocations[scopeKey] || [];
								const hasLocation =
									currentPickups.length > 0 || currentDropoffs.length > 0;

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
												placeholder='Chọn cung độ vận tải'
												options={distanceOptions}
											/>
										</div>

										<div className='w-full'>
											<FormMultiSelect
												control={form.control as any}
												name={`processCargoTypes.${scopeKey}`}
												label='Chủng loại hàng'
												placeholder='Chọn chủng loại hàng'
												options={cargoTypeOptions}
											/>
										</div>

										<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
											<FormMultiSelect
												control={form.control as any}
												name={`processPickupLocations.${scopeKey}`}
												label='Vị trí nhận (Không bắt buộc)'
												placeholder='Chọn vị trí nhận'
												options={pickupOptions}
											/>
											<FormMultiSelect
												control={form.control as any}
												name={`processDropoffLocations.${scopeKey}`}
												label='Vị trí đổ (Không bắt buộc)'
												placeholder='Chọn vị trí đổ'
												options={dropoffOptions}
											/>
										</div>

										{/* Bảng nhập đơn giá */}
										{currentQualities.length > 0 &&
											filteredItems.length > 0 && (
												<div className='space-y-2 pt-2'>
													<div className='text-xs font-semibold text-gray-700'>
														Bảng đơn giá ({filteredItems.length} tổ hợp)
													</div>

													<div className='w-full overflow-x-auto rounded-md border border-gray-200 bg-white'>
														<table className='w-full min-w-[850px] text-left text-sm'>
															<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-black'>
																<tr>
																	<th className='whitespace-nowrap px-3 py-2'>
																		Chất lượng
																	</th>
																	{currentCargoTypes.length > 0 && (
																		<th className='whitespace-nowrap px-3 py-2'>
																			Chủng loại hàng
																		</th>
																	)}
																	{hasLocation && (
																		<th className='whitespace-nowrap px-3 py-2'>
																			Vị trí nhận → Đổ
																		</th>
																	)}
																	{currentDists.length > 0 && (
																		<th className='whitespace-nowrap px-3 py-2'>
																			Cung độ
																		</th>
																	)}
																	<th className='w-36 min-w-[130px] whitespace-nowrap px-3 py-2'>
																		Đơn giá Nhiên liệu (đ/tkm)
																	</th>
																	<th className='w-36 min-w-[130px] whitespace-nowrap px-3 py-2'>
																		Đơn giá Động lực (đ/tkm)
																	</th>
																	<th className='w-36 min-w-[130px] whitespace-nowrap px-3 py-2'>
																		Đơn giá SCTX (đ/tkm)
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
																			(it.cargoTypeId || '') ===
																				(item.cargoTypeId || '') &&
																			(it.receivingLocationId || '') ===
																				(item.receivingLocationId || '') &&
																			(it.dumpingLocationId || '') ===
																				(item.dumpingLocationId || '') &&
																			(it.haulDistanceId || '') ===
																				(item.haulDistanceId || ''),
																	);

																	if (itemIndex === -1) return null;

																	return (
																		<tr
																			key={`${item.equipmentQuality}-${item.cargoTypeId || ''}-${item.receivingLocationId || ''}-${item.dumpingLocationId || ''}-${item.haulDistanceId || idx}`}
																			className='hover:bg-gray-50/50'
																		>
																			<td className='whitespace-nowrap px-3 py-2'>
																				<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																					Thiết bị loại {item.equipmentQuality}
																				</div>
																			</td>
																			{currentCargoTypes.length > 0 && (
																				<td className='whitespace-nowrap px-3 py-2'>
																					<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																						{item.cargoTypeName || '-'}
																					</div>
																				</td>
																			)}
																			{hasLocation && (
																				<td className='whitespace-nowrap px-3 py-2'>
																					<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																						{item.receivingLocationName ||
																						item.dumpingLocationName
																							? `${item.receivingLocationName || '...'} → ${item.dumpingLocationName || '...'}`
																							: '-'}
																					</div>
																				</td>
																			)}
																			{currentDists.length > 0 && (
																				<td className='whitespace-nowrap px-3 py-2'>
																					<div className='inline-flex h-9 items-center whitespace-nowrap rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																						{item.haulDistanceValue
																							? `${item.haulDistanceValue} km`
																							: '-'}
																					</div>
																				</td>
																			)}
																			<td className='w-36 min-w-[130px] px-3 py-2'>
																				<FormNumber
																					control={form.control as any}
																					name={`items.${itemIndex}.fuelUnitPrice`}
																					placeholder='Nhập đơn giá nhiên liệu'
																				/>
																			</td>
																			<td className='w-36 min-w-[130px] px-3 py-2'>
																				<FormNumber
																					control={form.control as any}
																					name={`items.${itemIndex}.powerUnitPrice`}
																					placeholder='Nhập đơn giá động lực'
																				/>
																			</td>
																			<td className='w-36 min-w-[130px] px-3 py-2'>
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
				<>
					<div className='mt-3 rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-300'>
						<div className='flex items-center gap-1.5 font-semibold text-blue-900 dark:text-blue-200'>
							<InfoIcon className='size-4 text-blue-600 dark:text-blue-400' />
							Lưu ý về Hệ số điều chỉnh đơn giá định mức (Cấu hình ở Danh mục):
						</div>
						<ul className='mt-1 list-disc space-y-0.5 pl-5 text-slate-700 dark:text-slate-300'>
							<li>
								Đơn giá nhiên liệu, SCTX tăng 5% theo công đoạn sản xuất, mùa mưa và
								loại hàng.
							</li>
							<li>
								Áp dụng hệ số điều chỉnh khi sản phẩm là Than, bùn, bã sàng, đá sàng
								đổ tại Kho 5 (Kho BHN) & Kho 6 (mức +75):
								<span className='font-medium'> Mức ≤ +65 (K = 1)</span>;
								<span className='font-medium'> +65 &lt; Mức ≤ +90 (K = 1,03)</span>;
								<span className='font-medium'> Mức &gt; +90 (K = 1,06)</span>.
							</li>
						</ul>
					</div>

					<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
				</>
			)}
		</FormProvider>
	);
});
