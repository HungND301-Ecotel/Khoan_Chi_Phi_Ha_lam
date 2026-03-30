/* eslint-disable react-hooks/incompatible-library */
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { DialogFooter } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Spinner } from '@/components/ui/spinner';
import {
	Table,
	TableBody,
	TableCell,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import {
	RAW_ACCEPTANCE_REPORT_FORM_DEFAULT,
	rawAcceptanceReportFormSchema,
	RawAcceptanceReportFormSchema,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/schema';
import { AcceptanceReportDetail } from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import {
	ADDITIONAL_COST_OPTIONS,
	CONTRACT_LIMIT_OPTIONS,
	CONTRACT_LIMIT_SECONDARY_OPTIONS,
	MaterialsIncludedInContractRevenue,
	AdditionalCost,
	OtherMaterialDetail,
	OTHER_MATERIAL_DETAIL_OPTIONS,
	QuotaBasedMaterial,
	Asset,
	MaterialType,
	EXPORTED_TYPE_OPTIONS,
	ISSUED_DETAIL_KEY_BY_TYPE,
	ISSUED_DETAIL_TYPE_BY_KEY,
	QuantityDetail,
	RECEIVED_TYPE_OPTIONS,
	SHIPPED_DETAIL_KEY_BY_TYPE,
	SHIPPED_DETAIL_TYPE_BY_KEY,
	ProductionOrder,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import { cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import {
	FormProvider,
	useFieldArray,
	useForm,
	useFormContext,
	useWatch,
} from 'react-hook-form';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type FieldName = any;

type ProcessGroupOption = {
	value: string;
	label: string;
};

type ProductionOrderOption = {
	value: string;
	label: string;
};

type ImportedItemMeta = {
	materialOrPartId: string;
	type: number;
};

type Equipment = {
	id: string;
	code: string;
	name: string;
};

const PRODUCTION_ORDER_OPTION_PREFIX = 'production-order:';
const EQUIPMENT_OPTION_PREFIX = 'equipment:';

function toProductionOrderOptionValue(id: string): string {
	return `${PRODUCTION_ORDER_OPTION_PREFIX}${id}`;
}

function toEquipmentOptionValue(id: string): string {
	return `${EQUIPMENT_OPTION_PREFIX}${id}`;
}

function resolveSelectionIds(value?: string | null): {
	productionOrderId: string | null;
	equipmentId: string | null;
} {
	if (!value) {
		return {
			productionOrderId: null,
			equipmentId: null,
		};
	}

	if (value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)) {
		const parsedId = normalizeProductionOrderId(
			value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length),
		);
		return {
			productionOrderId: parsedId,
			equipmentId: null,
		};
	}

	if (value.startsWith(EQUIPMENT_OPTION_PREFIX)) {
		const equipmentId = value.slice(EQUIPMENT_OPTION_PREFIX.length).trim();
		return {
			productionOrderId: null,
			equipmentId: equipmentId.length > 0 ? equipmentId : null,
		};
	}

	return {
		productionOrderId: normalizeProductionOrderId(value),
		equipmentId: null,
	};
}

type ProductionOutputScopeResponse = {
	processGroups?: {
		processGroupId: string;
	}[];
};

type QuantityBreakdown = Record<string, number | string>;
type ContractLimitBreakdown = Record<string, number | string>;

const CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS =
	CONTRACT_LIMIT_SECONDARY_OPTIONS.map((option) => ({
		value: String(option.value),
		label: option.label,
	}));
const DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE =
	CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS[0]?.value ?? '';
const DEFAULT_OTHER_MATERIAL_DETAIL_VALUE =
	OTHER_MATERIAL_DETAIL_OPTIONS[0]?.value ?? OtherMaterialDetail.None;

function normalizeProductionOrderId(value?: string | null): string | null {
	if (!value) return null;
	const trimmed = value.trim();
	return trimmed.length > 0 ? trimmed : null;
}

function parseQuantity(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

function mapIssuedDetailsToBreakdown(details?: QuantityDetail[]) {
	const breakdown: QuantityBreakdown = {};
	const selectedKeys: string[] = [];
	for (const detail of details || []) {
		const key = ISSUED_DETAIL_KEY_BY_TYPE[detail.type];
		if (!key) continue;
		selectedKeys.push(key);
		breakdown[key] = detail.quantity ?? 0;
	}

	return {
		selectedKeys:
			selectedKeys.length > 0 ? selectedKeys : [RECEIVED_TYPE_OPTIONS[0].value],
		breakdown,
		total: (details || []).reduce(
			(acc, item) => acc + (Number(item.quantity) || 0),
			0,
		),
	};
}

function mapShippedDetailsToBreakdown(details?: QuantityDetail[]) {
	const breakdown: QuantityBreakdown = {};
	const selectedKeys: string[] = [];
	for (const detail of details || []) {
		const key = SHIPPED_DETAIL_KEY_BY_TYPE[detail.type];
		if (!key) continue;
		selectedKeys.push(key);
		breakdown[key] = detail.quantity ?? 0;
	}

	return {
		selectedKeys:
			selectedKeys.length > 0 ? selectedKeys : [EXPORTED_TYPE_OPTIONS[0].value],
		breakdown,
		total: (details || []).reduce(
			(acc, item) => acc + (Number(item.quantity) || 0),
			0,
		),
	};
}

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === MaterialType.Material) {
		return MaterialsIncludedInContractRevenue.Material;
	}

	if (type === MaterialType.SparePart) {
		return MaterialsIncludedInContractRevenue.Maintain;
	}

	return null;
}

function getDefaultAdditionalCostByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) {
		return AdditionalCost.Material;
	}

	if (type === MaterialType.SparePart) {
		return AdditionalCost.Maintain;
	}

	return null;
}

const ItemType = {
	InContract: 1,
	OutContract: 2,
	SafetyAndWelfare: 3,
	Resource: 4,
	QuotaMaterials: 5,
} as const;

