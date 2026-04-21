import { Factor } from '@/features/main/catalog/adjustment/factor/columns';
import { Interpreter } from '@/features/main/catalog/adjustment/interpreter/columns';
import {
	CostPlanAdjustmentDetail,
	CostPlanAdjustmentSelection,
} from '@/features/main/cost/plan/types';

export type AdjustmentDetail = Factor & {
	adjustmentFactorDescriptions: Pick<
		Interpreter,
		| 'id'
		| 'description'
		| 'maintenanceAdjustmentValue'
		| 'electricityAdjustmentValue'
	>[];
};

export type PlannedMaintainCostDetailItemDescription = CostPlanAdjustmentDetail;

export type PlannedMaintainCostAdjustmentSelection = CostPlanAdjustmentSelection;

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
	trimmingCoefficient: number;
	costs: PlannedMaintainCostDetailItem[];
};
