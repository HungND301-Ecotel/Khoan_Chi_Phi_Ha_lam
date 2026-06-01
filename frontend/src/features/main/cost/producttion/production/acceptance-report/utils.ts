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
		closingBalanceContractQty: 0,
		closingBalanceContractAmount: 0,
	};
}

/**
 * Sum financial fields from multiple items
 * Sums quantity + amount columns; leaves prices at 0 for total rows
 */
function sumFinancialFields(items: UnifiedItem[]): FinancialFields {
	const result = createEmptyFinancialFields();

	items.forEach((item) => {
		result.openingBalanceTotalQty += item.openingBalanceTotalQty || 0;
		result.openingBalanceTotalAmount += item.openingBalanceTotalAmount || 0;
		result.openingBalanceOnSiteQty += item.openingBalanceOnSiteQty || 0;
		result.openingBalanceOnSiteAmount += item.openingBalanceOnSiteAmount || 0;
		result.openingBalancePendingQty += item.openingBalancePendingQty || 0;
		result.openingBalancePendingAmount += item.openingBalancePendingAmount || 0;
		result.openingBalanceContractQty += item.openingBalanceContractQty || 0;
		result.openingBalanceContractAmount +=
			item.openingBalanceContractAmount || 0;
		result.receiptTotalQty += item.receiptTotalQty || 0;
		result.receiptTotalAmountKH += item.receiptTotalAmountKH || 0;
		result.receiptWithReceiptQty += item.receiptWithReceiptQty || 0;
		result.receiptWithReceiptAmountKH += item.receiptWithReceiptAmountKH || 0;
		result.receiptWithReceiptAmountTT += item.receiptWithReceiptAmountTT || 0;
		result.receiptBorrowedQty += item.receiptBorrowedQty || 0;
		result.receiptBorrowedAmount += item.receiptBorrowedAmount || 0;
		result.receiptReturnPrevMonthQty += item.receiptReturnPrevMonthQty || 0;
		result.receiptReturnPrevMonthAmount +=
			item.receiptReturnPrevMonthAmount || 0;
		result.receiptHandoverQty += item.receiptHandoverQty || 0;
		result.receiptHandoverAmount += item.receiptHandoverAmount || 0;
		result.issueTotalQty += item.issueTotalQty || 0;
		result.issueTotalAmount += item.issueTotalAmount || 0;
		result.issueForProductionQty += item.issueForProductionQty || 0;
		result.issueForProductionAmount += item.issueForProductionAmount || 0;
		result.issueLongtermQty += item.issueLongtermQty || 0;
		result.issueLongtermAmount += item.issueLongtermAmount || 0;
		result.issueOtherQty += item.issueOtherQty || 0;
		result.issueOtherAmount += item.issueOtherAmount || 0;
		result.issueContractQty += item.issueContractQty || 0;
		result.issueContractAmount += item.issueContractAmount || 0;
		result.closingBalanceTotalQty += item.closingBalanceTotalQty || 0;
		result.closingBalanceTotalAmount += item.closingBalanceTotalAmount || 0;
		result.closingBalanceOnSiteQty += item.closingBalanceOnSiteQty || 0;
		result.closingBalanceOnSiteAmount += item.closingBalanceOnSiteAmount || 0;
		result.closingBalancePendingQty += item.closingBalancePendingQty || 0;
		result.closingBalancePendingAmount += item.closingBalancePendingAmount || 0;
		result.closingBalanceContractQty += item.closingBalanceContractQty || 0;
		result.closingBalanceContractAmount +=
			item.closingBalanceContractAmount || 0;
	});

	return result;
}

/**
 * Get item code based on item type
 */
function getItemCode(item: UnifiedItem): string {
	if ('sparePartCode' in item) return item.materialCode || item.sparePartCode;
	if ('materialCode' in item) return item.materialCode;
	if ('assetCode' in item) return item.assetCode;
	return '';
}

/**
 * Get item name based on item type
 */
