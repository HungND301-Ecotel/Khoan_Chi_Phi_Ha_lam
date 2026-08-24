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
import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { MotorizedExcavatorDozerUnitPrice } from './columns';
import {
	MOTORIZED_EXCAVATOR_DOZER_FORM_DEFAULT,
	motorizedExcavatorDozerFormSchema,
	MotorizedExcavatorDozerFormSchema,
} from './schema';

type MotorizedExcavatorDozerFormProps =
	ActionDialogProps<MotorizedExcavatorDozerUnitPrice> & {
		isDuplicate?: boolean;
	};

const DEFAULT_PROCESS_OPTIONS = [
	{
		label: 'Xúc đất đá (Xúc khối lượng)',
		value: 'Xúc đất đá (Xúc khối lượng)',
	},
	{
		label: 'Xúc than kho (Xúc khối lượng)',
		value: 'Xúc than kho (Xúc khối lượng)',
	},
	{
		label: 'Xúc than trung chuyển các kho',
		value: 'Xúc than trung chuyển các kho',
	},
	{ label: 'Giờ phục vụ', value: 'Giờ phục vụ' },
	{ label: 'Giờ di chuyển', value: 'Giờ di chuyển' },
	{ label: 'Giờ gạt phục vụ', value: 'Giờ gạt phục vụ' },
	{ label: 'Giờ gạt di chuyển', value: 'Giờ gạt di chuyển' },
];

