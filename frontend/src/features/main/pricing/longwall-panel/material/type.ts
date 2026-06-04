export type LongwallMaterialCost = {
	assignmentCodeId: string;
	materialId: string;
	norm: number;
	totalPrice: number;
};

export type LongwallMaterialDetailCost = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
	unitPrice: number;
	norm: number;
	totalPrice: number;
};

export type LongwallMaterialDetail = {
	id: string;
	code: string;
	processId: string;
	longwallParameters: { id: string; llc: string; lkc: number; mk: number };
	cuttingThickness: { id: string; value?: string; from?: string; to?: string };
	seamFaceId?: string;
	technologyId?: string;
	powerId?: string;
	hardnessId?: string;
	isLongwallMaterialUnitPriceCGH?: boolean;
	startMonth: string;
	endMonth: string;
	otherMaterialValue?: number;
	costs: LongwallMaterialDetailCost[];
};
