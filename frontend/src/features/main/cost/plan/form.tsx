import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSelect } from '@/components/form/form-select';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Department } from '@/features/main/catalog/department/columns';
import type { Product } from '@/features/main/catalog/product/columns';
import {
	normalizeProcessGroup,
	type ProcessGroup,
} from '@/features/main/catalog/process/group/columns';
import type { TransportRoute } from '@/features/main/catalog/transport-route/columns';
import type { Unit } from '@/features/main/catalog/unit/columns';
import type { DepartmentPlanGroup } from '@/features/main/cost/plan/columns';
import {
	DEPARTMENT_PLAN_FORM_DEFAULT,
	type DepartmentPlanFormSchema,
	departmentPlanFormSchema,
} from '@/features/main/cost/plan/schema';
import type { AdjustmentDetail } from '@/features/main/cost/plan/planed-maintain-cost/types';
import {
	type DepartmentPlannedDetail,
	mapDepartmentPlannedDetail,
} from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, TriangleAlertIcon } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';

type PlanFormProps = ActionDialogProps<DepartmentPlanGroup> & {
	onSuccess?: () => void;
};

import {
	MonthSection,
	formatMonthLabel,
	getInvalidSelectedProduct,
} from './khai-thac/form-section';
import {
	ProductionProcessOption,
	TransportMonthSection,
} from './van-tai-lo/form-section';
import { buildTransportMonthsPayload } from './van-tai-lo/utils';
import { MotorizedMonthSection } from './van-tai-co-gioi/form-section';
import { buildMotorizedTransportMonthsPayload } from './van-tai-co-gioi/utils';

const getPersistedItemKey = (
	item: DepartmentPlanFormSchema['months'][number]['items'][number],
) => item.outputId || item.productUnitPriceId || '';

