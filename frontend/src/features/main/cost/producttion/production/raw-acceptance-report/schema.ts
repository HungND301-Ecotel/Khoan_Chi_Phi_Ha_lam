import { z } from 'zod';

export const rawAcceptanceReportItemSchema = z
	.object({
		id: z.string().optional(),
		materialCode: z.string(),
		materialName: z.string(),
		unit: z.string(),
		plannedUnitPrice: z
			.number()
			.min(0, 'Đơn giá kế hoạch phải >= 0')
			.optional(),
		actualUnitPrice: z.number().min(0, 'Đơn giá thực tế phải >= 0'),
		receivedQuantity: z
			.any()
			.transform((val) => Number(val))
			.refine((val) => !Number.isNaN(val), {
				message: 'Số lượng lĩnh phải >= 0',
			})
			.refine((val) => val >= 0, {
				message: 'Số lượng lĩnh phải >= 0',
			}),
		exportedQuantity: z
			.any()
			.transform((val) => Number(val))
			.refine((val) => !Number.isNaN(val), {
				message: 'Số lượng xuất phải >= 0',
			})
			.refine((val) => val >= 0, {
				message: 'Số lượng xuất phải >= 0',
			}),
		receivedTypes: z.array(z.string()).optional(),
		exportedTypes: z.array(z.string()).optional(),
		receivedBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		exportedBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		// Checkbox flags
		showCategoryDropdown: z.boolean(),
		showAdditionalCostDropdown: z.boolean(),
		showContractLimitDropdown: z.boolean(),
		showAssetDropdown: z.boolean(),
		// Dropdown values
		category: z.number().nullable().optional(),
		categoryProcessGroup: z.string().nullable().optional(),
		categoryProductionOrderId: z.string().nullable().optional(),
		additionalCostCategory: z.number().nullable().optional(),
		additionalCostProductionOrderId: z.string().nullable().optional(),
		contractLimitCategory: z.number().nullable().optional(),
		contractLimitSubCategory: z.number().nullable().optional(),
		productionOrderId: z.string().nullable().optional(),
		// Quantity fields
		categoryQuantity: z.number().nullable().optional(),
		additionalCostQuantity: z.number().nullable().optional(),
		contractLimitQuantity: z.number().nullable().optional(),
		assetQuantity: z.number().nullable().optional(),
		// Material type for internal use
		type: z.number().optional(),
		itemType: z.number().optional(),
	})
	.refine(
		(data) => {
			if (
				data.showCategoryDropdown &&
				data.category &&
				!data.categoryProcessGroup
			) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn nhóm công đoạn',
			path: ['categoryProcessGroup'],
		},
	)
	.refine(
		(data) => {
			if (!data.showCategoryDropdown || data.category !== 3) return true;
			return data.categoryProductionOrderId != null;
		},
		{
			message: 'Phải chọn quyết định, lệnh sản xuất',
			path: ['categoryProductionOrderId'],
		},
	)
	.refine(
		(data) => {
			if (
				!data.showAdditionalCostDropdown ||
				(data.additionalCostCategory !== 2 && data.additionalCostCategory !== 3)
			) {
				return true;
			}

			return data.additionalCostProductionOrderId != null;
		},
		{
			message: 'Phải chọn quyết định, lệnh sản xuất',
			path: ['additionalCostProductionOrderId'],
		},
	)
	.refine(
		(data) => {
			// Only validate if at least one checkbox and dropdown is selected
			const hasCategoryActive =
				data.showCategoryDropdown &&
				data.category &&
				data.categoryProcessGroup &&
				(data.category !== 3 || data.categoryProductionOrderId != null);
			const hasAdditionalCostActive =
				data.showAdditionalCostDropdown &&
				data.additionalCostCategory &&
				((data.additionalCostCategory !== 2 &&
					data.additionalCostCategory !== 3) ||
					data.additionalCostProductionOrderId != null);
			const hasContractLimitActive =
				data.showContractLimitDropdown && data.contractLimitCategory;
			const hasAssetActive = data.showAssetDropdown;

			if (
				!hasCategoryActive &&
				!hasAdditionalCostActive &&
				!hasContractLimitActive &&
				!hasAssetActive
			) {
				return true; // No validation needed if nothing is selected
			}

			// Calculate sum of quantities where checkbox and dropdown are selected
			let totalQuantity = 0;

			if (hasCategoryActive && data.categoryQuantity != null) {
				totalQuantity += data.categoryQuantity;
			}

			if (hasAdditionalCostActive && data.additionalCostQuantity != null) {
				totalQuantity += data.additionalCostQuantity;
			}

			if (hasContractLimitActive && data.contractLimitQuantity != null) {
				totalQuantity += data.contractLimitQuantity;
			}

			if (hasAssetActive && data.assetQuantity != null) {
				totalQuantity += data.assetQuantity;
			}

			const exportedQty = Number(data.exportedQuantity);
			return Math.abs(totalQuantity - exportedQty) < 0.01; // Allow small floating point difference
		},
		{
			message: 'Tổng số lượng vật tư phải bằng số lượng xuất',
			path: ['categoryQuantity'], // Show error on first quantity field
		},
	);

export const rawAcceptanceReportFormSchema = z.object({
	productionId: z.string(),
	items: z
		.array(rawAcceptanceReportItemSchema)
		.min(1, 'Phải có ít nhất 1 vật tư'),
});

export type RawAcceptanceReportFormSchema = z.infer<
	typeof rawAcceptanceReportFormSchema
>;

export const RAW_ACCEPTANCE_REPORT_FORM_DEFAULT: RawAcceptanceReportFormSchema =
	{
		productionId: '',
		items: [],
	};
