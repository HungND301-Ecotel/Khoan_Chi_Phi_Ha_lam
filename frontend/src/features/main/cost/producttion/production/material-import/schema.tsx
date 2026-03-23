import z from 'zod';

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === 1) return 2; // Material -> Vật liệu
	if (type === 2) return 3; // SparePart -> SCTX
	return null;
}

function parseSchemaNumber(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

export const materialFormSchema = z
	.object({
		id: z.string().optional(),
		acceptanceReportItemId: z.string().optional(),
		materialOrPartId: z.string().optional(),
		materialCode: z.string().min(1, { message: 'Mã vật tư là bắt buộc' }),
		unitOfMeasureName: z.string().optional(),
		type: z.number().optional(),
		itemType: z.number().optional(),
		quantityReceived: z.number().min(0, { message: 'Số lượng lĩnh phải >= 0' }),
		quantityExported: z.number().min(0, { message: 'Số lượng xuất phải >= 0' }),
		receivedTypes: z.array(z.string()).optional(),
		exportedTypes: z.array(z.string()).optional(),
		receivedBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		exportedBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		quantity: z.number().min(0, { message: 'Số lượng phải >= 0' }),
		categoryQuantity: z.number().nullable().optional(),
		additionalCostQuantity: z.number().nullable().optional(),
		contractLimitQuantity: z.number().nullable().optional(),
		category: z.number().nullable(),
		categoryProcessGroup: z.string().nullable(),
		categoryProductionOrderId: z.string().nullable(),
		showCategoryDropdown: z.boolean(),
		additionalCostCategory: z.number().nullable(),
		additionalCostProductionOrderId: z.string().nullable(),
		otherMaterialDetail: z.number().nullable().optional(),
		showAdditionalCostDropdown: z.boolean(),
		contractLimitCategory: z.number().nullable(),
		contractLimitSubCategory: z.number().nullable(),
		contractLimitSubCategories: z.array(z.string()).optional(),
		contractLimitBreakdown: z
			.record(z.string(), z.union([z.number(), z.string()]))
			.optional(),
		showContractLimitDropdown: z.boolean(),
		assetCategory: z.number().nullable(),
		assetQuantity: z.number().nullable().optional(),
		showAssetDropdown: z.boolean(),
	})
	.refine(
		(data) =>
			data.showCategoryDropdown ||
			data.showAdditionalCostDropdown ||
			data.showContractLimitDropdown ||
			data.showAssetDropdown,
		{
			message: 'Phải chọn ít nhất một phân loại cho vật tư này',
			path: ['showCategoryDropdown'],
		},
	)
	.refine(
		(data) => {
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			if (!data.showCategoryDropdown || !categoryValue) return true;
			return !!data.categoryProcessGroup;
		},
		{
			message: 'Phải chọn nhóm công đoạn',
			path: ['categoryProcessGroup'],
		},
	)
	.refine(
		(data) => {
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			if (!data.showCategoryDropdown || categoryValue !== 3) return true;
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
			const exportedQty = Number(data.quantityExported);
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
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			const hasCategoryActive =
				data.showCategoryDropdown &&
				categoryValue &&
				data.categoryProcessGroup &&
				(categoryValue !== 3 || data.categoryProductionOrderId != null);
			const hasAdditionalCostActive =
				data.showAdditionalCostDropdown &&
				data.additionalCostCategory &&
				((data.additionalCostCategory !== 2 &&
					data.additionalCostCategory !== 3) ||
					data.additionalCostProductionOrderId != null) &&
				(data.additionalCostCategory !== 4 ||
					data.otherMaterialDetail != null);
			const hasContractLimitActive =
				data.showContractLimitDropdown &&
				data.contractLimitCategory &&
				((data.contractLimitCategory !== 2 &&
					data.contractLimitCategory !== 3) ||
					((data.contractLimitSubCategories?.length ?? 0) > 0));
			const hasAssetActive = data.showAssetDropdown;

			if (
				!hasCategoryActive &&
				!hasAdditionalCostActive &&
				!hasContractLimitActive &&
				!hasAssetActive
			) {
				return true;
			}

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

			const exportedQty = Number(data.quantityExported);
			return Math.abs(totalQuantity - exportedQty) < 0.01;
		},
		{
			message: 'Tổng số lượng vật tư phải bằng số lượng xuất',
			path: ['categoryQuantity'],
		},
	);

export type MaterialFormSchema = z.infer<typeof materialFormSchema>;

export const MATERIAL_FORM_DEFAULT: MaterialFormSchema = {
	id: undefined,
	acceptanceReportItemId: undefined,
	materialOrPartId: undefined,
	materialCode: '',
	unitOfMeasureName: undefined,
	type: undefined,
	itemType: undefined,
	quantityReceived: 0,
	quantityExported: 0,
	receivedTypes: undefined,
	exportedTypes: undefined,
	receivedBreakdown: undefined,
	exportedBreakdown: undefined,
	quantity: 0,
	categoryQuantity: null,
	additionalCostQuantity: null,
	contractLimitQuantity: null,
	category: null,
	categoryProcessGroup: null,
	categoryProductionOrderId: null,
	showCategoryDropdown: false,
	additionalCostCategory: null,
	additionalCostProductionOrderId: null,
	otherMaterialDetail: null,
	showAdditionalCostDropdown: false,
	contractLimitCategory: null,
	contractLimitSubCategory: null,
	contractLimitSubCategories: [],
	contractLimitBreakdown: {},
	showContractLimitDropdown: false,
	assetCategory: null,
	assetQuantity: null,
	showAssetDropdown: false,
};

export const materialsFormSchema = z.object({
	materials: z.array(materialFormSchema),
});
