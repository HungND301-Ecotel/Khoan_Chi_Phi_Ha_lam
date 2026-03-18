import z from 'zod';

export const materialFormSchema = z
	.object({
		id: z.string().optional(),
		acceptanceReportItemId: z.string().optional(),
		materialOrPartId: z.string().optional(),
		materialCode: z.string().min(1, { message: 'Mã vật tư là bắt buộc' }),
		unitOfMeasureName: z.string().optional(),
		type: z.number().optional(),
		quantityReceived: z
			.number()
			.min(0, { message: 'Số lượng lĩnh phải lớn hơn 0' }),
		quantityExported: z
			.number()
			.min(0, { message: 'Số lượng xuất phải lớn hơn 0' }),
		quantity: z.number().min(0, { message: 'Số lượng phải lớn hơn 0' }),
		categoryQuantity: z.number().nullable().optional(),
		additionalCostQuantity: z.number().nullable().optional(),
		contractLimitQuantity: z.number().nullable().optional(),
		// Category: Vật tư đã tính vào doanh thu khoán
		category: z.number().nullable(),
		categoryProcessGroup: z.string().nullable(),
		showCategoryDropdown: z.boolean(),
		// Decision to supplement costs: QUYẾT ĐỊNH BỔ SUNG CHI PHÍ
		additionalCostCategory: z.number().nullable(),
		showAdditionalCostDropdown: z.boolean(),
		// Materials/Supplies with contract limits: VẬT TƯ KHOÁN THEO HẠN MỨC
		contractLimitCategory: z.number().nullable(),
		contractLimitSubCategory: z.number().nullable(),
		showContractLimitDropdown: z.boolean(),
		// Assets: Tài sản
		assetCategory: z.number().nullable(),
		assetQuantity: z.number().nullable().optional(),
		showAssetDropdown: z.boolean(),
	})
	.refine(
		(data) => {
			// Require at least one checkbox to be selected
			return (
				data.showCategoryDropdown ||
				data.showAdditionalCostDropdown ||
				data.showContractLimitDropdown ||
				data.showAssetDropdown
			);
		},
		{
			message: 'Phải chọn ít nhất một phân loại cho vật tư này',
			path: ['showCategoryDropdown'],
		},
	)
	.refine(
		(data) => {
			// If category checkbox is selected, must have dropdown selected
			if (data.showCategoryDropdown && !data.category) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn danh mục khi đã tích checkbox',
			path: ['category'],
		},
	)
	.refine(
		(data) => {
			// If category checkbox and first dropdown are selected, must have process group
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
			// If category checkbox and dropdown are selected, must have quantity
			if (
				data.showCategoryDropdown &&
				data.category &&
				data.categoryProcessGroup &&
				data.categoryQuantity == null
			) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải nhập số lượng',
			path: ['categoryQuantity'],
		},
	)
	.refine(
		(data) => {
			// If additional cost checkbox is selected, must have dropdown selected
			if (data.showAdditionalCostDropdown && !data.additionalCostCategory) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn danh mục khi đã tích checkbox',
			path: ['additionalCostCategory'],
		},
	)
	.refine(
		(data) => {
			// If additional cost checkbox and dropdown are selected, must have quantity
			if (
				data.showAdditionalCostDropdown &&
				data.additionalCostCategory &&
				data.additionalCostQuantity == null
			) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải nhập số lượng',
			path: ['additionalCostQuantity'],
		},
	)
	.refine(
		(data) => {
			// If contract limit checkbox is selected, must have dropdown selected
			if (data.showContractLimitDropdown && !data.contractLimitCategory) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn danh mục khi đã tích checkbox',
			path: ['contractLimitCategory'],
		},
	)
	.refine(
		(data) => {
			// If contract limit category requires sub-category (MineSupport=2 or SupportAccessories=3)
			if (
				data.showContractLimitDropdown &&
				data.contractLimitCategory &&
				(data.contractLimitCategory === 2 ||
					data.contractLimitCategory === 3) &&
				!data.contractLimitSubCategory
			) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải chọn danh mục phụ (Lĩnh mới/Tái sử dụng)',
			path: ['contractLimitSubCategory'],
		},
	)
	.refine(
		(data) => {
			// If contract limit is fully selected, must have quantity
			if (data.showContractLimitDropdown && data.contractLimitCategory) {
				const needsSubCategory =
					data.contractLimitCategory === 2 || data.contractLimitCategory === 3;
				if (needsSubCategory && !data.contractLimitSubCategory) {
					return true; // Will be caught by previous validation
				}
				if (data.contractLimitQuantity == null) {
					return false;
				}
			}
			return true;
		},
		{
			message: 'Phải nhập số lượng',
			path: ['contractLimitQuantity'],
		},
	)
	.refine(
		(data) => {
			// If asset checkbox is selected, must have quantity
			if (data.showAssetDropdown && data.assetQuantity == null) {
				return false;
			}
			return true;
		},
		{
			message: 'Phải nhập số lượng',
			path: ['assetQuantity'],
		},
	)
	.refine(
		(data) => {
			// Only validate total if at least one checkbox and dropdown is selected
			const hasCategoryActive =
				data.showCategoryDropdown && data.category && data.categoryProcessGroup;
			const hasAdditionalCostActive =
				data.showAdditionalCostDropdown && data.additionalCostCategory;
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

			const exportedQty = Number(data.quantityExported);
			return Math.abs(totalQuantity - exportedQty) < 0.01; // Allow small floating point difference
		},
		{
			message: 'Tổng số lượng vật tư phải bằng số lượng xuất',
			path: ['categoryQuantity'], // Show error on first quantity field
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
	quantityReceived: 0,
	quantityExported: 0,
	quantity: 0,
	categoryQuantity: null,
	additionalCostQuantity: null,
	contractLimitQuantity: null,
	category: null,
	categoryProcessGroup: null,
	showCategoryDropdown: false,
	additionalCostCategory: null,
	showAdditionalCostDropdown: false,
	contractLimitCategory: null,
	contractLimitSubCategory: null,
	showContractLimitDropdown: false,
	assetCategory: null,
	assetQuantity: null,
	showAssetDropdown: false,
};

// Wrapper schema for the array of materials
export const materialsFormSchema = z.object({
	materials: z.array(materialFormSchema),
});
