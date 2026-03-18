import {
	FlatRow,
	MaterialItem,
	SCTXItem,
	AssetItem,
	MaterialGroup,
	SCTXGroup,
	AssetGroup,
	QuotaMaterialGroup,
	QuotaMaterialItem,
	HierarchicalRow,
	HierarchicalAcceptanceReport,
	CategoryGroup,
	TypeGroup,
	UnifiedItem,
	FinancialFields,
	GroupCodeGroup,
} from './types';

/**
 * Flatten material groups into rows with rowSpan metadata for group column
 */
export function flattenMaterialGroups(
	groups: MaterialGroup[],
): FlatRow<MaterialItem>[] {
	const result: FlatRow<MaterialItem>[] = [];

	groups.forEach((group) => {
		const rowSpan = group.items.length;

		group.items.forEach((item, index) => {
			result.push({
				...item,
				rowSpan: index === 0 ? rowSpan : undefined,
				isFirstInGroup: index === 0,
			});
		});
	});

	return result;
}

/**
 * Flatten SCTX groups into rows with rowSpan metadata for equipment column
 */
export function flattenSCTXGroups(groups: SCTXGroup[]): FlatRow<SCTXItem>[] {
	const result: FlatRow<SCTXItem>[] = [];

	groups.forEach((group) => {
		const rowSpan = group.items.length;

		group.items.forEach((item, index) => {
			result.push({
				...item,
				rowSpan: index === 0 ? rowSpan : undefined,
				isFirstInGroup: index === 0,
			});
		});
	});

	return result;
}

/**
 * Flatten quota material groups into rows with rowSpan metadata
 */
export function flattenQuotaMaterialGroups(
	groups: QuotaMaterialGroup[],
): FlatRow<QuotaMaterialItem>[] {
	const result: FlatRow<QuotaMaterialItem>[] = [];

	groups.forEach((group) => {
		const rowSpan = group.items.length;

		group.items.forEach((item, index) => {
			result.push({
				...item,
				rowSpan: index === 0 ? rowSpan : undefined,
				isFirstInGroup: index === 0,
			});
		});
	});

	return result;
}

/**
 * Flatten asset groups into rows with rowSpan metadata for asset group column
 */
export function flattenAssetGroups(groups: AssetGroup[]): FlatRow<AssetItem>[] {
	const result: FlatRow<AssetItem>[] = [];

	groups.forEach((group) => {
		const rowSpan = group.items.length;

		group.items.forEach((item, index) => {
			result.push({
				...item,
				rowSpan: index === 0 ? rowSpan : undefined,
				isFirstInGroup: index === 0,
			});
		});
	});

	return result;
}

/**
 * Flatten materials without grouping (for "Vật tư khác" in Additional Cost)
 */
export function flattenMaterialItems(
	items: MaterialItem[],
): FlatRow<MaterialItem>[] {
	return items.map((item) => ({
		...item,
		rowSpan: 1,
		isFirstInGroup: true,
	}));
}

/**
 * Calculate total amounts from material items
 */
export function calculateMaterialTotals(items: MaterialItem[]) {
	return items.reduce(
		(acc, item) => {
			acc.openingBalance += item.openingBalanceTotalAmount;
			acc.receipt += item.receiptTotalAmountKH;
			acc.issue += item.issueTotalAmount;
			acc.closingBalance += item.closingBalanceTotalAmount;
			return acc;
		},
		{
			openingBalance: 0,
			receipt: 0,
			issue: 0,
			closingBalance: 0,
		},
	);
}

/**
 * Calculate total amounts from SCTX items
 */
export function calculateSCTXTotals(items: SCTXItem[]) {
	return items.reduce(
		(acc, item) => {
			acc.openingBalance += item.openingBalanceTotalAmount;
			acc.receipt += item.receiptTotalAmountKH;
			acc.issue += item.issueTotalAmount;
			acc.closingBalance += item.closingBalanceTotalAmount;
			return acc;
		},
		{
			openingBalance: 0,
			receipt: 0,
			issue: 0,
			closingBalance: 0,
		},
	);
}

/**
 * Calculate total amounts from asset items
 */
export function calculateAssetTotals(items: AssetItem[]) {
	return items.reduce(
		(acc, item) => {
			acc.openingBalance += item.openingBalanceTotalAmount;
			acc.receipt += item.receiptTotalAmountKH;
			acc.issue += item.issueTotalAmount;
			acc.closingBalance += item.closingBalanceTotalAmount;
			return acc;
		},
		{
			openingBalance: 0,
			receipt: 0,
			issue: 0,
			closingBalance: 0,
		},
	);
}

