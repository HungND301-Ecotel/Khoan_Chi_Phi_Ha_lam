export const AdjustmentFactorType = {
	None: 0,
	K1: 4,
	K2: 5,
	K3: 6,
	K4: 7,
	K5: 8,
	K6: 9,
	K7: 10,
	K8: 11,
} as const;

export type AdjustmentFactorType =
	(typeof AdjustmentFactorType)[keyof typeof AdjustmentFactorType];

export const ADJUSTMENT_FACTOR_TYPE_LABELS: Record<number, string> = {
	[AdjustmentFactorType.None]: 'Chưa xác định',
	[AdjustmentFactorType.K1]: 'K1',
	[AdjustmentFactorType.K2]: 'K2',
	[AdjustmentFactorType.K3]: 'K3',
	[AdjustmentFactorType.K4]: 'K4',
	[AdjustmentFactorType.K5]: 'K5',
	[AdjustmentFactorType.K6]: 'K6',
	[AdjustmentFactorType.K7]: 'K7',
	[AdjustmentFactorType.K8]: 'K8',
};

export const ADJUSTMENT_FACTOR_TYPE_OPTIONS = [
	{ value: AdjustmentFactorType.K1, label: 'K1' },
	{ value: AdjustmentFactorType.K2, label: 'K2' },
	{ value: AdjustmentFactorType.K3, label: 'K3' },
	{ value: AdjustmentFactorType.K4, label: 'K4' },
	{ value: AdjustmentFactorType.K5, label: 'K5' },
	{ value: AdjustmentFactorType.K6, label: 'K6' },
	{ value: AdjustmentFactorType.K7, label: 'K7' },
	{ value: AdjustmentFactorType.K8, label: 'K8' },
];

export function isAdjustmentFactorFixedKey(key?: string) {
	return /^K[1-8]$/i.test((key ?? '').trim());
}
