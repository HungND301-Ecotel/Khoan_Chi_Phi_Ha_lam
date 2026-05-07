import { z } from 'zod';

function parseSchemaNumber(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

function requiresCategoryProcessGroup(data: { type?: number | null }): boolean {
	return data.type === 2;
}

const categoryAllocationSchema = z.object({
	processGroupId: z.string().nullable(),
	quantity: z.number().nullable().default(null),
	equipmentIds: z.array(z.string()).default([]),
});

export const rawAcceptanceReportItemSchema = z
	.object({
		id: z.string().optional(),
		usageTime: z.number().optional(),
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
		categoryProcessGroupIds: z.array(z.string()).optional(),
		categoryProductionOrderId: z.string().nullable().optional(),
		categoryEquipmentId: z.string().nullable().optional(),
		categoryEquipmentIds: z.array(z.string()).optional(),
		categoryAllocations: z.array(categoryAllocationSchema).optional(),
		additionalCostCategory: z.number().nullable().optional(),
		additionalCostProductionOrderId: z.string().nullable().optional(),
		additionalCostEquipmentId: z.string().nullable().optional(),
		otherMaterialDetail: z.number().nullable().optional(),
		contractLimitCategory: z.number().nullable().optional(),
		contractLimitSubCategory: z.number().nullable().optional(),
		contractLimitSubCategories: z.array(z.string()).optional(),
		contractLimitBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		materialOrPartId: z.string().optional(),
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
				requiresCategoryProcessGroup(data) &&
				(data.categoryProcessGroupIds?.length ?? 0) === 0
			) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn ít nhất 1 nhóm công đoạn',
			path: ['categoryProcessGroupIds'],
		},
	)
	.refine(
		(data) => {
			const needsEquipment =
				data.showCategoryDropdown &&
				data.category === 3 &&
				data.type === 2 &&
				data.itemType === 1;
			if (!needsEquipment) return true;
			const allocations = data.categoryAllocations ?? [];
			return (
				allocations.length > 0 &&
				(data.categoryEquipmentIds?.length ?? 0) >= allocations.length &&
				allocations.every(
					(allocation) => (allocation.equipmentIds?.length ?? 0) > 0,
				)
			);
		},
		{
			message: 'Mỗi nhóm công đoạn phải chọn ít nhất 1 thiết bị',
			path: ['categoryEquipmentIds'],
		},
	)
	.refine(
		(data) => {
			const needsAllocations =
				data.showCategoryDropdown &&
				data.category != null &&
				requiresCategoryProcessGroup(data);
			if (!needsAllocations) return true;

			const allocations = data.categoryAllocations ?? [];
			return allocations.every(
				(allocation) =>
					allocation.processGroupId != null &&
					allocation.processGroupId.length > 0,
			);
		},
		{
			message: 'Mỗi phân bổ phải có nhóm công đoạn',
			path: ['categoryAllocations'],
		},
	)
	.refine(
		(data) => {
			const needsAllocations =
				data.showCategoryDropdown &&
				data.category != null &&
				requiresCategoryProcessGroup(data);
			if (!needsAllocations) return true;

			const allocations = data.categoryAllocations ?? [];
			const total = allocations.reduce(
				(acc, allocation) => acc + parseSchemaNumber(allocation.quantity),
				0,
			);
			return Math.abs(total - parseSchemaNumber(data.categoryQuantity)) < 0.01;
		},
		{
			message:
				'Tổng số lượng phân bổ theo nhóm công đoạn phải bằng số lượng vật tư của cột',
			path: ['categoryAllocations'],
		},
	)
	.refine(
		(data) => {
			if (
				!data.showAdditionalCostDropdown ||
				data.additionalCostCategory !== 4
			) {
				return true;
			}

			return data.otherMaterialDetail != null;
		},
		{
			message: 'Phải chọn loại vật tư',
			path: ['otherMaterialDetail'],
		},
	)
	.refine(
		(data) => {
			const requiresSubCategory =
				data.contractLimitCategory === 2 || data.contractLimitCategory === 3;
			if (!data.showContractLimitDropdown || !requiresSubCategory) {
				return true;
			}

			return (data.contractLimitSubCategories?.length ?? 0) > 0;
		},
		{
			message: 'Phải chọn ít nhất 1 loại (Lĩnh mới/Tái sử dụng)',
			path: ['contractLimitSubCategories'],
		},
	)
	.refine(
		(data) => {
			const requiresSubCategory =
				data.contractLimitCategory === 2 || data.contractLimitCategory === 3;
			if (!data.showContractLimitDropdown || !requiresSubCategory) {
				return true;
			}

			const selectedKeys = data.contractLimitSubCategories ?? [];
			const total = selectedKeys.reduce(
				(acc, key) =>
					acc + parseSchemaNumber(data.contractLimitBreakdown?.[key]),
				0,
			);
			const exportedQty = Number(data.exportedQuantity);
			return Math.abs(total - exportedQty) < 0.01;
		},
		{
			message:
				'Tổng số lượng lĩnh mới và lĩnh tái sử dụng phải bằng số lượng xuất',
			path: ['contractLimitBreakdown'],
		},
	)
	.refine(
		(data) => {
			// Only validate if at least one checkbox and dropdown is selected
			const hasCategoryActive =
				data.showCategoryDropdown &&
				data.category &&
				(!requiresCategoryProcessGroup(data) ||
					(data.categoryAllocations?.length ?? 0) > 0) &&
				!(
					data.category === 3 &&
					data.type === 2 &&
					data.itemType === 1 &&
					(data.categoryAllocations?.some(
						(allocation) => (allocation.equipmentIds?.length ?? 0) === 0,
					) ??
						true)
				);
			const hasAdditionalCostActive =
				data.showAdditionalCostDropdown &&
				data.additionalCostCategory &&
				(data.additionalCostCategory !== 4 || data.otherMaterialDetail != null);
			const hasContractLimitActive =
				data.showContractLimitDropdown &&
				data.contractLimitCategory &&
				((data.contractLimitCategory !== 2 &&
					data.contractLimitCategory !== 3) ||
					(data.contractLimitSubCategories?.length ?? 0) > 0);
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

export type RawAcceptanceReportFormInput = z.input<
	typeof rawAcceptanceReportFormSchema
>;

export type RawAcceptanceReportFormSchema = z.output<
	typeof rawAcceptanceReportFormSchema
>;

export const RAW_ACCEPTANCE_REPORT_FORM_DEFAULT: RawAcceptanceReportFormSchema =
	{
		productionId: '',
		items: [],
	};
