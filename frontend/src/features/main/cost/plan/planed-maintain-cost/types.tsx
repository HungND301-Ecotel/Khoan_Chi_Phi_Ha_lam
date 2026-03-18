import { Factor } from '@/features/main/catalog/adjustment/factor/columns';
import { Interpreter } from '@/features/main/catalog/adjustment/interpreter/columns';

export type AdjustmentDetail = Factor & {
	adjustmentFactorDescriptions: Pick<
		Interpreter,
		| 'id'
		| 'description'
		| 'maintenanceAdjustmentValue'
		| 'electricityAdjustmentValue'
	>[];
};

export type PlannedMaintainCostDetailItemDescription = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	adjustmentFactorName: string;
	maintenanceAdjustmentValue: number;
};

export type PlannedMaintainCostDetailItem = {
	maintainUnitPriceId: string;
	maintainUnitPrice: number;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	quantity: number;
	k6AdjustmentFactorValue: number;
	totalPrice: number;
	adjustmentFactorDescriptions: PlannedMaintainCostDetailItemDescription[];
};

export type PlannedMaintainCostDetail = {
	id: string;
	productUnitPriceId: string;
	outputId: string;
	costs: PlannedMaintainCostDetailItem[];
};
