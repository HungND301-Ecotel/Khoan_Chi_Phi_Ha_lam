/**
 * Transformer to convert API response to HierarchicalAcceptanceReport format
 */
import {
	AcceptanceReportSectionKey,
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

const ADDITIONAL_COST_TYPE_LABELS: Record<number, string> = {
	2: 'Vật liệu',
	3: 'Sửa chữa thường xuyên',
	4: 'Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão',
};

const SECTION_A_TYPE_LABELS: Record<number, string> = {
	1: 'Vật liệu',
	2: 'Chi phí sửa chữa thường xuyên (Các loại vật tư SCTX theo kế hoạch vật tư)',
	3: 'Chi phí sửa chữa thường xuyên dài kỳ phân bổ',
};

const OTHER_MATERIAL_DETAIL_LABELS_BY_NUMBER: Record<number, string> = {
	2: 'Bảo hộ lao động',
	3: 'Vật tư phục vụ công tác an toàn',
};

const OTHER_MATERIAL_DETAIL_LABELS_BY_KEY: Record<string, string> = {
	BaoHoLaoDong: 'Bảo hộ lao động',
	VatTuPhucVuCongTacAnToan: 'Vật tư phục vụ công tác an toàn',
};

const SUB_GROUP_NAME_BY_CODE: Record<string, string> = {
	New: 'Lĩnh mới',
	Reusable: 'Lĩnh tái sử dụng',
};

function trimText(value?: string | null): string {
	return (value ?? '').trim();
}

function normalizeForCompare(value?: string | null): string {
	return trimText(value)
		.toLocaleLowerCase()
		.normalize('NFD')
		.replace(/[\u0300-\u036f]/g, '');
}

function getOtherMaterialDetailLabel(
	otherMaterialDetail?: string | number | null,
): string | null {
	if (otherMaterialDetail == null) return null;
	if (typeof otherMaterialDetail === 'number') {
		return OTHER_MATERIAL_DETAIL_LABELS_BY_NUMBER[otherMaterialDetail] ?? null;
	}
	return OTHER_MATERIAL_DETAIL_LABELS_BY_KEY[otherMaterialDetail] ?? null;
}

function resolveSectionAType(materialGroup: MaterialGroupDto): number | null {
	if (materialGroup.sectionAType != null) return materialGroup.sectionAType;
	if (getMaterialType(materialGroup.materialType) === 'Vật liệu') return 1;
	return null;
}

function resolveTypeName(
	materialGroup: MaterialGroupDto,
	sectionKey: AcceptanceReportSectionKey,
): string {
	if (sectionKey === 'sectionA') {
		const sectionAType = resolveSectionAType(materialGroup);
		return (
			(sectionAType != null ? SECTION_A_TYPE_LABELS[sectionAType] : null) ??
			getMaterialType(materialGroup.materialType)
		);
	}

	if (sectionKey === 'sectionB') {
		return (
			(materialGroup.additionalCostType != null
				? ADDITIONAL_COST_TYPE_LABELS[materialGroup.additionalCostType]
				: null) ?? getMaterialType(materialGroup.materialType)
		);
	}

	return getMaterialType(materialGroup.materialType);
}

function resolveTypeOrder(
	materialGroup: MaterialGroupDto,
	sectionKey: AcceptanceReportSectionKey,
): number {
	if (sectionKey === 'sectionA') {
		const sectionAType = resolveSectionAType(materialGroup);
		return sectionAType ?? 999;
	}
	if (sectionKey === 'sectionB') {
		return materialGroup.additionalCostType ?? 999;
	}
	return 999;
}

function normalizeGroupName(
	materialGroup: MaterialGroupDto,
	sectionKey: AcceptanceReportSectionKey,
): string {
	const groupCode = trimText(materialGroup.groupCode);
	const groupName = trimText(materialGroup.groupName);

	if (sectionKey === 'sectionA') {
		if (materialGroup.productionOrderId) {
			return `${groupName || groupCode}`;
		}
		if (
			resolveSectionAType(materialGroup) === 2 &&
			groupCode.toUpperCase() === 'VTK'
		) {
			return groupName || 'Vật tư khác (phụ tùng khác)';
		}
		return groupName || groupCode;
	}

	if (sectionKey === 'sectionB') {
		if (materialGroup.additionalCostType === 4) {
			return (
				getOtherMaterialDetailLabel(materialGroup.otherMaterialDetail) ||
				groupName ||
				groupCode
			);
		}
		if (groupCode.toUpperCase() === 'NO_ORDER') {
			return '';
		}
		if (materialGroup.productionOrderId) {
			return `${groupName || groupCode}`;
		}
		return groupName || groupCode;
	}

	return groupName || groupCode;
}

function shouldUseFlatItems(
	materialGroup: MaterialGroupDto,
	sectionKey: AcceptanceReportSectionKey,
): boolean {
	if (sectionKey !== 'sectionB') return false;
	if (materialGroup.additionalCostType !== 2) return false;

	const groupCode = trimText(materialGroup.groupCode).toUpperCase();
	return !materialGroup.productionOrderId && groupCode === 'NO_ORDER';
}

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
		openingBalanceOnSiteQty:
			material.beginningInventory?.remainingAtSite?.quantity ?? 0,
		openingBalanceOnSiteAmount:
			material.beginningInventory?.remainingAtSite?.amount ?? 0,

		// Tồn đầu kỳ - Chi phí chờ hạch toán (pending)
		openingBalancePendingQty: 0,
		openingBalancePendingAmount: material.beginningInventory?.pendingValue ?? 0,
		openingBalanceContractQty:
			material.beginningInventory?.remainingByOrder?.quantity ?? 0,
		openingBalanceContractAmount:
			material.beginningInventory?.remainingByOrder?.amount ?? 0,

		// Lĩnh trong kỳ - Tổng cộng
		receiptTotalQty: material.issuedInPeriod.total.quantity,
		receiptTotalAmountKH: material.issuedInPeriod.total.amount,

		// Lĩnh trong kỳ - Lĩnh vật tư đã trả phiếu
		receiptWithReceiptQty: material.issuedInPeriod.received.quantity,
		receiptWithReceiptAmountKH: material.issuedInPeriod.received.plannedAmount,
		receiptWithReceiptAmountTT: material.issuedInPeriod.received.actualAmount,
		receiptBorrowedQty:
			material.issuedInPeriod.borrowedNoVoucher?.quantity ?? 0,
		receiptBorrowedAmount:
			material.issuedInPeriod.borrowedNoVoucher?.amount ?? 0,
		receiptReturnPrevMonthQty:
			material.issuedInPeriod.returnPreviousMonthVoucher?.quantity ?? 0,
		receiptReturnPrevMonthAmount:
			material.issuedInPeriod.returnPreviousMonthVoucher?.amount ?? 0,
		receiptHandoverQty: material.issuedInPeriod.otherReceipt?.quantity ?? 0,
		receiptHandoverAmount: material.issuedInPeriod.otherReceipt?.amount ?? 0,

		// Xuất trong kỳ - Xuất cho sản xuất
		issueForProductionQty:
			material.exportedInPeriod.exportedToProduction.quantity,
		issueForProductionAmount:
			material.exportedInPeriod.exportedToProduction.amount,
		issueOtherQty: material.exportedInPeriod.otherExport?.quantity ?? 0,
		issueOtherAmount: material.exportedInPeriod.otherExport?.amount ?? 0,
		issueContractQty:
			material.exportedInPeriod.contractSettlement?.quantity ?? 0,
		issueContractAmount:
			material.exportedInPeriod.contractSettlement?.amount ?? 0,

		// Xuất trong kỳ - Chi phí vật tư dài kỳ
		issueLongtermQty: material.exportedInPeriod.longTermExpense?.amount ? 1 : 0,
		issueLongtermAmount: material.exportedInPeriod.longTermExpense?.amount ?? 0,

		// Xuất trong kỳ - Tổng cộng
		issueTotalQty: material.exportedInPeriod.total.quantity,
		issueTotalAmount: material.exportedInPeriod.total.amount,

		// Tồn cuối kỳ - Tổng cộng
		closingBalanceTotalQty: material.endingInventory?.total?.quantity ?? 0,
		closingBalanceTotalAmount: material.endingInventory?.total?.amount ?? 0,
		closingBalanceOnSiteQty:
			material.endingInventory?.remainingAtSite?.quantity ?? 0,
		closingBalanceOnSiteAmount:
			material.endingInventory?.remainingAtSite?.amount ?? 0,
		closingBalancePendingQty: 0,
		closingBalancePendingAmount: material.endingInventory?.pendingValue ?? 0,
		closingBalanceContractQty:
			material.endingInventory?.remainingByOrder?.quantity ?? 0,
		closingBalanceContractAmount:
			material.endingInventory?.remainingByOrder?.amount ?? 0,
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
	sectionKey: AcceptanceReportSectionKey,
): GroupCodeGroup[] {
	const result: GroupCodeGroup[] = [];
	const normalizedGroupCode = trimText(materialGroup.groupCode) || 'UNKNOWN';
	const normalizedGroupName = normalizeGroupName(materialGroup, sectionKey);

	// Handle subGroups if they exist and have materials
	if (materialGroup.subGroups && materialGroup.subGroups.length > 0) {
		materialGroup.subGroups.forEach((subGroup) => {
			if (subGroup.materials && subGroup.materials.length > 0) {
				const subGroupName =
					trimText(subGroup.subGroupName) ||
					SUB_GROUP_NAME_BY_CODE[subGroup.subGroupCode] ||
					subGroup.subGroupCode;

				result.push({
					groupCode: subGroup.subGroupCode,
					groupName: subGroupName,
					items: subGroup.materials.map((m) =>
						transformMaterialDetail(
							m,
							subGroup.subGroupCode,
							subGroupName,
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
			groupCode: normalizedGroupCode,
			groupName: normalizedGroupName,
			items: materialGroup.materials.map((m) =>
				transformMaterialDetail(
					m,
					normalizedGroupCode,
					normalizedGroupName,
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
	sectionKey: AcceptanceReportSectionKey,
): TypeGroup[] {
	const typeGroups = new Map<
		string,
		{ order: number; groups: GroupCodeGroup[]; flatItems: UnifiedItem[] }
	>();

	const orderedMaterialGroups =
		sectionKey === 'sectionA'
			? [
					...categoryItem.materialGroups.filter(
						(matGroup) =>
							!(
								(resolveSectionAType(matGroup) === 2 ||
									resolveSectionAType(matGroup) === 3) &&
								trimText(matGroup.productionOrderId).length > 0
							),
					),
					...categoryItem.materialGroups.filter(
						(matGroup) =>
							resolveSectionAType(matGroup) === 2 &&
							trimText(matGroup.productionOrderId).length > 0,
					),
					...categoryItem.materialGroups.filter(
						(matGroup) =>
							resolveSectionAType(matGroup) === 3 &&
							trimText(matGroup.productionOrderId).length > 0,
					),
				]
			: categoryItem.materialGroups;

	// Group materials by materialType
	orderedMaterialGroups.forEach((matGroup) => {
		const matType = resolveTypeName(matGroup, sectionKey);
		const order = resolveTypeOrder(matGroup, sectionKey);

		if (!typeGroups.has(matType)) {
			typeGroups.set(matType, { order, groups: [], flatItems: [] });
		}

		const grouped = groupMaterials(matGroup, categoryType, sectionKey);
		const target = typeGroups.get(matType)!;
		target.order = Math.min(target.order, order);

		const hasOnlyOneTopLevelGroup =
			grouped.length === 1 && (!matGroup.subGroups || matGroup.subGroups.length === 0);
		const isRedundantQuotaGroup =
			sectionKey === 'sectionC' &&
			hasOnlyOneTopLevelGroup &&
			normalizeForCompare(grouped[0]?.groupName) === normalizeForCompare(matType);

		if (shouldUseFlatItems(matGroup, sectionKey) || isRedundantQuotaGroup) {
			grouped.forEach((group) => {
				target.flatItems.push(...group.items);
			});
			return;
		}

		target.groups.push(...grouped);
	});

	return Array.from(typeGroups.entries())
		.sort((a, b) => a[1].order - b[1].order || a[0].localeCompare(b[0]))
		.map(([typeName, value]) => ({
			typeName,
			groups: value.groups,
			flatItems: value.flatItems.length > 0 ? value.flatItems : undefined,
		}));
}

type SectionCategoryConfig = {
	key: AcceptanceReportSectionKey;
	categoryType: number;
	categoryName: string;
};

const SECTION_CATEGORY_CONFIG: SectionCategoryConfig[] = [
	{
		key: 'sectionA',
		categoryType: 1,
		categoryName: 'Vật tư đã tính vào doanh thu khoán',
	},
	{
		key: 'sectionB',
		categoryType: 2,
		categoryName: 'Bổ sung chi phí',
	},
	{
		key: 'sectionC',
		categoryType: 3,
		categoryName: 'Vật tư theo hạn mức',
	},
	{
		key: 'sectionD',
		categoryType: 4,
		categoryName: 'Tài sản',
	},
];

function buildCategoryFromSection(
	sectionGroups: MaterialGroupDto[],
	config: SectionCategoryConfig,
): CategoryGroup {
	const categoryItem: ProductionOutputDetailItemDto = {
		categoryType: config.categoryType,
		categoryName: config.categoryName,
		materialGroups: sectionGroups,
	};

	return {
		categoryName: categoryItem.categoryName,
		types: createMaterialTypeGroups(
			categoryItem,
			categoryItem.categoryType,
			config.key,
		),
	};
}

function hasSectionData(apiResponse: ProductionOutputDto): boolean {
	return SECTION_CATEGORY_CONFIG.some(
		({ key }) => (apiResponse[key] ?? []).length > 0,
	);
}

function getLegacySectionKey(categoryType: number): AcceptanceReportSectionKey {
	switch (categoryType) {
		case 1:
			return 'sectionA';
		case 2:
			return 'sectionB';
		case 3:
			return 'sectionC';
		case 4:
			return 'sectionD';
		default:
			return 'sectionA';
	}
}

/**
 * Transform API response to HierarchicalAcceptanceReport
 */
export function transformApiResponseToHierarchical(
	apiResponse: ProductionOutputDto,
): HierarchicalAcceptanceReport {
	let categories: CategoryGroup[] = [];

	// Prefer new backend shape: sectionA, sectionB, sectionC, sectionD
	if (hasSectionData(apiResponse)) {
		categories = SECTION_CATEGORY_CONFIG.map((config) => {
			const sectionGroups = apiResponse[config.key] ?? [];
			return buildCategoryFromSection(sectionGroups, config);
		}).filter((category) => category.types.length > 0);
	} else {
		// Backward compatibility with legacy shape: items[]
		categories = (apiResponse.items || []).map((item) => ({
			categoryName: item.categoryName,
			types: createMaterialTypeGroups(
				item,
				item.categoryType,
				getLegacySectionKey(item.categoryType),
			),
		}));
	}

	return {
		id: apiResponse.productionOutputId,
		categories,
	};
}

export function applyProductionOrderNames(
	report: HierarchicalAcceptanceReport,
	productionOrderNameById: Record<string, string>,
): HierarchicalAcceptanceReport {
	return {
		...report,
		categories: report.categories.map((category) => ({
			...category,
			types: category.types.map((type) => ({
				...type,
				groups: type.groups.map((group) => {
					const productionOrderName = productionOrderNameById[group.groupCode];
					if (!productionOrderName) return group;

					return {
						...group,
						groupName: `${productionOrderName}`,
					};
				}),
			})),
		})),
	};
}
