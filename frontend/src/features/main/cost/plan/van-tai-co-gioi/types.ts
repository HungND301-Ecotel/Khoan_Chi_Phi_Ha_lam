export type MotorizedCategory =
	| 'scania'
	| 'excavator_dozer'
	| 'service_crane'
	| 'vacuum_truck';

export type MotorizedCategoryOption = {
	id: MotorizedCategory;
	code: string;
	name: string;
	description: string;
};

export const MOTORIZED_PLAN_CATEGORIES: MotorizedCategoryOption[] = [
	{
		id: 'scania',
		code: 'SCANIA',
		name: 'Vận chuyển (Xe Scania)',
		description: 'Vận chuyển than, đất đá, bùn, than cám & các loại cục',
	},
	{
		id: 'excavator_dozer',
		code: 'EXCAVATOR',
		name: 'Xúc, gạt, nâng',
		description:
			'Xúc đất đá, xúc than kho, giờ phục vụ, giờ di chuyển, giờ gạt',
	},
	{
		id: 'service_crane',
		code: 'SERVICE_CRANE',
		name: 'Phục vụ',
		description: 'Xe cẩu, xe cứu hỏa, xe tưới đường, xe bán tải',
	},
	{
		id: 'vacuum_truck',
		code: 'VACUUM_TRUCK',
		name: 'Hút bùn chất thải',
		description: 'Vận chuyển chất thải, hút bùn hố lắng',
	},
];
