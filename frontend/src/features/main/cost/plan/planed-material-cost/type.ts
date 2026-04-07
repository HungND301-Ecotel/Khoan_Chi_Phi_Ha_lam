export type UnifiedMaterial = {
	id: string;
	code: string;
	processId: string;
	processName: string;
	passportId: string;
	passportName: string;
	hardnessId: string;
	hardnessName: string;
	insertItemId: string;
	insertItemName: string;
	supportStepId: string;
	supportStepName: string;

	technologyId: string;
	technologyName: string;
	type: number;
	longwallParametersId: string;
	longwallParametersName: string;
	cuttingThicknessId: string;
	cuttingThicknessName: string;
	seamFaceId: string;
	seamFaceName: string;
	powerId?: string | null;
	powerName?: string;
	isLongwallMaterialUnitPriceCGH?: boolean;

	startMonth: string;
	endMonth: string;
	totalPrice: number;
};