function getMaterialBadge(
	type?: number | null,
	itemType?: number | null,
): {
	label: string;
	className: string;
} {
	if (
		type === MaterialType.Material &&
		itemType === ItemType.SafetyAndWelfare
	) {
		return {
			label:
				'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
			className:
				'rounded bg-emerald-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-emerald-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.Resource) {
		return {
			label: 'Tài sản',
			className:
				'rounded bg-violet-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-violet-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.QuotaMaterials) {
		return {
			label: 'Vật tư theo hạn mức',
			className:
				'rounded bg-cyan-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-cyan-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.InContract) {
		return {
			label: 'Vật tư, tài sản trong khoán',
			className:
				'rounded bg-blue-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-blue-700',
		};
	}

	if (type === MaterialType.Material && itemType === ItemType.OutContract) {
		return {
			label: 'Vật tư, tài sản khác',
			className:
				'rounded bg-slate-200 px-1.5 py-0.5 text-center text-[10px] font-medium text-slate-700',
		};
	}

	if (type === MaterialType.SparePart && itemType === ItemType.InContract) {
		return {
			label: 'Phụ tùng theo thiết bị',
			className:
				'rounded bg-amber-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-amber-700',
		};
	}

	if (type === MaterialType.SparePart && itemType === ItemType.OutContract) {
		return {
			label: 'Phụ tùng khác',
			className:
				'rounded bg-orange-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-orange-700',
		};
	}

	if (type === MaterialType.Material) {
		return {
			label: 'Vật liệu',
			className:
				'rounded bg-blue-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-blue-700',
		};
	}

	if (type === MaterialType.SparePart) {
		return {
			label: 'Phụ tùng',
			className:
				'rounded bg-amber-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-amber-700',
		};
	}

	return {
		label: 'Vật tư',
		className:
			'rounded bg-slate-200 px-1.5 py-0.5 text-center text-[10px] font-medium text-slate-700',
	};
}

export function RawAcceptanceReportForm({
	id,
	output,
	callback,
}: ProductCostFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [loading, setLoading] = useState(false);
	const [acceptanceReportId, setAcceptanceReportId] = useState<string>('');
	const [filePath, setFilePath] = useState<string>('');
	const [processGroupOptions, setProcessGroupOptions] = useState<
		ProcessGroupOption[]
	>([]);
	const [productionOrderOptions, setProductionOrderOptions] = useState<
		ProductionOrderOption[]
	>([]);
	const [importedItems, setImportedItems] = useState<ImportedItemMeta[]>([]);
	const [equipmentOptionsByPartId, setEquipmentOptionsByPartId] = useState<
		Record<string, ProductionOrderOption[]>
	>({});
	const [orderOrEquipmentOptionsByItemId, setOrderOrEquipmentOptionsByItemId] =
		useState<Record<string, ProductionOrderOption[]>>({});

	const form = useForm<RawAcceptanceReportFormSchema>({
		resolver: zodResolver(rawAcceptanceReportFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...RAW_ACCEPTANCE_REPORT_FORM_DEFAULT,
			productionId: id || '',
		},
	});

	useEffect(() => {
		let isMounted = true;

		const fetchProductionOrders = async () => {
			try {
				const response = await api.pagging<ProductionOrder>(
					API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				const options = response.result.data
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((item) => ({
						value: toProductionOrderOptionValue(item.id),
						label: `[Lệnh sản xuất] ${item.code} - ${item.name}`,
					}));

				setProductionOrderOptions(options);
			} catch (err) {
				if (!isMounted) return;
				setProductionOrderOptions([]);
				console.error('Failed to fetch production orders:', err);
			}
		};

		fetchProductionOrders();

		return () => {
			isMounted = false;
		};
	}, []);

	useEffect(() => {
		const nextOptionsByItemId: Record<string, ProductionOrderOption[]> = {};

		for (const item of importedItems) {
			const isPartItem = item.type === MaterialType.SparePart;
			const equipmentOptions = isPartItem
				? (equipmentOptionsByPartId[item.materialOrPartId] ?? [])
				: [];
			nextOptionsByItemId[item.materialOrPartId] = [
				...productionOrderOptions,
				...equipmentOptions,
			];
		}

		setOrderOrEquipmentOptionsByItemId(nextOptionsByItemId);
	}, [importedItems, productionOrderOptions, equipmentOptionsByPartId]);

	useEffect(() => {
		if (!id) {
			setProcessGroupOptions([]);
			return;
		}

		let isMounted = true;

		const fetchScopedProcessGroups = async () => {
			try {
				const [outputRes, processGroupRes] = await Promise.all([
					api.get<ProductionOutputScopeResponse>(
						API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(id),
					),
					api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
						ignorePagination: true,
					}),
				]);

				if (!isMounted) return;

				const allowedIds = new Set(
					(outputRes.result.processGroups || []).map(
						(group) => group.processGroupId,
					),
				);

				const options = processGroupRes.result.data
					.filter((group) => allowedIds.has(group.id))
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((group) => ({
						value: group.id,
						label: `${group.code} - ${group.name}`,
					}));

				setProcessGroupOptions(options);
			} catch (err) {
				if (!isMounted) return;
				setProcessGroupOptions([]);
				console.error('Failed to fetch scoped process groups:', err);
			}
		};

		fetchScopedProcessGroups();

		return () => {
			isMounted = false;
		};
	}, [id]);

	useEffect(() => {
		if (output?.acceptanceReportId) {
			setAcceptanceReportId(output.acceptanceReportId);
		}
	}, [output?.acceptanceReportId]);

	useEffect(() => {
		if (!id || !output?.acceptanceReportId) return;

		// Load existing data from API
		const fetchAcceptanceReport = async () => {
			setLoading(true);
			try {
				const response = await api.get<AcceptanceReportDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(
						output.acceptanceReportId!,
					),
				);

				if (response.result) {
					// Save id and filePath for update
					setAcceptanceReportId(response.result.id);
					setFilePath(response.result.filePath);

					const importedItemMetas: ImportedItemMeta[] = response.result.items
						.map((item) => ({
							materialOrPartId: item.partId ?? item.materialId ?? '',
							type: item.type,
						}))
						.filter((item) => item.materialOrPartId.length > 0);
					setImportedItems(importedItemMetas);

					const partIds = Array.from(
						new Set(
							response.result.items
								.map((item) => item.partId)
								.filter((partId): partId is string => Boolean(partId)),
						),
					);
					const fetchedEquipmentOptionsByPartId: Record<
						string,
						ProductionOrderOption[]
					> = {};

					await Promise.all(
						partIds.map(async (partId) => {
							try {
								const equipmentRes = await api.get<Equipment[]>(
									API.CATALOG.PART.PART_EQUIPMENT(partId),
								);
								const options = (equipmentRes.result ?? [])
									.sort((a, b) => a.code.localeCompare(b.code))
									.map((equipment) => ({
										value: toEquipmentOptionValue(equipment.id),
										label: `[Thiết bị] ${equipment.code} - ${equipment.name}`,
									}));
								fetchedEquipmentOptionsByPartId[partId] = options;
							} catch (error) {
								fetchedEquipmentOptionsByPartId[partId] = [];
								console.error(
									`Failed to fetch equipments by partId (${partId}):`,
									error,
								);
							}
						}),
					);

					setEquipmentOptionsByPartId(fetchedEquipmentOptionsByPartId);

					const items = response.result.items.map((item) => {
						// Map enum values to form field values
						const showCategoryDropdown =
							item.materialsIncludedInContractRevenue !==
								MaterialsIncludedInContractRevenue.None &&
							item.materialsIncludedInContractRevenue !== 0;
						const showAdditionalCostDropdown =
							item.additionalCost !== AdditionalCost.None &&
							item.additionalCost !== 0;
						const showContractLimitDropdown =
							item.quotaBasedMaterial !== QuotaBasedMaterial.None &&
							item.quotaBasedMaterial !== 0;
						const showAssetDropdown =
							item.asset !== Asset.None && item.asset !== 0;

						// Determine material code and name based on type
						// type = 1: Material, type = 2: Spare Part
						const materialCode =
							item.type === 1 ? item.materialCode : item.partCode;
						const materialName =
							item.type === 1 ? item.materialName : item.partName;
						const issuedBreakdown = mapIssuedDetailsToBreakdown(
							item.issuedDetails,
						);
						const shippedBreakdown = mapShippedDetailsToBreakdown(
							item.shippedDetails,
						);
						const receivedQuantity =
							item.issuedDetails && item.issuedDetails.length > 0
								? issuedBreakdown.total
								: item.issuedQuantity || 0;
						const exportedQuantity =
							item.shippedDetails && item.shippedDetails.length > 0
								? shippedBreakdown.total
								: item.shippedQuantity || 0;
						const quotaBasedMaterialQuantities =
							item.quotaBasedMaterialQuantities ?? [];
						const contractLimitSubCategories =
							showContractLimitDropdown &&
							quotaBasedMaterialQuantities.length > 0
								? quotaBasedMaterialQuantities.map((detail) =>
										String(detail.type),
									)
								: showContractLimitDropdown && item.quotaBasedMaterialType
									? [String(item.quotaBasedMaterialType)]
									: [];
						const contractLimitBreakdown = quotaBasedMaterialQuantities.reduce(
							(acc, detail) => {
								acc[String(detail.type)] = detail.quantity ?? 0;
								return acc;
							},
							{} as ContractLimitBreakdown,
						);
						const contractLimitQuantityFromDetails =
							quotaBasedMaterialQuantities.length > 0
								? quotaBasedMaterialQuantities.reduce(
										(acc, detail) => acc + (Number(detail.quantity) || 0),
										0,
									)
								: item.quotaBasedMaterialQuantity || 0;
						const categoryOrderOrEquipmentValue =
							item.categoryEquipmentId
								? toEquipmentOptionValue(item.categoryEquipmentId)
								: item.categoryProductionOrderId
									? toProductionOrderOptionValue(item.categoryProductionOrderId)
									: null;
						const additionalOrderOrEquipmentValue =
							item.additionalCostEquipmentId
								? toEquipmentOptionValue(item.additionalCostEquipmentId)
								: item.additionalCostProductionOrderId
									? toProductionOrderOptionValue(
											item.additionalCostProductionOrderId,
										)
									: null;
						const materialOrPartId = item.partId ?? item.materialId ?? '';

						return {
							id: item.id || '',
							materialOrPartId,
							materialCode: materialCode || '',
							materialName: materialName || '',
							unit: item.unitOfMeasureName || '',
							plannedUnitPrice: item.planCost || 0,
							actualUnitPrice: item.actualCost || 0,
							receivedQuantity,
							exportedQuantity,
							receivedTypes: issuedBreakdown.selectedKeys,
							exportedTypes: shippedBreakdown.selectedKeys,
							receivedBreakdown: issuedBreakdown.breakdown,
							exportedBreakdown: shippedBreakdown.breakdown,
							showCategoryDropdown,
							showAdditionalCostDropdown,
							showContractLimitDropdown,
							showAssetDropdown,
							category: showCategoryDropdown
								? item.materialsIncludedInContractRevenue
								: null,
							categoryProcessGroup: showCategoryDropdown
								? item.processGroupId || null
								: null,
							categoryProductionOrderId:
								showCategoryDropdown &&
								item.materialsIncludedInContractRevenue ===
									MaterialsIncludedInContractRevenue.Maintain
									? categoryOrderOrEquipmentValue
									: null,
							additionalCostCategory: showAdditionalCostDropdown
								? item.additionalCost
								: null,
							additionalCostProductionOrderId:
								showAdditionalCostDropdown &&
								(item.additionalCost === AdditionalCost.Material ||
									item.additionalCost === AdditionalCost.Maintain)
									? additionalOrderOrEquipmentValue
									: null,
							otherMaterialDetail:
								showAdditionalCostDropdown &&
								item.additionalCost === AdditionalCost.OtherMaterial
									? item.otherMaterialDetail &&
										item.otherMaterialDetail !== OtherMaterialDetail.None
										? item.otherMaterialDetail
										: DEFAULT_OTHER_MATERIAL_DETAIL_VALUE
									: null,
							productionOrderId: null,
							equipmentId: null,
							contractLimitCategory: showContractLimitDropdown
								? item.quotaBasedMaterial
								: null,
							contractLimitSubCategory: showContractLimitDropdown
								? item.quotaBasedMaterialType
								: null,
							contractLimitSubCategories,
							contractLimitBreakdown,
							categoryQuantity: showCategoryDropdown
								? item.materialsIncludedInContractRevenueQuantity || null
								: null,
							additionalCostQuantity: showAdditionalCostDropdown
								? item.additionalCostQuantity || null
								: null,
							contractLimitQuantity: showContractLimitDropdown
								? contractLimitQuantityFromDetails || null
								: null,
							assetQuantity: showAssetDropdown
								? item.assetMaterialQuantity || null
								: null,
							type: item.type,
							itemType: item.itemType ?? 0,
						};
					});

					form.reset({
						productionId: id || '',
						items,
					});
				}
			} catch (err) {
				console.error('Failed to fetch acceptance report:', err);
				error(
					err instanceof Error
						? err.message
						: 'Lỗi tải dữ liệu biên bản nghiệm thu',
				);
			} finally {
				setLoading(false);
			}
		};

		fetchAcceptanceReport();
	}, [id, output?.acceptanceReportId]);

	const handleSubmit = async (values: RawAcceptanceReportFormSchema) => {
		try {
			const reportId = acceptanceReportId || output?.acceptanceReportId || '';
			if (!reportId) {
				error('Thiếu thông tin cần thiết');
				return;
			}

			// Transform form data to API request
			const requestData = {
				id: reportId,
				filePath: filePath ?? '',
				items: values.items.map((item) => {
					const resolvedCategory =
						item.category ?? getDefaultCategoryByMaterialType(item.type);

					const materialsIncludedInContractRevenue =
						item.showCategoryDropdown && resolvedCategory
							? resolvedCategory
							: MaterialsIncludedInContractRevenue.None;

					const processGroupId =
						item.showCategoryDropdown && resolvedCategory
							? item.categoryProcessGroup || null
							: null;

					const additionalCost =
						item.showAdditionalCostDropdown && item.additionalCostCategory
							? item.additionalCostCategory
							: AdditionalCost.None;
					const otherMaterialDetail =
						item.showAdditionalCostDropdown &&
						item.additionalCostCategory === AdditionalCost.OtherMaterial
							? (item.otherMaterialDetail ??
								DEFAULT_OTHER_MATERIAL_DETAIL_VALUE)
							: OtherMaterialDetail.None;

					const categorySelection =
						item.showCategoryDropdown &&
						resolvedCategory === MaterialsIncludedInContractRevenue.Maintain
							? resolveSelectionIds(item.categoryProductionOrderId)
							: {
									productionOrderId: null,
									equipmentId: null,
								};

					const additionalSelection =
						item.showAdditionalCostDropdown &&
						(item.additionalCostCategory === AdditionalCost.Material ||
							item.additionalCostCategory === AdditionalCost.Maintain)
							? resolveSelectionIds(item.additionalCostProductionOrderId)
							: {
									productionOrderId: null,
									equipmentId: null,
								};

					let quotaBasedMaterial: number = QuotaBasedMaterial.None;
					let quotaBasedMaterialType: number =
						CONTRACT_LIMIT_SECONDARY_OPTIONS[0].value;
					let quotaBasedMaterialQuantities:
						| {
								type: number;
								quantity: number;
						  }[]
						| null = null;
					if (item.showContractLimitDropdown && item.contractLimitCategory) {
						quotaBasedMaterial = item.contractLimitCategory;
						const selectedSubCategories =
							item.contractLimitSubCategories &&
							item.contractLimitSubCategories.length > 0
								? item.contractLimitSubCategories.map((type) => Number(type))
								: item.contractLimitSubCategory != null
									? [item.contractLimitSubCategory]
									: [];

						quotaBasedMaterialType =
							selectedSubCategories[0] ??
							CONTRACT_LIMIT_SECONDARY_OPTIONS[0].value;
						if (selectedSubCategories.length > 0) {
							quotaBasedMaterialQuantities = selectedSubCategories.map(
								(type) => ({
									type,
									quantity: parseQuantity(
										item.contractLimitBreakdown?.[String(type)],
									),
								}),
							);
						} else if (item.contractLimitQuantity != null) {
							quotaBasedMaterialQuantities = [
								{
									type: quotaBasedMaterialType,
									quantity: parseQuantity(item.contractLimitQuantity),
								},
							];
						}
					}

					const asset = item.showAssetDropdown ? Asset.True : Asset.None;

					const receivedTypes =
						item.receivedTypes && item.receivedTypes.length > 0
							? item.receivedTypes
							: [RECEIVED_TYPE_OPTIONS[0].value];
					const exportedTypes =
						item.exportedTypes && item.exportedTypes.length > 0
							? item.exportedTypes
							: [EXPORTED_TYPE_OPTIONS[0].value];

					const issuedDetails: QuantityDetail[] = [];
					for (const key of receivedTypes) {
						const detailType =
							ISSUED_DETAIL_TYPE_BY_KEY[
								key as keyof typeof ISSUED_DETAIL_TYPE_BY_KEY
							];
						if (!detailType) continue;
						const quantity =
							receivedTypes.length > 1
								? parseQuantity(item.receivedBreakdown?.[key])
								: parseQuantity(item.receivedQuantity);
						issuedDetails.push({
							type: detailType,
							quantity,
						});
					}

					const shippedDetails: QuantityDetail[] = [];
					for (const key of exportedTypes) {
						const detailType =
							SHIPPED_DETAIL_TYPE_BY_KEY[
								key as keyof typeof SHIPPED_DETAIL_TYPE_BY_KEY
							];
						if (!detailType) continue;
						const quantity =
							exportedTypes.length > 1
								? parseQuantity(item.exportedBreakdown?.[key])
								: parseQuantity(item.exportedQuantity);
						shippedDetails.push({
							type: detailType,
							quantity,
						});
					}

					return {
						id: item.id || '',
						itemType: item.itemType ?? 0,
						categoryProductionOrderId: categorySelection.productionOrderId,
						categoryEquipmentId: categorySelection.equipmentId,
						additionalCostProductionOrderId:
							additionalSelection.productionOrderId,
						additionalCostEquipmentId: additionalSelection.equipmentId,
						issuedDetails,
						shippedDetails,
						materialsIncludedInContractRevenue,
						processGroupId,
						materialsIncludedInContractRevenueQuantity:
							item.categoryQuantity || 0,
						additionalCost,
						otherMaterialDetail,
						additionalCostQuantity: item.additionalCostQuantity || 0,
						quotaBasedMaterial,
						quotaBasedMaterialType,
						quotaBasedMaterialQuantities,
						asset,
						assetMaterialQuantity: item.assetQuantity || 0,
					};
				}),
			};

			// Call API to update acceptance report
			await api.put(API.PRODUCTION.ACCEPTANCE_REPORT.UPDATE, requestData);

			success(
				`${breadcrumb} đã được ${id ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider {...form}>
			<form
				onSubmit={form.handleSubmit(handleSubmit)}
				className='flex h-full flex-col gap-6'
			>
				{loading ? (
					<div className='flex h-full items-center justify-center'>
						<Spinner />
					</div>
				) : (
					<div className='min-h-0 flex-1 overflow-x-auto overflow-y-auto'>
						<div className='rounded-lg border shadow-sm'>
							<Table className='w-full'>
								<TableHeader className='bg-linear-to-r from-slate-50 to-slate-100'>
									<TableRow className='bg-linear-to-r from-slate-50 to-slate-100'>
										<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											STT
										</TableCell>
										<TableCell className='sticky left-16 z-20 w-[8%] min-w-32 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
											Mã vật tư
										</TableCell>
										<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
											Đơn vị tính
										</TableCell>
										<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Số lượng lĩnh
										</TableCell>
										<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Số lượng xuất
										</TableCell>
										<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Vật tư tính vào doanh thu khoán
										</TableCell>
										<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Bổ sung chi phí
										</TableCell>
										<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Vật tư theo hạn mức
										</TableCell>
										<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
											Tài sản
										</TableCell>
									</TableRow>
								</TableHeader>
								<TableBody>
									<RawAcceptanceReportRows
										processGroupOptions={processGroupOptions}
										productionOrderOptions={productionOrderOptions}
										orderOrEquipmentOptionsByItemId={
											orderOrEquipmentOptionsByItemId
										}
									/>
								</TableBody>
							</Table>
						</div>
					</div>
				)}

				<DialogFooter className='bg-muted sticky bottom-0 z-20 mt-auto w-full px-10 py-4'>
					<Button
						type='button'
						variant='outline'
						className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
						onClick={() => {
							setOpen(false);
						}}
					>
						Huỷ
					</Button>
					<Button
						type='submit'
						variant='default'
						className='h-8 w-24 shadow-none hover:shadow-none'
						disabled={form.formState.isSubmitting}
					>
						{form.formState.isSubmitting ? <Spinner /> : 'Lưu'}
					</Button>
				</DialogFooter>
			</form>
		</FormProvider>
	);
}

function RawAcceptanceReportRows({
	processGroupOptions,
	productionOrderOptions,
	orderOrEquipmentOptionsByItemId,
}: {
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	orderOrEquipmentOptionsByItemId: Record<string, ProductionOrderOption[]>;
}) {
	const form = useFormContext<{
		items: RawAcceptanceReportFormSchema['items'];
	}>();
	const { fields } = useFieldArray({
		control: form.control,
		name: 'items',
	});

	return (
		<>
			{fields.map((field, index) => (
				<RawAcceptanceReportRow
					key={field.id}
					index={index}
					processGroupOptions={processGroupOptions}
					productionOrderOptions={productionOrderOptions}
					orderOrEquipmentOptionsByItemId={orderOrEquipmentOptionsByItemId}
				/>
			))}
		</>
	);
}

function resetCellFields(
	form: ReturnType<
		typeof useFormContext<{ items: RawAcceptanceReportFormSchema['items'] }>
	>,
	basename: string,
	fields: {
		checkbox: string;
		dropdown?: string;
		dropdownSecondary?: string;
		dropdownTertiary?: string;
		quantity?: string;
	}[],
) {
	for (const field of fields) {
		form.setValue(`${basename}.${field.checkbox}` as FieldName, false);
		if (field.dropdown) {
			form.setValue(`${basename}.${field.dropdown}` as FieldName, null);
		}
		if (field.dropdownSecondary) {
			form.setValue(
				`${basename}.${field.dropdownSecondary}` as FieldName,
				null,
			);
		}
		if (field.dropdownTertiary) {
			form.setValue(`${basename}.${field.dropdownTertiary}` as FieldName, null);
		}
		if (field.quantity) {
			form.setValue(`${basename}.${field.quantity}` as FieldName, null);
		}
	}
}

function QuantityBreakdownInputs({
	selectedKeys,
	allOptions,
	values,
	onChange,
	isValid,
	equalWidth = false,
}: {
	selectedKeys: string[];
	allOptions: { value: string; label: string }[];
	values: QuantityBreakdown;
	onChange: (key: string, value: number | string) => void;
	isValid: boolean;
	equalWidth?: boolean;
}) {
	return (
		<>
			{selectedKeys.map((key) => {
				const option = allOptions.find((entry) => entry.value === key);
				return (
					<div
						key={key}
						className={cn(
							'flex flex-col gap-0.5',
							equalWidth ? 'min-w-0 flex-1' : 'w-24 shrink-0',
						)}
					>
						<label
							className='truncate text-[10px] leading-tight font-medium text-slate-500'
							title={option?.label}
						>
							{option?.label}
						</label>
						<Input
							type='number'
							min={0}
							step='any'
							value={values[key] ?? ''}
							onChange={(event) =>
								onChange(
									key,
									event.target.value === '' ? '' : Number(event.target.value),
								)
							}
							placeholder='0'
							className={cn(
								'text-center',
								!isValid && 'border-red-400 focus-visible:ring-red-300',
							)}
						/>
					</div>
				);
			})}
		</>
	);
}

function RawAcceptanceReportRow({
	index,
	processGroupOptions,
	productionOrderOptions,
	orderOrEquipmentOptionsByItemId,
}: {
	index: number;
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	orderOrEquipmentOptionsByItemId: Record<string, ProductionOrderOption[]>;
}) {
	const form = useFormContext<{
		items: RawAcceptanceReportFormSchema['items'];
	}>();
	const basename = `items.${index}` as const;

	const showCategoryDropdown = useWatch({
		control: form.control,
		name: `${basename}.showCategoryDropdown` as FieldName,
	});
	const showAdditionalCostDropdown = useWatch({
		control: form.control,
		name: `${basename}.showAdditionalCostDropdown` as FieldName,
	});
	const showContractLimitDropdown = useWatch({
		control: form.control,
		name: `${basename}.showContractLimitDropdown` as FieldName,
	});
	const showAssetDropdown = useWatch({
		control: form.control,
		name: `${basename}.showAssetDropdown` as FieldName,
	});
	const categoryValue = useWatch({
		control: form.control,
		name: `${basename}.category` as FieldName,
	});
	const categoryProcessGroupValue = useWatch({
		control: form.control,
		name: `${basename}.categoryProcessGroup` as FieldName,
	});
	const categoryProductionOrderId = useWatch({
		control: form.control,
		name: `${basename}.categoryProductionOrderId` as FieldName,
	});
	const additionalCostCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.additionalCostCategory` as FieldName,
	});
	const additionalCostProductionOrderId = useWatch({
		control: form.control,
		name: `${basename}.additionalCostProductionOrderId` as FieldName,
	});
	const otherMaterialDetailValue = useWatch({
		control: form.control,
		name: `${basename}.otherMaterialDetail` as FieldName,
	});
	const contractLimitCategoryValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitCategory` as FieldName,
	});
	const contractLimitSubCategoriesValue = useWatch({
		control: form.control,
		name: `${basename}.contractLimitSubCategories` as FieldName,
	}) as string[] | undefined;
	const contractLimitBreakdown = useWatch({
		control: form.control,
		name: `${basename}.contractLimitBreakdown` as FieldName,
	}) as ContractLimitBreakdown | undefined;
	const exportedQuantityWatch = useWatch({
		control: form.control,
		name: `${basename}.exportedQuantity` as FieldName,
	});
	const materialTypeValue = useWatch({
		control: form.control,
		name: `${basename}.type` as FieldName,
	});
	const materialOrPartId = useWatch({
		control: form.control,
		name: `${basename}.materialOrPartId` as FieldName,
	}) as string | undefined;
	const itemTypeValue = useWatch({
		control: form.control,
		name: `${basename}.itemType` as FieldName,
	});
	const receivedTypes = useWatch({
		control: form.control,
		name: `${basename}.receivedTypes` as FieldName,
	}) as string[] | undefined;
	const exportedTypes = useWatch({
		control: form.control,
		name: `${basename}.exportedTypes` as FieldName,
	}) as string[] | undefined;
	const receivedBreakdown = useWatch({
		control: form.control,
		name: `${basename}.receivedBreakdown` as FieldName,
	}) as QuantityBreakdown | undefined;
	const exportedBreakdown = useWatch({
		control: form.control,
		name: `${basename}.exportedBreakdown` as FieldName,
	}) as QuantityBreakdown | undefined;

	const defaultCategoryByType =
		getDefaultCategoryByMaterialType(materialTypeValue);
	const defaultAdditionalCostByType =
		getDefaultAdditionalCostByMaterialType(materialTypeValue);
	const isSafetyAndWelfareMaterial =
		materialTypeValue === MaterialType.Material &&
		itemTypeValue === ItemType.SafetyAndWelfare;
	const isSparePartByEquipment =
		materialTypeValue === MaterialType.SparePart &&
		itemTypeValue === ItemType.InContract;
	const resolvedCategoryValue = categoryValue ?? defaultCategoryByType;
	const orderOrEquipmentOptions =
		(materialOrPartId
			? orderOrEquipmentOptionsByItemId[materialOrPartId]
			: undefined) ?? productionOrderOptions;
	const equipmentOptions = orderOrEquipmentOptions.filter((option) =>
		option.value.startsWith(EQUIPMENT_OPTION_PREFIX),
	);
	const productionOrderOnlyOptions = orderOrEquipmentOptions.filter((option) =>
		option.value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX),
	);
	const categoryOrderOrEquipmentOptions = isSparePartByEquipment
		? [...equipmentOptions, ...productionOrderOnlyOptions]
		: orderOrEquipmentOptions;
	const additionalCostOrderOrEquipmentOptions = isSparePartByEquipment
		? productionOrderOnlyOptions
		: orderOrEquipmentOptions;
	const additionalCostOptionsByType =
		defaultAdditionalCostByType == null
			? ADDITIONAL_COST_OPTIONS
			: ADDITIONAL_COST_OPTIONS.filter(
					(option) =>
						option.value === defaultAdditionalCostByType ||
						option.value === AdditionalCost.OtherMaterial,
				);

	const prevState = useRef({
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showContractLimitDropdown: false,
		showAssetDropdown: false,
	});

	const prevDropdownState = useRef({
		category: null as number | null | undefined,
		categoryProcessGroup: null as string | null | undefined,
		categoryProductionOrderId: null as string | null | undefined,
		additionalCostCategory: null as number | null | undefined,
		additionalCostProductionOrderId: null as string | null | undefined,
		otherMaterialDetail: null as number | null | undefined,
		contractLimitCategory: null as number | null | undefined,
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showAssetDropdown: false,
	});
	const prevContractLimitSelectionSignatureRef = useRef('');

	// Determine if secondary combobox is needed for contract limit
	const needsSecondComboBox =
		contractLimitCategoryValue === QuotaBasedMaterial.MineSupport ||
		contractLimitCategoryValue === QuotaBasedMaterial.SupportAccessories;
	const categoryNeedsProductionOrder =
		resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain;
	const additionalCostNeedsProductionOrder =
		additionalCostCategoryValue === AdditionalCost.Material ||
		additionalCostCategoryValue === AdditionalCost.Maintain;
	const additionalCostNeedsOtherMaterialDetail =
		additionalCostCategoryValue === AdditionalCost.OtherMaterial;
	const contractLimitSelectedKeys =
		contractLimitSubCategoriesValue &&
		contractLimitSubCategoriesValue.length > 0
			? contractLimitSubCategoriesValue
			: [];
	const contractLimitSelectedTypes = contractLimitSelectedKeys.map((item) =>
		Number(item),
	);
	const contractLimitBreakdownTotal = contractLimitSelectedTypes.reduce(
		(acc, type) => acc + (Number(contractLimitBreakdown?.[String(type)]) || 0),
		0,
	);

	useEffect(() => {
		if (!receivedTypes || receivedTypes.length === 0) {
			form.setValue(`${basename}.receivedTypes` as FieldName, [
				RECEIVED_TYPE_OPTIONS[0].value,
			]);
		}
		if (!exportedTypes || exportedTypes.length === 0) {
			form.setValue(`${basename}.exportedTypes` as FieldName, [
				EXPORTED_TYPE_OPTIONS[0].value,
			]);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	useEffect(() => {
		if (!receivedTypes) return;
		const current = receivedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...receivedTypes].sort().join(',')
		) {
			return;
		}

		const count = receivedTypes.length;
		const divided =
			count > 0
				? (Number(
						form.getValues(`${basename}.receivedQuantity` as FieldName),
					) || 0) / count
				: 0;
		const next: QuantityBreakdown = {};
		for (const key of receivedTypes) {
			next[key] = divided;
		}
		form.setValue(`${basename}.receivedBreakdown` as FieldName, next);
	}, [receivedTypes, receivedBreakdown, form, basename]);

	useEffect(() => {
		if (!exportedTypes) return;
		const current = exportedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...exportedTypes].sort().join(',')
		) {
			return;
		}

		const count = exportedTypes.length;
		const divided =
			count > 0 ? (Number(exportedQuantityWatch) || 0) / count : 0;
		const next: QuantityBreakdown = {};
		for (const key of exportedTypes) {
			next[key] = divided;
		}
		form.setValue(`${basename}.exportedBreakdown` as FieldName, next);
	}, [exportedTypes, exportedBreakdown, exportedQuantityWatch, form, basename]);

	useEffect(() => {
		const prev = prevState.current;

		const justEnabledCategory =
			!prev.showCategoryDropdown && showCategoryDropdown;
		const justEnabledAdditional =
			!prev.showAdditionalCostDropdown && showAdditionalCostDropdown;
		const justEnabledContractLimit =
			!prev.showContractLimitDropdown && showContractLimitDropdown;
		const justEnabledAsset = !prev.showAssetDropdown && showAssetDropdown;

		// Thêm: track vừa TẮT
		const justDisabledCategory =
			prev.showCategoryDropdown && !showCategoryDropdown;
		const justDisabledAdditional =
			prev.showAdditionalCostDropdown && !showAdditionalCostDropdown;
		const justDisabledContractLimit =
			prev.showContractLimitDropdown && !showContractLimitDropdown;

		if (justEnabledContractLimit) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'category',
					dropdownSecondary: 'categoryProcessGroup',
					dropdownTertiary: 'categoryProductionOrderId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostProductionOrderId',
					dropdownTertiary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
			]);
		} else if (justEnabledAsset) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'category',
					dropdownSecondary: 'categoryProcessGroup',
					dropdownTertiary: 'categoryProductionOrderId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostProductionOrderId',
					dropdownTertiary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{
					checkbox: 'showContractLimitDropdown',
					dropdown: 'contractLimitCategory',
					quantity: 'contractLimitQuantity',
				},
			]);
		} else if (justEnabledCategory) {
			if (prev.showContractLimitDropdown) {
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			}
			if (prev.showAssetDropdown) {
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			}

			if (categoryValue == null && defaultCategoryByType != null) {
				form.setValue(
					`${basename}.category` as FieldName,
					defaultCategoryByType,
				);
			}

			if (!categoryProcessGroupValue && processGroupOptions.length === 1) {
				form.setValue(
					`${basename}.categoryProcessGroup` as FieldName,
					processGroupOptions[0].value,
				);
			}

			if (
				resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain &&
				categoryProductionOrderId == null &&
				categoryOrderOrEquipmentOptions.length > 0
			) {
				form.setValue(
					`${basename}.categoryProductionOrderId` as FieldName,
					categoryOrderOrEquipmentOptions[0].value,
				);
			}
		} else if (justEnabledAdditional) {
			if (prev.showContractLimitDropdown) {
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			}
			if (prev.showAssetDropdown) {
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			}

			if (
				isSafetyAndWelfareMaterial &&
				additionalCostCategoryValue !== AdditionalCost.OtherMaterial
			) {
				form.setValue(
					`${basename}.additionalCostCategory` as FieldName,
					AdditionalCost.OtherMaterial,
				);
			} else if (
				additionalCostCategoryValue == null &&
				defaultAdditionalCostByType != null
			) {
				form.setValue(
					`${basename}.additionalCostCategory` as FieldName,
					defaultAdditionalCostByType,
				);
			}

			if (
				additionalCostCategoryValue === AdditionalCost.Material ||
				additionalCostCategoryValue === AdditionalCost.Maintain
			) {
				const hasValidSelection =
					additionalCostProductionOrderId != null &&
					additionalCostOrderOrEquipmentOptions.some(
						(option) => option.value === additionalCostProductionOrderId,
					);
				if (!hasValidSelection) {
					form.setValue(
						`${basename}.additionalCostProductionOrderId` as FieldName,
						additionalCostOrderOrEquipmentOptions[0]?.value ?? null,
					);
				}
			}
			if (
				additionalCostCategoryValue === AdditionalCost.OtherMaterial &&
				otherMaterialDetailValue == null
			) {
				form.setValue(
					`${basename}.otherMaterialDetail` as FieldName,
					DEFAULT_OTHER_MATERIAL_DETAIL_VALUE,
				);
			}
		}

		// Reset giá trị khi uncheck
		if (justDisabledCategory) {
			form.setValue(`${basename}.category` as FieldName, null);
			form.setValue(`${basename}.categoryProcessGroup` as FieldName, null);
			form.setValue(`${basename}.categoryProductionOrderId` as FieldName, null);
			form.setValue(`${basename}.categoryQuantity` as FieldName, null);
		}
		if (justDisabledAdditional) {
			form.setValue(`${basename}.additionalCostCategory` as FieldName, null);
			form.setValue(
				`${basename}.additionalCostProductionOrderId` as FieldName,
				null,
			);
			form.setValue(`${basename}.otherMaterialDetail` as FieldName, null);
			form.setValue(`${basename}.additionalCostQuantity` as FieldName, null);
		}
		if (justDisabledContractLimit) {
			form.setValue(`${basename}.contractLimitCategory` as FieldName, null);
			form.setValue(`${basename}.contractLimitQuantity` as FieldName, null);
			form.setValue(`${basename}.contractLimitSubCategory` as FieldName, null);
			form.setValue(`${basename}.contractLimitSubCategories` as FieldName, []);
			form.setValue(`${basename}.contractLimitBreakdown` as FieldName, {});
			prevContractLimitSelectionSignatureRef.current = '';
		}
		const justDisabledAsset = prev.showAssetDropdown && !showAssetDropdown;
		if (justDisabledAsset) {
			form.setValue(`${basename}.assetQuantity` as FieldName, null);
		}

		prevState.current = {
			showCategoryDropdown,
			showAdditionalCostDropdown,
			showContractLimitDropdown,
			showAssetDropdown,
		};
	}, [
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
		categoryValue,
		categoryProcessGroupValue,
		additionalCostCategoryValue,
		defaultCategoryByType,
		defaultAdditionalCostByType,
		isSafetyAndWelfareMaterial,
		processGroupOptions,
		categoryOrderOrEquipmentOptions,
		additionalCostOrderOrEquipmentOptions,
		resolvedCategoryValue,
		categoryProductionOrderId,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showAdditionalCostDropdown || !isSafetyAndWelfareMaterial) return;
		if (additionalCostCategoryValue === AdditionalCost.OtherMaterial) return;
		form.setValue(
			`${basename}.additionalCostCategory` as FieldName,
			AdditionalCost.OtherMaterial,
		);
	}, [
		showAdditionalCostDropdown,
		isSafetyAndWelfareMaterial,
		additionalCostCategoryValue,
		form,
		basename,
	]);

	// Auto-calculate quantity values when dropdowns are selected
	useEffect(() => {
		const prev = prevDropdownState.current;
		const categoryRequiresProductionOrder =
			resolvedCategoryValue === MaterialsIncludedInContractRevenue.Maintain;
		const additionalRequiresProductionOrder =
			additionalCostCategoryValue === AdditionalCost.Material ||
			additionalCostCategoryValue === AdditionalCost.Maintain;
		const additionalRequiresOtherDetail =
			additionalCostCategoryValue === AdditionalCost.OtherMaterial;

		const hasCategoryActiveNow = Boolean(
			showCategoryDropdown &&
				resolvedCategoryValue &&
				categoryProcessGroupValue &&
				(!categoryRequiresProductionOrder || categoryProductionOrderId != null),
		);
		const hasAdditionalCostActiveNow = Boolean(
			showAdditionalCostDropdown &&
				additionalCostCategoryValue &&
				(!additionalRequiresProductionOrder ||
					additionalCostProductionOrderId != null) &&
				(!additionalRequiresOtherDetail || otherMaterialDetailValue != null),
		);
		const hasCategoryActiveBefore = Boolean(
			prev.showCategoryDropdown &&
				prev.category &&
				prev.categoryProcessGroup &&
				(prev.category !== MaterialsIncludedInContractRevenue.Maintain ||
					prev.categoryProductionOrderId != null),
		);
		const hasAdditionalCostActiveBefore = Boolean(
			prev.showAdditionalCostDropdown &&
				prev.additionalCostCategory &&
				((prev.additionalCostCategory !== AdditionalCost.Material &&
					prev.additionalCostCategory !== AdditionalCost.Maintain) ||
					prev.additionalCostProductionOrderId != null) &&
				(prev.additionalCostCategory !== AdditionalCost.OtherMaterial ||
					prev.otherMaterialDetail != null),
		);
		const categoryJustReady = !hasCategoryActiveBefore && hasCategoryActiveNow;
		const additionalCostJustSelected =
			!hasAdditionalCostActiveBefore && hasAdditionalCostActiveNow;
		// Check if contractLimit dropdown just got a value
		const contractLimitJustSelected =
			!prev.contractLimitCategory && contractLimitCategoryValue;
		// Check if asset checkbox just got enabled
		const assetJustEnabled = !prev.showAssetDropdown && showAssetDropdown;

		const exportedQty = Number(exportedQuantityWatch) || 0;

		if (assetJustEnabled && exportedQty > 0) {
			// Asset gets full exported quantity
			form.setValue(`${basename}.assetQuantity` as FieldName, exportedQty);
		} else if (contractLimitJustSelected && exportedQty > 0) {
			if (needsSecondComboBox) {
				const nextSelectedTypes =
					contractLimitSelectedTypes.length > 0
						? contractLimitSelectedTypes
						: [CONTRACT_LIMIT_SECONDARY_OPTIONS[0].value];
				const splitQty = exportedQty / nextSelectedTypes.length;
				const nextBreakdown: ContractLimitBreakdown = {};
				for (const type of nextSelectedTypes) {
					nextBreakdown[String(type)] = splitQty;
				}
				form.setValue(
					`${basename}.contractLimitSubCategories` as FieldName,
					nextSelectedTypes.map((type) => String(type)),
				);
				form.setValue(
					`${basename}.contractLimitBreakdown` as FieldName,
					nextBreakdown,
				);
				form.setValue(
					`${basename}.contractLimitQuantity` as FieldName,
					exportedQty,
				);
			} else {
				// Contract limit gets full exported quantity
				form.setValue(
					`${basename}.contractLimitQuantity` as FieldName,
					exportedQty,
				);
			}
		} else if (categoryJustReady || additionalCostJustSelected) {
			if (hasCategoryActiveNow && hasAdditionalCostActiveNow) {
				// Both category and additional cost are selected - split in half
				const halfQty = exportedQty / 2;
				form.setValue(`${basename}.categoryQuantity` as FieldName, halfQty);
				form.setValue(
					`${basename}.additionalCostQuantity` as FieldName,
					halfQty,
				);
			} else if (hasCategoryActiveNow && categoryJustReady) {
				// Only category is selected - full quantity
				form.setValue(`${basename}.categoryQuantity` as FieldName, exportedQty);
			} else if (hasAdditionalCostActiveNow && additionalCostJustSelected) {
				// Only additional cost is selected - full quantity
				form.setValue(
					`${basename}.additionalCostQuantity` as FieldName,
					exportedQty,
				);
			}
		}

		prevDropdownState.current = {
			category: categoryValue,
			categoryProcessGroup: categoryProcessGroupValue,
			categoryProductionOrderId,
			additionalCostCategory: additionalCostCategoryValue,
			additionalCostProductionOrderId,
			otherMaterialDetail: otherMaterialDetailValue,
			contractLimitCategory: contractLimitCategoryValue,
			showCategoryDropdown: showCategoryDropdown,
			showAdditionalCostDropdown: showAdditionalCostDropdown,
			showAssetDropdown: showAssetDropdown,
		};
	}, [
		categoryValue,
		resolvedCategoryValue,
		categoryProcessGroupValue,
		categoryProductionOrderId,
		additionalCostCategoryValue,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		contractLimitCategoryValue,
		exportedQuantityWatch,
		form,
		basename,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
	]);

	useEffect(() => {
		if (!showCategoryDropdown || !resolvedCategoryValue) return;
		if (processGroupOptions.length !== 1) return;
		if (categoryProcessGroupValue) return;

		form.setValue(
			`${basename}.categoryProcessGroup` as FieldName,
			processGroupOptions[0].value,
		);
	}, [
		showCategoryDropdown,
		resolvedCategoryValue,
		categoryProcessGroupValue,
		processGroupOptions,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showCategoryDropdown) return;
		if (categoryValue == null && defaultCategoryByType != null) {
			form.setValue(`${basename}.category` as FieldName, defaultCategoryByType);
			return;
		}

		if (resolvedCategoryValue !== MaterialsIncludedInContractRevenue.Maintain) {
			if (categoryProductionOrderId != null) {
				form.setValue(
					`${basename}.categoryProductionOrderId` as FieldName,
					null,
				);
			}
			return;
		}

		if (
			categoryOrderOrEquipmentOptions.length > 0 &&
			categoryProductionOrderId == null
		) {
			form.setValue(
				`${basename}.categoryProductionOrderId` as FieldName,
				categoryOrderOrEquipmentOptions[0].value,
			);
		}
	}, [
		showCategoryDropdown,
		categoryValue,
		defaultCategoryByType,
		resolvedCategoryValue,
		categoryProductionOrderId,
		categoryOrderOrEquipmentOptions,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showAdditionalCostDropdown) return;

		if (additionalCostNeedsProductionOrder) {
			const hasValidSelection =
				additionalCostProductionOrderId != null &&
				additionalCostOrderOrEquipmentOptions.some(
					(option) => option.value === additionalCostProductionOrderId,
				);
			if (!hasValidSelection) {
				form.setValue(
					`${basename}.additionalCostProductionOrderId` as FieldName,
					additionalCostOrderOrEquipmentOptions[0]?.value ?? null,
				);
			}
			if (otherMaterialDetailValue != null) {
				form.setValue(`${basename}.otherMaterialDetail` as FieldName, null);
			}
			return;
		}

		if (additionalCostNeedsOtherMaterialDetail) {
			if (additionalCostProductionOrderId != null) {
				form.setValue(
					`${basename}.additionalCostProductionOrderId` as FieldName,
					null,
				);
			}
			if (otherMaterialDetailValue == null) {
				form.setValue(
					`${basename}.otherMaterialDetail` as FieldName,
					DEFAULT_OTHER_MATERIAL_DETAIL_VALUE,
				);
			}
			return;
		}

		if (additionalCostProductionOrderId != null) {
			form.setValue(
				`${basename}.additionalCostProductionOrderId` as FieldName,
				null,
			);
		}
		if (otherMaterialDetailValue != null) {
			form.setValue(`${basename}.otherMaterialDetail` as FieldName, null);
		}
	}, [
		showAdditionalCostDropdown,
		additionalCostNeedsProductionOrder,
		additionalCostNeedsOtherMaterialDetail,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		additionalCostOrderOrEquipmentOptions,
		form,
		basename,
	]);

	// Reset contract limit detail fields when contractLimitCategory changes
	const prevContractLimitCategoryRef = useRef(contractLimitCategoryValue);
	useEffect(() => {
		if (prevContractLimitCategoryRef.current !== contractLimitCategoryValue) {
			form.setValue(`${basename}.contractLimitSubCategory` as FieldName, null);
			form.setValue(`${basename}.contractLimitSubCategories` as FieldName, []);
			form.setValue(`${basename}.contractLimitBreakdown` as FieldName, {});
			form.setValue(`${basename}.contractLimitQuantity` as FieldName, null);
			prevContractLimitSelectionSignatureRef.current = '';
			prevContractLimitCategoryRef.current = contractLimitCategoryValue;
		}
	}, [contractLimitCategoryValue, form, basename]);

	useEffect(() => {
		if (!showContractLimitDropdown || !contractLimitCategoryValue) return;
		if (needsSecondComboBox) return;

		const currentValue = form.getValues(
			`${basename}.contractLimitQuantity` as FieldName,
		);
		if (currentValue != null && currentValue !== '') return;

		form.setValue(
			`${basename}.contractLimitQuantity` as FieldName,
			Number(exportedQuantityWatch) || 0,
		);
	}, [
		showContractLimitDropdown,
		contractLimitCategoryValue,
		needsSecondComboBox,
		exportedQuantityWatch,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		const selectedKeys =
			contractLimitSubCategoriesValue &&
			contractLimitSubCategoriesValue.length > 0
				? contractLimitSubCategoriesValue
				: [];

		if (selectedKeys.length === 0) {
			if (!DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE) return;
			form.setValue(`${basename}.contractLimitSubCategories` as FieldName, [
				DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE,
			]);
			return;
		}

		const selectedSignature = [...selectedKeys].sort().join(',');
		if (prevContractLimitSelectionSignatureRef.current === selectedSignature) {
			return;
		}

		const exportedQty = Number(exportedQuantityWatch) || 0;
		const divided = exportedQty / selectedKeys.length;
		const nextBreakdown: ContractLimitBreakdown = {};
		for (const key of selectedKeys) {
			nextBreakdown[key] = divided;
		}
		form.setValue(
			`${basename}.contractLimitBreakdown` as FieldName,
			nextBreakdown,
		);
		prevContractLimitSelectionSignatureRef.current = selectedSignature;
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitSubCategoriesValue,
		exportedQuantityWatch,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		const currentContractLimitQuantity = Number(
			form.getValues(`${basename}.contractLimitQuantity` as FieldName) ?? 0,
		);
		if (
			Math.abs(currentContractLimitQuantity - contractLimitBreakdownTotal) <
			0.0001
		) {
			return;
		}
		form.setValue(
			`${basename}.contractLimitQuantity` as FieldName,
			contractLimitBreakdownTotal,
		);
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitBreakdownTotal,
		form,
		basename,
	]);

	const materialCode = form.watch(`${basename}.materialCode` as FieldName);
	const unit = form.watch(`${basename}.unit` as FieldName);
	const receivedQuantity = form.watch(
		`${basename}.receivedQuantity` as FieldName,
	);
	const watchedExportedQuantity = form.watch(
		`${basename}.exportedQuantity` as FieldName,
	);
	const categoryQuantity = form.watch(
		`${basename}.categoryQuantity` as FieldName,
	);
	const additionalCostQuantity = form.watch(
		`${basename}.additionalCostQuantity` as FieldName,
	);
	const contractLimitQuantity = form.watch(
		`${basename}.contractLimitQuantity` as FieldName,
	);
	const assetQuantity = form.watch(`${basename}.assetQuantity` as FieldName);

	// Calculate total and validation status
	const calculateTotal = () => {
		let total = 0;
		const hasCategoryActive =
			showCategoryDropdown &&
			resolvedCategoryValue &&
			categoryProcessGroupValue &&
			(!categoryNeedsProductionOrder || categoryProductionOrderId != null);
		const hasAdditionalCostActive =
			showAdditionalCostDropdown &&
			additionalCostCategoryValue &&
			(!additionalCostNeedsProductionOrder ||
				additionalCostProductionOrderId != null) &&
			(!additionalCostNeedsOtherMaterialDetail ||
				otherMaterialDetailValue != null);
		const hasContractLimitActive =
			showContractLimitDropdown &&
			contractLimitCategoryValue &&
			(!needsSecondComboBox || contractLimitSelectedTypes.length > 0);
		const hasAssetActive = showAssetDropdown;

		if (hasCategoryActive && categoryQuantity != null) {
			total += Number(categoryQuantity);
		}
		if (hasAdditionalCostActive && additionalCostQuantity != null) {
			total += Number(additionalCostQuantity);
		}
		if (hasContractLimitActive && contractLimitQuantity != null) {
			total += Number(contractLimitQuantity);
		}
		if (hasAssetActive && assetQuantity != null) {
			total += Number(assetQuantity);
		}

		return total;
	};

	const totalQuantity = calculateTotal();
	const exportedQty = Number(watchedExportedQuantity) || 0;
	const isValidTotal = Math.abs(totalQuantity - exportedQty) < 0.01;
	const activeReceivedKeys =
		receivedTypes && receivedTypes.length > 0
			? receivedTypes
			: [RECEIVED_TYPE_OPTIONS[0].value];
	const activeExportedKeys =
		exportedTypes && exportedTypes.length > 0
			? exportedTypes
			: [EXPORTED_TYPE_OPTIONS[0].value];
	const showReceivedBreakdown = activeReceivedKeys.length > 1;
	const showExportedBreakdown = activeExportedKeys.length > 1;
	const materialBadge = getMaterialBadge(materialTypeValue, itemTypeValue);

	const handleReceivedBreakdownChange = (key: string, value: number | string) =>
		form.setValue(`${basename}.receivedBreakdown` as FieldName, {
			...(receivedBreakdown ?? {}),
			[key]: value,
		});
	const handleExportedBreakdownChange = (key: string, value: number | string) =>
		form.setValue(`${basename}.exportedBreakdown` as FieldName, {
			...(exportedBreakdown ?? {}),
			[key]: value,
		});
	const handleContractLimitBreakdownChange = (
		key: string,
		value: number | string,
	) =>
		form.setValue(`${basename}.contractLimitBreakdown` as FieldName, {
			...(contractLimitBreakdown ?? {}),
			[key]: value,
		});
	const isContractLimitBreakdownValid =
		!needsSecondComboBox ||
		Math.abs(contractLimitBreakdownTotal - exportedQty) < 0.01;

	return (
		<TableRow
			className={cn(
				'transition-colors',
				materialTypeValue === MaterialType.Material
					? 'bg-blue-50/40 hover:bg-blue-50/70'
					: materialTypeValue === MaterialType.SparePart
						? 'bg-amber-50/40 hover:bg-amber-50/70'
						: 'hover:bg-slate-50/50',
			)}
		>
			<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
				{index + 1}
			</TableCell>
			<TableCell className='sticky left-16 z-20 w-[10%] min-w-32 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
				<div className='flex flex-col gap-1'>
					<Input
						readOnly
						value={materialCode || ''}
						className='border-slate-300 bg-slate-100 font-medium text-slate-500'
					/>
					<span className={materialBadge.className}>{materialBadge.label}</span>
				</div>
			</TableCell>
			<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
				<Input
					readOnly
					value={unit || ''}
					className='border-slate-300 bg-slate-100 text-slate-500'
				/>
			</TableCell>
			<TableCell className='border-b border-slate-200 px-4 py-4'>
				{(() => {
					const receivedQty = Number(receivedQuantity) || 0;
					const subTotal = activeReceivedKeys.reduce(
						(acc, key) => acc + (Number((receivedBreakdown ?? {})[key]) || 0),
						0,
					);
					const isReceivedValid =
						!showReceivedBreakdown || Math.abs(subTotal - receivedQty) < 0.01;

					return (
						<div className='flex flex-col gap-2'>
							<div className='flex items-end gap-2'>
								<div
									className={cn(
										'flex shrink-0 flex-col gap-0.5',
										showReceivedBreakdown ? 'w-24' : 'w-full',
									)}
								>
									<label className='text-[10px] font-medium text-slate-500'>
										Tổng
									</label>
									<Input
										readOnly
										value={receivedQty}
										className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
									/>
								</div>
								{showReceivedBreakdown && (
									<QuantityBreakdownInputs
										selectedKeys={activeReceivedKeys}
										allOptions={RECEIVED_TYPE_OPTIONS}
										values={receivedBreakdown ?? {}}
										onChange={handleReceivedBreakdownChange}
										isValid={isReceivedValid}
									/>
								)}
							</div>
							<FormMultiSelect
								control={form.control}
								name={`${basename}.receivedTypes` as FieldName}
								options={RECEIVED_TYPE_OPTIONS}
								placeholder='Chọn loại lĩnh'
							/>
							{showReceivedBreakdown && !isReceivedValid && (
								<p className='text-[11px] font-medium text-red-500'>
									Tổng các mục ({subTotal}) phải bằng số lượng lĩnh (
									{receivedQty})
								</p>
							)}
						</div>
					);
				})()}
			</TableCell>
			<TableCell className='border-b border-slate-200 px-4 py-4'>
				{(() => {
					const subTotal = activeExportedKeys.reduce(
						(acc, key) => acc + (Number((exportedBreakdown ?? {})[key]) || 0),
						0,
					);
					const isExportedValid =
						!showExportedBreakdown || Math.abs(subTotal - exportedQty) < 0.01;

					return (
						<div className='flex flex-col gap-2'>
							<div className='flex items-end gap-2'>
								<div
									className={cn(
										'flex shrink-0 flex-col gap-0.5',
										showExportedBreakdown ? 'w-24' : 'w-full',
									)}
								>
									<label className='text-[10px] font-medium text-slate-500'>
										Tổng
									</label>
									<Input
										readOnly
										value={exportedQty}
										className='pointer-events-none cursor-not-allowed! border-slate-300 bg-slate-100 text-center! text-slate-500!'
									/>
								</div>
								{showExportedBreakdown && (
									<QuantityBreakdownInputs
										selectedKeys={activeExportedKeys}
										allOptions={EXPORTED_TYPE_OPTIONS}
										values={exportedBreakdown ?? {}}
										onChange={handleExportedBreakdownChange}
										isValid={isExportedValid}
									/>
								)}
							</div>
							<FormMultiSelect
								control={form.control}
								name={`${basename}.exportedTypes` as FieldName}
								options={EXPORTED_TYPE_OPTIONS}
								placeholder='Chọn loại xuất'
							/>
							{showExportedBreakdown && !isExportedValid && (
								<p className='text-[11px] font-medium text-red-500'>
									Tổng các mục ({subTotal}) phải bằng số lượng xuất (
									{exportedQty})
								</p>
							)}
						</div>
					);
				})()}
			</TableCell>

			{/* Vật tư tính vào doanh thu khoán */}
			<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showCategoryDropdown` as FieldName}
						/>
					</div>
					{!showCategoryDropdown &&
						!showAdditionalCostDropdown &&
						!showContractLimitDropdown &&
						!showAssetDropdown &&
						form.formState.errors?.items?.[index]?.showCategoryDropdown && (
							<p className='text-xs text-red-600'>
								{
									form.formState.errors.items[index]?.showCategoryDropdown
										?.message
								}
							</p>
						)}
					{showCategoryDropdown && (
						<>
							{resolvedCategoryValue && (
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.categoryProcessGroup` as FieldName}
										options={processGroupOptions}
										placeholder='Chọn nhóm công đoạn'
									/>
								</div>
							)}
							{resolvedCategoryValue ===
								MaterialsIncludedInContractRevenue.Maintain && (
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.categoryProductionOrderId` as FieldName}
										options={categoryOrderOrEquipmentOptions}
										placeholder='Chọn quyết định, lệnh sản xuất'
									/>
								</div>
							)}
							{resolvedCategoryValue &&
								categoryProcessGroupValue &&
								(!categoryNeedsProductionOrder ||
									categoryProductionOrderId != null) && (
									<div className='w-full'>
										<label className='mb-1.5 block text-xs font-medium text-slate-600'>
											Số lượng vật tư
										</label>
										<FormNumber
											control={form.control}
											name={`${basename}.categoryQuantity` as FieldName}
											placeholder='Nhập số lượng'
										/>
										<div
											className={cn(
												'mt-1 text-xs',
												isValidTotal ? 'text-green-600' : 'text-red-600',
											)}
										>
											Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
										</div>
									</div>
								)}
						</>
					)}
				</div>
			</TableCell>

			{/* Bổ sung chi phí */}
			<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showAdditionalCostDropdown` as FieldName}
						/>
					</div>
					{showAdditionalCostDropdown && (
						<>
							{!isSafetyAndWelfareMaterial && (
								<div className='w-full'>
									<FormComboBox
										control={form.control}
										name={`${basename}.additionalCostCategory` as FieldName}
										options={additionalCostOptionsByType}
										placeholder='Chọn danh mục'
									/>
								</div>
							)}
							{additionalCostCategoryValue && (
								<>
									{additionalCostNeedsProductionOrder && (
										<div className='w-full'>
											<FormComboBox
												control={form.control}
												name={
													`${basename}.additionalCostProductionOrderId` as FieldName
												}
												options={additionalCostOrderOrEquipmentOptions}
												placeholder='Chọn quyết định, lệnh sản xuất'
											/>
										</div>
									)}
									{additionalCostNeedsOtherMaterialDetail && (
										<div className='w-full'>
											<FormComboBox
												control={form.control}
												name={`${basename}.otherMaterialDetail` as FieldName}
												options={OTHER_MATERIAL_DETAIL_OPTIONS}
												placeholder='Chọn loại vật tư'
											/>
										</div>
									)}
									{(!additionalCostNeedsProductionOrder ||
										additionalCostProductionOrderId != null) &&
										(!additionalCostNeedsOtherMaterialDetail ||
											otherMaterialDetailValue != null) && (
											<div className='w-full'>
												<label className='mb-1.5 block text-xs font-medium text-slate-600'>
													Số lượng vật tư
												</label>
												<FormNumber
													control={form.control}
													name={
														`${basename}.additionalCostQuantity` as FieldName
													}
													placeholder='Nhập số lượng'
												/>
												<div
													className={cn(
														'mt-1 text-xs',
														isValidTotal ? 'text-green-600' : 'text-red-600',
													)}
												>
													Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
												</div>
											</div>
										)}
								</>
							)}
						</>
					)}
				</div>
			</TableCell>

			{/* Vật tư theo hạn mức */}
			<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showContractLimitDropdown` as FieldName}
						/>
					</div>
					{showContractLimitDropdown && (
						<>
							<div className='w-full'>
								<FormComboBox
									control={form.control}
									name={`${basename}.contractLimitCategory` as FieldName}
									options={CONTRACT_LIMIT_OPTIONS}
									placeholder='Chọn danh mục'
								/>
							</div>
							{contractLimitCategoryValue && needsSecondComboBox && (
								<>
									<div className='w-full'>
										<FormMultiSelect
											control={form.control}
											name={
												`${basename}.contractLimitSubCategories` as FieldName
											}
											options={CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS}
											placeholder='Chọn loại (Lĩnh mới/Tái sử dụng)'
										/>
									</div>
									{contractLimitSelectedTypes.length > 0 && (
										<div className='w-full space-y-2'>
											<div className='flex w-full items-end gap-2'>
												<QuantityBreakdownInputs
													selectedKeys={contractLimitSelectedKeys}
													allOptions={CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS}
													values={contractLimitBreakdown ?? {}}
													onChange={handleContractLimitBreakdownChange}
													isValid={isContractLimitBreakdownValid}
													equalWidth
												/>
											</div>
											{!isContractLimitBreakdownValid && (
												<p className='text-xs text-red-600'>
													Tổng lĩnh mới + lĩnh tái sử dụng phải bằng số lượng
													xuất ({contractLimitBreakdownTotal} / {exportedQty})
												</p>
											)}
										</div>
									)}
								</>
							)}
							{contractLimitCategoryValue &&
								(!needsSecondComboBox ||
									contractLimitSelectedTypes.length > 0) && (
									<div className='w-full'>
										{!needsSecondComboBox && (
											<>
												<label className='mb-1.5 block text-xs font-medium text-slate-600'>
													Số lượng vật tư
												</label>
												<FormNumber
													control={form.control}
													name={
														`${basename}.contractLimitQuantity` as FieldName
													}
													placeholder='Nhập số lượng'
												/>
											</>
										)}
										<div
											className={cn(
												'mt-1 text-xs',
												isValidTotal ? 'text-green-600' : 'text-red-600',
											)}
										>
											Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
										</div>
									</div>
								)}
						</>
					)}
				</div>
			</TableCell>

			{/* Tài sản */}
			<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
				<div className='flex flex-col items-center gap-3'>
					<div className='flex justify-center *:w-auto!'>
						<FormCheckBox
							control={form.control}
							name={`${basename}.showAssetDropdown` as FieldName}
						/>
					</div>
					{showAssetDropdown && (
						<div className='w-full'>
							<label className='mb-1.5 block text-xs font-medium text-slate-600'>
								Số lượng
							</label>
							<FormNumber
								control={form.control}
								name={`${basename}.assetQuantity` as FieldName}
								placeholder='Nhập số lượng'
							/>
							<div
								className={cn(
									'mt-1 text-xs',
									isValidTotal ? 'text-green-600' : 'text-red-600',
								)}
							>
								Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
							</div>
						</div>
					)}
				</div>
			</TableCell>
		</TableRow>
	);
}
