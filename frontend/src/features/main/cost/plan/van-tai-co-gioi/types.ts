export type MotorizedCategory =
	| 'scania'
	| 'excavator_dozer'
	| 'service_crane'
	| 'vacuum_truck';

export type MotorizedCategoryOption = {
	id: MotorizedCategory;
	code: string;
	name: string;
};

export const MOTORIZED_PLAN_CATEGORIES: MotorizedCategoryOption[] = [
	{
		id: 'scania',
		code: 'SCANIA',
		name: 'Vận chuyển (Xe Scania)',
	},
	{
		id: 'excavator_dozer',
		code: 'EXCAVATOR',
		name: 'Xúc, gạt, nâng',
	},
	{
		id: 'service_crane',
		code: 'SERVICE_CRANE',
		name: 'Phục vụ',
	},
	{
		id: 'vacuum_truck',
		code: 'VACUUM_TRUCK',
		name: 'Hút bùn chất thải',
	},
];
