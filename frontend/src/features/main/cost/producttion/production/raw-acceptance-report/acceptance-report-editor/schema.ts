import z from 'zod';

function isUnresolvedRow(data: { resolutionStatus?: string | null }): boolean {
	return data.resolutionStatus === 'unresolved';
}

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === 1) return 2; // Material -> Vật liệu
	if (type === 2) return 3; // SparePart -> SCTX
	return null;
}

function requiresCategoryProcessGroup(data: { type?: number | null }): boolean {
	return data.type === 2;
}

function parseSchemaNumber(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

const categoryAllocationSchema = z.object({
	processGroupId: z.string().nullable(),
	quantity: z.number().nullable().default(null),
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
		category: z.number().nullable(),
		isLongTermTracking: z.boolean().default(false),
		categoryProcessGroup: z.string().nullable(),
		categoryProcessGroupIds: z.array(z.string()).optional(),
		categoryProductionOrderId: z.string().nullable(),
		categoryEquipmentId: z.string().nullable().optional(),
		categoryEquipmentIds: z.array(z.string()).optional(),
		categoryAllocations: z.array(categoryAllocationSchema).optional(),
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
			if (isUnresolvedRow(data)) return true;
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			if (!data.showCategoryDropdown || !categoryValue) return true;
			if (!requiresCategoryProcessGroup(data)) return true;
			return (data.categoryProcessGroupIds?.length ?? 0) > 0;
		},
		{
			message: 'Phải chọn ít nhất 1 nhóm công đoạn',
			path: ['categoryProcessGroupIds'],
		},
	)
	.refine(
		(data) => {
			if (isUnresolvedRow(data)) return true;
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			const needsEquipment =
				categoryValue === 3 && data.type === 2 && data.itemType === 1;
			if (!data.showCategoryDropdown || !needsEquipment) return true;
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
			if (isUnresolvedRow(data)) return true;
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			if (
				!data.showCategoryDropdown ||
				!categoryValue ||
				!requiresCategoryProcessGroup(data)
			) {
				return true;
			}

			return (data.categoryAllocations ?? []).every(
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
			if (isUnresolvedRow(data)) return true;
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			if (
				!data.showCategoryDropdown ||
				!categoryValue ||
				!requiresCategoryProcessGroup(data)
			) {
				return true;
			}

			const total = (data.categoryAllocations ?? []).reduce(
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
			const categoryValue =
				data.category ?? getDefaultCategoryByMaterialType(data.type);
			const hasCategoryActive =
				data.showCategoryDropdown &&
				categoryValue &&
				(!requiresCategoryProcessGroup(data) ||
					(data.categoryAllocations?.length ?? 0) > 0) &&
				!(
					categoryValue === 3 &&
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
	category: null,
	isLongTermTracking: false,
	categoryProcessGroup: null,
	categoryProcessGroupIds: [],
	categoryProductionOrderId: null,
	categoryEquipmentId: null,
	categoryEquipmentIds: [],
	categoryAllocations: [],
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

export type MaterialsFormInput = z.input<typeof materialsFormSchema>;

export type MaterialsFormSchema = z.output<typeof materialsFormSchema>;

export const acceptanceReportEditorFormSchema = materialsFormSchema;

export type AcceptanceReportEditorFormInput = MaterialsFormInput;

export type AcceptanceReportEditorFormSchema = MaterialsFormSchema;
