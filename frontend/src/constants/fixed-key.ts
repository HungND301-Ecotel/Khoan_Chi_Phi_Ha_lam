export type FixedKey = {
	id: string;
	code: string;
	name: string;
	type: number;
	isSystem: boolean;
};

export const FixedKeyType = {
	None: 0,
	ProcessGroup: 1,
	MaterialsIncludedInContractRevenue: 2,
	AdditionalCost: 3,
	OtherMaterialDetail: 4,
	QuotaBasedMaterial: 5,
	QuotaBasedMaterialType: 6,
	Asset: 7,
	IssuedQuantityType: 8,
	ShippedQuantityType: 9,
} as const;

export const FIXED_KEY_TYPE_OPTIONS = [
	{ value: String(FixedKeyType.ProcessGroup), label: 'Nhóm công đoạn sản xuất' },
	{
		value: String(FixedKeyType.MaterialsIncludedInContractRevenue),
		label: 'Vật tư tính vào doanh thu khoán',
	},
	{ value: String(FixedKeyType.AdditionalCost), label: 'Bổ sung chi phí' },
	{
		value: String(FixedKeyType.OtherMaterialDetail),
		label: 'Chi tiết vật tư khác',
	},
	{ value: String(FixedKeyType.QuotaBasedMaterial), label: 'Vật tư theo hạn mức' },
	{
		value: String(FixedKeyType.QuotaBasedMaterialType),
		label: 'Loại vật tư theo hạn mức',
	},
	{ value: String(FixedKeyType.Asset), label: 'Tài sản' },
	{ value: String(FixedKeyType.IssuedQuantityType), label: 'Loại lĩnh' },
	{ value: String(FixedKeyType.ShippedQuantityType), label: 'Loại xuất' },
] as const;

export const FIXED_KEY_TYPE_LABELS: Record<number, string> = {
	[FixedKeyType.None]: 'Không xác định',
	[FixedKeyType.ProcessGroup]: 'Nhóm công đoạn sản xuất',
	[FixedKeyType.MaterialsIncludedInContractRevenue]:
		'Vật tư tính vào doanh thu khoán',
	[FixedKeyType.AdditionalCost]: 'Bổ sung chi phí',
	[FixedKeyType.OtherMaterialDetail]: 'Chi tiết vật tư khác',
	[FixedKeyType.QuotaBasedMaterial]: 'Vật tư theo hạn mức',
	[FixedKeyType.QuotaBasedMaterialType]: 'Loại vật tư theo hạn mức',
	[FixedKeyType.Asset]: 'Tài sản',
	[FixedKeyType.IssuedQuantityType]: 'Loại lĩnh',
	[FixedKeyType.ShippedQuantityType]: 'Loại xuất',
};

const normalizeFixedKeyCode = (value: string) =>
	(value ?? '')
		.replace(/[^a-zA-Z0-9]/g, '')
		.toUpperCase();

export function getFixedKeyTypeLabel(type: number) {
	return FIXED_KEY_TYPE_LABELS[type] ?? 'Không xác định';
}

export function findFixedKeyByCode(
	fixedKeys: FixedKey[],
	type: number,
	code?: string | null,
) {
	if (!code) return undefined;
	const normalizedCode = normalizeFixedKeyCode(code);
	return fixedKeys.find(
		(item) =>
			item.type === type && normalizeFixedKeyCode(item.code) === normalizedCode,
	);
}

export function findFixedKeyIdByCode(
	fixedKeys: FixedKey[],
	type: number,
	code?: string | null,
) {
	return findFixedKeyByCode(fixedKeys, type, code)?.id ?? null;
}

export function findMappedFixedKeyId(
	fixedKeys: FixedKey[],
	type: number,
	codeMap: Record<number, string>,
	value?: number | null,
) {
	if (value == null) return null;
	return findFixedKeyIdByCode(fixedKeys, type, codeMap[value]);
}

export const ACCEPTANCE_REPORT_FIXED_KEY_CODES = {
	materialsIncludedInContractRevenue: {
		2: 'Material',
		3: 'Maintain',
	},
	additionalCost: {
		2: 'Material',
		3: 'Maintain',
		4: 'SafeAndWelfare',
	},
	otherMaterialDetail: {
		2: 'BaoHoLaoDong',
		3: 'VatTuPhucVuCongTacAnToan',
	},
	quotaBasedMaterial: {
		2: 'MineSupport',
		3: 'SupportAccessories',
		4: 'MineTimber',
	},
	quotaBasedMaterialType: {
		1: 'New',
		2: 'Reusable',
	},
	asset: {
		2: 'True',
	},
	issuedQuantityType: {
		1: 'LinhVatTuTraPhieu',
		2: 'VayVhuaTraPhieu',
		3: 'TraPhieuThangTruoc',
		4: 'LinhKhac',
	},
	shippedQuantityType: {
		1: 'XuatChoSanXuat',
		2: 'XuatKhac',
		3: 'QuyetToanGiaoKhoan',
	},
} as const;