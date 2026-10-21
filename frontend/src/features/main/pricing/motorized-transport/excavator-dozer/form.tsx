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
import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { MotorizedExcavatorDozerUnitPrice } from './columns';
import { MotorizedSubFormHandle } from '../scania/form';
import {
	computeNextPeriodMonths,
	ExcavatorPeriodData,
} from '../unit-price/types';
import { ApplyToVehiclesButton } from '../apply-to-vehicles';
import { copyVehicleConfig } from '../copy-vehicle-config';

export type MotorizedExcavatorDozerFormProps =
	ActionDialogProps<MotorizedExcavatorDozerUnitPrice> & {
		isDuplicate?: boolean;
		hideConfirmButton?: boolean;
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

const QUALITY_OPTIONS = [
	{ label: 'Thiết bị loại A', value: 'A' },
	{ label: 'Thiết bị loại B', value: 'B' },
	{ label: 'Thiết bị loại C', value: 'C' },
];

// Xúc gạt nâng khoán theo công đoạn: công đoạn tính theo giờ dùng đ/h, còn lại đ/tấn
const getProcessUnitLabel = (procText: string) =>
	procText.toLowerCase().includes('giờ') ? 'đ/h' : 'đ/tấn';

function computeItemsForExcavatorPeriod(
	assignmentCodeIds: string[],
	equipmentProcesses: Record<string, string[]>,
	equipmentQualities: Record<string, string[]>,
	contractCodes: any[],
	processOptions: any[],
	existingItems: any[] = [],
): any[] {
	const newItems: any[] = [];

	assignmentCodeIds.forEach((ccId: string) => {
		const cc = contractCodes.find((c) => c.id === ccId);
		const title = cc ? (cc.code ? `${cc.code} - ${cc.name}` : cc.name) : ccId;
		const materialName = cc ? cc.name || cc.code || ccId : ccId;
		const selectedProcs: string[] = equipmentProcesses[ccId] || [];
		// Chất lượng thiết bị chọn 1 lần theo Nhóm vật tư, tài sản (máy)
		const currentQualities: string[] = equipmentQualities[ccId] || [];

		selectedProcs.forEach((procValue: string) => {
			const procObj = processOptions.find(
				(p) => p.value === procValue || p.label === procValue,
			);
			const procName = procObj?.label || procValue || '';

			currentQualities.forEach((quality: string) => {
				const existing = existingItems.find(
					(it: any) =>
						it.assignmentCodeId === ccId &&
						it.productionProcessId === procValue &&
						it.equipmentQuality === quality,
				);

				newItems.push({
					tempId:
						existing?.tempId ||
						`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
					id: existing?.id,
					detailId: existing?.detailId,
					assignmentCodeId: ccId,
					productionProcessId: procValue,
					productionProcessName: procName,
					equipmentQuality: quality,
					title,
					materialName,
					fuelUnitPrice: existing ? existing.fuelUnitPrice : 0,
					maintenanceUnitPrice: existing ? existing.maintenanceUnitPrice : 0,
				});
			});
		});
	});

	return newItems;
}

interface ExcavatorPeriodSectionProps {
	period: ExcavatorPeriodData;
	totalPeriods: number;
	contractCodes: any[];
	processOptions: any[];
	onUpdate: (updated: ExcavatorPeriodData) => void;
	onCopy: () => void;
	onDelete: () => void;
}

function ExcavatorPeriodSection({
	period,
	totalPeriods,
	contractCodes,
	processOptions,
	onUpdate,
	onCopy,
	onDelete,
}: ExcavatorPeriodSectionProps) {
	const handleAssignmentCodesChange = (newAcIds: string[]) => {
		const nextItems = computeItemsForExcavatorPeriod(
			newAcIds,
			period.equipmentProcesses,
			period.equipmentQualities,
			contractCodes,
			processOptions,
			period.items,
		);
		onUpdate({
			...period,
			assignmentCodeIds: newAcIds,
			items: nextItems,
		});
	};

	const handleProcessesChange = (ccId: string, newProcs: string[]) => {
		const nextProcs = { ...period.equipmentProcesses, [ccId]: newProcs };
		const nextItems = computeItemsForExcavatorPeriod(
			period.assignmentCodeIds,
			nextProcs,
			period.equipmentQualities,
			contractCodes,
			processOptions,
			period.items,
		);
		onUpdate({
			...period,
			equipmentProcesses: nextProcs,
			items: nextItems,
		});
	};

	// Chất lượng thiết bị khoá theo máy (ccId)
	const handleQualitiesChange = (ccId: string, qualities: string[]) => {
		const nextQualities = {
			...period.equipmentQualities,
			[ccId]: qualities,
		};
		const nextItems = computeItemsForExcavatorPeriod(
			period.assignmentCodeIds,
			period.equipmentProcesses,
			nextQualities,
			contractCodes,
			processOptions,
			period.items,
		);
		onUpdate({
			...period,
			equipmentQualities: nextQualities,
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

	// Tên máy hiển thị và các trường theo máy của dòng đơn giá (khớp computeItemsForExcavatorPeriod)
	const getProcName = (procValue: string) => {
		const p = processOptions.find(
			(opt) => opt.value === procValue || opt.label === procValue,
		);
		return p?.label || procValue || '';
	};

	const getVehicleTitle = (id: string) => {
		const cc = contractCodes.find((c) => c.id === id);
		return cc ? (cc.code ? `${cc.code} - ${cc.name}` : cc.name) : id;
	};
	const getVehicleItemFields = (id: string) => {
		const cc = contractCodes.find((c) => c.id === id);
		return {
			title: getVehicleTitle(id),
			materialName: cc ? cc.name || cc.code || id : id,
		};
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

			{/* Hàng 2: Nhóm vật tư, tài sản (máy xúc / gạt) */}
			<MultiSelect
				label='Nhóm vật tư, tài sản'
				placeholder='Chọn nhóm máy xúc/gạt'
				options={contractCodes.map((item) => ({
					label: item.code ? `${item.code} - ${item.name}` : item.name,
					value: item.id,
				}))}
				values={(period.assignmentCodeIds || []).map((id) => {
					const item = contractCodes.find((c) => c.id === id);
					return {
						value: id,
						label: item
							? item.code
								? `${item.code} - ${item.name}`
								: item.name
							: id,
					};
				})}
				onValuesChange={(selected) =>
					handleAssignmentCodesChange(selected.map((s) => s.value))
				}
			/>

			{/* Form chi tiết từng máy */}
			{(period.assignmentCodeIds || []).map((ccId: string) => {
				const cc = contractCodes.find((c) => c.id === ccId);
				const title = cc
					? cc.code
						? `${cc.code} - ${cc.name}`
						: cc.name
					: ccId;
				const materialName = cc ? cc.name || cc.code || ccId : ccId;
				const selectedProcList: string[] =
					period.equipmentProcesses?.[ccId] || [];
				const currentQualities: string[] =
					period.equipmentQualities?.[ccId] || [];

				return (
					<div
						key={ccId}
						className='space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-xs'
					>
						<div className='flex items-center justify-between border-b border-neutral-100 pb-2'>
							<span className='text-sm font-semibold text-black'>{title}</span>
							<ApplyToVehiclesButton
								sourceId={ccId}
								vehicleIds={period.assignmentCodeIds || []}
								getTitle={getVehicleTitle}
								onApply={(targetIds) =>
									onUpdate(
										copyVehicleConfig(
											period,
											ccId,
											targetIds,
											getVehicleItemFields,
										),
									)
								}
							/>
						</div>

						{/* CHẤT LƯỢNG THIẾT BỊ (chọn 1 lần cho máy) & CÔNG ĐOẠN SẢN XUẤT */}
						<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
							<MultiSelect
								label='Chất lượng thiết bị'
								placeholder='Chọn chất lượng thiết bị'
								options={QUALITY_OPTIONS}
								values={currentQualities.map((q) => ({
									label: `Thiết bị loại ${q}`,
									value: q,
								}))}
								onValuesChange={(selected) =>
									handleQualitiesChange(
										ccId,
										selected.map((s) => s.value),
									)
								}
							/>
						<MultiSelect
							label='Công đoạn sản xuất'
							placeholder='Chọn công đoạn sản xuất'
							options={processOptions.map((opt) => ({
								label: opt.label,
								value: opt.value,
							}))}
							values={selectedProcList.map((id) => {
								const p = processOptions.find(
									(opt) => opt.value === id || opt.label === id,
								);
								return { value: id, label: p ? p.label : id };
							})}
							onValuesChange={(selected) =>
								handleProcessesChange(
									ccId,
									selected.map((s) => s.value),
								)
							}
						/>
						</div>

						{/* BẢNG ĐƠN GIÁ: mỗi chất lượng thiết bị 1 bảng, mỗi dòng 1 công đoạn sản xuất */}
						{currentQualities.map((quality: string) => {
							const qualityItems = period.items.filter(
								(it: any) =>
									it.assignmentCodeId === ccId &&
									it.equipmentQuality === quality,
							);
							if (qualityItems.length === 0) return null;

							return (
								<div
									key={quality}
									className='overflow-hidden rounded-md border border-neutral-200 bg-white shadow-xs'
								>
									<div className='border-b border-neutral-200 bg-neutral-100 px-3.5 py-2.5'>
										<span className='rounded-md border border-neutral-300 bg-white px-3 py-1 text-sm font-semibold text-black shadow-2xs'>
											{materialName} (Loại {quality})
										</span>
									</div>
									<div className='scrollbar-sm w-full overflow-x-auto'>
										<table className='w-full min-w-[600px] text-left text-sm'>
											<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
												<tr>
													<th className='px-4 py-2.5 whitespace-nowrap'>
														Công đoạn sản xuất
													</th>
													<th className='w-24 px-4 py-2.5 whitespace-nowrap'>ĐVT</th>
													<th className='w-40 min-w-[140px] px-4 py-2.5 whitespace-nowrap'>
														Đơn giá Nhiên liệu
													</th>
													<th className='w-40 min-w-[140px] px-4 py-2.5 whitespace-nowrap'>
														Đơn giá SCTX
													</th>
												</tr>
											</thead>
											<tbody className='divide-y divide-neutral-100'>
												{qualityItems.map((item: any) => {
													const procText = getProcName(item.productionProcessId);
													return (
														<tr
															key={item.tempId || item.id}
															className='transition-colors hover:bg-neutral-50/50'
														>
															<td className='px-4 py-2 text-black'>{procText}</td>
															<td className='w-24 px-4 py-2 whitespace-nowrap text-black'>
																{getProcessUnitLabel(procText)}
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
																	placeholder='Nhập đơn giá nhiên liệu'
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
	);
}

export const MotorizedExcavatorDozerForm = forwardRef<
	MotorizedSubFormHandle,
	MotorizedExcavatorDozerFormProps
>(function MotorizedExcavatorDozerForm(
	{
		data,
		row,
		isDuplicate = false,
		hideConfirmButton = false,
	}: MotorizedExcavatorDozerFormProps,
	ref,
) {
	useMeta();
	const popup = usePopup();
	const { setOpen } = useDialog();
	const [contractCodes, setContractCodes] = useState<any[]>([]);
	const [processOptions, setProcessOptions] = useState<any[]>(
		DEFAULT_PROCESS_OPTIONS,
	);

	const [periods, setPeriods] = useState<ExcavatorPeriodData[]>([
		{
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: '',
			endMonth: '',
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			items: [],
		},
	]);

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
					row.assignmentCodeId ||
					(row as any).equipmentId ||
					(row as any).equipmentName ||
					'';
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

					// Chất lượng thiết bị gom theo máy (chọn 1 lần cho Nhóm vật tư, tài sản)
					if (!initialQualitiesMap[initialCcId]) {
						initialQualitiesMap[initialCcId] = [];
					}
					if (q && !initialQualitiesMap[initialCcId].includes(q)) {
						initialQualitiesMap[initialCcId].push(q);
					}

					const firstDetail = p.details?.[0];
					initialItems.push({
						tempId: `item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: isDuplicate ? undefined : p.id,
						detailId: isDuplicate ? undefined : firstDetail?.id,
						assignmentCodeId: initialCcId,
						productionProcessId: procId,
						equipmentQuality: q,
						title: p.assignmentCodeName || p.equipmentName || initialCcId,
						fuelUnitPrice: firstDetail?.fuelUnitPrice ?? p.fuelUnitPrice ?? 0,
						maintenanceUnitPrice:
							firstDetail?.maintenanceUnitPrice ?? p.maintenanceUnitPrice ?? 0,
					});
				});

				setPeriods([
					{
						tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: isDuplicate ? undefined : row.id,
						startMonth: row.startMonth?.substring(0, 7) || '',
						endMonth: row.endMonth?.substring(0, 7) || '',
						assignmentCodeIds: initialCcId ? [initialCcId] : [],
						equipmentQualities: initialQualitiesMap,
						equipmentProcesses: initialCcId
							? { [initialCcId]: initialProcsList }
							: {},
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
		const newPeriod: ExcavatorPeriodData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [],
			equipmentProcesses: {},
			equipmentQualities: {},
			items: [],
		};
		setPeriods((prev) => [...prev, newPeriod]);
	};

	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;
		const nextMonths = computeNextPeriodMonths(target.endMonth);
		const copiedPeriod: ExcavatorPeriodData = {
			...target,
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			startMonth: nextMonths.startMonth,
			endMonth: nextMonths.endMonth,
			assignmentCodeIds: [...target.assignmentCodeIds],
			equipmentProcesses: JSON.parse(JSON.stringify(target.equipmentProcesses)),
			equipmentQualities: JSON.parse(JSON.stringify(target.equipmentQualities)),
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

	const handleUpdatePeriod = (index: number, updated: ExcavatorPeriodData) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const submitInternal = async (forceCreate?: boolean): Promise<boolean> => {
		try {
			if (periods.length === 0) {
				popup.error('Cần có ít nhất một khoảng thời gian áp dụng');
				return false;
			}

			for (let i = 0; i < periods.length; i++) {
				const p = periods[i];
				const order = i + 1;
				if (!p.startMonth || !p.startMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian bắt đầu ở khoảng thời gian thứ ${order} của Máy xúc/gạt`,
					);
					return false;
				}
				if (!p.endMonth || !p.endMonth.trim()) {
					popup.error(
						`Vui lòng chọn Thời gian kết thúc ở khoảng thời gian thứ ${order} của Máy xúc/gạt`,
					);
					return false;
				}
				if (p.startMonth > p.endMonth) {
					popup.error(
						`Thời gian bắt đầu không được lớn hơn thời gian kết thúc ở khoảng thời gian thứ ${order} của Máy xúc/gạt`,
					);
					return false;
				}
				if (p.assignmentCodeIds.length === 0) {
					popup.error(
						`Vui lòng chọn Nhóm máy xúc/gạt ở khoảng thời gian thứ ${order}`,
					);
					return false;
				}
				const missingQualityId = p.assignmentCodeIds.find(
					(ccId) => (p.equipmentQualities[ccId] || []).length === 0,
				);
				if (missingQualityId) {
					const cc = contractCodes.find((c) => c.id === missingQualityId);
					popup.error(
						`Vui lòng chọn Chất lượng thiết bị cho ${cc?.name || cc?.code || missingQualityId} ở khoảng thời gian thứ ${order} của Máy xúc/gạt`,
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

				for (const item of p.items) {
					let procId = item.productionProcessId;
					const foundProc = processOptions.find(
						(po) => po.value === procId || po.label === procId,
					);
					if (foundProc) {
						procId = foundProc.value;
					}

					if (!procId) {
						popup.error(
							`Vui lòng chọn công đoạn sản xuất cho ${item.title || 'mục đã chọn'}`,
						);
						return false;
					}

					const payload = {
						assignmentCodeId: item.assignmentCodeId,
						equipmentQuality: item.equipmentQuality,
						productionProcessId: procId,
						startMonth,
						endMonth,
						details: [
							{
								fuelUnitPrice: Number(item.fuelUnitPrice) || 0,
								maintenanceUnitPrice: Number(item.maintenanceUnitPrice) || 0,
							},
						],
					};

					if (item.id && !isDuplicate && !isPeriodCreate) {
						await api.put(
							API.PRICING.MOTORIZED_TRANSPORT.EXCAVATOR_DOZER.UPDATE,
							{
								id: item.id,
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
					<span>Máy xúc, máy gạt</span>
				</div>

				{/* DANH SÁCH CÁC KHOẢNG THỜI GIAN */}
				<div className='flex flex-col gap-5'>
					{periods.map((period, index) => (
						<ExcavatorPeriodSection
							key={period.tempId}
							period={period}
							totalPeriods={periods.length}
							contractCodes={contractCodes}
							processOptions={processOptions}
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

export default MotorizedExcavatorDozerForm;
