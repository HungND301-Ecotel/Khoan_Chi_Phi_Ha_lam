export type Material = {
	id: string;
	code: string;
	processId: string;
	passportId: string;
	hardnessId: string;
	insertItemId: string;
	supportStepId: string;
	processName: string;
	passportName: string;
	hardnessName: string;
	insertItemName: string;
	supportStepName: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export type MaterialDetail = {
	costs: Array<{
		assignmentCodeId: string;
		totalPrice: number;
	}>;
	otherMaterialValue?: number;
};

export type SupportAndDrillingMaterial = {
	id: string;
	code: string;
	processId: string;
	processName: string;
	passportId: string;
	passportName: string;
	hardnessId: string;
	hardnessName: string;
	technologyId?: string | null;
	technologyName?: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export type SupportAndDrillingMaterialDetail = {
	id: string;
	code: string;
	processId: string;
	passportId: string;
	hardnessId: string;
	technologyId?: string | null;
	startMonth: string;
	endMonth: string;
	costs: Array<{
		assignmentCodeId: string;
		totalPrice: number;
	}>;
	otherMaterialValue?: number;
};