/**
 * Get all items from material groups
 */
export function getAllMaterialItems(groups: MaterialGroup[]): MaterialItem[] {
	return groups.flatMap((group) => group.items);
}

/**
 * Get all items from SCTX groups
 */
export function getAllSCTXItems(groups: SCTXGroup[]): SCTXItem[] {
	return groups.flatMap((group) => group.items);
}

/**
 * Get all items from asset groups
 */
export function getAllAssetItems(groups: AssetGroup[]): AssetItem[] {
	return groups.flatMap((group) => group.items);
}

/**
 * Get all items from quota material groups
 */
export function getAllQuotaMaterialItems(
	groups: QuotaMaterialGroup[],
): QuotaMaterialItem[] {
	return groups.flatMap((group) => group.items);
}
// ============================================================================
// NEW HIERARCHICAL UTILITY FUNCTIONS
// ============================================================================

/**
 * Create empty financial fields (all zeros)
 */
function createEmptyFinancialFields(): FinancialFields {
	return {
		priceKH: 0,
		priceTT: 0,
		openingBalanceTotalQty: 0,
		openingBalanceTotalAmount: 0,
		openingBalanceOnSiteQty: 0,
		openingBalanceOnSiteAmount: 0,
		openingBalancePendingQty: 0,
		openingBalancePendingAmount: 0,
		openingBalanceContractQty: 0,
		openingBalanceContractAmount: 0,
		receiptTotalQty: 0,
		receiptTotalAmountKH: 0,
		receiptWithReceiptQty: 0,
		receiptWithReceiptAmountKH: 0,
		receiptWithReceiptAmountTT: 0,
		receiptBorrowedQty: 0,
		receiptBorrowedAmount: 0,
		receiptReturnPrevMonthQty: 0,
		receiptReturnPrevMonthAmount: 0,
		receiptHandoverQty: 0,
		receiptHandoverPercent: 0,
		receiptHandoverAmount: 0,
		issueTotalQty: 0,
		issueTotalAmount: 0,
		issueForProductionQty: 0,
		issueForProductionAmount: 0,
		issueLongtermQty: 0,
		issueLongtermAmount: 0,
		issueOtherQty: 0,
		issueOtherAmount: 0,
		issueContractQty: 0,
		issueContractAmount: 0,
		closingBalanceTotalQty: 0,
		closingBalanceTotalAmount: 0,
		closingBalanceOnSiteQty: 0,
		closingBalanceOnSiteAmount: 0,
		closingBalancePendingQty: 0,
		closingBalancePendingAmount: 0,
	};
}

/**
 * Sum financial fields from multiple items
 * Only sums "Thành tiền" (Amount) columns, leaves quantities and prices at 0
 */
function sumFinancialFields(items: UnifiedItem[]): FinancialFields {
	const result = createEmptyFinancialFields();

	items.forEach((item) => {
		// Only accumulate "Thành tiền" (Amount) fields, not quantities or prices
		result.openingBalanceTotalAmount += item.openingBalanceTotalAmount || 0;
		result.openingBalanceOnSiteAmount += item.openingBalanceOnSiteAmount || 0;
		result.openingBalancePendingAmount += item.openingBalancePendingAmount || 0;
		result.openingBalanceContractAmount +=
			item.openingBalanceContractAmount || 0;
		result.receiptTotalAmountKH += item.receiptTotalAmountKH || 0;
		result.receiptWithReceiptAmountKH += item.receiptWithReceiptAmountKH || 0;
		result.receiptWithReceiptAmountTT += item.receiptWithReceiptAmountTT || 0;
		result.receiptBorrowedAmount += item.receiptBorrowedAmount || 0;
		result.receiptReturnPrevMonthAmount +=
			item.receiptReturnPrevMonthAmount || 0;
		result.receiptHandoverAmount += item.receiptHandoverAmount || 0;
		result.issueTotalAmount += item.issueTotalAmount || 0;
		result.issueForProductionAmount += item.issueForProductionAmount || 0;
		result.issueLongtermAmount += item.issueLongtermAmount || 0;
		result.issueOtherAmount += item.issueOtherAmount || 0;
		result.issueContractAmount += item.issueContractAmount || 0;
		result.closingBalanceTotalAmount += item.closingBalanceTotalAmount || 0;
		result.closingBalanceOnSiteAmount += item.closingBalanceOnSiteAmount || 0;
		result.closingBalancePendingAmount += item.closingBalancePendingAmount || 0;
	});

	return result;
}