export function PlanForm({ data, row, onSuccess }: PlanFormProps) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [products, setProducts] = useState<Product[]>([]);
	const [units, setUnits] = useState<Unit[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [akProcessGroupIds, setAkProcessGroupIds] = useState<Set<string>>(
		new Set(),
	);
	const [legacyProductIds, setLegacyProductIds] = useState<Map<string, string>>(
		new Map(),
	);
	const [transportRoutes, setTransportRoutes] = useState<TransportRoute[]>([]);
	const [contractCodes, setContractCodes] = useState<
		{ id: string; code: string; name: string; unitOfMeasureId?: string }[]
	>([]);
	const [productionProcesses, setProductionProcesses] = useState<
		ProductionProcessOption[]
	>([]);
	const [vtcgProductionProcesses, setVtcgProductionProcesses] = useState<
		ProductionProcessOption[]
	>([]);
	const [distances, setDistances] = useState<any[]>([]);
	const [cargoTypes, setCargoTypes] = useState<any[]>([]);
	const [locations, setLocations] = useState<any[]>([]);
	const [adjustments, setAdjustments] = useState<AdjustmentDetail[]>([]);
	// K1 (Hệ số chất lượng thiết bị) và K2 (Hệ số điều kiện môi trường), lấy từ Danh mục Hệ số điều chỉnh
	const [k1Adjustment, setK1Adjustment] = useState<AdjustmentDetail | null>(
		null,
	);
	const [k2Adjustment, setK2Adjustment] = useState<AdjustmentDetail | null>(
		null,
	);
	const isEdit = !!row;

	const form = useForm<DepartmentPlanFormSchema>({
		resolver: zodResolver(departmentPlanFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: DEPARTMENT_PLAN_FORM_DEFAULT,
	});

	const {
		fields: monthFields,
		append,
		remove,
	} = useFieldArray({
		control: form.control,
		name: 'months',
	});

	const {
		fields: transportMonthFields,
		append: appendTransportMonth,
		remove: removeTransportMonth,
	} = useFieldArray({
		control: form.control,
		name: 'transportMonths',
	});

	const {
		fields: motorizedMonthFields,
		append: appendMotorizedMonth,
		remove: removeMotorizedMonth,
	} = useFieldArray({
		control: form.control,
		name: 'motorizedMonths',
	});

	const watchedMonths = useWatch({
		control: form.control,
		name: 'months',
		defaultValue: DEPARTMENT_PLAN_FORM_DEFAULT.months,
	}) as DepartmentPlanFormSchema['months'];

	const watchedPlanMode = useWatch({
		control: form.control,
		name: 'planMode',
	});

	const targetDeptId = (row as any)?.departmentId || row?.id;
	const prevPlanModeRef = useRef<string | undefined>(undefined);

	const loadPlanDataByMode = async (mode: string, deptId: string) => {
		if (!deptId) return;

		if (mode === 'vantaicogioi') {
			try {
				const detailRes = await api.get<{
					departmentId: string;
					months: {
						month: string;
						lowValuePerishableSupply?: boolean;
						items: {
							id: string;
							productionProcessId?: string;
							productionProcessCode?: string;
							productionProcessName?: string;
							equipmentId?: string;
							equipmentQuality?: string;
							haulDistanceId?: string;
							haulDistanceValue?: string;
							productionMeters?: number;
							unitOfMeasureId?: string;
						}[];
					}[];
				}>(
					API.COST.TRANSPORT_PLAN_LINE.DETAIL_PLANNED_BY_DEPARTMENT(deptId),
				);
				const detail = detailRes.result;

				const months = (detail.months || [])
					.map((m) => {
						const vtcgItems = (m.items || []).filter(
							(it: any) =>
								it.haulDistanceId ||
								it.haulDistanceValue ||
								it.processGroupCode === 'VTCG' ||
								(it.processGroupName || '').toLowerCase().includes('cơ giới') ||
								(it.productionProcessName || '').toLowerCase().includes('cơ giới') ||
								!it.transportRouteId,
						);
						if (!vtcgItems.length) return null;

						const items = vtcgItems.map((it: any) => ({
							id: it.id,
							equipmentId: it.equipmentId,
							equipmentCode: it.equipmentCode,
							equipmentName: it.equipmentName,
							equipmentQuality: it.equipmentQuality,
							productionProcessId: it.productionProcessId,
							productionProcessCode: it.productionProcessCode,
							productionProcessName: it.productionProcessName,
							haulDistanceId: it.haulDistanceId,
							haulDistanceValue: it.haulDistanceValue,
							cargoTypeId: it.cargoTypeId,
							cargoTypeName: it.cargoTypeName,
							receivingLocationId: it.receivingLocationId,
							receivingLocationName: it.receivingLocationName,
							dumpingLocationId: it.dumpingLocationId,
							dumpingLocationName: it.dumpingLocationName,
							productionMeters: it.productionMeters,
							fuelAdjustmentFactor: it.fuelAdjustmentFactor ?? 1.0,
							unitOfMeasureId: it.unitOfMeasureId,
							unitName: it.unitOfMeasureName,
						}));

						const eqIds = [
							...new Set(items.map((it) => it.equipmentId).filter(Boolean)),
						] as string[];

						const eqProcessesMap: Record<string, string[]> = {};
						const eqQualitiesMap: Record<string, string[]> = {};
						const eqDistancesMap: Record<string, string[]> = {};
						const procCargoTypesMap: Record<string, string[]> = {};
						const procPickupLocationsMap: Record<string, string[]> = {};
						const procDropoffLocationsMap: Record<string, string[]> = {};

						items.forEach((it) => {
							if (it.equipmentId) {
								// 1. Equipment -> Processes
								if (it.productionProcessId) {
									if (!eqProcessesMap[it.equipmentId]) {
										eqProcessesMap[it.equipmentId] = [];
									}
									if (!eqProcessesMap[it.equipmentId].includes(it.productionProcessId)) {
										eqProcessesMap[it.equipmentId].push(it.productionProcessId);
									}
								}

								// 2. Equipment / Scope -> Qualities
								if (it.equipmentQuality) {
									if (!eqQualitiesMap[it.equipmentId]) {
										eqQualitiesMap[it.equipmentId] = [];
									}
									if (!eqQualitiesMap[it.equipmentId].includes(it.equipmentQuality)) {
										eqQualitiesMap[it.equipmentId].push(it.equipmentQuality);
									}

									if (it.productionProcessId) {
										const scopeKey = `${it.equipmentId}_${it.productionProcessId}`;
										if (!eqQualitiesMap[scopeKey]) {
											eqQualitiesMap[scopeKey] = [];
										}
										if (!eqQualitiesMap[scopeKey].includes(it.equipmentQuality)) {
											eqQualitiesMap[scopeKey].push(it.equipmentQuality);
										}
									}
								}

								// 3. Equipment / Scope -> Distances
								if (it.haulDistanceId) {
									if (!eqDistancesMap[it.equipmentId]) {
										eqDistancesMap[it.equipmentId] = [];
									}
									if (!eqDistancesMap[it.equipmentId].includes(it.haulDistanceId)) {
										eqDistancesMap[it.equipmentId].push(it.haulDistanceId);
									}

									if (it.productionProcessId) {
										const scopeKey = `${it.equipmentId}_${it.productionProcessId}`;
										if (!eqDistancesMap[scopeKey]) {
											eqDistancesMap[scopeKey] = [];
										}
										if (!eqDistancesMap[scopeKey].includes(it.haulDistanceId)) {
											eqDistancesMap[scopeKey].push(it.haulDistanceId);
										}
									}
								}
							}

							// 4. Process & Scope -> CargoTypes, Locations
							if (it.productionProcessId) {
								const scopeKey = it.equipmentId
									? `${it.equipmentId}_${it.productionProcessId}`
									: undefined;

								if (it.cargoTypeId) {
									if (!procCargoTypesMap[it.productionProcessId]) {
										procCargoTypesMap[it.productionProcessId] = [];
									}
									if (!procCargoTypesMap[it.productionProcessId].includes(it.cargoTypeId)) {
										procCargoTypesMap[it.productionProcessId].push(it.cargoTypeId);
									}
									if (scopeKey) {
										if (!procCargoTypesMap[scopeKey]) {
											procCargoTypesMap[scopeKey] = [];
										}
										if (!procCargoTypesMap[scopeKey].includes(it.cargoTypeId)) {
											procCargoTypesMap[scopeKey].push(it.cargoTypeId);
										}
									}
								}
								if (it.receivingLocationId) {
									if (!procPickupLocationsMap[it.productionProcessId]) {
										procPickupLocationsMap[it.productionProcessId] = [];
									}
									if (!procPickupLocationsMap[it.productionProcessId].includes(it.receivingLocationId)) {
										procPickupLocationsMap[it.productionProcessId].push(it.receivingLocationId);
									}
									if (scopeKey) {
										if (!procPickupLocationsMap[scopeKey]) {
											procPickupLocationsMap[scopeKey] = [];
										}
										if (!procPickupLocationsMap[scopeKey].includes(it.receivingLocationId)) {
											procPickupLocationsMap[scopeKey].push(it.receivingLocationId);
										}
									}
								}
								if (it.dumpingLocationId) {
									if (!procDropoffLocationsMap[it.productionProcessId]) {
										procDropoffLocationsMap[it.productionProcessId] = [];
									}
									if (!procDropoffLocationsMap[it.productionProcessId].includes(it.dumpingLocationId)) {
										procDropoffLocationsMap[it.productionProcessId].push(it.dumpingLocationId);
									}
									if (scopeKey) {
										if (!procDropoffLocationsMap[scopeKey]) {
											procDropoffLocationsMap[scopeKey] = [];
										}
										if (!procDropoffLocationsMap[scopeKey].includes(it.dumpingLocationId)) {
											procDropoffLocationsMap[scopeKey].push(it.dumpingLocationId);
										}
									}
								}
							}
						});

						return {
							month: m.month.substring(0, 10),
							lowValuePerishableSupply: m.lowValuePerishableSupply ?? false,
							motorizedCategory: 'scania' as const,
							assignmentCodeIds: eqIds,
							equipmentQualities: eqQualitiesMap,
							equipmentProcesses: eqProcessesMap,
							equipmentDistances: eqDistancesMap,
							processCargoTypes: procCargoTypesMap,
							processPickupLocations: procPickupLocationsMap,
							processDropoffLocations: procDropoffLocationsMap,
							items,
						};
					})
					.filter((m): m is NonNullable<typeof m> => m !== null);

				form.setValue('months', []);
				form.setValue('transportMonths', []);
				form.setValue('motorizedMonths', months);
			} catch {
				form.setValue('months', []);
				form.setValue('transportMonths', []);
				form.setValue('motorizedMonths', []);
			}
		} else if (mode === 'vantailo') {
			try {
				const detailRes = await api.get<{
					departmentId: string;
					months: {
						month: string;
						lowValuePerishableSupply?: boolean;
						items: {
							id: string;
							productionProcessId?: string;
							transportRouteId?: string;
							routeDepartmentId?: string;
							equipmentId?: string;
							equipmentQuality?: string;
							productionMeters?: number;
							unitOfMeasureId?: string;
							k1?: any;
							k2?: any;
						}[];
					}[];
				}>(
					API.COST.TRANSPORT_PLAN_LINE.DETAIL_PLANNED_BY_DEPARTMENT(deptId),
				);
				const detail = detailRes.result;

				const months = (detail.months || [])
					.map((m) => {
						const vtlItems = (m.items || []).filter(
							(it: any) =>
								it.transportRouteId ||
								(!it.haulDistanceId && !it.haulDistanceValue && !it.processGroupCode?.includes('VTCG')),
						);
						if (!vtlItems.length) return null;

						const processIds = [
							...new Set(
								vtlItems.map((it) => it.productionProcessId).filter(Boolean),
							),
						] as string[];

						const processes = processIds.map((pId) => {
							const pItems = vtlItems.filter(
								(it) => it.productionProcessId === pId,
							);

							const routeDepartmentIdsMap: Record<string, string[]> = {};
							pItems.forEach((it) => {
								if (it.transportRouteId && it.routeDepartmentId) {
									if (!routeDepartmentIdsMap[it.transportRouteId]) {
										routeDepartmentIdsMap[it.transportRouteId] = [];
									}
									if (
										!routeDepartmentIdsMap[it.transportRouteId].includes(
											it.routeDepartmentId,
										)
									) {
										routeDepartmentIdsMap[it.transportRouteId].push(
											it.routeDepartmentId,
										);
									}
								}
							});

							const contractCodeQualityIdsMap: Record<string, string[]> = {};
							pItems.forEach((it) => {
								if (it.equipmentId && it.equipmentQuality) {
									if (!contractCodeQualityIdsMap[it.equipmentId]) {
										contractCodeQualityIdsMap[it.equipmentId] = [];
									}
									if (
										!contractCodeQualityIdsMap[it.equipmentId].includes(
											it.equipmentQuality,
										)
									) {
										contractCodeQualityIdsMap[it.equipmentId].push(
											it.equipmentQuality,
										);
									}
								}
							});

							return {
								productionProcessId: pId,
								routeIds: [
									...new Set(
										pItems.map((it) => it.transportRouteId).filter(Boolean),
									),
								] as string[],
								routeDepartmentIds: routeDepartmentIdsMap,
								contractCodeIds: [
									...new Set(
										pItems.map((it) => it.equipmentId).filter(Boolean),
									),
								] as string[],
								contractCodeQualityIds: contractCodeQualityIdsMap,
								items: pItems.map((it: any) => ({
									id: it.id,
									transportRouteId: it.transportRouteId,
									departmentId: it.routeDepartmentId,
									contractCodeId: it.equipmentId,
									equipmentQuality: it.equipmentQuality,
									productionMeters: it.productionMeters,
									unitOfMeasureId: it.unitOfMeasureId,
									k1Factor: it.k1
										? {
												adjustmentFactorId: it.k1.adjustmentFactorId,
												adjustmentFactorDescriptionId:
													it.k1.adjustmentFactorDescriptionId,
												customValue: it.k1.customValue,
											}
										: undefined,
									k2Factor: it.k2
										? {
												adjustmentFactorId: it.k2.adjustmentFactorId,
												adjustmentFactorDescriptionId:
													it.k2.adjustmentFactorDescriptionId,
												customValue: it.k2.customValue,
											}
										: undefined,
								})),
							};
						});

						return {
							month: m.month.substring(0, 10),
							lowValuePerishableSupply: m.lowValuePerishableSupply ?? false,
							processIds,
							processes,
						};
					})
					.filter((m): m is NonNullable<typeof m> => m !== null);

				form.setValue('months', []);
				form.setValue('transportMonths', months);
				form.setValue('motorizedMonths', []);
			} catch {
				form.setValue('months', []);
				form.setValue('transportMonths', []);
				form.setValue('motorizedMonths', []);
			}
		} else {
			try {
				const detail = await api.get<DepartmentPlannedDetail>(
					API.COST.PRODUCT.DETAIL_PLANNED_BY_DEPARTMENT(deptId),
				);
				const mappedDetail = mapDepartmentPlannedDetail(detail.result);

				form.setValue(
					'months',
					mappedDetail.months.map((month) => ({
						month: month.month.substring(0, 10),
						items: month.items.map((item) => ({
							productUnitPriceId: item.productUnitPriceId,
							outputId: item.outputId,
							productId: item.productId,
							unitOfMeasureId: item.unitOfMeasureId,
							productionMeters: item.productionMeters,
							planAshContent: item.planAshContent ?? 0,
						})),
					})),
				);
				form.setValue('transportMonths', []);
				form.setValue('motorizedMonths', []);

				setLegacyProductIds(
					new Map(
						mappedDetail.months.flatMap((month) =>
							month.items.flatMap((item) => {
								const itemKey = item.outputId || item.productUnitPriceId;
								if (!itemKey) return [];
								return [[itemKey, item.productId] as const];
							}),
						),
					),
				);
			} catch {
				form.setValue('months', []);
				form.setValue('transportMonths', []);
				form.setValue('motorizedMonths', []);
			}
		}
	};

	// Làm mới dữ liệu Vận tải lò / Vận tải cơ giới / Khai thác khi đổi chế độ kế hoạch
	useEffect(() => {
		if (
			prevPlanModeRef.current !== undefined &&
			prevPlanModeRef.current !== watchedPlanMode
		) {
			if (isEdit && targetDeptId) {
				loadPlanDataByMode(watchedPlanMode, targetDeptId);
			} else {
				if (watchedPlanMode === 'vantailo') {
					form.setValue('months', []);
					form.setValue('motorizedMonths', []);
				} else if (watchedPlanMode === 'vantaicogioi') {
					form.setValue('months', []);
					form.setValue('transportMonths', []);
				} else {
					form.setValue('transportMonths', []);
					form.setValue('motorizedMonths', []);
				}
			}
		}
		prevPlanModeRef.current = watchedPlanMode;
	}, [watchedPlanMode, form, isEdit, targetDeptId]);

	const invalidLegacySelections = useMemo(() => {
		if (!isEdit) return [];

		return watchedMonths.flatMap((month, monthIndex) =>
			month.items.flatMap((item, itemIndex) => {
				const legacyProductId = legacyProductIds.get(getPersistedItemKey(item));
				if (!legacyProductId || legacyProductId !== item.productId) {
					return [];
				}

				const product = getInvalidSelectedProduct(
					products,
					item.productId,
					month.month,
				);
				if (!product) return [];

				return [
					{
						monthIndex,
						itemIndex,
						month: month.month,
						product,
					},
				];
			}),
		);
	}, [isEdit, legacyProductIds, products, watchedMonths]);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Product>(API.CATALOG.PRODUCT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Unit>(API.CATALOG.UNIT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<{ processGroupId: string }>(
				API.CATALOG.AK_FACTOR_CONFIG.LIST,
				{
					ignorePagination: true,
				},
			),
			// Transport data
			api.pagging<TransportRoute>(API.CATALOG.TRANSPORT_ROUTE.LIST, {
				ignorePagination: true,
			}),
			api.pagging<{
				id: string;
				code: string;
				name: string;
				unitOfMeasureId?: string;
			}>(API.CATALOG.CONTRACT_CODE.LIST, { ignorePagination: true }),
			api.pagging<ProductionProcessOption>(API.CATALOG.PROCESS.STEP.LIST, {
				ignorePagination: true,
			}),
			api
				.get<AdjustmentDetail[]>(API.CATALOG.ADJUSTMENT.FACTOR.DETAILS)
				.catch(() => ({
					result: [] as AdjustmentDetail[],
				})),
		]);

		promises.then(
			async ([
				productsRes,
				unitsRes,
				departmentsRes,
				akConfigs,
				routesRes,
				contractsRes,
				processesRes,
				adjustmentsRes,
			]) => {
				setProducts(productsRes.result.data ?? []);
				setUnits(unitsRes.result.data ?? []);
				setDepartments(departmentsRes.result.data ?? []);
				setAkProcessGroupIds(
					new Set(
						(akConfigs.result.data || [])
							.map((item) => item.processGroupId)
							.filter((id) => !!id),
					),
				);
				setTransportRoutes(routesRes.result.data ?? []);
				setContractCodes(contractsRes.result.data ?? []);
				setAdjustments((adjustmentsRes as any)?.result || []);

				// Lọc các Công đoạn sản xuất thuộc Vận tải lò và Vận tải cơ giới
				const allProcesses = processesRes.result.data || [];
				const vtlProcesses = allProcesses.filter((p) => {
					const group = (p.processGroupName || '').toLowerCase();
					const name = (p.name || '').toLowerCase();
					const code = (p.code || '').toUpperCase();

					if (
						group.includes('cơ giới') ||
						group.includes('vtcg') ||
						code.startsWith('VC_') ||
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

				const vtcgProcesses = allProcesses.filter((p) => {
					const group = (p.processGroupName || '').toLowerCase();
					const name = (p.name || '').toLowerCase();
					const code = (p.code || '').toUpperCase();

					return (
						group.includes('cơ giới') ||
						group.includes('vtcg') ||
						code.startsWith('VC_') ||
						code === 'GDC' ||
						code === 'GPV' ||
						code === 'GGDC' ||
						code === 'GGPV' ||
						name.startsWith('giờ phục vụ') ||
						name.startsWith('giờ di chuyển') ||
						name.startsWith('giờ gạt') ||
						name.includes('vận chuyển')
					);
				});

				setProductionProcesses(
					vtlProcesses.length > 0 ? vtlProcesses : allProcesses,
				);
				setVtcgProductionProcesses(
					vtcgProcesses.length > 0 ? vtcgProcesses : allProcesses,
				);

				if (!row) return;

				const targetPlanMode =
					(row as any)?.planMode ||
					((row as any)?.hasVanTaiCoGioi
						? 'vantaicogioi'
						: row?.hasVanTaiLo && !row?.hasKhaiThac
							? 'vantailo'
							: 'khaithac');

				form.setValue('departmentId', targetDeptId);
				form.setValue('planMode', targetPlanMode as any);
				await loadPlanDataByMode(targetPlanMode, targetDeptId);
			},
		);

		// Fetch VTCG catalogs
		api
			.pagging(API.CATALOG.PARAMETER.TRANSPORT_DISTANCE.LIST, {
				ignorePagination: true,
			})
			.then((res: any) => setDistances(res?.result?.data || res?.data || []))
			.catch(() => {});

		api
			.pagging(API.CATALOG.CARGO_TYPE.LIST, { ignorePagination: true })
			.then((res: any) => setCargoTypes(res?.result?.data || res?.data || []))
			.catch(() => {});

		api
			.pagging(API.CATALOG.TRANSPORT_LOCATION.LIST, {
				ignorePagination: true,
			})
			.then((res: any) => setLocations(res?.result?.data || res?.data || []))
			.catch(() => {});
	}, [form, row]);

	// Lấy K1 (Hệ số chất lượng thiết bị) và K2 (Hệ số điều kiện môi trường)
	// từ Danh mục Hệ số điều chỉnh của nhóm công đoạn Vận tải lò (VTL)
	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			})
			.then((res) => {
				const groups = (res.result.data ?? []).map(normalizeProcessGroup);
				const vtlGroup = groups.find(
					(group) => group.fixedKeyType === ProcessGroupType.VTL,
				);
				if (!vtlGroup) return;

				return api
					.get<AdjustmentDetail[]>(API.CATALOG.ADJUSTMENT.FACTOR.DETAILS, {
						processGroupId: vtlGroup.id,
					})
					.then((factorsRes) => {
						const factors = factorsRes.result || [];
						setK1Adjustment(
							factors.find((f) => (f.fixedKeyKey ?? f.code) === 'K1') ?? null,
						);
						setK2Adjustment(
							factors.find((f) => (f.fixedKeyKey ?? f.code) === 'K2') ?? null,
						);
					});
			})
			.catch(() => {});
	}, []);

	useEffect(() => {
		if (!row) {
			form.reset(DEPARTMENT_PLAN_FORM_DEFAULT);
			setLegacyProductIds(new Map());
		}
	}, [form, row]);

	const productMap = useMemo(
		() => new Map(products.map((product) => [product.id, product])),
		[products],
	);

	const syncProductUnit = (
		productId: string,
		unitOfMeasureId: string,
		origin?: { monthIndex: number; itemIndex: number },
	) => {
		form.getValues('months').forEach((month, monthIndex) => {
			month.items.forEach((item, itemIndex) => {
				if (item.productId !== productId) return;
				if (
					origin &&
					origin.monthIndex === monthIndex &&
					origin.itemIndex === itemIndex
				) {
					return;
				}
				if (item.unitOfMeasureId === unitOfMeasureId) return;

				form.setValue(
					`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
					unitOfMeasureId,
					{
						shouldDirty: true,
						shouldValidate: true,
					},
				);
			});
		});
	};

	useEffect(() => {
		const firstUnitByProduct = new Map<string, string>();

		watchedMonths.forEach((month, monthIndex) => {
			month.items.forEach((item, itemIndex) => {
				if (!item.productId || !item.unitOfMeasureId) return;
				const existing = firstUnitByProduct.get(item.productId);
				if (!existing) {
					firstUnitByProduct.set(item.productId, item.unitOfMeasureId);
					return;
				}
				if (existing !== item.unitOfMeasureId) {
					form.setValue(
						`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
						existing,
						{
							shouldDirty: true,
							shouldValidate: true,
						},
					);
				}
			});
		});
	}, [form, watchedMonths]);

	const shouldPreserveInvalidSelection = (
		item: DepartmentPlanFormSchema['months'][number]['items'][number],
		month?: string,
	) => {
		if (!isEdit || !month) return false;
		const itemKey = getPersistedItemKey(item);
		if (!itemKey) return false;

		const legacyProductId = legacyProductIds.get(itemKey);
		return !!legacyProductId && legacyProductId === item.productId;
	};

	useEffect(() => {
		watchedMonths.forEach((month, monthIndex) => {
			month.items.forEach((item, itemIndex) => {
				if (!getInvalidSelectedProduct(products, item.productId, month.month)) {
					return;
				}

				if (shouldPreserveInvalidSelection(item, month.month)) {
					return;
				}

				form.setValue(`months.${monthIndex}.items.${itemIndex}.productId`, '', {
					shouldDirty: true,
					shouldValidate: true,
				});
				form.setValue(
					`months.${monthIndex}.items.${itemIndex}.unitOfMeasureId`,
					'',
					{
						shouldDirty: true,
						shouldValidate: true,
					},
				);
			});
		});
	}, [form, products, watchedMonths, isEdit, legacyProductIds]);

	const handleSubmit = async (values: DepartmentPlanFormSchema) => {
		try {
			if (values.planMode === 'vantaicogioi') {
				const motorizedPayload = {
					departmentId: values.departmentId,
					months: buildMotorizedTransportMonthsPayload(values.motorizedMonths),
				};

				if (isEdit) {
					await api.put(
						API.COST.TRANSPORT_PLAN_LINE.UPDATE_PLANNED_BY_DEPARTMENT,
						motorizedPayload,
					);
				} else {
					await api.post(
						API.COST.TRANSPORT_PLAN_LINE.CREATE_PLANNED_BY_DEPARTMENT,
						motorizedPayload,
					);
				}
			} else if (values.planMode === 'vantailo') {
				const transportPayload = {
					departmentId: values.departmentId,
					months: buildTransportMonthsPayload(values.transportMonths),
				};

				if (isEdit) {
					await api.put(
						API.COST.TRANSPORT_PLAN_LINE.UPDATE_PLANNED_BY_DEPARTMENT,
						transportPayload,
					);
				} else {
					await api.post(
						API.COST.TRANSPORT_PLAN_LINE.CREATE_PLANNED_BY_DEPARTMENT,
						transportPayload,
					);
				}
			} else {
				const payload = {
					departmentId: values.departmentId,
					months: values.months.map((month) => ({
						month: month.month,
						items: month.items.map((item) => {
							const product = productMap.get(item.productId);
							const isAkApplicable =
								!!product?.processGroupId &&
								akProcessGroupIds.has(product.processGroupId);

							return {
								productUnitPriceId: item.productUnitPriceId,
								outputId: item.outputId,
								productId: item.productId,
								unitOfMeasureId: item.unitOfMeasureId,
								productionMeters: item.productionMeters,
								planAshContent: isAkApplicable ? (item.planAshContent ?? 0) : 0,
							};
						}),
					})),
				};

				if (isEdit) {
					await api.put(API.COST.PRODUCT.UPDATE_PLANNED_BY_DEPARTMENT, payload);
				} else {
					await api.post(
						API.COST.PRODUCT.CREATE_PLANNED_BY_DEPARTMENT,
						payload,
					);
				}
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${isEdit ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
			onSuccess?.();
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider
			context={form}
			onSubmit={handleSubmit}
			onInvalid={(errors) => {
				console.error('Form validation errors:', errors);
				// eslint-disable-next-line @typescript-eslint/no-explicit-any
				const getFirstErrorMsg = (errObj: any): string | undefined => {
					if (!errObj) return undefined;
					if (typeof errObj.message === 'string') return errObj.message;
					if (Array.isArray(errObj)) {
						for (const item of errObj) {
							const msg = getFirstErrorMsg(item);
							if (msg) return msg;
						}
					} else if (typeof errObj === 'object') {
						for (const key of Object.keys(errObj)) {
							const msg = getFirstErrorMsg(errObj[key]);
							if (msg) return msg;
						}
					}
					return undefined;
				};

				const firstMsg = getFirstErrorMsg(errors);
				popup.error(
					firstMsg ||
						'Vui lòng kiểm tra lại các thông tin nhập trong form (đơn vị, thời gian, công đoạn sản xuất, sản lượng...)',
				);
			}}
		>
			<FormRow>
				<FormComboBox
					control={form.control}
					name='departmentId'
					label='Đơn vị'
					placeholder='Chọn đơn vị'
					options={departments.map((department) => ({
						label: `${department.code} - ${department.name}`,
						value: department.id,
					}))}
					disabled={isEdit}
				/>
			</FormRow>

			{/* Chọn chế độ: Khai thác / Vận tải lò / Vận tải cơ giới */}
			<FormRow>
				<FormSelect
					control={form.control}
					name='planMode'
					label='Chọn'
					placeholder='Chọn chế độ kế hoạch'
					options={[
						{ value: 'khaithac', label: 'Khai thác' },
						{ value: 'vantailo', label: 'Vận tải lò' },
						{ value: 'vantaicogioi', label: 'Vận tải cơ giới' },
					]}
				/>
			</FormRow>

			<FormSeparator />

			{/* ========== FORM KHAI THÁC ========== */}
			{watchedPlanMode === 'khaithac' && (
				<>
					<div className='flex flex-col gap-4'>
						{invalidLegacySelections.length > 0 && (
							<Alert variant='destructive'>
								<TriangleAlertIcon />
								<AlertTitle>
									Một số sản phẩm đang chọn không còn thuộc khoảng thời gian áp
									dụng
								</AlertTitle>
								<AlertDescription>
									<div className='space-y-1'>
										{invalidLegacySelections.map((selection) => (
											<p
												key={`${selection.monthIndex}-${selection.itemIndex}-${selection.product.id}`}
											>
												{`${formatMonthLabel(selection.month)}: ${selection.product.code} - ${selection.product.name}`}
											</p>
										))}
									</div>
								</AlertDescription>
							</Alert>
						)}

						{monthFields.map((field, monthIndex) => (
							<MonthSection
								key={field.id}
								form={form}
								monthIndex={monthIndex}
								canRemove={monthFields.length > 1}
								onRemoveMonth={() => remove(monthIndex)}
								products={products}
								units={units}
								akProcessGroupIds={akProcessGroupIds}
								shouldPreserveInvalidSelection={shouldPreserveInvalidSelection}
								onSyncProductUnit={syncProductUnit}
							/>
						))}
					</div>

					{typeof form.formState.errors.months?.message === 'string' && (
						<FieldError errors={[form.formState.errors.months]} />
					)}

					<Button
						type='button'
						variant='ghost'
						size='sm'
						className='h-fit w-fit bg-transparent'
						onClick={() =>
							append({
								month: '',
								items: [
									{
										productId: '',
										unitOfMeasureId: '',
										productionMeters: Number.NaN,
										planAshContent: 0,
									},
								],
							})
						}
					>
						<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
						<span>Thêm thời gian</span>
					</Button>
				</>
			)}

			{/* ========== FORM VẬN TẢI LÒ ========== */}
			{watchedPlanMode === 'vantailo' && (
				<>
					<div className='flex flex-col gap-4'>
						{transportMonthFields.map((field, monthIndex) => (
							<TransportMonthSection
								key={field.id}
								form={form}
								monthIndex={monthIndex}
								canRemove={transportMonthFields.length > 1}
								onRemoveMonth={() => removeTransportMonth(monthIndex)}
								productionProcesses={productionProcesses}
								transportRoutes={transportRoutes}
								departments={departments}
								contractCodes={contractCodes}
								units={units}
								adjustments={adjustments}
								k1Adjustment={k1Adjustment}
								k2Adjustment={k2Adjustment}
							/>
						))}
					</div>

					{typeof form.formState.errors.transportMonths?.message ===
						'string' && (
						<FieldError errors={[form.formState.errors.transportMonths]} />
					)}

					<Button
						type='button'
						variant='ghost'
						size='sm'
						className='h-fit w-fit bg-transparent'
						onClick={() =>
							appendTransportMonth({
								month: '',
								lowValuePerishableSupply: false,
								processIds: [],
								processes: [],
							})
						}
					>
						<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
						<span>Thêm thời gian</span>
					</Button>
				</>
			)}

			{/* ========== FORM VẬN TẢI CƠ GIỚI ========== */}
			{watchedPlanMode === 'vantaicogioi' && (
				<>
					<div className='flex flex-col gap-4'>
						{motorizedMonthFields.map((field, monthIndex) => (
							<MotorizedMonthSection
								key={field.id}
								form={form}
								monthIndex={monthIndex}
								canRemove={motorizedMonthFields.length > 1}
								onRemoveMonth={() => removeMotorizedMonth(monthIndex)}
								productionProcesses={vtcgProductionProcesses}
								assignmentCodes={contractCodes}
								distances={distances}
								cargoTypes={cargoTypes}
								locations={locations}
							/>
						))}
					</div>

					{typeof (form.formState.errors as any).motorizedMonths?.message ===
						'string' && (
						<FieldError
							errors={[(form.formState.errors as any).motorizedMonths]}
						/>
					)}

					<Button
						type='button'
						variant='ghost'
						size='sm'
						className='h-fit w-fit bg-transparent'
						onClick={() =>
							appendMotorizedMonth({
								month: '',
								lowValuePerishableSupply: false,
								motorizedCategory: 'scania',
								assignmentCodeIds: [],
								equipmentQualities: {},
								equipmentProcesses: {},
								equipmentDistances: {},
								processCargoTypes: {},
								processPickupLocations: {},
								processDropoffLocations: {},
								items: [],
							})
						}
					>
						<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
						<span>Thêm thời gian</span>
					</Button>
				</>
			)}

			<FormSeparator />

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