function getItemName(item: UnifiedItem): string {
	if ('sparePartName' in item) return item.materialName || item.sparePartName;
	if ('materialName' in item) return item.materialName;
	if ('assetName' in item) return item.assetName;
	return '';
}

function normalizeForCompare(value?: string | null): string {
	return (value ?? '')
		.trim()
		.toLocaleLowerCase()
		.replace(/đ/g, 'd')
		.normalize('NFD')
		.replace(/[\u0300-\u036f]/g, '');
}

function getAllItemsFromGroup(group: GroupCodeGroup): UnifiedItem[] {
	return [
		...group.items,
		...(group.childGroups?.flatMap((childGroup) => getAllItemsFromGroup(childGroup)) ??
			[]),
	];
}

function getAllItemsFromType(type: TypeGroup): UnifiedItem[] {
	return [
		...type.groups.flatMap((group) => getAllItemsFromGroup(group)),
		...(type.flatItems ?? []),
	];
}

function isContractedRevenueCategoryName(categoryName: string): boolean {
	return (
		normalizeForCompare(categoryName) ===
		'vat tu da tinh vao doanh thu khoan'
	);
}

function isSectionASctxSubtypeName(typeName: string): boolean {
	const normalized = normalizeForCompare(typeName);
	return (
		normalized ===
			'chi phi sua chua thuong xuyen (cac loai vat tu sctx theo ke hoach vat tu)' ||
		normalized === 'chi phi sua chua thuong xuyen dai ky phan bo'
	);
}

function getSectionASctxSubtypeCode(typeName: string): string {
	const normalized = normalizeForCompare(typeName);

	if (
		normalized ===
		'chi phi sua chua thuong xuyen (cac loai vat tu sctx theo ke hoach vat tu)'
	) {
		return 'CPSCTX-TKHVT';
	}

	return '';
}

/**
 * Calculate totals for a type group (sum all items under the type)
 */
export function calculateTypeTotals(type: TypeGroup): FinancialFields {
	return sumFinancialFields(getAllItemsFromType(type));
}

/**
 * Calculate totals for a category (sum all items in all types)
 */