/**
 * Get item code based on item type
 */
function getItemCode(item: UnifiedItem): string {
	if ('materialCode' in item) return item.materialCode;
	if ('sparePartCode' in item) return item.sparePartCode;
	if ('assetCode' in item) return item.assetCode;
	return '';
}

/**
 * Get item name based on item type
 */
function getItemName(item: UnifiedItem): string {
	if ('materialName' in item) return item.materialName;
	if ('sparePartName' in item) return item.sparePartName;
	if ('assetName' in item) return item.assetName;
	return '';
}

/**
 * Calculate totals for a type group (sum all items under the type)
 */
export function calculateTypeTotals(type: TypeGroup): FinancialFields {
	const allItems: UnifiedItem[] = [];

	// Collect items from groups
	type.groups.forEach((group) => {
		allItems.push(...group.items);
	});

	// Collect flat items if any
	if (type.flatItems) {
		allItems.push(...type.flatItems);
	}

	return sumFinancialFields(allItems);
}

/**
 * Calculate totals for a category (sum all items in all types)
 */
export function calculateCategoryTotals(
	category: CategoryGroup,
): FinancialFields {
	const allItems: UnifiedItem[] = [];

	category.types.forEach((type) => {
		// Collect items from groups
		type.groups.forEach((group) => {
			allItems.push(...group.items);
		});

		// Collect flat items if any
		if (type.flatItems) {
			allItems.push(...type.flatItems);
		}
	});

	return sumFinancialFields(allItems);
}

/**
 * Calculate totals for a group (sum all items in the group)
 */
export function calculateGroupTotals(group: GroupCodeGroup): FinancialFields {
	return sumFinancialFields(group.items);
}

/**
 * Create a row for group header (only shows label, no financial data)
 */
export function createGroupRow(
	groupCode: string,
	groupName: string,
	level: number,
): HierarchicalRow {
	return {
		id: `group-${groupCode}`,
		rowType: 'group',
		label: groupName,
		level,
		data: undefined, // Groups don't show financial data
	};
}

/**
 * Flatten hierarchical acceptance report data into flat array of rows for rendering
 */
export function flattenHierarchicalData(
	report: HierarchicalAcceptanceReport,
): HierarchicalRow[] {
	const result: HierarchicalRow[] = [];

	// Level 0: Categories
	report.categories.forEach((category, catIndex) => {
		const categoryTotals = calculateCategoryTotals(category);

		// Category row with totals
		result.push({
			id: `cat-${catIndex}`,
			rowType: 'category',
			label: category.categoryName,
			level: 0,
			data: categoryTotals,
		});

		// Level 1: Types
		category.types.forEach((type, typeIndex) => {
			const typeTotals = calculateTypeTotals(type);

			// Type row with totals
			result.push({
				id: `cat-${catIndex}-type-${typeIndex}`,
				rowType: 'type',
				label: type.typeName,
				level: 1,
				data: typeTotals,
			});

			// Level 2: Groups (if any)
			if (type.groups.length > 0) {
				type.groups.forEach((group, groupIndex) => {
					// Group row - check if should show totals
					const groupData = group.showTotals
						? calculateGroupTotals(group)
						: undefined;

					result.push({
						id: `cat-${catIndex}-type-${typeIndex}-grp-${groupIndex}`,
						rowType: 'group',
						label: group.groupName,
						level: 2,
						data: groupData, // Show totals if group.showTotals is true
					});

					// Level 3: Items
					group.items.forEach((item, itemIndex) => {
						result.push({
							id: `cat-${catIndex}-type-${typeIndex}-grp-${groupIndex}-item-${itemIndex}`,
							rowType: 'item',
							label: '', // Item rows don't use label
							level: 3,
							itemCode: getItemCode(item),
							itemName: getItemName(item),
							unit: item.unit,
							data: item,
						});
					});
				});
			}

			// Handle flat items (no grouping, e.g., "Vật tư khác")
			if (type.flatItems && type.flatItems.length > 0) {
				type.flatItems.forEach((item, itemIndex) => {
					result.push({
						id: `cat-${catIndex}-type-${typeIndex}-flat-${itemIndex}`,
						rowType: 'item',
						label: '',
						level: 2, // One level less since no group
						itemCode: getItemCode(item),
						itemName: getItemName(item),
						unit: item.unit,
						data: item,
					});
				});
			}
		});
	});

	return result;
}
