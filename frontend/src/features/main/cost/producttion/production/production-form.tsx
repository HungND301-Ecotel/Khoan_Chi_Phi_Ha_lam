import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import type { Production } from './columns';
import { KhaiThacGroupFields } from './khai-thac/form-section';
import {
	getProductionFormDefault,
	PRODUCTION_GROUP_DEFAULT,
	productionFormSchema,
	type ProductionFormMode,
	type ProductionFormSchema,
	type ProductionGroupSchema,
} from './production-form-schema';
import {
	VanTaiLoGroupFields,
	type ProductionProcessOption,
} from './van-tai-lo/form-section';

type ProductionOutputDetailProduct = {
	productId: string;
	productionMeters: number;
	actualAshContent?: number;
};

type ProductionOutputDetailTransportLine = {
	productionProcessId: string;
	equipmentId?: string | null;
	equipmentQuality?: string | null;
	transportRouteId?: string | null;
	routeDepartmentId?: string | null;
	productionMeters: number;
};

type ProductionOutputDetailProcessGroup = {
	processGroupId: string;
	planProductionMeters: number;
	standardProductionMeters: number;
	products: ProductionOutputDetailProduct[];
	transportLines?: ProductionOutputDetailTransportLine[];
};

type ProductionOutputDetail = {
	id: string;
	startMonth: string;
	endMonth?: string;
	departmentId?: string | null;
	acceptanceReportId?: string | null;
	productionMeters: number;
	standardProductionMeters: number;
	processGroups?: ProductionOutputDetailProcessGroup[];
};

type ProductionFormProps = ActionDialogProps<Production> & {
	onSuccess?: () => void;
};
type ProductionGroupProduct = ProductionGroupSchema['products'][number];

function formatCodeNameOption(code?: string | null, name?: string | null) {
	return [code, name].filter(Boolean).join(' - ');
}

function createGroupDefault(): ProductionGroupSchema {
	return { ...PRODUCTION_GROUP_DEFAULT };
}

// Nhóm công đoạn kiểu VTL/VTCG dùng khối field khác hẳn (Công đoạn/Tuyến/Thiết bị thay vì Sản
// phẩm) — xác định qua code hoặc fixedKeyType của processGroupId đã chọn.
function resolveGroupType(
	processGroupId: string,
	processGroupsById: Map<string, ProcessGroup>,
): 'khaithac' | 'vantailo' {
	const processGroup = processGroupsById.get(processGroupId);
	if (!processGroup) return 'khaithac';
	const code = (processGroup.code || '').trim().toUpperCase();
	if (code === 'VTL' || code === 'VTCG') return 'vantailo';
	const fixedKeyType = processGroup.fixedKeyType ?? (processGroup as any).type;
	if (
		fixedKeyType === ProcessGroupType.VTL ||
		fixedKeyType === ProcessGroupType.VTCG ||
		fixedKeyType === 4 ||
		fixedKeyType === 5 ||
		fixedKeyType === 12 ||
		fixedKeyType === 13
	) {
		return 'vantailo';
	}
	const name = (processGroup.name || '').toLowerCase();
	if (name.includes('vận tải')) return 'vantailo';
	return 'khaithac';
}

function isSameStringArray(current: string[] = [], next: string[] = []) {
	if (current.length !== next.length) return false;
	return current.every((value, index) => value === next[index]);
}

function isSameProductionRows(
	current: ProductionGroupProduct[] = [],
	next: ProductionGroupProduct[] = [],
) {
	if (current.length !== next.length) return false;

	return current.every((row, index) => {
		const nextRow = next[index];
		if (!nextRow || row.productId !== nextRow.productId) return false;

		const sameMeters =
			row.productionMeters === nextRow.productionMeters ||
			(Number.isNaN(row.productionMeters) &&
				Number.isNaN(nextRow.productionMeters));

		const sameAshContent =
			(row.actualAshContent ?? 0) === (nextRow.actualAshContent ?? 0) ||
			(Number.isNaN(row.actualAshContent ?? 0) &&
				Number.isNaN(nextRow.actualAshContent ?? 0));

		return sameMeters && sameAshContent;
	});
}