export function calculateCategoryTotals(
	category: CategoryGroup,
): FinancialFields {
	const allItems: UnifiedItem[] = [];

	category.types.forEach((type) => {
		type.groups.forEach((group) => {
			allItems.push(...getAllItemsFromGroup(group));
		});

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
	return sumFinancialFields(getAllItemsFromGroup(group));
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
		code: groupCode,
		name: groupName,
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

	const appendGroupRows = (
		group: GroupCodeGroup,
		baseId: string,
		level: number,
	) => {
		const groupData = group.showTotals ? calculateGroupTotals(group) : undefined;

		result.push({
			id: `${baseId}-group-${group.groupCode}`,
			rowType: 'group',
			label: group.groupName,
			code: group.displayCode ?? group.groupCode,
			name: group.groupName,
			level,
			data: groupData,
		});

		group.childGroups?.forEach((childGroup, childIndex) => {
			appendGroupRows(childGroup, `${baseId}-child-${childIndex}`, level + 1);
		});

		group.items.forEach((item, itemIndex) => {
			result.push({
				id: `${baseId}-item-${itemIndex}`,
				rowType: 'item',
				label: '',
				level: level + 1,
				code: getItemCode(item),
				name: getItemName(item),
				itemCode: getItemCode(item),
				itemName: getItemName(item),
				unit: item.unit,
				data: item,
			});
		});
	};

	// Level 0: Categories
	report.categories.forEach((category, catIndex) => {
		const categoryTotals = calculateCategoryTotals(category);
		const isAssetCategory =
			category.categoryName.trim().toLocaleLowerCase() === 'tài sản';

		// Category row with totals
		result.push({
			id: `cat-${catIndex}`,
			rowType: 'category',
			label: category.categoryName,
			code: '',
			name: category.categoryName,
			level: 0,
			data: categoryTotals,
		});

		if (isAssetCategory) {
			let assetItemIndex = 0;

			category.types.forEach((type) => {
				type.groups.forEach((group) => {
					group.items.forEach((item) => {
						result.push({
							id: `cat-${catIndex}-asset-item-${assetItemIndex}`,
							rowType: 'item',
							label: '',
							level: 1,
							code: getItemCode(item),
							name: getItemName(item),
							itemCode: getItemCode(item),
							itemName: getItemName(item),
							unit: item.unit,
							data: item,
						});
						assetItemIndex += 1;
					});
				});

				type.flatItems?.forEach((item) => {
					result.push({
						id: `cat-${catIndex}-asset-flat-item-${assetItemIndex}`,
						rowType: 'item',
						label: '',
						level: 1,
						code: getItemCode(item),
						name: getItemName(item),
						itemCode: getItemCode(item),
						itemName: getItemName(item),
						unit: item.unit,
						data: item,
					});
					assetItemIndex += 1;
				});
			});

			return;
		}

		const shouldGroupSectionASctx =
			isContractedRevenueCategoryName(category.categoryName) &&
			category.types.some((type) => isSectionASctxSubtypeName(type.typeName));
		let hasRenderedSectionASctxParent = false;

		// Level 1: Types
		category.types.forEach((type, typeIndex) => {
			const isSectionASctxSubtype = isSectionASctxSubtypeName(type.typeName);

			if (shouldGroupSectionASctx && isSectionASctxSubtype) {
				if (hasRenderedSectionASctxParent) {
					return;
				}

				const sctxTypes = category.types.filter((candidate) =>
					isSectionASctxSubtypeName(candidate.typeName),
				);
				const sctxParentTotals = sumFinancialFields(
					sctxTypes.flatMap((candidate) => getAllItemsFromType(candidate)),
				);

				result.push({
					id: `cat-${catIndex}-type-sctx-parent`,
					rowType: 'type',
					label: 'Chi phí Sửa chữa thường xuyên',
					code: '',
					name: 'Chi phí Sửa chữa thường xuyên',
					level: 1,
					data: sctxParentTotals,
				});

				sctxTypes.forEach((sctxType, sctxIndex) => {
					const subtypeTotals = calculateTypeTotals(sctxType);

					result.push({
						id: `cat-${catIndex}-type-sctx-sub-${sctxIndex}`,
						rowType: 'group',
						label: sctxType.typeName,
						code: getSectionASctxSubtypeCode(sctxType.typeName),
						name: sctxType.typeName,
						level: 2,
						data: subtypeTotals,
					});

					sctxType.groups.forEach((group, groupIndex) => {
						appendGroupRows(
							group,
							`cat-${catIndex}-type-sctx-sub-${sctxIndex}-grp-${groupIndex}`,
							3,
						);
					});

					sctxType.flatItems?.forEach((item, itemIndex) => {
						result.push({
							id: `cat-${catIndex}-type-sctx-sub-${sctxIndex}-flat-${itemIndex}`,
							rowType: 'item',
							label: '',
							level: 3,
							code: getItemCode(item),
							name: getItemName(item),
							itemCode: getItemCode(item),
							itemName: getItemName(item),
							unit: item.unit,
							data: item,
						});
					});
				});

				hasRenderedSectionASctxParent = true;
				return;
			}

			const typeTotals = calculateTypeTotals(type);

			// Type row with totals
			result.push({
				id: `cat-${catIndex}-type-${typeIndex}`,
				rowType: 'type',
				label: type.typeName,
				code: '',
				name: type.typeName,
				level: 1,
				data: typeTotals,
			});

			// Level 2: Groups (if any)
			if (type.groups.length > 0) {
				type.groups.forEach((group, groupIndex) => {
					appendGroupRows(
						group,
						`cat-${catIndex}-type-${typeIndex}-grp-${groupIndex}`,
						2,
					);
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
						code: getItemCode(item),
						name: getItemName(item),
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
