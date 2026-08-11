import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Department } from '@/features/main/catalog/department/columns';
import { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import { TransportUnitPrice } from '@/features/main/pricing/transport/unit-price/columns';
import {
	TRANSPORT_UNIT_PRICE_SCHEMA_DEFAULT,
	TransportUnitPriceSchema,
	transportUnitPriceSchema,
} from '@/features/main/pricing/transport/unit-price/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';

type TransportUnitPriceFormProps = ActionDialogProps<TransportUnitPrice> & {
	isDuplicate?: boolean;
};

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

	const childList =
		row && (row as any).items && (row as any).items.length > 0
			? (row as any).items
			: row
				? [row]
				: [];

	const routeIds = Array.from(
		new Set(childList.map((i: any) => i.transportRouteId).filter(Boolean)),
	) as string[];

	const deptIds = Array.from(
		new Set(childList.map((i: any) => i.departmentId).filter(Boolean)),
	) as string[];

	const contractIds = Array.from(
		new Set(
			childList
				.map((i: any) => i.contractCodeId || i.equipmentId)
				.filter(Boolean),
		),
	) as string[];

	const qualities = Array.from(
		new Set(childList.map((i: any) => i.equipmentQuality).filter(Boolean)),
	) as string[];

	const routeDepts: Record<string, string[]> = {};
	childList.forEach((i: any) => {
		if (i.transportRouteId && i.departmentId) {
			if (!routeDepts[i.transportRouteId]) routeDepts[i.transportRouteId] = [];
			if (!routeDepts[i.transportRouteId].includes(i.departmentId)) {
				routeDepts[i.transportRouteId].push(i.departmentId);
			}
		}
	});

	const form = useForm<TransportUnitPriceSchema>({
		resolver: zodResolver(transportUnitPriceSchema) as any,
		mode: 'onSubmit',
		defaultValues: row
			? {
					transportMode: row.transportMode || 'conveyor',
					startMonth: row.startMonth,
					endMonth: row.endMonth,
					productionProcessId: row.productionProcessId || '',
					transportRouteIds:
						routeIds.length > 0
							? routeIds
							: row.transportRouteId
								? [row.transportRouteId]
								: [],
					departmentIds:
						deptIds.length > 0
							? deptIds
							: row.departmentId
								? [row.departmentId]
								: [],
					routeDepartmentIds: routeDepts,
					contractCodeIds:
						contractIds.length > 0
							? contractIds
							: row.contractCodeId
								? [row.contractCodeId]
								: row.equipmentId
									? [row.equipmentId]
									: [],
					equipmentQualities:
						qualities.length > 0
							? qualities
							: row.equipmentQuality
								? [row.equipmentQuality]
								: [],
					qualityPrices: {},
					materialFuelUnitPrice: row.materialFuelUnitPrice ?? null,
					powerUnitPrice: row.powerUnitPrice ?? null,
					maintenanceUnitPrice: row.maintenanceUnitPrice ?? null,
					quantity: row.quantity ?? null,
					unitOfMeasureId: row.unitOfMeasureId || '',
					isLowVolumeCase: row.isLowVolumeCase ?? false,
					items: childList.map((i: any) => ({
						id: i.id,
						title:
							i.transportRouteName ||
							i.contractCodeName ||
							i.equipmentName ||
							'Mục đơn giá',
						transportRouteId: i.transportRouteId || '',
						departmentId: i.departmentId || '',
						contractCodeId: i.contractCodeId || i.equipmentId || '',
						equipmentQuality: i.equipmentQuality || '',
						materialFuelUnitPrice: i.materialFuelUnitPrice ?? null,
						powerUnitPrice: i.powerUnitPrice ?? null,
						maintenanceUnitPrice: i.maintenanceUnitPrice ?? null,
						quantity: i.quantity ?? null,
						unitOfMeasureId: i.unitOfMeasureId || '',
						isLowVolumeCase: i.isLowVolumeCase ?? false,
					})),
				}
			: TRANSPORT_UNIT_PRICE_SCHEMA_DEFAULT,
	});

	const selectedProductionProcessId = useWatch({
		control: form.control,
		name: 'productionProcessId',
	});

	const transportMode = useWatch({
		control: form.control,
		name: 'transportMode',
	});

	const selectedRouteIds =
		useWatch({
			control: form.control,
			name: 'transportRouteIds',
		}) || [];

	const routeDepartmentIds =
		useWatch({
			control: form.control,
			name: 'routeDepartmentIds',
		}) || {};

	const selectedContractCodeIds =
		useWatch({
			control: form.control,
			name: 'contractCodeIds',
		}) || [];

	const selectedEquipmentQualities =
		useWatch({
			control: form.control,
			name: 'equipmentQualities',
		}) || [];

	const items =
		useWatch({
			control: form.control,
			name: 'items',
		}) || [];

	// Fetch Catalogs
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

					// Loại bỏ các công đoạn của Vận tải cơ giới (Giờ di chuyển, Giờ phục vụ...)
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
	}, []);

	// Tự động suy ra transportMode dựa trên Công đoạn sản xuất được chọn
	useEffect(() => {
		if (!selectedProductionProcessId) return;
		const selectedProc = productionProcesses.find(
			(p) => p.id === selectedProductionProcessId,
		);
		if (selectedProc) {
			const code = (selectedProc.code || '').toUpperCase();
			const procName = selectedProc.name.toLowerCase();
			let detectedMode:
				| 'conveyor'
				| 'shaft'
				| 'cable_winch'
				| 'monorail'
				| 'other' = 'conveyor';

			if (code === 'TKVVCVL' || procName.includes('trục kéo')) {
				detectedMode = 'cable_winch';
			} else if (
				code === 'VTT' ||
				(procName.includes('vận tải trục') && !procName.includes('trục kéo'))
			) {
				detectedMode = 'shaft';
			} else if (
				code.startsWith('MV') ||
				procName.includes('monoray') ||
				procName.includes('monorail')
			) {
				detectedMode = 'monorail';
			} else if (code === 'TBK' || procName.includes('thiết bị khác')) {
				detectedMode = 'other';
			} else {
				detectedMode = 'conveyor';
			}

			if (form.getValues('transportMode') !== detectedMode) {
				form.setValue('transportMode', detectedMode as any);
			}
		}
	}, [selectedProductionProcessId, productionProcesses]);

	// Reset form selections khi người dùng đổi Công đoạn sản xuất khác
	const prevProcessIdRef = useRef<string | undefined>(undefined);
	useEffect(() => {
		if (
			prevProcessIdRef.current !== undefined &&
			prevProcessIdRef.current !== selectedProductionProcessId
		) {
			form.setValue('transportRouteIds', []);
			form.setValue('departmentIds', []);
			form.setValue('routeDepartmentIds', {});
			form.setValue('contractCodeIds', []);
			form.setValue('equipmentQualities', []);
			form.setValue('items', []);
			form.setValue('qualityPrices', {});
		}
		prevProcessIdRef.current = selectedProductionProcessId;
	}, [selectedProductionProcessId, form]);

	// Tự động sinh danh sách hàng nhập đơn giá khi chọn từ FormMultiSelect
	useEffect(() => {
		if (row && !isDuplicate) return;

		let newItems: any[] = [];

		if (transportMode === 'conveyor' || transportMode === 'shaft') {
			if (selectedRouteIds.length > 0) {
				selectedRouteIds.forEach((routeId) => {
					const route = routes.find((r) => r.id === routeId);
					const isSpecialRoute = !!route?.isSpecialLowVolume;
					const deptsForRoute = routeDepartmentIds[routeId] || [];

					deptsForRoute.forEach((deptId) => {
						if (isSpecialRoute) {
							// Dòng 1: Sản lượng >= 10.000t/tháng (Trường hợp bình thường)
							const existingNormal = items.find(
								(it) =>
									it.transportRouteId === routeId &&
									it.departmentId === deptId &&
									!it.isLowVolumeCase,
							);
							newItems.push({
								transportRouteId: routeId,
								departmentId: deptId,
								title: route ? `${route.code} - ${route.name}` : routeId,
								materialFuelUnitPrice:
									existingNormal?.materialFuelUnitPrice ?? null,
								powerUnitPrice: existingNormal?.powerUnitPrice ?? null,
								maintenanceUnitPrice:
									existingNormal?.maintenanceUnitPrice ?? null,
								isLowVolumeCase: false,
							});

							// Dòng 2: Sản lượng < 10.000t/tháng (Trường hợp đặc biệt)
							const existingLow = items.find(
								(it) =>
									it.transportRouteId === routeId &&
									it.departmentId === deptId &&
									it.isLowVolumeCase,
							);
							newItems.push({
								transportRouteId: routeId,
								departmentId: deptId,
								title: route ? `${route.code} - ${route.name}` : routeId,
								materialFuelUnitPrice:
									existingLow?.materialFuelUnitPrice ?? null,
								powerUnitPrice: existingLow?.powerUnitPrice ?? null,
								maintenanceUnitPrice: existingLow?.maintenanceUnitPrice ?? null,
								isLowVolumeCase: true,
							});
						} else {
							const existing = items.find(
								(it) =>
									it.transportRouteId === routeId && it.departmentId === deptId,
							);
							newItems.push({
								transportRouteId: routeId,
								departmentId: deptId,
								title: route ? `${route.code} - ${route.name}` : routeId,
								materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
								powerUnitPrice: existing?.powerUnitPrice ?? null,
								maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
								isLowVolumeCase: false,
							});
						}
					});
				});
			}
		} else if (transportMode === 'cable_winch') {
			if (selectedRouteIds.length > 0) {
				selectedRouteIds.forEach((routeId) => {
					const route = routes.find((r) => r.id === routeId);
					const isLowVolume = !!route?.isSpecialLowVolume;
					const existing = items.find((it) => it.transportRouteId === routeId);

					newItems.push({
						transportRouteId: routeId,
						title: route ? `${route.code} - ${route.name}` : routeId,
						materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
						powerUnitPrice: isLowVolume
							? 0
							: (existing?.powerUnitPrice ?? null),
						maintenanceUnitPrice: isLowVolume
							? 115834567
							: (existing?.maintenanceUnitPrice ?? null),
						isLowVolumeCase: isLowVolume,
					});
				});
			}
		} else if (transportMode === 'monorail') {
			if (
				selectedContractCodeIds.length > 0 &&
				selectedEquipmentQualities.length > 0
			) {
				selectedContractCodeIds.forEach((ccId) => {
					const cc = contractCodes.find((c) => c.id === ccId);
					selectedEquipmentQualities.forEach((quality) => {
						const existing = items.find(
							(it) =>
								it.contractCodeId === ccId && it.equipmentQuality === quality,
						);
						newItems.push({
							contractCodeId: ccId,
							equipmentQuality: quality,
							title: cc ? `${cc.code} - ${cc.name}` : ccId,
							materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
							powerUnitPrice: existing?.powerUnitPrice ?? null,
							maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
						});
					});
				});
			}
		} else if (transportMode === 'other') {
			if (selectedContractCodeIds.length > 0) {
				selectedContractCodeIds.forEach((ccId) => {
					const cc = contractCodes.find((c) => c.id === ccId);
					const existing = items.find((it) => it.contractCodeId === ccId);
					newItems.push({
						contractCodeId: ccId,
						title: cc ? `${cc.code} - ${cc.name}` : ccId,
						materialFuelUnitPrice: existing?.materialFuelUnitPrice ?? null,
						powerUnitPrice: existing?.powerUnitPrice ?? null,
						maintenanceUnitPrice: existing?.maintenanceUnitPrice ?? null,
						quantity: existing?.quantity ?? null,
						unitOfMeasureId: existing?.unitOfMeasureId || '',
					});
				});
			}
		}

		form.setValue('items', newItems);
	}, [
		transportMode,
		JSON.stringify(selectedRouteIds),
		JSON.stringify(routeDepartmentIds),
		JSON.stringify(selectedContractCodeIds),
		JSON.stringify(selectedEquipmentQualities),
		routes,
		contractCodes,
	]);

	const handleRemoveItem = (index: number) => {
		const updated = items.filter((_, i) => i !== index);
		form.setValue('items', updated);
	};

	const formatDateOnly = (dateStr?: string) => {
		if (!dateStr) return '2026-01-01';
		const clean = dateStr.substring(0, 10);
		if (clean.length === 7) return `${clean}-01`;
		return clean;
	};

	const isGuid = (str?: string | null) =>
		!!str && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{8,12}$/i.test(str);

	const handleSubmit = async (values: TransportUnitPriceSchema) => {
		try {
			const formattedStartMonth = formatDateOnly(values.startMonth);
			const formattedEndMonth = formatDateOnly(values.endMonth);
			const procObj = productionProcesses.find(
				(p) => p.id === values.productionProcessId,
			);
			const productionProcessName = procObj?.name || row?.productionProcessName || '';
			const productionProcessCode = procObj?.code || row?.productionProcessCode || '';

			if (row?.id && !isDuplicate) {
				const itemsToUpdate =
					values.items && values.items.length > 0 ? values.items : [];

				for (const singleItem of itemsToUpdate) {
					const qualityVal =
						singleItem?.equipmentQuality ||
						values.equipmentQualities?.[0] ||
						values.equipmentQuality ||
						null;
					const adjustmentFactorDescriptionId = isGuid(qualityVal)
						? qualityVal
						: null;
					const equipmentQualityStr = !isGuid(qualityVal) ? qualityVal : null;
					const targetId = singleItem.id || row.id;

					await api.put(API.PRICING.TRANSPORT.UPDATE, {
						id: targetId,
						productionProcessId: values.productionProcessId,
						productionProcessName,
						productionProcessCode,
						transportRouteId:
							singleItem?.transportRouteId || values.transportRouteId || null,
						departmentId:
							singleItem?.departmentId ||
							values.departmentIds?.[0] ||
							values.departmentId ||
							null,
						equipmentId:
							singleItem?.contractCodeId ||
							values.contractCodeIds?.[0] ||
							values.materialId ||
							null,
						adjustmentFactorDescriptionId,
						equipmentQuality: equipmentQualityStr,
						materialFuelUnitPrice:
							singleItem?.materialFuelUnitPrice ??
							values.materialFuelUnitPrice ??
							null,
						powerUnitPrice:
							singleItem?.powerUnitPrice ?? values.powerUnitPrice ?? null,
						maintenanceUnitPrice:
							singleItem?.maintenanceUnitPrice ??
							values.maintenanceUnitPrice ??
							null,
						quantity: singleItem?.quantity ?? values.quantity ?? null,
						unitOfMeasureId:
							singleItem?.unitOfMeasureId || values.unitOfMeasureId || null,
						isLowVolumeCase:
							singleItem?.isLowVolumeCase ?? values.isLowVolumeCase ?? false,
						startMonth: formattedStartMonth,
						endMonth: formattedEndMonth,
					});
				}
			} else {
				const itemsToSubmit =
					values.items && values.items.length > 0 ? values.items : [];

				for (const it of itemsToSubmit) {
					const qualityVal = it.equipmentQuality || null;
					const adjustmentFactorDescriptionId = isGuid(qualityVal)
						? qualityVal
						: null;
					const equipmentQualityStr = !isGuid(qualityVal) ? qualityVal : null;

					await api.post(API.PRICING.TRANSPORT.CREATE, {
						productionProcessId: values.productionProcessId,
						productionProcessName,
						productionProcessCode,
						transportRouteId:
							it.transportRouteId || values.transportRouteId || null,
						departmentId: it.departmentId || values.departmentId || null,
						equipmentId:
							it.contractCodeId ||
							values.contractCodeId ||
							values.materialId ||
							null,
						adjustmentFactorDescriptionId,
						equipmentQuality: equipmentQualityStr,
						materialFuelUnitPrice:
							it.materialFuelUnitPrice ?? values.materialFuelUnitPrice ?? null,
						powerUnitPrice: it.powerUnitPrice ?? values.powerUnitPrice ?? null,
						maintenanceUnitPrice:
							it.maintenanceUnitPrice ?? values.maintenanceUnitPrice ?? null,
						quantity: it.quantity ?? values.quantity ?? null,
						unitOfMeasureId:
							it.unitOfMeasureId || values.unitOfMeasureId || null,
						isLowVolumeCase:
							it.isLowVolumeCase ?? values.isLowVolumeCase ?? false,
						startMonth: formattedStartMonth,
						endMonth: formattedEndMonth,
					});
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
		<FormProvider context={form as any} onSubmit={handleSubmit as any}>
			{/* 1. Ngày tháng lên trên đầu */}
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu (Tháng/Năm)'
					className='flex-1'
				/>

				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc (Tháng/Năm)'
					className='flex-1'
				/>
			</FormRow>

			{/* 2. Công đoạn sản xuất */}
			<FormComboBox
				control={form.control}
				name='productionProcessId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={productionProcesses.map((p) => ({
					value: p.id,
					label: p.code ? `${p.code} - ${p.name}` : p.name,
				}))}
			/>

			{/* 3. FormMultiSelect chọn thông tin */}
			{(transportMode === 'conveyor' ||
				transportMode === 'shaft' ||
				transportMode === 'cable_winch') && (
				<FormMultiSelect
					control={form.control}
					name='transportRouteIds'
					label='Tuyến vận tải'
					placeholder='Chọn Tuyến vận tải'
					options={routes.map((r) => ({
						value: r.id,
						label: `${r.code} - ${r.name}`,
					}))}
				/>
			)}

			{transportMode === 'monorail' && (
				<>
					<FormMultiSelect
						control={form.control}
						name='contractCodeIds'
						label='Nhóm vật tư, tài sản'
						placeholder='Chọn Nhóm vật tư, tài sản'
						options={contractCodes.map((c) => ({
							value: c.id,
							label: `${c.code} - ${c.name}`,
						}))}
					/>

					<FormMultiSelect
						control={form.control}
						name='equipmentQualities'
						label='Chất lượng thiết bị'
						placeholder='Chọn Chất lượng thiết bị'
						options={[
							{ value: 'A', label: 'Thiết bị loại A' },
							{ value: 'B', label: 'Thiết bị loại B' },
							{ value: 'C', label: 'Thiết bị loại C' },
						]}
					/>
				</>
			)}

			{transportMode === 'other' && (
				<FormMultiSelect
					control={form.control}
					name='contractCodeIds'
					label='Nhóm vật tư, tài sản'
					placeholder='Chọn Nhóm vật tư, tài sản'
					options={contractCodes.map((c) => ({
						value: c.id,
						label: `${c.code} - ${c.name}`,
					}))}
				/>
			)}

			{/* 4. BẢNG NHẬP ĐƠN GIÁ */}
			{/* Conveyor & Shaft Mode: Mỗi Tuyến vận tải bọc riêng 1 block với Multiselect Đơn vị và Bảng giá theo Đơn vị */}
			{(transportMode === 'conveyor' || transportMode === 'shaft') &&
				selectedProductionProcessId &&
				selectedRouteIds.length > 0 && (
					<>
						<FormSeparator />
						<div className='space-y-4'>
							<div className='flex items-center justify-between'>
								<div className='text-xs font-semibold text-gray-500 uppercase'>
									Danh sách bóc tách đơn giá các tuyến đã chọn (
									{selectedRouteIds.length})
								</div>
							</div>

							{selectedRouteIds.map((routeId) => {
								const route = routes.find((r) => r.id === routeId);
								const deptsForRoute = routeDepartmentIds[routeId] || [];
								const isSpecialRoute = !!route?.isSpecialLowVolume;

								return (
									<div
										key={routeId}
										className='space-y-3 rounded-lg border border-gray-200 bg-white p-4 shadow-xs'
									>
										<div className='flex items-center justify-between text-sm font-semibold text-gray-800'>
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

										<FormMultiSelect
											control={form.control}
											name={`routeDepartmentIds.${routeId}` as any}
											label='Đơn vị áp dụng cho tuyến này'
											placeholder='Chọn Đơn vị'
											options={departments.map((d) => ({
												value: d.id,
												label: `${d.code} - ${d.name}`,
											}))}
										/>

										{deptsForRoute.length > 0 && (
											<div className='mt-2 overflow-hidden rounded-md border border-gray-200 bg-white'>
												<table className='w-full text-left text-sm'>
													<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
														<tr>
															<th className='min-w-[200px] px-4 py-3'>
																Đơn vị
															</th>
															<th className='min-w-[180px] px-4 py-3'>
																{transportMode === 'conveyor'
																	? 'Đơn giá VL, NL (đ/tấn)'
																	: 'Đơn giá VL, NL (đ/xe)'}
															</th>
															<th className='min-w-[180px] px-4 py-3'>
																{transportMode === 'conveyor'
																	? 'Đơn giá Động lực (đ/tấn)'
																	: 'Đơn giá Động lực (đ/xe)'}
															</th>
															<th className='min-w-[180px] px-4 py-3'>
																{transportMode === 'conveyor'
																	? 'Đơn giá SCTX (đ/tấn)'
																	: 'Đơn giá SCTX (đ/xe)'}
															</th>
														</tr>
													</thead>
													<tbody className='divide-y divide-gray-100'>
														{items
															.map((it, idx) => ({ ...it, originalIndex: idx }))
															.filter(
																(it) =>
																	it.transportRouteId === routeId &&
																	!!it.departmentId &&
																	deptsForRoute.includes(it.departmentId),
															)
															.map((item) => {
																const dept = departments.find(
																	(d) => d.id === item.departmentId,
																);
																const isLowVolume = !!item.isLowVolumeCase;
																const deptName = dept
																	? `${dept.code} - ${dept.name}`
																	: item.departmentId;
																const rowLabel = isLowVolume
																	? `${deptName} (Sản lượng < 10,000t/tháng)`
																	: isSpecialRoute
																		? `${deptName} (Sản lượng ≥ 10,000t/tháng)`
																		: deptName;

																return (
																	<tr
																		key={`${item.departmentId}_${item.isLowVolumeCase ? 'low' : 'normal'}`}
																		className='hover:bg-gray-50/50'
																	>
																		<td className='px-4 py-3 font-semibold whitespace-nowrap text-gray-800'>
																			<span>{rowLabel}</span>
																		</td>
																		<td className='px-4 py-2'>
																			<FormNumber
																				control={form.control}
																				name={
																					`items.${item.originalIndex}.materialFuelUnitPrice` as any
																				}
																				placeholder='Nhập đơn giá'
																			/>
																		</td>
																		<td className='px-4 py-2'>
																			<FormNumber
																				control={form.control}
																				name={
																					`items.${item.originalIndex}.powerUnitPrice` as any
																				}
																				placeholder='Nhập đơn giá'
																			/>
																		</td>
																		<td className='px-4 py-2'>
																			<FormNumber
																				control={form.control}
																				name={
																					`items.${item.originalIndex}.maintenanceUnitPrice` as any
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
					</>
				)}

			{/* Monorail Mode: Mỗi Nhóm vật tư, tài sản bọc 1 khối Chất lượng thiết bị tương ứng */}
			{transportMode === 'monorail' &&
				selectedProductionProcessId &&
				selectedContractCodeIds.length > 0 &&
				selectedEquipmentQualities.length > 0 && (
					<>
						<FormSeparator />
						<div className='space-y-4'>
							<div className='flex items-center justify-between'>
								<div className='text-xs font-semibold text-gray-500 uppercase'>
									Danh sách các mục đã chọn ( {selectedContractCodeIds.length}{' '}
									nhóm vật tư )
								</div>
							</div>

							{selectedContractCodeIds.map((ccId) => {
								const cc = contractCodes.find((c) => c.id === ccId);
								return (
									<div
										key={ccId}
										className='space-y-3 rounded-lg border border-gray-200 bg-white p-4 shadow-xs'
									>
										<div className='text-sm font-semibold text-gray-800'>
											{cc ? `${cc.code} - ${cc.name}` : ccId}
										</div>

										<div className='overflow-hidden rounded-md border border-gray-200 bg-white'>
											<table className='w-full text-left text-sm'>
												<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold text-gray-600 uppercase'>
													<tr>
														<th className='min-w-[180px] px-4 py-3'>
															Chất lượng thiết bị
														</th>
														<th className='min-w-[200px] px-4 py-3'>
															Đơn giá vật liệu, nhiên liệu (đ/h)
														</th>
														<th className='min-w-[180px] px-4 py-3'>
															Đơn giá động lực (đ/h)
														</th>
														<th className='min-w-[180px] px-4 py-3'>
															Đơn giá SCTX (đ/h)
														</th>
													</tr>
												</thead>
												<tbody className='divide-y divide-gray-100'>
													{selectedEquipmentQualities.map((quality) => {
														const itemIndex = items.findIndex(
															(it) =>
																it.contractCodeId === ccId &&
																it.equipmentQuality === quality,
														);
														if (itemIndex === -1) return null;

														return (
															<tr key={quality} className='hover:bg-gray-50/50'>
																<td className='px-4 py-3 font-semibold whitespace-nowrap text-gray-800'>
																	Thiết bị loại {quality}
																</td>
																<td className='px-4 py-2'>
																	<FormNumber
																		control={form.control}
																		name={
																			`items.${itemIndex}.materialFuelUnitPrice` as any
																		}
																		placeholder='Nhập đơn giá'
																	/>
																</td>
																<td className='px-4 py-2'>
																	<FormNumber
																		control={form.control}
																		name={
																			`items.${itemIndex}.powerUnitPrice` as any
																		}
																		placeholder='Nhập đơn giá'
																	/>
																</td>
																<td className='px-4 py-2'>
																	<FormNumber
																		control={form.control}
																		name={
																			`items.${itemIndex}.maintenanceUnitPrice` as any
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
								);
							})}
						</div>
					</>
				)}

			{/* Các mode khác (cable_winch, other): Hiển thị danh sách các mục đã chọn */}
			{(transportMode === 'cable_winch' || transportMode === 'other') &&
				selectedProductionProcessId &&
				items.length > 0 && (
					<>
						<FormSeparator />
						<div className='space-y-3'>
							<div className='flex items-center justify-between'>
								<div className='text-xs font-semibold text-gray-500 uppercase'>
									Danh sách các mục đã chọn ({items.length})
								</div>
							</div>

							<div className='flex w-full flex-col gap-3 pt-1 pb-3'>
								{items.map((item, index) => (
									<div
										key={index}
										className='flex w-full items-end gap-3 rounded-md border bg-[#fafafa] p-3 shadow-xs'
									>
										{/* Tên mục / tuyến đã chọn */}
										<div className='flex min-w-[200px] flex-[1.5] flex-col gap-1.5'>
											<Label className='truncate text-xs font-medium text-gray-600'>
												Mục {index + 1}
											</Label>
											<div
												className='flex h-9 items-center truncate rounded-md border bg-white px-3 py-1 text-sm font-semibold text-gray-800 shadow-xs'
												title={item.title}
											>
												{item.title}
											</div>
										</div>

										{/* Nếu là thiết bị khác -> Nhập Định mức & Đơn vị tính */}
										{transportMode === 'other' && (
											<>
												<div className='min-w-[110px] flex-1'>
													<FormNumber
														control={form.control}
														name={`items.${index}.quantity` as any}
														label='Định mức'
														placeholder='Nhập định mức'
													/>
												</div>

												<div className='min-w-[120px] flex-1'>
													<FormComboBox
														control={form.control}
														name={`items.${index}.unitOfMeasureId` as any}
														label='Đơn vị tính'
														placeholder='Chọn đơn vị tính'
														options={units.map((unit) => ({
															value: unit.id,
															label: unit.name,
														}))}
													/>
												</div>
											</>
										)}

										{/* Các ô nhập Đơn giá thành phần trải hàng ngang */}
										<div className='min-w-[140px] flex-1'>
											<FormNumber
												control={form.control}
												name={`items.${index}.materialFuelUnitPrice` as any}
												label='Vật liệu, nhiên liệu (đ/tháng)'
												placeholder='Nhập đơn giá'
											/>
										</div>

										<div className='min-w-[130px] flex-1'>
											<FormNumber
												control={form.control}
												name={`items.${index}.powerUnitPrice` as any}
												disabled={item.isLowVolumeCase}
												label='Động lực (đ/tháng)'
												placeholder={
													item.isLowVolumeCase ? '0 đ/tháng' : 'Nhập đơn giá'
												}
											/>
										</div>

										<div className='min-w-[130px] flex-1'>
											<FormNumber
												control={form.control}
												name={`items.${index}.maintenanceUnitPrice` as any}
												disabled={item.isLowVolumeCase}
												label='SCTX (đ/tháng)'
												placeholder={
													item.isLowVolumeCase
														? '115.834.567 đ/tháng'
														: 'Nhập đơn giá'
												}
											/>
										</div>

										{/* Tag sản lượng thấp nếu có */}
										{item.isLowVolumeCase && (
											<div className='mb-0.5 flex h-9 shrink-0 items-center'>
												<span className='rounded-full bg-amber-100 px-2 py-1 text-xs font-semibold whitespace-nowrap text-amber-800'>
													⚡ Sản lượng &lt; 10,000t/tháng
												</span>
											</div>
										)}

										{/* Nút xóa item */}
										<div className='mb-0.5 flex h-9 shrink-0 items-center'>
											<Button
												type='button'
												variant='ghost'
												size='icon'
												className='text-red-500 hover:bg-red-50 hover:text-red-600'
												onClick={() => handleRemoveItem(index)}
												title='Xóa mục này'
											>
												<XCircleIcon className='size-5' />
											</Button>
										</div>
									</div>
								))}
							</div>
						</div>
					</>
				)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