function calculateTotals(groups: ProductionGroupSchema[] = []) {
	return {
		plannedOutput: groups.reduce(
			(sum, group) => sum + (group.planProductionMeters || 0),
			0,
		),
		productionMeters: groups.reduce((sum, group) => {
			const productMeters = (group.products || []).reduce(
				(productSum, product) => productSum + (product.productionMeters || 0),
				0,
			);
			const transportMeters = (group.transportProcesses || []).reduce(
				(processSum, process) =>
					processSum +
					(process.items || []).reduce(
						(itemSum, item) => itemSum + (item.productionMeters || 0),
						0,
					),
				0,
			);
			return sum + productMeters + transportMeters;
		}, 0),
		standardProductionMeters: groups.reduce(
			(sum, group) => sum + (group.standardProductionMeters || 0),
			0,
		),
	};
}

function buildProcessGroupPayload(
	groups: ProductionGroupSchema[] = [],
	akProcessGroupIds: Set<string> = new Set(),
) {
	return groups.map((group) => ({
		processGroupId: group.processGroupId,
		planProductionMeters: group.planProductionMeters,
		standardProductionMeters: group.standardProductionMeters,
		products: (group.products || []).map((product) => ({
			productId: product.productId,
			productionMeters: product.productionMeters,
			actualAshContent: akProcessGroupIds.has(group.processGroupId)
				? (product.actualAshContent ?? 0)
				: 0,
		})),
		transportLines: (group.transportProcesses || []).flatMap((process) =>
			(process.items || [])
				.filter(
					(item) =>
						typeof item.productionMeters === 'number' &&
						!Number.isNaN(item.productionMeters),
				)
				.map((item) => ({
					productionProcessId: process.productionProcessId,
					equipmentId: item.equipmentId || undefined,
					equipmentQuality: item.equipmentQuality || undefined,
					transportRouteId: item.transportRouteId || undefined,
					routeDepartmentId: item.routeDepartmentId || undefined,
					productionMeters: item.productionMeters,
				})),
		),
	}));
}

// Gộp danh sách transportLines phẳng (từ API) thành processIds + transportProcesses theo đúng
// cấu trúc form — nhóm theo productionProcessId, mỗi dòng giữ nguyên Tuyến HOẶC Thiết bị.
function mapTransportLinesToFormState(
	transportLines: ProductionOutputDetailTransportLine[] = [],
) {
	const byProcess = new Map<
		string,
		ProductionGroupSchema['transportProcesses'][number]
	>();

	transportLines.forEach((line) => {
		const existing = byProcess.get(line.productionProcessId) ?? {
			productionProcessId: line.productionProcessId,
			routeIds: [],
			routeDepartmentIds: {},
			equipmentIds: [],
			equipmentQualities: [],
			equipmentQualitiesMap: {},
			items: [],
		};

		if (line.transportRouteId) {
			existing.routeIds = [
				...new Set([...existing.routeIds, line.transportRouteId]),
			];
		}
		if (line.transportRouteId && line.routeDepartmentId) {
			const deptsForRoute =
				existing.routeDepartmentIds[line.transportRouteId] || [];
			existing.routeDepartmentIds = {
				...existing.routeDepartmentIds,
				[line.transportRouteId]: [
					...new Set([...deptsForRoute, line.routeDepartmentId]),
				],
			};
		}
		if (line.equipmentId) {
			existing.equipmentIds = [
				...new Set([...existing.equipmentIds, line.equipmentId]),
			];
		}
		if (line.equipmentQuality) {
			existing.equipmentQualities = [
				...new Set([...existing.equipmentQualities, line.equipmentQuality]),
			];
		}
		if (line.equipmentId && line.equipmentQuality) {
			const qualitiesForEq =
				existing.equipmentQualitiesMap[line.equipmentId] || [];
			existing.equipmentQualitiesMap = {
				...existing.equipmentQualitiesMap,
				[line.equipmentId]: [
					...new Set([...qualitiesForEq, line.equipmentQuality]),
				],
			};
		}
		existing.items = [
			...existing.items,
			{
				transportRouteId: line.transportRouteId || undefined,
				routeDepartmentId: line.routeDepartmentId || undefined,
				equipmentId: line.equipmentId || undefined,
				equipmentQuality: line.equipmentQuality || undefined,
				productionMeters: line.productionMeters,
			},
		];

		byProcess.set(line.productionProcessId, existing);
	});

	const transportProcesses = Array.from(byProcess.values());
	return {
		processIds: transportProcesses.map((p) => p.productionProcessId),
		transportProcesses,
	};
}