export function MotorizedExcavatorDozerForm({
	data,
	row,
	isDuplicate = false,
}: MotorizedExcavatorDozerFormProps) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();
	const [contractCodes, setContractCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>(
		DEFAULT_PROCESS_OPTIONS,
	);

	const form = useForm<MotorizedExcavatorDozerFormSchema>({
		resolver: zodResolver(motorizedExcavatorDozerFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: MOTORIZED_EXCAVATOR_DOZER_FORM_DEFAULT,
	});

	const selectedContractCodeIds =
		useWatch({
			control: form.control as any,
			name: 'contractCodeIds',
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

	const items =
		useWatch({
			control: form.control as any,
			name: 'items',
		}) || [];

	// Fetch Catalogs (ContractCode & ProcessStep)
	useEffect(() => {
		Promise.all([
			api.pagging<any>(API.CATALOG.CONTRACT_CODE.LIST, {
				ignorePagination: true,
			}),
			api.pagging<any>(API.CATALOG.PROCESS.STEP.LIST, {
				ignorePagination: true,
			}),
		])
			.then(([ccRes, procRes]) => {
				const fetchedCc = ccRes.result?.data || [];
				setContractCodes(fetchedCc);

				const fetchedProc = procRes.result?.data || [];
				if (fetchedProc.length > 0) {
					const mappedProc = fetchedProc.map((item: any) => ({
						label: item.code ? `${item.code} - ${item.name}` : item.name,
						value: item.id,
						rawName: item.name,
					}));
					setProcessOptions(mappedProc);
				}

				if (!row) return;

				const allProcs: any[] = (row as any).allProcesses || [row];
				const initialCcId =
					row.assignmentCodeId || row.equipmentId || row.equipmentName || '';
				const initialProcsList: string[] = [];
				const initialQualitiesMap: Record<string, string[]> = {};
				const initialItems: any[] = [];

				allProcs.forEach((p: any) => {
					const q = (p.equipmentQuality || 'A')
						.replace(/^Thiết bị loại\s*/i, '')
						.replace(/^Loại\s*/i, '')
						.trim();

					const procId =
						p.productionProcessId ||
						p.productionProcessName ||
						p.productionProcess ||
						'';
					if (procId && !initialProcsList.includes(procId)) {
						initialProcsList.push(procId);
					}

					const scopeKey = `${initialCcId}_${procId}`;
					if (!initialQualitiesMap[scopeKey]) {
						initialQualitiesMap[scopeKey] = [];
					}
					if (q && !initialQualitiesMap[scopeKey].includes(q)) {
						initialQualitiesMap[scopeKey].push(q);
					}

					const firstDetail = p.details?.[0];
					initialItems.push({
						id: p.id,
						assignmentCodeId: initialCcId,
						productionProcessId: procId,
						equipmentQuality: q,
						title: p.assignmentCodeName || p.equipmentName || initialCcId,
						fuelUnitPrice: firstDetail?.fuelUnitPrice ?? p.fuelUnitPrice ?? 0,
						maintenanceUnitPrice:
							firstDetail?.maintenanceUnitPrice ?? p.maintenanceUnitPrice ?? 0,
					});
				});

				form.reset({
					startMonth: row.startMonth?.substring(0, 7),
					endMonth: row.endMonth?.substring(0, 7),
					contractCodeIds: initialCcId ? [initialCcId] : [],
					equipmentQualities: initialQualitiesMap,
					equipmentProcesses: initialCcId ? { [initialCcId]: initialProcsList } : {},
					items: initialItems,
				});
			})
			.catch((err) => {
				console.error(err);
			});
	}, [form, row]);

	// Auto sync items matrix when contractCodeIds, equipmentQualities or equipmentProcesses change
	useEffect(() => {
		if (row && !isDuplicate) return;

		let newItems: any[] = [];
		if (selectedContractCodeIds.length > 0) {
			selectedContractCodeIds.forEach((ccId: string) => {
				const cc = contractCodes.find((c) => c.id === ccId);
				const title = cc
					? cc.code
						? `${cc.code} - ${cc.name}`
						: cc.name
					: ccId;
				const selectedProcs: string[] = watchedEquipmentProcesses?.[ccId] || [];

				selectedProcs.forEach((procValue: string) => {
					const scopeKey = `${ccId}_${procValue}`;
					const qualities: string[] = watchedEquipmentQualities?.[scopeKey] || [];

					qualities.forEach((quality: string) => {
						const existing = items.find(
							(it: any) =>
								it.assignmentCodeId === ccId &&
								it.productionProcessId === procValue &&
								it.equipmentQuality === quality,
						);
						newItems.push({
							assignmentCodeId: ccId,
							productionProcessId: procValue,
							equipmentQuality: quality,
							title,
							fuelUnitPrice: existing?.fuelUnitPrice ?? 0,
							maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? 0,
						});
					});
				});
			});
		}

		form.setValue('items', newItems);
	}, [
		JSON.stringify(selectedContractCodeIds),
		JSON.stringify(watchedEquipmentQualities),
		JSON.stringify(watchedEquipmentProcesses),
		contractCodes,
		row,
		isDuplicate,
	]);

	const handleSubmit = async (values: MotorizedExcavatorDozerFormSchema) => {
		try {
			const itemsToSubmit =
				values.items && values.items.length > 0 ? values.items : [];

			if (itemsToSubmit.length === 0) {
				popup.error(
					'Vui lòng chọn công đoạn sản xuất và chất lượng thiết bị cho các nhóm vật tư đã chọn',
				);
				return;
			}

			for (const item of itemsToSubmit) {
				const procObj = processOptions.find(
					(p) =>
						p.value === item.productionProcessId ||
						p.label === item.productionProcessId ||
						p.rawName === item.productionProcessId,
				);
				const procId = procObj?.value || item.productionProcessId;

				if (!procId) {
					popup.error(
						`Vui lòng chọn công đoạn sản xuất cho ${item.title || 'mục đã chọn'}`,
					);
					return;
				}

				const payload = {
					assignmentCodeId: item.assignmentCodeId,
					equipmentQuality: item.equipmentQuality,
					productionProcessId: procId,
					startMonth:
						values.startMonth.length === 7
							? `${values.startMonth}-01`
							: values.startMonth,
					endMonth:
						values.endMonth.length === 7
							? `${values.endMonth}-01`
							: values.endMonth,
					details: [
						{
							fuelUnitPrice: Number(item.fuelUnitPrice) || 0,
							maintenanceUnitPrice: Number(item.maintenanceUnitPrice) || 0,
						},
					],
				};

				if (row && !isDuplicate) {
					await api.put(
						API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.UPDATE,
						{
							id: item.id || row.id,
							...payload,
						},
					);
				} else {
					await api.post(
						API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.CREATE,
						payload,
					);
				}
			}

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

			<div className='space-y-4 rounded-lg border border-gray-200 bg-white p-4 shadow-2xs'>
				<div className='flex items-center gap-2 border-b border-gray-100 pb-2 text-sm font-semibold text-gray-800'>
					<span className='h-2.5 w-2.5 rounded-full bg-blue-600' />
					<span>Máy xúc, máy gạt</span>
				</div>

				{/* CẤP 1: CHỌN NHÓM VẬT TƯ, TÀI SẢN */}
				<FormMultiSelect
					control={form.control as any}
					name='contractCodeIds'
					label='1. Nhóm vật tư, tài sản (Máy xúc / Máy gạt)'
					placeholder='Chọn nhóm máy xúc/gạt'
					options={contractCodes.map((item) => ({
						label: item.code ? `${item.code} - ${item.name}` : item.name,
						value: item.id,
					}))}
					disabled={!!row && !isDuplicate}
				/>

				{/* LẶP QUA TỪNG NHÓM THIẾT BỊ */}
				{selectedContractCodeIds.map((ccId: string) => {
					const cc = contractCodes.find((c) => c.id === ccId);
					const title = cc
						? cc.code
							? `${cc.code} - ${cc.name}`
							: cc.name
						: ccId;
					const selectedProcList: string[] =
						watchedEquipmentProcesses?.[ccId] || [];

					return (
						<div
							key={ccId}
							className='space-y-4 rounded-lg border border-gray-300 bg-white p-4 shadow-2xs'
						>
							{/* HEADER THIẾT BỊ */}
							<div className='flex items-center justify-between border-b border-gray-200 pb-2'>
								<span className='text-sm font-bold text-gray-900'>
									{title}
								</span>
							</div>

							{/* CẤP 2: CHỌN CÔNG ĐOẠN CHO THIẾT BỊ NÀY */}
							<FormMultiSelect
								control={form.control as any}
								name={`equipmentProcesses.${ccId}`}
								label={`2. Công đoạn sản xuất áp dụng cho [${title}]`}
								placeholder='Chọn công đoạn sản xuất'
								options={processOptions.map((opt) => ({
									label: opt.label,
									value: opt.value,
								}))}
							/>

							{/* LẶP QUA TỪNG CÔNG ĐOẠN SẢN XUẤT */}
							{selectedProcList.map((procValue: string) => {
								const scopeKey = `${ccId}_${procValue}`;
								const procObj = processOptions.find(
									(p) => p.value === procValue || p.label === procValue,
								);
								const procText = procObj?.label || procValue || '';
								const isHourly =
									procText === 'Giờ phục vụ' ||
									procText === 'Giờ di chuyển' ||
									procText === 'Giờ gạt phục vụ' ||
									procText === 'Giờ gạt di chuyển' ||
									procText.toLowerCase().includes('giờ');

								const itemUnitLabel = isHourly ? '(đ/h)' : '(đ/tấn)';
								const currentQualities: string[] =
									watchedEquipmentQualities?.[scopeKey] || [];

								return (
									<div
										key={procValue}
										className='space-y-4 rounded-lg border border-gray-300 bg-gray-50/40 p-4 shadow-xs'
									>
										{/* HEADER CỤM CÔNG ĐOẠN */}
										<div className='flex items-center gap-2 border-b border-gray-200 pb-2'>
											<span className='h-2 w-2 rounded-full bg-blue-500' />
											<span className='text-xs font-semibold uppercase text-blue-800'>
												{procText}
											</span>
										</div>

										{/* CHỌN CHẤT LƯỢNG THIẾT BỊ CHO CÔNG ĐOẠN NÀY (DÀI HẾT TRANG) */}
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

										{/* BẢNG ĐƠN GIÁ */}
										{currentQualities.length > 0 && (
											<div className='space-y-2 pt-2'>
												<div className='text-xs font-semibold text-gray-700'>
													Bảng đơn giá ({currentQualities.length} tổ hợp)
												</div>

												<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
													<table className='w-full text-left text-sm'>
														<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-black'>
															<tr>
																<th className='w-[40%] px-3 py-2'>
																	Chất lượng thiết bị
																</th>
																<th className='w-[30%] px-3 py-2'>
																	Đơn giá Nhiên liệu {itemUnitLabel}
																</th>
																<th className='w-[30%] px-3 py-2'>
																	Đơn giá SCTX {itemUnitLabel}
																</th>
															</tr>
														</thead>
														<tbody className='divide-y divide-gray-100'>
															{currentQualities.map(
																(quality: string) => {
																	const itemIndex = items.findIndex(
																		(it: any) =>
																			it.assignmentCodeId === ccId &&
																			it.productionProcessId === procValue &&
																			it.equipmentQuality === quality,
																	);
																	if (itemIndex === -1) return null;

																	return (
																		<tr
																			key={quality}
																			className='hover:bg-gray-50/50'
																		>
																			<td className='px-3 py-2'>
																				<div className='flex h-9 items-center rounded-md border border-gray-300 bg-white px-3 text-xs font-medium text-black'>
																					Thiết bị loại {quality}
																				</div>
																			</td>
																			<td className='px-3 py-2'>
																				<FormNumber
																					control={form.control as any}
																					name={`items.${itemIndex}.fuelUnitPrice`}
																					placeholder='Nhập đơn giá nhiên liệu'
																				/>
																			</td>
																			<td className='px-3 py-2'>
																				<FormNumber
																					control={form.control as any}
																					name={`items.${itemIndex}.maintenanceUnitPrice`}
																					placeholder='Nhập đơn giá SCTX'
																				/>
																			</td>
																		</tr>
																	);
																},
															)}
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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
