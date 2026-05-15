import {
	AdditionalCost,
	CategoryAllocation,
	EXPORTED_TYPE_OPTIONS,
	ISSUED_DETAIL_KEY_BY_TYPE,
	MaterialType,
	MaterialsIncludedInContractRevenue,
	OTHER_MATERIAL_DETAIL_OPTIONS,
	QuantityDetail,
	RECEIVED_TYPE_OPTIONS,
	SHIPPED_DETAIL_KEY_BY_TYPE,
} from './types';

export const PRODUCTION_ORDER_OPTION_PREFIX = 'production-order:';
export const ASSIGNMENT_CODE_OPTION_PREFIX = 'assignment-code:';
export const NONE_PRODUCTION_ORDER_ID = '__none__';

export function toProductionOrderOptionValue(id: string): string {
	return `${PRODUCTION_ORDER_OPTION_PREFIX}${id}`;
}

export function toAssignmentCodeOptionValue(id: string): string {
	return `${ASSIGNMENT_CODE_OPTION_PREFIX}${id}`;
}

export function parseProductionOrderOptionId(value: string): string {
	return value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)
		? value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length)
		: value;
}

export function parseAssignmentCodeOptionId(value: string): string {
	return value.startsWith(ASSIGNMENT_CODE_OPTION_PREFIX)
		? value.slice(ASSIGNMENT_CODE_OPTION_PREFIX.length)
		: value;
}

export function normalizeProductionOrderId(value?: string | null): string | null {
	if (!value) return null;
	const trimmedValue = value.trim();
	if (trimmedValue === NONE_PRODUCTION_ORDER_ID) return null;
	return trimmedValue.length > 0 ? trimmedValue : null;
}

export function resolveSelectionIds(value?: string | null): {
	productionOrderId: string | null;
	assignmentCodeId: string | null;
} {
	if (!value) {
		return {
			productionOrderId: null,
			assignmentCodeId: null,
		};
	}

	if (value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)) {
		const parsedId = normalizeProductionOrderId(
			value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length),
		);
		return {
			productionOrderId: parsedId,
			assignmentCodeId: null,
		};
	}

	if (value.startsWith(ASSIGNMENT_CODE_OPTION_PREFIX)) {
		const assignmentCodeId = value
			.slice(ASSIGNMENT_CODE_OPTION_PREFIX.length)
			.trim();
		return {
			productionOrderId: null,
			assignmentCodeId:
				assignmentCodeId.length > 0 ? assignmentCodeId : null,
		};
	}

	return {
		productionOrderId: normalizeProductionOrderId(value),
		assignmentCodeId: null,
	};
}

export function parseQuantity(
	value: number | string | null | undefined,
): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

export function getDefaultCategoryByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) {
		return MaterialsIncludedInContractRevenue.Material;
	}

	if (type === MaterialType.SparePart) {
		return MaterialsIncludedInContractRevenue.Maintain;
	}

	return null;
}

export function getDefaultAdditionalCostByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) {
		return AdditionalCost.Material;
	}

	if (type === MaterialType.SparePart) {
		return AdditionalCost.Maintain;
	}

	return null;
}

export function supportsLongTermTracking(
	type?: number | null,
	showCategoryDropdown?: boolean | null,
	category?: number | null,
): boolean {
	return (
		type === MaterialType.SparePart &&
		Boolean(showCategoryDropdown) &&
		category === MaterialsIncludedInContractRevenue.Maintain
	);
}

export function buildCategoryAllocationsForPayload(
	allocations: CategoryAllocation[] | undefined,
	materialsIncludedInContractRevenue: number,
): CategoryAllocation[] | null {
	if (
		materialsIncludedInContractRevenue !==
		MaterialsIncludedInContractRevenue.Maintain
	) {
		return null;
	}

	const normalized = (allocations ?? [])
		.filter((allocation) => allocation.processGroupId)
		.map((allocation) => ({
			processGroupId: allocation.processGroupId,
			quantity: parseQuantity(allocation.quantity),
			assignmentCodeIds: Array.from(
				new Set(allocation.assignmentCodeIds ?? allocation.equipmentIds ?? []),
			),
			equipmentIds: Array.from(
				new Set(allocation.assignmentCodeIds ?? allocation.equipmentIds ?? []),
			),
		}));

	return normalized.length > 0 ? normalized : null;
}

export function mapIssuedDetailsToBreakdown(details?: QuantityDetail[]) {
	const breakdown: Record<string, number | string> = {};
	const selectedKeys: string[] = [];
	for (const detail of details || []) {
		const key = ISSUED_DETAIL_KEY_BY_TYPE[detail.type];
		if (!key) continue;
		selectedKeys.push(key);
		breakdown[key] = detail.quantity ?? 0;
	}

	return {
		selectedKeys:
			selectedKeys.length > 0 ? selectedKeys : [RECEIVED_TYPE_OPTIONS[0].value],
		breakdown,
		total: (details || []).reduce(
			(acc, item) => acc + (Number(item.quantity) || 0),
			0,
		),
	};
}

export function mapShippedDetailsToBreakdown(details?: QuantityDetail[]) {
	const breakdown: Record<string, number | string> = {};
	const selectedKeys: string[] = [];
	for (const detail of details || []) {
		const key = SHIPPED_DETAIL_KEY_BY_TYPE[detail.type];
		if (!key) continue;
		selectedKeys.push(key);
		breakdown[key] = detail.quantity ?? 0;
	}

	return {
		selectedKeys:
			selectedKeys.length > 0 ? selectedKeys : [EXPORTED_TYPE_OPTIONS[0].value],
		breakdown,
		total: (details || []).reduce(
			(acc, item) => acc + (Number(item.quantity) || 0),
			0,
		),
	};
}

export function normalizeCode(value?: string | null): string {
	return value?.trim().toUpperCase() ?? '';
}

export const DEFAULT_OTHER_MATERIAL_DETAIL_VALUE =
	OTHER_MATERIAL_DETAIL_OPTIONS[0]?.value ?? 1;