export function ProductionForm({ data, row, onSuccess }: ProductionFormProps) {
	const isEdit = !!row;
	const mode: ProductionFormMode = isEdit ? 'edit' : 'create';
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);
	const [products, setProducts] = useState<Product[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [transportRoutes, setTransportRoutes] = useState<TransportRoute[]>([]);
	const [contractCodes, setContractCodes] = useState<
		{ id: string; code: string; name: string }[]
	>([]);
	const [productionProcesses, setProductionProcesses] = useState<
		(ProductionProcessOption & { processGroupId: string })[]
	>([]);
	const [akProcessGroupIds, setAkProcessGroupIds] = useState<Set<string>>(
		new Set(),
	);

	const form = useForm<ProductionFormSchema>({
		resolver: zodResolver(productionFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: getProductionFormDefault(mode),
	});

	const {
		fields: groupFields,
		append,
		remove,
	} = useFieldArray({
		control: form.control,
		name: 'groups',
	});

	const watchedGroups = useWatch({
		control: form.control,
		name: 'groups',
		defaultValue: [] as ProductionGroupSchema[],
	}) as ProductionGroupSchema[];

	useEffect(() => {
		form.reset(getProductionFormDefault(mode));
	}, [mode, form]);

	useEffect(() => {
		if (!isEdit || !row) return;

		api
			.get<ProductionOutputDetail>(
				API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(row.id),
			)
			.then((res) => {
				const {
					startMonth,
					departmentId,
					processGroups,
					productionMeters,
					standardProductionMeters,
				} = res.result;

				const mappedGroups: ProductionGroupSchema[] = (
					processGroups || []
				).map((group) => {
					const mappedProducts = (group.products || []).map((product) => ({
						productId: product.productId,
						productionMeters: product.productionMeters,
						actualAshContent: product.actualAshContent ?? 0,
					}));
					const { processIds, transportProcesses } =
						mapTransportLinesToFormState(group.transportLines);

					return {
						groupType: transportProcesses.length > 0 ? 'vantailo' : 'khaithac',
						processGroupId: group.processGroupId,
						planProductionMeters: group.planProductionMeters ?? 0,
						standardProductionMeters: group.standardProductionMeters,
						productIds: mappedProducts.map((product) => product.productId),
						products: mappedProducts,
						processIds,
						transportProcesses,
					};
				});

				form.reset({
					mode: 'edit',
					startMonth: startMonth.substring(0, 10),
					departmentId: departmentId ?? '',
					plannedOutput: calculateTotals(mappedGroups).plannedOutput,
					productionMeters,
					standardProductionMeters,
					groups:
						mappedGroups.length > 0 ? mappedGroups : [createGroupDefault()],
				});
			});
	}, [isEdit, row, form]);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			}),
			api.pagging<Product>(API.CATALOG.PRODUCT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<{ processGroupId: string }>(
				API.CATALOG.AK_FACTOR_CONFIG.LIST,
				{
					ignorePagination: true,
				},
			),
			api.pagging<TransportRoute>(API.CATALOG.TRANSPORT_ROUTE.LIST, {
				ignorePagination: true,
			}),
			api.pagging<{ id: string; code: string; name: string }>(
				API.CATALOG.CONTRACT_CODE.LIST,
				{ ignorePagination: true },
			),
			api.pagging<ProductionProcessOption & { processGroupId: string }>(
				API.CATALOG.PROCESS.STEP.LIST,
				{ ignorePagination: true },
			),
		]);

		promises.then(
			([
				departmentRes,
				processGroupRes,
				productRes,
				akFactorConfigRes,
				routesRes,
				contractsRes,
				processesRes,
			]) => {
				setDepartments(
					[...departmentRes.result.data].sort((a, b) =>
						a.code.localeCompare(b.code),
					),
				);
				setProcessGroups(
					[...processGroupRes.result.data]
						.map(normalizeProcessGroup)
						.sort((a, b) => a.code.localeCompare(b.code)),
				);
				setProducts(
					[...productRes.result.data].sort((a, b) =>
						a.code.localeCompare(b.code),
					),
				);
				setAkProcessGroupIds(
					new Set(
						(akFactorConfigRes.result.data || [])
							.map((item) => item.processGroupId)
							.filter((id) => !!id),
					),
				);
				setTransportRoutes(routesRes.result.data ?? []);
				setContractCodes(contractsRes.result.data ?? []);
				setProductionProcesses(processesRes.result.data ?? []);
			},
		);
	}, []);

	const processGroupsById = new Map(processGroups.map((g) => [g.id, g]));

	// Xác định lại groupType mỗi khi người dùng đổi Nhóm công đoạn sản xuất của 1 khối — dọn field
	// của loại cũ (Sản phẩm hoặc Công đoạn/Tuyến/Thiết bị) khi đổi loại, tránh mang dữ liệu sai sang.
	const prevGroupTypesRef = useRef<Record<number, string>>({});
	useEffect(() => {
		if (processGroups.length === 0) return;

		watchedGroups.forEach((group, groupIndex) => {
			if (!group.processGroupId) return;

			const correctType = resolveGroupType(
				group.processGroupId,
				processGroupsById,
			);
			const prevType = prevGroupTypesRef.current[groupIndex];

			if (prevType === undefined) {
				prevGroupTypesRef.current[groupIndex] = correctType;
				if (group.groupType !== correctType) {
					form.setValue(`groups.${groupIndex}.groupType`, correctType);
				}
				return;
			}

			if (prevType === correctType) return;

			prevGroupTypesRef.current[groupIndex] = correctType;
			form.setValue(`groups.${groupIndex}.groupType`, correctType);
			form.setValue(`groups.${groupIndex}.productIds`, []);
			form.setValue(`groups.${groupIndex}.products`, []);
			form.setValue(`groups.${groupIndex}.processIds`, []);
			form.setValue(`groups.${groupIndex}.transportProcesses`, []);
		});
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [
		JSON.stringify(watchedGroups.map((g) => g.processGroupId)),
		processGroups,
	]);

	useEffect(() => {
		if (products.length === 0) {
			return;
		}

		watchedGroups.forEach((group, groupIndex) => {
			if (group.groupType === 'vantailo') return;

			const validProductIds = products
				.filter((product) => product.processGroupId === group.processGroupId)
				.map((product) => product.id);

			const nextProductIds = (group.productIds || []).filter(
				(productId: string) => validProductIds.includes(productId),
			);

			const currentRows = group.products || [];
			const nextRows = nextProductIds.map((productId: string) => {
				const existing = currentRows.find(
					(row: ProductionGroupProduct) => row.productId === productId,
				);
				return {
					productId,
					productionMeters: existing?.productionMeters ?? 0,
					actualAshContent: existing?.actualAshContent ?? 0,
				};
			});

			if (!isSameStringArray(group.productIds || [], nextProductIds)) {
				form.setValue(`groups.${groupIndex}.productIds`, nextProductIds, {
					shouldValidate: true,
					shouldDirty: true,
				});
			}

			if (!isSameProductionRows(currentRows, nextRows)) {
				form.setValue(`groups.${groupIndex}.products`, nextRows, {
					shouldValidate: true,
					shouldDirty: true,
				});
			}
		});
	}, [watchedGroups, products, form]);

	const handleProductsChange = (
		groupIndex: number,
		values: MultiSelectOption[],
	) => {
		const selectedProductIds = values.map((value) => value.value);
		const currentRows = form.getValues(`groups.${groupIndex}.products`) || [];
		const nextRows = selectedProductIds.map((productId: string) => {
			const existing = currentRows.find(
				(row: ProductionGroupProduct) => row.productId === productId,
			);
			return {
				productId,
				productionMeters: existing?.productionMeters ?? 0,
				actualAshContent: existing?.actualAshContent ?? 0,
			};
		});

		form.setValue(`groups.${groupIndex}.productIds`, selectedProductIds, {
			shouldValidate: true,
			shouldDirty: true,
		});

		form.setValue(`groups.${groupIndex}.products`, nextRows, {
			shouldValidate: true,
			shouldDirty: true,
		});
	};

	const handleSubmit = async (values: ProductionFormSchema) => {
		try {
			const groups = values.groups || [];
			const { productionMeters, standardProductionMeters } =
				calculateTotals(groups);
			const processGroupsPayload = buildProcessGroupPayload(
				groups,
				akProcessGroupIds,
			);

			if (isEdit && row) {
				await api.put(API.PRODUCTION.PRODUCTION_OUTPUT.UPDATE, {
					id: row.id,
					startMonth: values.startMonth,
					endMonth: values.startMonth,
					departmentId: values.departmentId,
					acceptanceReportId: row.acceptanceReportId || null,
					productionMeters,
					standardProductionMeters,
					processGroups: processGroupsPayload,
				});
			} else {
				await api.post(API.PRODUCTION.PRODUCTION_OUTPUT.CREATE, {
					startMonth: values.startMonth,
					endMonth: values.startMonth,
					departmentId: values.departmentId,
					productionMeters,
					standardProductionMeters,
					processGroups: processGroupsPayload,
				});
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

	const totals = calculateTotals(watchedGroups);

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
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
				/>
			</FormRow>
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Tổng sản lượng kế hoạch</Label>
					<Input readOnly value={formatNumber(totals.plannedOutput)} />
				</div>
				<div className='flex-1 space-y-2'>
					<Label>Tổng sản lượng thực tế</Label>
					<Input readOnly value={formatNumber(totals.productionMeters)} />
				</div>
				<div className='flex-1 space-y-2'>
					<Label>Tổng sản lượng định mức</Label>
					<Input
						readOnly
						value={formatNumber(totals.standardProductionMeters)}
					/>
				</div>
			</FormRow>

			<div className='flex flex-col gap-4'>
				{groupFields.map((field, groupIndex) => {
					const group = watchedGroups[groupIndex] || createGroupDefault();
					// Tính thẳng từ processGroupId đang chọn — không đợi useEffect đồng bộ
					// group.groupType (chỉ dùng field đó để validate lúc submit).
					const isVanTaiLo = group.processGroupId
						? resolveGroupType(group.processGroupId, processGroupsById) ===
							'vantailo'
						: false;
					const isAkApplicableForGroup =
						!!group.processGroupId &&
						akProcessGroupIds.has(group.processGroupId);

					const groupProductMeters = (group.products || []).reduce(
						(sum: number, product: ProductionGroupProduct) => {
							if (Number.isNaN(product.productionMeters)) return sum;
							return sum + (product.productionMeters || 0);
						},
						0,
					);
					const groupTransportMeters = (group.transportProcesses || []).reduce(
						(sum, process) =>
							sum +
							(process.items || []).reduce(
								(itemSum, item) =>
									Number.isNaN(item.productionMeters)
										? itemSum
										: itemSum + (item.productionMeters || 0),
								0,
							),
						0,
					);
					const totalProductionMeters = groupProductMeters + groupTransportMeters;

					const groupProcessesForGroup = productionProcesses.filter(
						(p) => p.processGroupId === group.processGroupId,
					);
					const availableProcesses =
						groupProcessesForGroup.length > 0
							? groupProcessesForGroup
							: productionProcesses;

					return (
						<div
							key={field.id}
							className='flex flex-col gap-4 rounded-sm border border-[#999999] p-4'
						>
							<div className='flex items-center justify-end'>
								<Button
									type='button'
									variant='ghost'
									size='sm'
									className='text-error hover:text-error-muted bg-transparent'
									onClick={() => remove(groupIndex)}
									disabled={groupFields.length === 1}
								>
									<XCircleIcon className='size-4' />
									<span>Xóa</span>
								</Button>
							</div>

							<FormRow>
								<FormComboBox
									control={form.control}
									name={`groups.${groupIndex}.processGroupId`}
									label='Nhóm công đoạn sản xuất'
									placeholder='Chọn nhóm công đoạn sản xuất'
									options={processGroups.map((processGroup) => ({
										label: formatCodeNameOption(
											processGroup.code,
											processGroup.name,
										),
										value: processGroup.id,
									}))}
								/>

								<div className='flex-1 space-y-2'>
									<FormNumber
										control={form.control}
										name={`groups.${groupIndex}.planProductionMeters`}
										label='Sản lượng kế hoạch'
										placeholder='Nhập sản lượng kế hoạch'
									/>
								</div>

								<div className='flex-1 space-y-2'>
									<Label>Sản lượng thực tế</Label>
									<Input readOnly value={formatNumber(totalProductionMeters)} />
								</div>

								<div className='flex-1'>
									<FormNumber
										control={form.control}
										name={`groups.${groupIndex}.standardProductionMeters`}
										label='Sản lượng định mức (Qđm)'
										placeholder='Nhập sản lượng định mức'
									/>
								</div>
							</FormRow>

							{isVanTaiLo ? (
								<VanTaiLoGroupFields
									form={form}
									groupIndex={groupIndex}
									productionProcesses={availableProcesses}
									transportRoutes={transportRoutes}
									contractCodes={contractCodes}
									departments={departments}
								/>
							) : (
								<KhaiThacGroupFields
									form={form}
									groupIndex={groupIndex}
									group={group}
									products={products}
									isAkApplicableForGroup={isAkApplicableForGroup}
									onProductsChange={handleProductsChange}
								/>
							)}
						</div>
					);
				})}

				<Button
					type='button'
					variant='ghost'
					size='sm'
					className='h-fit w-fit bg-transparent px-0'
					onClick={() => append(createGroupDefault())}
				>
					<PlusCircleIcon className='text-primary size-4' />
					<span>Thêm nhóm công đoạn sản xuất</span>
				</Button>
			</div>

			<FormSeparator />

			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
