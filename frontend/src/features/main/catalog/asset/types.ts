export type Asset = {
	id: string;
	code: string;
	name: string;
	assignmentCodeIds: string[];
	isSlideAssignmentCode: boolean;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	usageTime: number;
	costAmount: number;
	actualAmount: number;
};
