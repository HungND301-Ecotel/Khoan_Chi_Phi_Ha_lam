export const LowValuePerishableSupplyType = {
	TunnelExcavation: 1,
	Longwall: 2,
} as const;

export type LowValuePerishableSupplyType =
	(typeof LowValuePerishableSupplyType)[keyof typeof LowValuePerishableSupplyType];

export const LowValuePerishableSupplyInclusion = {
	Exclude: 1,
	Include: 2,
} as const;

export type LowValuePerishableSupplyInclusion =
	(typeof LowValuePerishableSupplyInclusion)[keyof typeof LowValuePerishableSupplyInclusion];

export const LOW_VALUE_PERISHABLE_SUPPLY_INCLUSION_OPTIONS = [
	{
		label: 'Không gồm đơn giá vật tư mau hỏng rẻ tiền',
		value: LowValuePerishableSupplyInclusion.Exclude,
	},
	{
		label: 'Gồm đơn giá vật tư mau hỏng rẻ tiền',
		value: LowValuePerishableSupplyInclusion.Include,
	},
];
