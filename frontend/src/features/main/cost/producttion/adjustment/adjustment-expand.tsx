import { ActionDialogProps } from '@/components/datatable';
import { Accordion } from '@/components/ui/accordion';
import { ProcessGroupType } from '@/constants/process-group';
import { AdjustmentMaterialCost } from '@/features/main/cost/producttion/adjustment/adjustment-material-cost/index';
import {
	AdjustmentCostProductDetail,
	AdjustmentProductionOutput,
} from './type';
import { AdjustmentMaintainCost } from './adjustment-maintain-cost';
import { AdjustmentElectricityCost } from './adjustment-electricity-cost';
import { ProductionAdjustment } from './columns';
import { useMemo, useState } from 'react';

const ADJUSTMENT_MATERIAL_COST_VALUE = 'adjustment-material-cost';
const ADJUSTMENT_MAINTAIN_COST_VALUE = 'adjustment-maintain-cost';
const ADJUSTMENT_ELECTRICITY_COST_VALUE = 'adjustment-electricity-cost';

type AdjustmentExpandProps = ActionDialogProps<ProductionAdjustment> & {
	monthId?: string;
};

export function AdjustmentExpand({ row, data }: AdjustmentExpandProps) {
	const [openedCosts, setOpenedCosts] = useState<string[]>([]);

	const adjustment = useMemo<AdjustmentCostProductDetail | undefined>(() => {
		if (!row) return undefined;

		return {
			id: row.productUnitPriceId ?? row.id,
			productId: row.productId,
			productCode: row.productCode,
			productName: row.productName,
			processGroupId: row.processGroupId,
			processGroupCode: row.processGroupCode,
			processGroupName: row.processGroupName ?? '',
			fixedKeyType:
				(row.fixedKeyType as ProcessGroupType | undefined) ??
				(row.processGroupType as ProcessGroupType | undefined) ??
				ProcessGroupType.None,
			processGroupType: row.processGroupType as ProcessGroupType | undefined,
			unitOfMeasureId: row.unitOfMeasureId ?? '',
			unitOfMeasureName: row.unitOfMeasureName ?? '',
			departmentId: row.departmentId,
			departmentCode: row.departmentCode,
			departmentName: row.departmentName,
			outputs: [],
			productionOutputs: [],
		};
	}, [row]);

	const productionOutput = useMemo<AdjustmentProductionOutput | undefined>(() => {
		if (!row?.productionOutputId) return undefined;

		return {
			id: row.productionOutputId,
			productionOutputId: row.productionOutputId,
			startMonth: row.startMonth,
			endMonth: row.endMonth,
			productionMeters: row.totalProductionMeters,
			adjTotalPrice: row.adjustmentTotalCost,
			standardProductionMeters: row.standardProductionMeters ?? 0,
			actualAshContent: row.actualAshContent,
			akRate: row.akRate ?? 0,
			akRatePercent: row.akRatePercent ?? 0,
		};
	}, [row]);

	return (
		<Accordion
			type='multiple'
			className='mx-2 space-y-2'
			value={openedCosts}
			onValueChange={setOpenedCosts}
		>
			<AdjustmentMaterialCost
				id={row?.plannedOutputId}
				adjustment={adjustment}
				productionOutput={productionOutput}
				callback={data.refresh}
				isOpen={openedCosts.includes(ADJUSTMENT_MATERIAL_COST_VALUE)}
			/>

			<AdjustmentMaintainCost
				id={row?.plannedOutputId}
				adjustment={adjustment}
				productionOutput={productionOutput}
				callback={data.refresh}
				isOpen={openedCosts.includes(ADJUSTMENT_MAINTAIN_COST_VALUE)}
			/>

			<AdjustmentElectricityCost
				id={row?.plannedOutputId}
				adjustment={adjustment}
				productionOutput={productionOutput}
				callback={data.refresh}
				isOpen={openedCosts.includes(ADJUSTMENT_ELECTRICITY_COST_VALUE)}
			/>
		</Accordion>
	);
}
