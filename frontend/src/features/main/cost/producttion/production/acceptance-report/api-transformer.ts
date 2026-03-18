/**
 * Transformer to convert API response to HierarchicalAcceptanceReport format
 */
import {
	MaterialDetailDto,
	MaterialGroupDto,
	ProductionOutputDetailItemDto,
	ProductionOutputDto,
} from './api-types';
import {
	AssetItem,
	CategoryGroup,
	FinancialFields,
	GroupCodeGroup,
	HierarchicalAcceptanceReport,
	MaterialItem,
	QuotaMaterialItem,
	TypeGroup,
	UnifiedItem,
} from './types';

/**
 * Transform material detail from API to UnifiedItem format
 */
function transformMaterialDetail(
	material: MaterialDetailDto,
	groupCode: string,
	groupName: string,
	categoryType: number,
): UnifiedItem {
	// Base financial fields
	const financialFields: Partial<FinancialFields> = {
		priceKH: material.plannedUnitPrice,
		priceTT: material.actualUnitPrice,

		// Tồn đầu kỳ - Tổng cộng
		openingBalanceTotalQty: material.beginningInventory?.total?.quantity ?? 0,
		openingBalanceTotalAmount: material.beginningInventory?.total?.amount ?? 0,

		// Tồn đầu kỳ - Chi phí chờ hạch toán (pending)
		openingBalancePendingQty: 0,
		openingBalancePendingAmount: material.beginningInventory?.pendingValue ?? 0,

		// Lĩnh trong kỳ - Tổng cộng
		receiptTotalQty: material.issuedInPeriod.total.quantity,
		receiptTotalAmountKH: material.issuedInPeriod.received.plannedAmount,

		// Lĩnh trong kỳ - Lĩnh vật tư đã trả phiếu
		receiptWithReceiptQty: material.issuedInPeriod.received.quantity,
		receiptWithReceiptAmountKH: material.issuedInPeriod.received.plannedAmount,
		receiptWithReceiptAmountTT: material.issuedInPeriod.received.actualAmount,

		// Xuất trong kỳ - Xuất cho sản xuất
		issueForProductionQty:
			material.exportedInPeriod.exportedToProduction.quantity,
		issueForProductionAmount:
			material.exportedInPeriod.exportedToProduction.amount,

		// Xuất trong kỳ - Chi phí vật tư dài kỳ
		issueLongtermQty: material.exportedInPeriod.longTermExpense?.amount ? 1 : 0,
		issueLongtermAmount: material.exportedInPeriod.longTermExpense?.amount ?? 0,

		// Xuất trong kỳ - Tổng cộng
		issueTotalQty: material.exportedInPeriod.total.quantity,
		issueTotalAmount: material.exportedInPeriod.total.amount,

		// Tồn cuối kỳ - Tổng cộng
		closingBalanceTotalQty: material.endingInventory?.total?.quantity ?? 0,
		closingBalanceTotalAmount: material.endingInventory?.total?.amount ?? 0,
	};

	// Create unified item based on category type
	switch (categoryType) {
		case 1: // Vật tư tính vào doanh thu khoán
		case 2: // Bổ sung chi phí
			return {
				id: material.materialId,
				assignmentCode: groupCode,
				groupCode,
				groupName,
				materialCode: material.materialCode,
				materialName: material.materialName,
				unit: material.unitOfMeasureName,
				...financialFields,
			} as MaterialItem;

		case 3: // Vật tư theo hạn mức (quota materials)
			return {
				id: material.materialId,
				assignmentCode: groupCode,
				groupCode,
				groupName,
				materialCode: material.materialCode,
				materialName: material.materialName,
				unit: material.unitOfMeasureName,
				classification: (material.state as 'NEW' | 'REUSE' | null) || null,
				...financialFields,
			} as QuotaMaterialItem;

		case 4: // Tài sản (assets)
			return {
				id: material.materialId,
				assetGroup: groupCode,
				groupCode,
				groupName,
				assetCode: material.materialCode,
				assetName: material.materialName,
				unit: material.unitOfMeasureName,
				...financialFields,
			} as AssetItem;

		default:
			return {
				id: material.materialId,
				assignmentCode: groupCode,
				groupCode,
				groupName,
				materialCode: material.materialCode,
				materialName: material.materialName,
				unit: material.unitOfMeasureName,
				...financialFields,
			} as MaterialItem;
	}
}

/**
 * Group materials by materialType and groupCode
 */
function groupMaterials(
	materialGroup: MaterialGroupDto,
	categoryType: number,
): GroupCodeGroup[] {
	const result: GroupCodeGroup[] = [];

	// Handle subGroups if they exist and have materials
	if (materialGroup.subGroups && materialGroup.subGroups.length > 0) {
		materialGroup.subGroups.forEach((subGroup) => {
			if (subGroup.materials && subGroup.materials.length > 0) {
				result.push({
					groupCode: subGroup.subGroupCode,
					groupName: subGroup.subGroupName,
					items: subGroup.materials.map((m) =>
						transformMaterialDetail(
							m,
							subGroup.subGroupCode,
							subGroup.subGroupName,
							categoryType,
						),
					),
					showTotals: true, // SubGroups show totals like type rows
				});
			}
		});
	}

	// Handle top-level materials (materials not in subGroups)
	if (materialGroup.materials && materialGroup.materials.length > 0) {
		result.push({
			groupCode: materialGroup.groupCode,
			groupName: materialGroup.groupName,
			items: materialGroup.materials.map((m) =>
				transformMaterialDetail(
					m,
					materialGroup.groupCode,
					materialGroup.groupName,
					categoryType,
				),
			),
		});
	}

	return result;
}

/**
 * Determine material type ("Vật liệu", "SCTX", etc.) from materialType field
 */
function getMaterialType(materialType: string): string {
	if (materialType && materialType.trim()) {
		return materialType;
	}
	// Fallback to default
	return 'Vật liệu';
}

/**
 * Create material type groups for category
 */
function createMaterialTypeGroups(
	categoryItem: ProductionOutputDetailItemDto,
	categoryType: number,
): TypeGroup[] {
	const typeGroups = new Map<string, GroupCodeGroup[]>();

	// Group materials by materialType
	categoryItem.materialGroups.forEach((matGroup) => {
		const matType = getMaterialType(matGroup.materialType);

		if (!typeGroups.has(matType)) {
			typeGroups.set(matType, []);
		}

		const grouped = groupMaterials(matGroup, categoryType);
		typeGroups.get(matType)!.push(...grouped);
	});

	return Array.from(typeGroups.entries()).map(([typeName, groups]) => ({
		typeName,
		groups,
	}));
}

/**
 * Transform API response to HierarchicalAcceptanceReport
 */
export function transformApiResponseToHierarchical(
	apiResponse: ProductionOutputDto,
): HierarchicalAcceptanceReport {
	const categories: CategoryGroup[] = (apiResponse.items || []).map((item) => ({
		categoryName: item.categoryName,
		types: createMaterialTypeGroups(item, item.categoryType),
	}));

	return {
		id: apiResponse.productionOutputId,
		categories,
	};
}
