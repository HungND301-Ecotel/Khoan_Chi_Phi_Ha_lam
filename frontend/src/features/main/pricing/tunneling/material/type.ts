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
