import type {
	AcceptanceReportDetail,
	AcceptanceReportItem,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import {
	Asset,
	AdditionalCost,
	CreateAcceptanceReportRequest,
	ImportResolutionStatus,
	ItemType,
	MaterialType,
	OtherMaterialDetail,
	QuotaBasedMaterial,
	QuotaBasedMaterialType,
	QuantityDetail,
	ISSUED_DETAIL_TYPE_BY_KEY,
	SHIPPED_DETAIL_TYPE_BY_KEY,
	EXPORTED_TYPE_OPTIONS,
	RECEIVED_TYPE_OPTIONS,
	MaterialsIncludedInContractRevenue,
	type AcceptanceReportItemDto,
	type AcceptanceReportEditorMode,
	type ImportedItemMeta,
	type QuotaBasedMaterialQuantityDetail,
	type UpdateAcceptanceReportRequest,
	type UnresolvedAcceptanceReportItemDto,
} from './types';
import type {
	AcceptanceReportEditorFormSchema,
	AcceptanceReportEditorRow,
} from './schema';
import {
	DEFAULT_OTHER_MATERIAL_DETAIL_VALUE,
	buildCategoryAllocationsForPayload,
	getDefaultAdditionalCostByMaterialType,
	getDefaultCategoryByMaterialType,
	mapIssuedDetailsToBreakdown,
	mapShippedDetailsToBreakdown,
	normalizeProductionOrderId,
	parseQuantity,
	resolveSelectionIds,
} from './helpers';
import { MATERIAL_FORM_DEFAULT } from './schema';

export function mapResolvedImportItem(
	item: AcceptanceReportItemDto,
): AcceptanceReportEditorRow {
	const isSafetyAndWelfareMaterial =
		item.type === MaterialType.Material &&
		item.itemType === ItemType.SafetyAndWelfare;
	const isAssetMaterial =
		item.type === MaterialType.Material && item.itemType === ItemType.Resource;
	const isQuotaMaterial =
		item.type === MaterialType.Material &&
		item.itemType === ItemType.QuotaMaterials;
	const defaultCategory = getDefaultCategoryByMaterialType(item.type);
	const defaultAdditionalCost = isSafetyAndWelfareMaterial
		? AdditionalCost.OtherMaterial
		: getDefaultAdditionalCostByMaterialType(item.type);

	return {
		...MATERIAL_FORM_DEFAULT,
		id: item.partId ?? item.materialId ?? '',
		acceptanceReportItemId: item.reportItemId || undefined,
		materialOrPartId: item.partId ?? item.materialId ?? '',
		resolutionStatus: ImportResolutionStatus.Resolved,
		sourceRowNumber: item.rowNumber ?? null,
		partType: item.partType ?? null,
		materialCode: item.materialCode,
		materialName:
			item.type === MaterialType.Material
				? (item.materialName ?? '')
				: (item.partName ?? ''),
		unitOfMeasureName: item.unitOfMeasureName,
		type: item.type,
		itemType: item.itemType,
		quantityReceived: item.issuedQuantity,
		quantityExported: item.shippedQuantity,
		receivedTypes: [RECEIVED_TYPE_OPTIONS[0].value],
		exportedTypes: [EXPORTED_TYPE_OPTIONS[0].value],
		quantity: item.issuedQuantity + item.shippedQuantity,
		category: defaultCategory,
		categoryProcessGroupIds: [],
		categoryEquipmentIds: [],
		categoryAllocations: [],
		additionalCostCategory: defaultAdditionalCost,
		showAdditionalCostDropdown: isSafetyAndWelfareMaterial,
		showAssetDropdown: isAssetMaterial,
		showContractLimitDropdown: isQuotaMaterial,
	};
}

export function mapUnresolvedImportItem(
	item: UnresolvedAcceptanceReportItemDto,
): AcceptanceReportEditorRow {
	return {
		...MATERIAL_FORM_DEFAULT,
		id: `unresolved:${item.rowNumber}:${item.materialCode}`,
		acceptanceReportItemId: item.reportItemId || undefined,
		materialOrPartId: undefined,
		resolutionStatus: ImportResolutionStatus.Unresolved,
		unresolvedReason: item.unresolvedReason,
		sourceRowNumber: item.rowNumber,
		materialCode: item.materialCode,
		materialName: item.materialName ?? '',
		unitOfMeasureName: item.unitOfMeasureName,
		quantityReceived: item.issuedQuantity,
		quantityExported: item.shippedQuantity,
		quantity: item.issuedQuantity + item.shippedQuantity,
	};
}

export function mapRawAcceptanceItemToEditorRow(
	item: AcceptanceReportItem,
): AcceptanceReportEditorRow {
	const showCategoryDropdown =
		item.materialsIncludedInContractRevenue !==
			MaterialsIncludedInContractRevenue.None &&
		item.materialsIncludedInContractRevenue !== 0;
	const showAdditionalCostDropdown =
		item.additionalCost !== AdditionalCost.None && item.additionalCost !== 0;
	const showContractLimitDropdown =
		item.quotaBasedMaterial !== QuotaBasedMaterial.None &&
		item.quotaBasedMaterial !== 0;
	const showAssetDropdown = item.asset !== Asset.None && item.asset !== 0;

	const issuedBreakdown = mapIssuedDetailsToBreakdown(item.issuedDetails);
	const shippedBreakdown = mapShippedDetailsToBreakdown(item.shippedDetails);
	const receivedQuantity =
		item.issuedDetails && item.issuedDetails.length > 0
			? issuedBreakdown.total
			: item.issuedQuantity || 0;
	const exportedQuantity =
		item.shippedDetails && item.shippedDetails.length > 0
			? shippedBreakdown.total
			: item.shippedQuantity || 0;
	const quotaBasedMaterialQuantities = item.quotaBasedMaterialQuantities ?? [];
	const contractLimitSubCategories =
		showContractLimitDropdown && quotaBasedMaterialQuantities.length > 0
			? quotaBasedMaterialQuantities.map(
					(detail: QuotaBasedMaterialQuantityDetail) => String(detail.type),
				)
			: showContractLimitDropdown && item.quotaBasedMaterialType
				? [String(item.quotaBasedMaterialType)]
				: [];
	const contractLimitBreakdown = quotaBasedMaterialQuantities.reduce(
		(
			acc: Record<string, number | string>,
			detail: QuotaBasedMaterialQuantityDetail,
		) => {
			acc[String(detail.type)] = detail.quantity ?? 0;
			return acc;
		},
		{} as Record<string, number | string>,
	);
	const contractLimitQuantityFromDetails =
		quotaBasedMaterialQuantities.length > 0
			? quotaBasedMaterialQuantities.reduce(
					(acc: number, detail: QuotaBasedMaterialQuantityDetail) =>
						acc + (Number(detail.quantity) || 0),
					0,
				)
			: item.quotaBasedMaterialQuantity || 0;
	const categoryAllocations = (item.categoryAllocations ?? []).map(
		(allocation: {
			processGroupId: string | null;
			quantity: number | null;
			equipmentIds: string[];
		}) => ({
			processGroupId: allocation.processGroupId ?? null,
			quantity: allocation.quantity ?? 0,
			equipmentIds: allocation.equipmentIds ?? [],
		}),
	);

	return {
		...MATERIAL_FORM_DEFAULT,
		id: item.id,
		usageTime: item.usageTime ?? 0,
		acceptanceReportItemId: item.id,
		materialOrPartId: item.partId ?? item.materialId ?? undefined,
		partType: item.partType ?? null,
		materialCode:
			item.type === MaterialType.Material
				? (item.materialCode ?? '')
				: (item.partCode ?? ''),
		materialName:
			item.type === MaterialType.Material
				? (item.materialName ?? '')
				: (item.partName ?? ''),
		unitOfMeasureName: item.unitOfMeasureName ?? '',
		type: item.type,
		itemType: item.itemType,
		quantityReceived: receivedQuantity,
		quantityExported: exportedQuantity,
		receivedTypes: issuedBreakdown.selectedKeys,
		exportedTypes: shippedBreakdown.selectedKeys,
		receivedBreakdown: issuedBreakdown.breakdown,
		exportedBreakdown: shippedBreakdown.breakdown,
		quantity: receivedQuantity + exportedQuantity,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
		category:
			showCategoryDropdown
				? item.materialsIncludedInContractRevenue || null
				: null,
		categoryProcessGroup: item.processGroupId ?? null,
		categoryProcessGroupIds: categoryAllocations.map(
			(allocation: { processGroupId: string | null }) =>
				allocation.processGroupId ?? '',
		).filter(Boolean),
		categoryProductionOrderId: item.categoryProductionOrderId ?? null,
		categoryEquipmentId: item.categoryEquipmentId ?? null,
		categoryEquipmentIds: categoryAllocations.map(
			(allocation: { equipmentIds: string[] }) =>
				allocation.equipmentIds?.[0] ?? '',
		).filter(Boolean),
		categoryAllocations,
		categoryQuantity:
			showCategoryDropdown
				? (item.materialsIncludedInContractRevenueQuantity ?? 0)
				: null,
		additionalCostCategory:
			showAdditionalCostDropdown ? item.additionalCost || null : null,
		additionalCostProductionOrderId:
			item.additionalCostEquipmentId != null
				? item.additionalCostEquipmentId
				: (item.additionalCostProductionOrderId ?? null),
		otherMaterialDetail:
			showAdditionalCostDropdown &&
			item.additionalCost === AdditionalCost.OtherMaterial
				? (item.otherMaterialDetail ?? DEFAULT_OTHER_MATERIAL_DETAIL_VALUE)
				: null,
		additionalCostQuantity:
			showAdditionalCostDropdown ? (item.additionalCostQuantity ?? 0) : null,
		contractLimitCategory:
			showContractLimitDropdown ? item.quotaBasedMaterial || null : null,
		contractLimitSubCategory:
			contractLimitSubCategories.length > 0
				? Number(contractLimitSubCategories[0])
				: null,
		contractLimitSubCategories,
		contractLimitBreakdown,
		contractLimitQuantity:
			showContractLimitDropdown ? contractLimitQuantityFromDetails : null,
		assetCategory: showAssetDropdown ? Asset.True : null,
		assetQuantity: showAssetDropdown ? (item.assetMaterialQuantity ?? 0) : null,
		resolutionStatus: ImportResolutionStatus.Resolved,
	};
}

export function mapAcceptanceReportDetailToEditorForm(
	detail: AcceptanceReportDetail,
): AcceptanceReportEditorFormSchema {
	return {
		materials: detail.items.map(mapRawAcceptanceItemToEditorRow),
	};
}

type BuildRequestContext = {
	filePath: string;
	productionOutputId?: string;
	reportId?: string;
};

function buildQuotaBasedMaterialPayload(
	item: AcceptanceReportEditorRow,
): {
	quotaBasedMaterial: number;
	quotaBasedMaterialType: number;
	quotaBasedMaterialQuantity: number;
	quotaBasedMaterialQuantities: QuotaBasedMaterialQuantityDetail[] | null;
} {
	let quotaBasedMaterial: number = QuotaBasedMaterial.None;
	let quotaBasedMaterialType: number = QuotaBasedMaterialType.New;
	let quotaBasedMaterialQuantities: QuotaBasedMaterialQuantityDetail[] | null =
		null;

	if (item.showContractLimitDropdown && item.contractLimitCategory) {
		quotaBasedMaterial = item.contractLimitCategory;
		const selectedSubCategories =
			item.contractLimitSubCategories && item.contractLimitSubCategories.length > 0
				? item.contractLimitSubCategories.map((type) => Number(type))
				: item.contractLimitSubCategory != null
					? [item.contractLimitSubCategory]
					: [];

		quotaBasedMaterialType =
			selectedSubCategories[0] ?? QuotaBasedMaterialType.New;
		if (selectedSubCategories.length > 0) {
			quotaBasedMaterialQuantities = selectedSubCategories.map((type) => ({
				type,
				quantity: parseQuantity(item.contractLimitBreakdown?.[String(type)]),
			}));
		} else if (item.contractLimitQuantity != null) {
			quotaBasedMaterialQuantities = [
				{
					type: quotaBasedMaterialType,
					quantity: parseQuantity(item.contractLimitQuantity),
				},
			];
		}
	}

	return {
		quotaBasedMaterial,
		quotaBasedMaterialType,
		quotaBasedMaterialQuantity: item.contractLimitQuantity || 0,
		quotaBasedMaterialQuantities,
	};
}

function buildIssuedDetails(item: AcceptanceReportEditorRow): QuantityDetail[] {
	const receivedTypes =
		item.receivedTypes && item.receivedTypes.length > 0
			? item.receivedTypes
			: [RECEIVED_TYPE_OPTIONS[0].value];

	return receivedTypes.flatMap((key) => {
		const detailType =
			ISSUED_DETAIL_TYPE_BY_KEY[key as keyof typeof ISSUED_DETAIL_TYPE_BY_KEY];
		if (!detailType) return [];

		const quantity =
			receivedTypes.length > 1
				? parseQuantity(item.receivedBreakdown?.[key])
				: parseQuantity(item.quantityReceived);

		return [{ type: detailType, quantity }];
	});
}

function buildShippedDetails(item: AcceptanceReportEditorRow): QuantityDetail[] {
	const exportedTypes =
		item.exportedTypes && item.exportedTypes.length > 0
			? item.exportedTypes
			: [EXPORTED_TYPE_OPTIONS[0].value];

	return exportedTypes.flatMap((key) => {
		const detailType =
			SHIPPED_DETAIL_TYPE_BY_KEY[
				key as keyof typeof SHIPPED_DETAIL_TYPE_BY_KEY
			];
		if (!detailType) return [];

		const quantity =
			exportedTypes.length > 1
				? parseQuantity(item.exportedBreakdown?.[key])
				: parseQuantity(item.quantityExported);

		return [{ type: detailType, quantity }];
	});
}

function buildBasePayload(item: AcceptanceReportEditorRow) {
	const resolvedCategory =
		item.category ?? getDefaultCategoryByMaterialType(item.type);
	const materialsIncludedInContractRevenue =
		item.showCategoryDropdown && resolvedCategory
			? resolvedCategory
			: MaterialsIncludedInContractRevenue.None;
	const categoryAllocations = buildCategoryAllocationsForPayload(
		item.categoryAllocations,
		materialsIncludedInContractRevenue,
	);
	const firstCategoryAllocation = categoryAllocations?.[0] ?? null;
	const processGroupId =
		item.showCategoryDropdown &&
		resolvedCategory &&
		item.type === MaterialType.SparePart
			? (firstCategoryAllocation?.processGroupId ?? item.categoryProcessGroup) ||
				null
			: null;
	const isSafetyAndWelfareMaterial =
		item.type === MaterialType.Material &&
		item.itemType === ItemType.SafetyAndWelfare;
	const resolvedAdditionalCostCategory =
		item.showAdditionalCostDropdown && isSafetyAndWelfareMaterial
			? AdditionalCost.OtherMaterial
			: item.additionalCostCategory;
	const additionalCost =
		item.showAdditionalCostDropdown && resolvedAdditionalCostCategory
			? resolvedAdditionalCostCategory
			: AdditionalCost.None;
	const otherMaterialDetail =
		item.showAdditionalCostDropdown &&
		resolvedAdditionalCostCategory === AdditionalCost.OtherMaterial
			? (item.otherMaterialDetail ?? OtherMaterialDetail.None)
			: OtherMaterialDetail.None;
	const categoryProductionOrderId =
		item.showCategoryDropdown &&
		resolvedCategory === MaterialsIncludedInContractRevenue.Maintain
			? normalizeProductionOrderId(item.categoryProductionOrderId)
			: null;
	const categoryEquipmentId =
		item.showCategoryDropdown &&
		resolvedCategory === MaterialsIncludedInContractRevenue.Maintain
			? (firstCategoryAllocation?.equipmentIds?.[0] ??
				item.categoryEquipmentId ??
				null)
			: null;
	const additionalSelection =
		item.showAdditionalCostDropdown &&
		(resolvedAdditionalCostCategory === AdditionalCost.Material ||
			resolvedAdditionalCostCategory === AdditionalCost.Maintain)
			? resolveSelectionIds(item.additionalCostProductionOrderId)
			: {
					productionOrderId: null,
					equipmentId: null,
				};
	const quotaPayload = buildQuotaBasedMaterialPayload(item);

	return {
		item,
		categoryAllocations,
		processGroupId,
		additionalCost,
		otherMaterialDetail,
		categoryProductionOrderId,
		categoryEquipmentId,
		additionalSelection,
		materialsIncludedInContractRevenue,
		quotaPayload,
		issuedDetails: buildIssuedDetails(item),
		shippedDetails: buildShippedDetails(item),
	};
}

export function buildAcceptanceReportRequest(
	mode: AcceptanceReportEditorMode,
	values: AcceptanceReportEditorFormSchema,
	context: BuildRequestContext,
): CreateAcceptanceReportRequest | UpdateAcceptanceReportRequest {
	if (mode === 'import') {
		return {
			productionOutputId: context.productionOutputId ?? '',
			filePath: context.filePath ?? '',
			items: values.materials.map((item) => {
				const base = buildBasePayload(item);
				return {
					acceptanceReportItemId: item.acceptanceReportItemId || null,
					materialId:
						item.type === MaterialType.Material
							? item.materialOrPartId || null
							: null,
					partId:
						item.type === MaterialType.SparePart
							? item.materialOrPartId || null
							: null,
					usageTime: item.usageTime ?? 0,
					type: item.type || MaterialType.Material,
					itemType: item.itemType || ItemType.InContract,
					categoryProductionOrderId: base.categoryProductionOrderId,
					categoryEquipmentId: base.categoryEquipmentId,
					additionalCostProductionOrderId:
						base.additionalSelection.productionOrderId,
					additionalCostEquipmentId: base.additionalSelection.equipmentId,
					issuedDetails: base.issuedDetails,
					shippedDetails: base.shippedDetails,
					materialsIncludedInContractRevenue:
						base.materialsIncludedInContractRevenue,
					categoryAllocations: base.categoryAllocations,
					processGroupId: base.processGroupId,
					materialsIncludedInContractRevenueQuantity:
						item.categoryQuantity || 0,
					additionalCost: base.additionalCost,
					otherMaterialDetail: base.otherMaterialDetail,
					additionalCostQuantity: item.additionalCostQuantity || 0,
					quotaBasedMaterial: base.quotaPayload.quotaBasedMaterial,
					quotaBasedMaterialType: base.quotaPayload.quotaBasedMaterialType,
					quotaBasedMaterialQuantity:
						base.quotaPayload.quotaBasedMaterialQuantity,
					quotaBasedMaterialQuantities:
						base.quotaPayload.quotaBasedMaterialQuantities,
					asset: item.showAssetDropdown ? Asset.True : Asset.None,
					assetMaterialQuantity: item.assetQuantity || 0,
				};
			}),
		};
	}

	return {
		id: context.reportId ?? '',
		filePath: context.filePath ?? '',
		items: values.materials.map((item) => {
			const base = buildBasePayload(item);
			return {
				id: item.id || '',
				usageTime: item.usageTime ?? 0,
				itemType: item.itemType ?? 0,
				categoryAllocations: base.categoryAllocations,
				categoryProductionOrderId: base.categoryProductionOrderId,
				categoryEquipmentId: base.categoryEquipmentId,
				additionalCostProductionOrderId:
					base.additionalSelection.productionOrderId,
				additionalCostEquipmentId: base.additionalSelection.equipmentId,
				issuedQuantity: parseQuantity(item.quantityReceived),
				shippedQuantity: parseQuantity(item.quantityExported),
				issuedDetails: base.issuedDetails,
				shippedDetails: base.shippedDetails,
				materialsIncludedInContractRevenue:
					base.materialsIncludedInContractRevenue,
				processGroupId: base.processGroupId,
				materialsIncludedInContractRevenueQuantity: item.categoryQuantity || 0,
				additionalCost: base.additionalCost,
				otherMaterialDetail: base.otherMaterialDetail,
				additionalCostQuantity: item.additionalCostQuantity || 0,
				quotaBasedMaterial: base.quotaPayload.quotaBasedMaterial,
				quotaBasedMaterialType: base.quotaPayload.quotaBasedMaterialType,
				quotaBasedMaterialQuantities:
					base.quotaPayload.quotaBasedMaterialQuantities,
				asset: item.showAssetDropdown ? Asset.True : Asset.None,
				assetMaterialQuantity: item.assetQuantity || 0,
			};
		}),
	};
}

export function extractImportedItems(
	rows: AcceptanceReportEditorRow[],
): ImportedItemMeta[] {
	return rows
		.filter((item) => Boolean(item.materialOrPartId))
		.map((item) => ({
			materialOrPartId: item.materialOrPartId ?? '',
			type: item.type ?? MaterialType.Material,
		}));
}
