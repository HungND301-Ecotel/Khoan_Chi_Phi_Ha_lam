import z from 'zod';

function isUnresolvedRow(data: { resolutionStatus?: string | null }): boolean {
	return data.resolutionStatus === 'unresolved';
}

function parseSchemaNumber(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

const categoryAllocationSchema = z.object({
	processGroupId: z.string().nullable(),
	quantity: z.number().nullable().default(null),
	assignmentCodeIds: z.array(z.string()).default([]),
	equipmentIds: z.array(z.string()).default([]),
});

export const materialFormSchema = z
	.object({
		id: z.string().optional(),
		usageTime: z.number().optional(),
		acceptanceReportItemId: z.string().optional(),
		materialOrPartId: z.string().optional(),
		resolutionStatus: z.enum(['resolved', 'unresolved']).optional(),
		unresolvedReason: z.string().optional(),
		sourceRowNumber: z.number().nullable().optional(),
		createdEntityGroup: z.enum(['material', 'part']).nullable().optional(),
		createdSpecificType: z.number().nullable().optional(),
		partType: z.number().nullable().optional(),
		documentNumber: z.string().optional(),
		postingDate: z.string().nullable().optional(),
		materialCode: z.string().min(1, { message: 'Mã vật tư là bắt buộc' }),
		materialName: z.string().optional(),
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
		categoryType: z.number().nullable(),
		category: z.number().nullable(),
		isLongTermTracking: z.boolean().default(false),
		categoryProcessGroup: z.string().nullable(),
		categoryProcessGroupIds: z.array(z.string()).optional(),
		categoryProductionOrderId: z.string().nullable(),
		categoryAssignmentCodeId: z.string().nullable().optional(),
		categoryEquipmentId: z.string().nullable().optional(),
		categoryAssignmentCodeIds: z.array(z.string()).optional(),
		categoryEquipmentIds: z.array(z.string()).optional(),
		categoryAllocations: z.array(categoryAllocationSchema).optional(),
		showCategoryDropdown: z.boolean(),
		additionalCostCategory: z.number().nullable(),
		additionalCostProductionOrderId: z.string().nullable(),
		additionalCostAssignmentCodeId: z.string().nullable().optional(),
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
			isUnresolvedRow(data) ||
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
			if (isUnresolvedRow(data) || !data.showCategoryDropdown) return true;
			return data.categoryType != null;
		},
		{
			message: 'Phải chọn loại vật tư',
			path: ['categoryType'],
		},
	)
	.refine(
		(data) => {
			if (isUnresolvedRow(data) || !data.showAdditionalCostDropdown) return true;
			return data.additionalCostCategory != null;
		},
		{
			message: 'Phải chọn loại bổ sung chi phí',
			path: ['additionalCostCategory'],
		},
	)
	.refine(
		(data) => {
			if (isUnresolvedRow(data)) return true;
			if (!data.showCategoryDropdown || data.categoryType == null) return true;
			return data.categoryQuantity != null;
		},
		{
			message: 'Phải nhập số lượng vật tư',
			path: ['categoryQuantity'],
		},
	)
	.refine(
		(data) => {
			if (isUnresolvedRow(data)) return true;
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
			if (isUnresolvedRow(data)) return true;
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
			if (isUnresolvedRow(data)) return true;
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
			if (isUnresolvedRow(data)) return true;
			const hasCategoryActive =
				data.showCategoryDropdown && data.categoryType != null;
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

export type MaterialFormInput = z.input<typeof materialFormSchema>;

export type MaterialFormSchema = z.output<typeof materialFormSchema>;

export type AcceptanceReportEditorRowInput = MaterialFormInput;

export type AcceptanceReportEditorRow = MaterialFormSchema;

export const MATERIAL_FORM_DEFAULT: MaterialFormSchema = {
	id: undefined,
	usageTime: undefined,
	acceptanceReportItemId: undefined,
	materialOrPartId: undefined,
	resolutionStatus: 'resolved',
	unresolvedReason: undefined,
	sourceRowNumber: null,
	createdEntityGroup: null,
	createdSpecificType: null,
	partType: null,
	documentNumber: '',
	postingDate: null,
	materialCode: '',
	materialName: undefined,
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
	categoryType: null,
	category: null,
	isLongTermTracking: false,
	categoryProcessGroup: null,
	categoryProcessGroupIds: [],
	categoryProductionOrderId: null,
	categoryAssignmentCodeId: null,
	categoryEquipmentId: null,
	categoryAssignmentCodeIds: [],
	categoryEquipmentIds: [],
	categoryAllocations: [],
	showCategoryDropdown: false,
	additionalCostCategory: null,
	additionalCostProductionOrderId: null,
	additionalCostAssignmentCodeId: null,
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

export type MaterialsFormInput = z.input<typeof materialsFormSchema>;

export type MaterialsFormSchema = z.output<typeof materialsFormSchema>;

export const acceptanceReportEditorFormSchema = materialsFormSchema;

export type AcceptanceReportEditorFormInput = MaterialsFormInput;

export type AcceptanceReportEditorFormSchema = MaterialsFormSchema;
