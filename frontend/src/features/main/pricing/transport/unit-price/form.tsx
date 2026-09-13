/* eslint-disable react-hooks/incompatible-library */
import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { MonthYearInput } from '@/components/form/form-month-year';
import { FormNumberInput } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { Button } from '@/components/ui/button';
import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Department } from '@/features/main/catalog/department/columns';
import type { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import {
	detectTransportMode,
	type TransportMode,
	type TransportUnitPrice,
} from '@/features/main/pricing/transport/unit-price/columns';
import {
	TRANSPORT_COMMON_FORM_DEFAULT,
	type TransportCommonFormSchema,
	transportCommonFormSchema,
	type TransportPeriodCardData,
	type TransportPeriodRowItem,
	validateTransportPeriods,
} from '@/features/main/pricing/transport/unit-price/schema';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import {
	Check,
	ChevronDownIcon,
	Copy,
	PlusCircleIcon,
	XCircleIcon,
} from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';

type TransportUnitPriceFormProps = ActionDialogProps<TransportUnitPrice> & {
	isDuplicate?: boolean;
};

const QUALITY_OPTIONS: MultiSelectOption[] = [
	{ value: 'A', label: 'Thiết bị loại A' },
	{ value: 'B', label: 'Thiết bị loại B' },
	{ value: 'C', label: 'Thiết bị loại C' },
];

function SearchableSelect({
	value,
	onChange,
	placeholder,
	options = [],
	className,
}: {
	value?: string;
	onChange: (val: string) => void;
	placeholder?: string;
	options?: { value: string; label: string }[];
	className?: string;
}) {
	const [open, setOpen] = useState(false);
	const selected = options.find((o) => o.value === value);

	return (
		<Popover open={open} onOpenChange={setOpen}>
			<PopoverTrigger asChild>
				<Button
					variant='outline'
					role='combobox'
					aria-expanded={open}
					className={cn(
						'h-9 w-full justify-between rounded-md border-neutral-300 bg-white px-3 font-normal text-black shadow-none hover:bg-neutral-50',
						!selected && 'text-muted-foreground',
						className,
					)}
				>
					<span className='truncate'>
						{selected ? selected.label : placeholder}
					</span>
					<ChevronDownIcon className='ml-2 size-4 shrink-0 opacity-50' />
				</Button>
			</PopoverTrigger>
			<PopoverContent
				className='w-[var(--radix-popover-trigger-width)] min-w-[260px] p-0'
				align='start'
			>
				<Command>
					<CommandInput placeholder='Tìm kiếm...' />
					<CommandList className='max-h-56'>
						<CommandEmpty>Không tìm thấy.</CommandEmpty>
						<CommandGroup>
							{options.map((option) => (
								<CommandItem
									key={option.value}
									value={option.label}
									onSelect={() => {
										onChange(option.value);
										setOpen(false);
									}}
								>
									<Check
										className={cn(
											'mr-2 size-4',
											value === option.value ? 'opacity-100' : 'opacity-0',
										)}
									/>
									<span className='truncate'>{option.label}</span>
								</CommandItem>
							))}
						</CommandGroup>
					</CommandList>
				</Command>
			</PopoverContent>
		</Popover>
	);
}

const isGuid = (str?: string | null) =>
	!!str && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{8,12}$/i.test(str);

const formatDateOnly = (dateStr?: string) => {
	if (!dateStr) return '2026-01-01';
	const clean = dateStr.substring(0, 10);
	if (clean.length === 7) return `${clean}-01`;
	return clean;
};

// Đồng bộ danh sách các dòng đơn giá khi chọn từ MultiSelect
function computeSynchronizedItems(
	mode: TransportMode,
	routes: TransportRoute[],
	_contractCodes: { id: string; code: string; name: string }[],
	selectedRouteIds: string[] = [],
	routeDepartmentIds: Record<string, string[]> = {},
	selectedContractCodeIds: string[] = [],
	contractCodeQualityIds: Record<string, string[]> = {},
	prevItems: TransportPeriodRowItem[] = [],
): TransportPeriodRowItem[] {
	const newItems: TransportPeriodRowItem[] = [];

	if (mode === 'conveyor' || mode === 'shaft') {
		selectedRouteIds.forEach((routeId) => {
			const route = routes.find((r) => r.id === routeId);
			const isSpecialRoute = !!route?.isSpecialLowVolume;
			const deptsForRoute = routeDepartmentIds[routeId] || [];

			deptsForRoute.forEach((deptId) => {
				if (isSpecialRoute) {
					// Dòng 1: Sản lượng >= 10.000 tấn/tháng (Trường hợp bình thường)
					const existingNormal = prevItems.find(
						(it) =>
							it.transportRouteId === routeId &&
							it.departmentId === deptId &&
							!it.isLowVolumeCase,
					);
					newItems.push({
						tempId:
							existingNormal?.tempId ||
							`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: existingNormal?.id,
						transportRouteId: routeId,
						departmentId: deptId,
						materialFuelUnitPrice:
							existingNormal?.materialFuelUnitPrice ?? null,
						powerUnitPrice: existingNormal?.powerUnitPrice ?? null,
						maintenanceUnitPrice: existingNormal?.maintenanceUnitPrice ?? null,
						isLowVolumeCase: false,
					});

					// Dòng 2: Sản lượng < 10.000 tấn/tháng (Trường hợp đặc biệt)
					const existingLow = prevItems.find(
						(it) =>
							it.transportRouteId === routeId &&
							it.departmentId === deptId &&
							it.isLowVolumeCase,
					);
					newItems.push({
						tempId:
							existingLow?.tempId ||
							`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: existingLow?.id,
						transportRouteId: routeId,
						departmentId: deptId,
						materialFuelUnitPrice: existingLow?.materialFuelUnitPrice ?? null,
						powerUnitPrice: existingLow?.powerUnitPrice ?? null,
						maintenanceUnitPrice: existingLow?.maintenanceUnitPrice ?? null,
						isLowVolumeCase: true,
					});
				} else {
					const existing = prevItems.find(
						(it) =>
							it.transportRouteId === routeId &&
							it.departmentId === deptId &&
							!it.isLowVolumeCase,
					);
					newItems.push({
						tempId:
							existing?.tempId ||
							`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
						id: existing?.id,
						transportRouteId: routeId,
						departmentId: deptId,
						materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
						powerUnitPrice: existing?.powerUnitPrice ?? null,
						maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
						isLowVolumeCase: false,
					});
				}
			});
		});
	} else if (mode === 'cable_winch') {
		selectedRouteIds.forEach((routeId) => {
			const route = routes.find((r) => r.id === routeId);
			const isLowVolume = !!route?.isSpecialLowVolume;
			const existing = prevItems.find((it) => it.transportRouteId === routeId);

			newItems.push({
				tempId:
					existing?.tempId ||
					`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: existing?.id,
				transportRouteId: routeId,
				materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
				powerUnitPrice: isLowVolume ? 0 : (existing?.powerUnitPrice ?? null),
				maintenanceUnitPrice: isLowVolume
					? 115834567
					: (existing?.maintenanceUnitPrice ?? null),
				isLowVolumeCase: isLowVolume,
			});
		});
	} else if (mode === 'monorail') {
		selectedContractCodeIds.forEach((ccId) => {
			const qualitiesForCc = contractCodeQualityIds[ccId] || [];
			qualitiesForCc.forEach((quality) => {
				const existing = prevItems.find(
					(it) =>
						(it.equipmentId === ccId || (it as any).contractCodeId === ccId) &&
						it.equipmentQuality === quality,
				);
				newItems.push({
					tempId:
						existing?.tempId ||
						`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
					id: existing?.id,
					equipmentId: ccId,
					equipmentQuality: quality,
					materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
					powerUnitPrice: existing?.powerUnitPrice ?? null,
					maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
				});
			});
		});
	} else if (mode === 'other') {
		selectedContractCodeIds.forEach((ccId) => {
			const existing = prevItems.find(
				(it) => it.equipmentId === ccId || (it as any).contractCodeId === ccId,
			);
			newItems.push({
				tempId:
					existing?.tempId ||
					`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: existing?.id,
				equipmentId: ccId,
				materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
				powerUnitPrice: existing?.powerUnitPrice ?? null,
				maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
				quantity: existing?.quantity ?? null,
				unitOfMeasureId: existing?.unitOfMeasureId || '',
			});
		});
	}

	return newItems;
}

interface TransportPeriodSectionProps {
	period: TransportPeriodCardData;
	totalPeriods: number;
	transportMode: TransportMode;
	routes: TransportRoute[];
	departments: Department[];
	contractCodes: { id: string; code: string; name: string }[];
	units: { id: string; name: string }[];
	unitLabel: string;
	onUpdate: (updated: TransportPeriodCardData) => void;
	onCopy: () => void;
	onDelete: () => void;
	onItemsDeleted: (deletedIds: string[]) => void;
}

function TransportPeriodSection({
	period,
	totalPeriods,
	transportMode,
	routes,
	departments,
	contractCodes,
	units,
	unitLabel,
	onUpdate,
	onCopy,
	onDelete,
	onItemsDeleted,
}: TransportPeriodSectionProps) {
	// Options cho Tuyến vận tải
	const routeOptions: MultiSelectOption[] = useMemo(() => {
		return routes.map((r) => ({
			value: r.id,
			label: `${r.code} - ${r.name}`,
		}));
	}, [routes]);

	const selectedRouteOptions: MultiSelectOption[] = useMemo(() => {
		return (period.selectedRouteIds || []).map((id) => {
			const r = routes.find((route) => route.id === id);
			return {
				value: id,
				label: r ? `${r.code} - ${r.name}` : id,
			};
		});
	}, [routes, period.selectedRouteIds]);

	// Options cho Đơn vị
	const departmentOptions: MultiSelectOption[] = useMemo(() => {
		return departments.map((d) => ({
			value: d.id,
			label: `${d.code} - ${d.name}`,
		}));
	}, [departments]);

	// Options cho Nhóm vật tư, tài sản
	const contractCodeOptions: MultiSelectOption[] = useMemo(() => {
		return contractCodes.map((c) => ({
			value: c.id,
			label: `${c.code} - ${c.name}`,
		}));
	}, [contractCodes]);

	const selectedContractCodeOptions: MultiSelectOption[] = useMemo(() => {
		return (period.selectedContractCodeIds || []).map((id) => {
			const c = contractCodes.find((cc) => cc.id === id);
			return {
				value: id,
				label: c ? `${c.code} - ${c.name}` : id,
			};
		});
	}, [contractCodes, period.selectedContractCodeIds]);

	// Xử lý thay đổi Tuyến vận tải
	const handleRoutesChange = (selected: MultiSelectOption[]) => {
		const newRouteIds = selected.map((s) => s.value);
		const nextRouteDeptIds: Record<string, string[]> = {};
		newRouteIds.forEach((rid) => {
			if (period.routeDepartmentIds?.[rid]) {
				nextRouteDeptIds[rid] = period.routeDepartmentIds[rid];
			}
		});

		const nextItems = computeSynchronizedItems(
			transportMode,
			routes,
			contractCodes,
			newRouteIds,
			nextRouteDeptIds,
			period.selectedContractCodeIds,
			period.contractCodeQualityIds,
			period.items,
		);

		const nextItemIds = new Set(nextItems.map((i) => i.id).filter(Boolean));
		const removedItemIds = period.items
			.map((i) => i.id)
			.filter((id): id is string => Boolean(id) && !nextItemIds.has(id));
		if (removedItemIds.length > 0) {
			onItemsDeleted(removedItemIds);
		}

		onUpdate({
			...period,
			selectedRouteIds: newRouteIds,
			routeDepartmentIds: nextRouteDeptIds,
			items: nextItems,
		});
	};

	// Xử lý thay đổi Đơn vị của một Tuyến vận tải
	const handleDeptChangeForRoute = (
		routeId: string,
		selected: MultiSelectOption[],
	) => {
		const newDeptIds = selected.map((s) => s.value);
		const nextRouteDeptIds = {
			...(period.routeDepartmentIds || {}),
			[routeId]: newDeptIds,
		};

		const nextItems = computeSynchronizedItems(
			transportMode,
			routes,
			contractCodes,
			period.selectedRouteIds,
			nextRouteDeptIds,
			period.selectedContractCodeIds,
			period.contractCodeQualityIds,
			period.items,
		);

		const nextItemIds = new Set(nextItems.map((i) => i.id).filter(Boolean));
		const removedItemIds = period.items
			.map((i) => i.id)
			.filter((id): id is string => Boolean(id) && !nextItemIds.has(id));
		if (removedItemIds.length > 0) {
			onItemsDeleted(removedItemIds);
		}

		onUpdate({
			...period,
			routeDepartmentIds: nextRouteDeptIds,
			items: nextItems,
		});
	};

	// Xử lý thay đổi Nhóm vật tư, tài sản
	const handleContractCodesChange = (selected: MultiSelectOption[]) => {
		const newCcIds = selected.map((s) => s.value);
		const nextQualityIds: Record<string, string[]> = {};
		newCcIds.forEach((cid) => {
			if (period.contractCodeQualityIds?.[cid]) {
				nextQualityIds[cid] = period.contractCodeQualityIds[cid];
			}
		});

		const nextItems = computeSynchronizedItems(
			transportMode,
			routes,
			contractCodes,
			period.selectedRouteIds,
			period.routeDepartmentIds,
			newCcIds,
			nextQualityIds,
			period.items,
		);

		const nextItemIds = new Set(nextItems.map((i) => i.id).filter(Boolean));
		const removedItemIds = period.items
			.map((i) => i.id)
			.filter((id): id is string => Boolean(id) && !nextItemIds.has(id));
		if (removedItemIds.length > 0) {
			onItemsDeleted(removedItemIds);
		}

		onUpdate({
			...period,
			selectedContractCodeIds: newCcIds,
			contractCodeQualityIds: nextQualityIds,
			items: nextItems,
		});
	};

	// Xử lý thay đổi Chất lượng thiết bị của một Nhóm vật tư
	const handleQualityChangeForCc = (
		ccId: string,
		selected: MultiSelectOption[],
	) => {
		const newQualities = selected.map((s) => s.value);
		const nextQualityIds = {
			...(period.contractCodeQualityIds || {}),
			[ccId]: newQualities,
		};

		const nextItems = computeSynchronizedItems(
			transportMode,
			routes,
			contractCodes,
			period.selectedRouteIds,
			period.routeDepartmentIds,
			period.selectedContractCodeIds,
			nextQualityIds,
			period.items,
		);

		const nextItemIds = new Set(nextItems.map((i) => i.id).filter(Boolean));
		const removedItemIds = period.items
			.map((i) => i.id)
			.filter((id): id is string => Boolean(id) && !nextItemIds.has(id));
		if (removedItemIds.length > 0) {
			onItemsDeleted(removedItemIds);
		}

		onUpdate({
			...period,
			contractCodeQualityIds: nextQualityIds,
			items: nextItems,
		});
	};

	// Cập nhật giá trị đơn giá / định mức của một dòng
	const handleItemValueChange = (
		tempId: string,
		field: keyof TransportPeriodRowItem,
		val: any,
	) => {
		const nextItems = period.items.map((it) => {
			if (it.tempId === tempId) {
				return { ...it, [field]: val };
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

			{/* ============================================================== */}
			{/* Case 1: CONVEYOR & SHAFT */}
			{/* ============================================================== */}
			{(transportMode === 'conveyor' || transportMode === 'shaft') && (
				<>
					<MultiSelect
						label='Tuyến vận tải'
						placeholder='Chọn Tuyến vận tải'
						options={routeOptions}
						values={selectedRouteOptions}
						onValuesChange={handleRoutesChange}
					/>

					{(period.selectedRouteIds || []).length > 0 && (
						<div className='mt-1 flex flex-col gap-4'>
							{(period.selectedRouteIds || []).map((routeId) => {
								const route = routes.find((r) => r.id === routeId);
								const isSpecialRoute = !!route?.isSpecialLowVolume;
								const deptsForRoute =
									period.routeDepartmentIds?.[routeId] || [];

								const selectedDeptOptions: MultiSelectOption[] =
									deptsForRoute.map((deptId) => {
										const d = departments.find((dept) => dept.id === deptId);
										return {
											value: deptId,
											label: d ? `${d.code} - ${d.name}` : deptId,
										};
									});

								const routeItems = period.items.filter(
									(it) => it.transportRouteId === routeId,
								);

								return (
									<div
										key={routeId}
										className='flex flex-col gap-3 rounded-lg border border-neutral-200 bg-white p-4 shadow-xs'
									>
										<div className='flex items-center justify-between text-sm font-semibold text-black'>
											<div className='flex items-center gap-2'>
												<span>
													{route ? `${route.code} - ${route.name}` : routeId}
												</span>
												{isSpecialRoute && (
													<span className='rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-800'>
														Tuyến đặc biệt
													</span>
												)}
											</div>
										</div>

										<MultiSelect
											label='Đơn vị áp dụng cho tuyến này'
											placeholder='Chọn Đơn vị'
											options={departmentOptions}
											values={selectedDeptOptions}
											onValuesChange={(selected) =>
												handleDeptChangeForRoute(routeId, selected)
											}
										/>

										{deptsForRoute.length > 0 && routeItems.length > 0 && (
											<div className='mt-2 overflow-hidden rounded-md border border-neutral-200 bg-white'>
												<table className='w-full text-left text-sm text-black'>
													<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
														<tr>
															<th className='min-w-[220px] px-4 py-2.5'>
																Đơn vị
															</th>
															<th className='min-w-[180px] px-4 py-2.5'>
																Đơn giá vật liệu, nhiên liệu ({unitLabel})
															</th>
															<th className='min-w-[180px] px-4 py-2.5'>
																Đơn giá động lực ({unitLabel})
															</th>
															<th className='min-w-[180px] px-4 py-2.5'>
																Đơn giá SCTX ({unitLabel})
															</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-neutral-100'>
														{routeItems.map((item) => {
															const dept = departments.find(
																(d) => d.id === item.departmentId,
															);
															const isLowVolume = !!item.isLowVolumeCase;
															const deptName = dept
																? `${dept.code} - ${dept.name}`
																: item.departmentId;
															const rowLabel = isLowVolume
																? `${deptName} (Sản lượng < 10.000 tấn/tháng)`
																: isSpecialRoute
																	? `${deptName} (Sản lượng ≥ 10.000 tấn/tháng)`
																	: deptName;

															return (
																<tr
																	key={item.tempId}
																	className='transition-colors hover:bg-neutral-50/50'
																>
																	<td className='px-4 py-2.5 font-medium whitespace-nowrap text-black'>
																		<span>{rowLabel}</span>
																	</td>
																	<td className='px-4 py-2'>
																		<FormNumberInput
																			value={
																				item.materialFuelUnitPrice ?? undefined
																			}
																			onValueChange={(val) =>
																				handleItemValueChange(
																					item.tempId,
																					'materialFuelUnitPrice',
																					val,
																				)
																			}
																			placeholder='Nhập đơn giá'
																		/>
																	</td>
																	<td className='px-4 py-2'>
																		<FormNumberInput
																			value={item.powerUnitPrice ?? undefined}
																			onValueChange={(val) =>
																				handleItemValueChange(
																					item.tempId,
																					'powerUnitPrice',
																					val,
																				)
																			}
																			placeholder='Nhập đơn giá'
																		/>
																	</td>
																	<td className='px-4 py-2'>
																		<FormNumberInput
																			value={
																				item.maintenanceUnitPrice ?? undefined
																			}
																			onValueChange={(val) =>
																				handleItemValueChange(
																					item.tempId,
																					'maintenanceUnitPrice',
																					val,
																				)
																			}
																			placeholder='Nhập đơn giá'
																		/>
																	</td>
																</tr>
															);
														})}
													</tbody>
												</table>
											</div>
										)}
									</div>
								);
							})}
						</div>
					)}
				</>
			)}

			{/* ============================================================== */}
			{/* Case 2: MONORAIL */}
			{/* ============================================================== */}
			{transportMode === 'monorail' && (
				<>
					<MultiSelect
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn Nhóm vật tư, tài sản'
						options={contractCodeOptions}
						values={selectedContractCodeOptions}
						onValuesChange={handleContractCodesChange}
					/>

					{(period.selectedContractCodeIds || []).length > 0 && (
						<div className='mt-1 flex flex-col gap-4'>
							{(period.selectedContractCodeIds || []).map((ccId) => {
								const cc = contractCodes.find((c) => c.id === ccId);
								const qualitiesForCc =
									period.contractCodeQualityIds?.[ccId] || [];

								const selectedQualityOptions: MultiSelectOption[] =
									qualitiesForCc.map((q) => {
										const opt = QUALITY_OPTIONS.find((o) => o.value === q);
										return opt || { value: q, label: `Thiết bị loại ${q}` };
									});

								const ccItems = period.items.filter(
									(it) => it.equipmentId === ccId,
								);

								return (
									<div
										key={ccId}
										className='flex flex-col gap-3 rounded-lg border border-neutral-200 bg-white p-4 shadow-xs'
									>
										<div className='text-sm font-semibold text-black'>
											{cc ? `${cc.code} - ${cc.name}` : ccId}
										</div>

										<MultiSelect
											label='Chất lượng thiết bị'
											placeholder='Chọn Chất lượng thiết bị'
											options={QUALITY_OPTIONS}
											values={selectedQualityOptions}
											onValuesChange={(selected) =>
												handleQualityChangeForCc(ccId, selected)
											}
										/>

										{qualitiesForCc.length > 0 && ccItems.length > 0 && (
											<div className='mt-2 overflow-hidden rounded-md border border-neutral-200 bg-white'>
												<table className='w-full text-left text-sm text-black'>
													<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
														<tr>
															<th className='min-w-[180px] px-4 py-2.5'>
																Chất lượng thiết bị
															</th>
															<th className='min-w-[200px] px-4 py-2.5'>
																Đơn giá vật liệu, nhiên liệu (đ/h)
															</th>
															<th className='min-w-[180px] px-4 py-2.5'>
																Đơn giá động lực (đ/h)
															</th>
															<th className='min-w-[180px] px-4 py-2.5'>
																Đơn giá SCTX (đ/h)
															</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-neutral-100'>
														{ccItems.map((item) => (
															<tr
																key={item.tempId}
																className='transition-colors hover:bg-neutral-50/50'
															>
																<td className='px-4 py-2.5 font-medium whitespace-nowrap text-black'>
																	Thiết bị loại {item.equipmentQuality}
																</td>
																<td className='px-4 py-2'>
																	<FormNumberInput
																		value={
																			item.materialFuelUnitPrice ?? undefined
																		}
																		onValueChange={(val) =>
																			handleItemValueChange(
																				item.tempId,
																				'materialFuelUnitPrice',
																				val,
																			)
																		}
																		placeholder='Nhập đơn giá'
																	/>
																</td>
																<td className='px-4 py-2'>
																	<FormNumberInput
																		value={item.powerUnitPrice ?? undefined}
																		onValueChange={(val) =>
																			handleItemValueChange(
																				item.tempId,
																				'powerUnitPrice',
																				val,
																			)
																		}
																		placeholder='Nhập đơn giá'
																	/>
																</td>
																<td className='px-4 py-2'>
																	<FormNumberInput
																		value={
																			item.maintenanceUnitPrice ?? undefined
																		}
																		onValueChange={(val) =>
																			handleItemValueChange(
																				item.tempId,
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
										)}
									</div>
								);
							})}
						</div>
					)}
				</>
			)}

			{/* ============================================================== */}
			{/* Case 3: CABLE WINCH */}
			{/* ============================================================== */}
			{transportMode === 'cable_winch' && (
				<>
					<MultiSelect
						label='Tuyến vận tải'
						placeholder='Chọn Tuyến vận tải'
						options={routeOptions}
						values={selectedRouteOptions}
						onValuesChange={handleRoutesChange}
					/>

					{period.items.length > 0 && (
						<div className='mt-1 flex flex-col gap-3'>
							<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
								<table className='w-full text-left text-sm text-black'>
									<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
										<tr>
											<th className='min-w-[240px] px-4 py-2.5'>
												Tuyến vận tải
											</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá vật liệu, nhiên liệu (đ/tháng)
											</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá động lực (đ/tháng)
											</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá SCTX (đ/tháng)
											</th>
										</tr>
									</thead>
									<tbody className='divide-y divide-neutral-100'>
										{period.items.map((item) => {
											const route = routes.find(
												(r) => r.id === item.transportRouteId,
											);
											const isLowVolume = !!item.isLowVolumeCase;
											return (
												<tr
													key={item.tempId}
													className='transition-colors hover:bg-neutral-50/50'
												>
													<td className='px-4 py-2.5 font-medium text-black'>
														<div className='flex items-center gap-2'>
															<span>
																{route
																	? `${route.code} - ${route.name}`
																	: item.transportRouteId}
															</span>
															{isLowVolume && (
																<span className='rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold whitespace-nowrap text-amber-800'>
																	⚡ Sản lượng &lt; 10.000 tấn/tháng
																</span>
															)}
														</div>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.materialFuelUnitPrice ?? undefined}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'materialFuelUnitPrice',
																	val,
																)
															}
															placeholder='Nhập đơn giá'
														/>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.powerUnitPrice ?? undefined}
															disabled={isLowVolume}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'powerUnitPrice',
																	val,
																)
															}
															placeholder={
																isLowVolume ? '0 đ/tháng' : 'Nhập đơn giá'
															}
														/>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.maintenanceUnitPrice ?? undefined}
															disabled={isLowVolume}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'maintenanceUnitPrice',
																	val,
																)
															}
															placeholder={
																isLowVolume
																	? '115.834.567 đ/tháng'
																	: 'Nhập đơn giá'
															}
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
				</>
			)}

			{/* ============================================================== */}
			{/* Case 4: OTHER */}
			{/* ============================================================== */}
			{transportMode === 'other' && (
				<>
					<MultiSelect
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn Nhóm vật tư, tài sản'
						options={contractCodeOptions}
						values={selectedContractCodeOptions}
						onValuesChange={handleContractCodesChange}
					/>

					{period.items.length > 0 && (
						<div className='mt-1 flex flex-col gap-3'>
							<div className='overflow-hidden rounded-md border border-neutral-200 bg-white'>
								<table className='w-full text-left text-sm text-black'>
									<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
										<tr>
											<th className='min-w-[220px] px-4 py-2.5'>
												Nhóm vật tư, tài sản
											</th>
											<th className='min-w-[140px] px-4 py-2.5'>Định mức</th>
											<th className='min-w-[160px] px-4 py-2.5'>Đơn vị tính</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá vật liệu, nhiên liệu (đ/tháng)
											</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá động lực (đ/tháng)
											</th>
											<th className='min-w-[180px] px-4 py-2.5'>
												Đơn giá SCTX (đ/tháng)
											</th>
										</tr>
									</thead>
									<tbody className='divide-y divide-neutral-100'>
										{period.items.map((item) => {
											const cc = contractCodes.find(
												(c) => c.id === item.equipmentId,
											);
											return (
												<tr
													key={item.tempId}
													className='transition-colors hover:bg-neutral-50/50'
												>
													<td className='px-4 py-2.5 font-medium whitespace-nowrap text-black'>
														{cc ? `${cc.code} - ${cc.name}` : item.equipmentId}
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.quantity ?? undefined}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'quantity',
																	val,
																)
															}
															placeholder='Nhập định mức'
														/>
													</td>
													<td className='px-4 py-2'>
														<SearchableSelect
															value={item.unitOfMeasureId}
															placeholder='Chọn ĐVT'
															options={units.map((u) => ({
																value: u.id,
																label: u.name,
															}))}
															onChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'unitOfMeasureId',
																	val,
																)
															}
														/>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.materialFuelUnitPrice ?? undefined}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'materialFuelUnitPrice',
																	val,
																)
															}
															placeholder='Nhập đơn giá'
														/>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.powerUnitPrice ?? undefined}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'powerUnitPrice',
																	val,
																)
															}
															placeholder='Nhập đơn giá'
														/>
													</td>
													<td className='px-4 py-2'>
														<FormNumberInput
															value={item.maintenanceUnitPrice ?? undefined}
															onValueChange={(val) =>
																handleItemValueChange(
																	item.tempId,
																	'maintenanceUnitPrice',
																	val,
																)
															}
															placeholder='Nhập đơn giá'
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
				</>
			)}
		</div>
	);
}

export function TransportUnitPriceForm({
	data,
	row,
	isDuplicate = false,
}: TransportUnitPriceFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();

	const [productionProcesses, setProductionProcesses] = useState<
		{ id: string; code?: string; name: string }[]
	>([]);
	const [routes, setRoutes] = useState<TransportRoute[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [contractCodes, setContractCodes] = useState<
		{ id: string; code: string; name: string }[]
	>([]);
	const [units, setUnits] = useState<{ id: string; name: string }[]>([]);
	const [deletedItemIds, setDeletedItemIds] = useState<string[]>([]);

	const form = useForm<TransportCommonFormSchema>({
		resolver: zodResolver(transportCommonFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...TRANSPORT_COMMON_FORM_DEFAULT,
			productionProcessId: row?.productionProcessId || '',
		},
	});

	const selectedProductionProcessId = useWatch({
		control: form.control,
		name: 'productionProcessId',
	});

	const [periods, setPeriods] = useState<TransportPeriodCardData[]>([
		{
			tempId: `period_${Date.now()}`,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
			selectedRouteIds: [],
			routeDepartmentIds: {},
			selectedContractCodeIds: [],
			contractCodeQualityIds: {},
			items: [],
		},
	]);

	// Tải danh mục
	useEffect(() => {
		api
			.pagging<{
				id: string;
				code?: string;
				name: string;
				processGroupName?: string;
			}>(API.CATALOG.PROCESS.STEP.LIST, { ignorePagination: true })
			.then((res) => {
				const fetched = res.result.data || [];
				const vtlProcesses = fetched.filter((p) => {
					const group = (p.processGroupName || '').toLowerCase();
					const name = (p.name || '').toLowerCase();
					const code = (p.code || '').toUpperCase();

					// Loại bỏ các công đoạn của Vận tải cơ giới
					if (
						group.includes('cơ giới') ||
						code === 'GDC' ||
						code === 'GPV' ||
						code === 'GGDC' ||
						code === 'GGPV' ||
						name.startsWith('giờ phục vụ') ||
						name.startsWith('giờ di chuyển') ||
						name.startsWith('giờ gạt')
					) {
						return false;
					}

					return (
						!p.processGroupName ||
						group.includes('vận tải lò') ||
						group.includes('vtl') ||
						group.includes('vận tải')
					);
				});
				const finalProcesses = vtlProcesses.length > 0 ? vtlProcesses : fetched;
				setProductionProcesses(finalProcesses);
				if (
					!row &&
					finalProcesses.length > 0 &&
					!form.getValues('productionProcessId')
				) {
					form.setValue('productionProcessId', finalProcesses[0].id);
				}
			})
			.catch(() => {});

		api
			.pagging<TransportRoute>(API.CATALOG.TRANSPORT_ROUTE.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				setRoutes(res.result.data || []);
			})
			.catch(() => {});

		api
			.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				setDepartments(res.result.data || []);
			})
			.catch(() => {});

		api
			.pagging<{ id: string; code: string; name: string }>(
				API.CATALOG.CONTRACT_CODE.LIST,
				{ ignorePagination: true },
			)
			.then((res) => {
				setContractCodes(res.result.data || []);
			})
			.catch(() => {});

		api
			.pagging<{ id: string; name: string }>(API.CATALOG.UNIT.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				setUnits(res.result.data || []);
			})
			.catch(() => {});
	}, [row, form]);

	// Tải dữ liệu các khoảng thời gian khi mở form ở chế độ Sửa / Duplicate
	useEffect(() => {
		if (!row) return;

		form.setValue('productionProcessId', row.productionProcessId || '');

		const periodsToLoad: {
			id?: string;
			startMonth?: string;
			endMonth?: string;
			items: any[];
		}[] =
			row.periods && row.periods.length > 0
				? row.periods
				: [
						{
							id: row.id,
							startMonth: row.startMonth,
							endMonth: row.endMonth,
							items: row.items && row.items.length > 0 ? row.items : [row],
						},
					];

		const loadedPeriods: TransportPeriodCardData[] = periodsToLoad.map((p) => {
			const rawItems = (p.items || []) as any[];

			const routeIds = Array.from(
				new Set(rawItems.map((i) => i.transportRouteId).filter(Boolean)),
			) as string[];

			const routeDepts: Record<string, string[]> = {};
			rawItems.forEach((i) => {
				if (i.transportRouteId && i.departmentId) {
					if (!routeDepts[i.transportRouteId]) {
						routeDepts[i.transportRouteId] = [];
					}
					if (!routeDepts[i.transportRouteId].includes(i.departmentId)) {
						routeDepts[i.transportRouteId].push(i.departmentId);
					}
				}
			});

			const contractIds = Array.from(
				new Set(
					rawItems
						.map((i) => i.contractCodeId || i.equipmentId)
						.filter(Boolean),
				),
			) as string[];

			const contractCodeQualities: Record<string, string[]> = {};
			rawItems.forEach((i) => {
				const ccId = i.contractCodeId || i.equipmentId;
				if (ccId && i.equipmentQuality) {
					if (!contractCodeQualities[ccId]) {
						contractCodeQualities[ccId] = [];
					}
					if (!contractCodeQualities[ccId].includes(i.equipmentQuality)) {
						contractCodeQualities[ccId].push(i.equipmentQuality);
					}
				}
			});

			const items: TransportPeriodRowItem[] = rawItems.map((item) => ({
				tempId:
					item.id ||
					`item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: isDuplicate ? undefined : item.id,
				transportRouteId: item.transportRouteId || '',
				departmentId: item.departmentId || '',
				equipmentId: item.equipmentId || item.contractCodeId || '',
				equipmentQuality: item.equipmentQuality || '',
				materialFuelUnitPrice: item.materialFuelUnitPrice ?? null,
				powerUnitPrice: item.powerUnitPrice ?? null,
				maintenanceUnitPrice: item.maintenanceUnitPrice ?? null,
				quantity: item.quantity ?? null,
				unitOfMeasureId: item.unitOfMeasureId || '',
				isLowVolumeCase: item.isLowVolumeCase ?? false,
			}));

			return {
				tempId:
					p.id ||
					`period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: isDuplicate ? undefined : p.id,
				startMonth: (p.startMonth ?? '').substring(0, 10),
				endMonth: (p.endMonth ?? '').substring(0, 10),
				selectedRouteIds: routeIds,
				routeDepartmentIds: routeDepts,
				selectedContractCodeIds: contractIds,
				contractCodeQualityIds: contractCodeQualities,
				items,
			};
		});

		setPeriods(loadedPeriods);
	}, [row, isDuplicate, form]);

	// Phương thức vận tải tương ứng với công đoạn
	const transportMode: TransportMode = useMemo(() => {
		if (!selectedProductionProcessId) return 'conveyor';
		const proc = productionProcesses.find(
			(p) => p.id === selectedProductionProcessId,
		);
		return detectTransportMode(
			proc?.code || row?.productionProcessCode,
			proc?.name || row?.productionProcessName,
		);
	}, [selectedProductionProcessId, productionProcesses, row]);

	// Reset selections khi người dùng đổi Công đoạn sản xuất khác
	const prevProcessIdRef = useRef<string | undefined>(undefined);
	useEffect(() => {
		if (
			prevProcessIdRef.current !== undefined &&
			prevProcessIdRef.current !== selectedProductionProcessId
		) {
			setPeriods((prev) =>
				prev.map((p) => ({
					...p,
					selectedRouteIds: [],
					routeDepartmentIds: {},
					selectedContractCodeIds: [],
					contractCodeQualityIds: {},
					items: [],
				})),
			);
		}
		prevProcessIdRef.current = selectedProductionProcessId;
	}, [selectedProductionProcessId]);

	// Nhãn đơn vị tính tiền tệ theo phương thức vận tải
	const unitLabel = useMemo(() => {
		switch (transportMode) {
			case 'conveyor':
				return 'đ/tấn';
			case 'shaft':
				return 'đ/xe';
			case 'cable_winch':
			case 'other':
				return 'đ/tháng';
			case 'monorail':
				return 'đ/h';
			default:
				return 'đ';
		}
	}, [transportMode]);

	// Thêm khoảng thời gian
	const handleAddPeriod = () => {
		let nextStart = new Date().toISOString().substring(0, 10);
		let nextEnd = new Date().toISOString().substring(0, 10);

		if (periods.length > 0) {
			const sortedEndDates = periods
				.map((p) => p.endMonth)
				.filter(Boolean)
				.sort();
			const latestEnd = sortedEndDates[sortedEndDates.length - 1];
			if (latestEnd) {
				const date = new Date(latestEnd);
				const nextStartDate = new Date(
					date.getFullYear(),
					date.getMonth() + 1,
					1,
				);
				const nextEndDate = new Date(
					date.getFullYear() + 1,
					date.getMonth() + 1,
					0,
				);
				nextStart = nextStartDate.toISOString().substring(0, 10);
				nextEnd = nextEndDate.toISOString().substring(0, 10);
			}
		}

		const newPeriod: TransportPeriodCardData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			startMonth: nextStart,
			endMonth: nextEnd,
			selectedRouteIds: [],
			routeDepartmentIds: {},
			selectedContractCodeIds: [],
			contractCodeQualityIds: {},
			items: [],
		};

		setPeriods((prev) => [...prev, newPeriod]);
	};

	// Sao chép khoảng thời gian
	const handleCopyPeriod = (index: number) => {
		const target = periods[index];
		if (!target) return;

		let nextStart = target.startMonth;
		let nextEnd = target.endMonth;
		if (target.endMonth) {
			const date = new Date(target.endMonth);
			const nextStartDate = new Date(
				date.getFullYear(),
				date.getMonth() + 1,
				1,
			);
			const nextEndDate = new Date(
				date.getFullYear() + 1,
				date.getMonth() + 1,
				0,
			);
			nextStart = nextStartDate.toISOString().substring(0, 10);
			nextEnd = nextEndDate.toISOString().substring(0, 10);
		}

		const copiedPeriod: TransportPeriodCardData = {
			tempId: `period_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
			id: undefined,
			startMonth: nextStart,
			endMonth: nextEnd,
			selectedRouteIds: [...(target.selectedRouteIds || [])],
			routeDepartmentIds: { ...(target.routeDepartmentIds || {}) },
			selectedContractCodeIds: [...(target.selectedContractCodeIds || [])],
			contractCodeQualityIds: { ...(target.contractCodeQualityIds || {}) },
			items: target.items.map((item) => ({
				...item,
				tempId: `item_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
				id: undefined,
			})),
		};

		setPeriods((prev) => {
			const next = [...prev];
			next.splice(index + 1, 0, copiedPeriod);
			return next;
		});
	};

	// Xóa khoảng thời gian
	const handleDeletePeriod = (indexToDelete: number) => {
		if (periods.length <= 1) {
			popup.error('Mã đơn giá cần có ít nhất một khoảng thời gian áp dụng.');
			return;
		}

		const periodToDelete = periods[indexToDelete];
		if (!isDuplicate) {
			const itemIdsToDelete = periodToDelete.items
				.map((i) => i.id)
				.filter(Boolean) as string[];
			if (itemIdsToDelete.length > 0) {
				setDeletedItemIds((prev) => [...prev, ...itemIdsToDelete]);
			}
		}

		setPeriods((prev) => prev.filter((_, idx) => idx !== indexToDelete));
	};

	const handleUpdatePeriod = (
		index: number,
		updated: TransportPeriodCardData,
	) => {
		setPeriods((prev) => {
			const next = [...prev];
			next[index] = updated;
			return next;
		});
	};

	const handleItemsDeleted = (removedIds: string[]) => {
		if (!isDuplicate && removedIds.length > 0) {
			setDeletedItemIds((prev) => [...prev, ...removedIds]);
		}
	};

	const handleSubmit = async (values: TransportCommonFormSchema) => {
		const validationError = validateTransportPeriods(periods, transportMode);
		if (validationError) {
			popup.error(validationError);
			return;
		}

		try {
			const procObj = productionProcesses.find(
				(p) => p.id === values.productionProcessId,
			);
			const productionProcessName =
				procObj?.name || row?.productionProcessName || '';
			const productionProcessCode =
				procObj?.code || row?.productionProcessCode || '';

			if (row?.id && !isDuplicate) {
				// 1. Xóa các mục bị xóa khỏi backend
				if (deletedItemIds.length > 0) {
					if (deletedItemIds.length === 1) {
						await api.delete(API.PRICING.TRANSPORT.DELETE(deletedItemIds[0]));
					} else {
						await api.delete(API.PRICING.TRANSPORT.DELETES, deletedItemIds);
					}
				}

				// 2. Cập nhật các mục đã có id và thêm mới các mục mới
				for (const period of periods) {
					const startMonth = formatDateOnly(period.startMonth);
					const endMonth = formatDateOnly(period.endMonth);

					for (const it of period.items) {
						const qualityVal = it.equipmentQuality || null;
						const adjustmentFactorDescriptionId = isGuid(qualityVal)
							? qualityVal
							: null;
						const equipmentQualityStr = !isGuid(qualityVal) ? qualityVal : null;

						const payload = {
							productionProcessId: values.productionProcessId,
							productionProcessName,
							productionProcessCode,
							transportRouteId:
								transportMode === 'conveyor' ||
								transportMode === 'shaft' ||
								transportMode === 'cable_winch'
									? it.transportRouteId || null
									: null,
							departmentId:
								transportMode === 'conveyor' || transportMode === 'shaft'
									? it.departmentId || null
									: null,
							equipmentId:
								transportMode === 'monorail' || transportMode === 'other'
									? it.equipmentId || null
									: null,
							adjustmentFactorDescriptionId,
							equipmentQuality:
								transportMode === 'monorail' ? equipmentQualityStr : null,
							materialFuelUnitPrice: it.materialFuelUnitPrice ?? null,
							powerUnitPrice: it.powerUnitPrice ?? null,
							maintenanceUnitPrice: it.maintenanceUnitPrice ?? null,
							quantity:
								transportMode === 'other' ? (it.quantity ?? null) : null,
							unitOfMeasureId:
								transportMode === 'other' ? it.unitOfMeasureId || null : null,
							isLowVolumeCase:
								transportMode === 'conveyor' ||
								transportMode === 'shaft' ||
								transportMode === 'cable_winch'
									? (it.isLowVolumeCase ?? false)
									: false,
							startMonth,
							endMonth,
						};

						if (it.id) {
							await api.put(API.PRICING.TRANSPORT.UPDATE, {
								...payload,
								id: it.id,
							});
						} else {
							await api.post(API.PRICING.TRANSPORT.CREATE, payload);
						}
					}
				}
			} else {
				// Tạo mới hoàn toàn (hoặc Sao chép)
				for (const period of periods) {
					const startMonth = formatDateOnly(period.startMonth);
					const endMonth = formatDateOnly(period.endMonth);

					for (const it of period.items) {
						const qualityVal = it.equipmentQuality || null;
						const adjustmentFactorDescriptionId = isGuid(qualityVal)
							? qualityVal
							: null;
						const equipmentQualityStr = !isGuid(qualityVal) ? qualityVal : null;

						const payload = {
							productionProcessId: values.productionProcessId,
							productionProcessName,
							productionProcessCode,
							transportRouteId:
								transportMode === 'conveyor' ||
								transportMode === 'shaft' ||
								transportMode === 'cable_winch'
									? it.transportRouteId || null
									: null,
							departmentId:
								transportMode === 'conveyor' || transportMode === 'shaft'
									? it.departmentId || null
									: null,
							equipmentId:
								transportMode === 'monorail' || transportMode === 'other'
									? it.equipmentId || null
									: null,
							adjustmentFactorDescriptionId,
							equipmentQuality:
								transportMode === 'monorail' ? equipmentQualityStr : null,
							materialFuelUnitPrice: it.materialFuelUnitPrice ?? null,
							powerUnitPrice: it.powerUnitPrice ?? null,
							maintenanceUnitPrice: it.maintenanceUnitPrice ?? null,
							quantity:
								transportMode === 'other' ? (it.quantity ?? null) : null,
							unitOfMeasureId:
								transportMode === 'other' ? it.unitOfMeasureId || null : null,
							isLowVolumeCase:
								transportMode === 'conveyor' ||
								transportMode === 'shaft' ||
								transportMode === 'cable_winch'
									? (it.isLowVolumeCase ?? false)
									: false,
							startMonth,
							endMonth,
						};
						await api.post(API.PRICING.TRANSPORT.CREATE, payload);
					}
				}
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit} className='gap-4'>
			{/* Phần 1: Công đoạn sản xuất */}
			<FormComboBox
				control={form.control}
				name='productionProcessId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={productionProcesses.map((p) => ({
					value: p.id,
					label: p.code ? `${p.code} - ${p.name}` : p.name,
				}))}
				disabled={!!row && !isDuplicate}
			/>

			<FormSeparator label='Danh sách khoảng thời gian áp dụng' />

			{/* Phần 2: Danh sách các khoảng thời gian */}
			<div className='flex flex-col gap-5'>
				{periods.map((period, index) => (
					<TransportPeriodSection
						key={period.tempId}
						period={period}
						totalPeriods={periods.length}
						transportMode={transportMode}
						routes={routes}
						departments={departments}
						contractCodes={contractCodes}
						units={units}
						unitLabel={unitLabel}
						onUpdate={(updated) => handleUpdatePeriod(index, updated)}
						onDelete={() => handleDeletePeriod(index)}
						onCopy={() => handleCopyPeriod(index)}
						onItemsDeleted={handleItemsDeleted}
					/>
				))}

				{/* Nút Thêm thời gian ở dưới cùng */}
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

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
