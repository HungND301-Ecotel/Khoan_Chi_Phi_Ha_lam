import { ActionDialogProps } from '@/components/datatable';
import { Accordion } from '@/components/ui/accordion';
import { Skeleton } from '@/components/ui/skeleton';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { PlanedElectricityCost } from '@/features/main/cost/plan/planed-electricity-cost';
import { PlanedMaintainCost } from '@/features/main/cost/plan/planed-maintain-cost';
import { PlanedMaterialCost } from '@/features/main/cost/plan/planed-material-cost';
import {
	CostProduct,
	CostProductDetail,
	CostProductDetailOutput,
	mapCostProductDetail,
} from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { useCallback, useEffect, useMemo, useState } from 'react';

const PLANED_MATERIAL_COST_VALUE = 'planed-material-cost';
const PLANED_MAINTAIN_COST_VALUE = 'planed-maintain-cost';
const PLANED_ELECTRICITY_COST_VALUE = 'planed-electricity-cost';

type PlanExpandProps = ActionDialogProps<CostProduct> & {
	monthId?: string;
};

function isSameMonth(output: CostProductDetailOutput, monthId?: string) {
	if (!monthId) return true;
	return output.startMonth?.substring(0, 10) === monthId.substring(0, 10);
}

export function PlanExpand({ row, data, monthId }: PlanExpandProps) {
	const [plan, setPlan] = useState<CostProductDetail>();
	const [openedCosts, setOpenedCosts] = useState<string[]>([]);
	const [reloadKey, setReloadKey] = useState(0);
	const [loading, setLoading] = useState<boolean>(!!row);

	const loadPlanDetail = useCallback(async () => {
		if (!row?.id) return;
		setLoading(true);
		try {
			const planDetail = await api.get<CostProductDetail>(
				API.COST.PRODUCT.DETAIL_PLANNED(row.id),
			);
			setPlan(mapCostProductDetail(planDetail.result));
		} finally {
			setLoading(false);
		}
	}, [row?.id]);

	useEffect(() => {
		loadPlanDetail();
	}, [loadPlanDetail]);

	const handleRefreshExpandData = useCallback(async () => {
		await Promise.all([data.refresh(), loadPlanDetail()]);
		setReloadKey((prev) => prev + 1);
	}, [data, loadPlanDetail]);

	const matchedOutput = useMemo(
		() => plan?.outputs.find((output) => isSameMonth(output, monthId)),
		[monthId, plan?.outputs],
	);

	if (loading)
		return (
			<div className='mx-2 flex h-10 items-center gap-8 rounded-sm bg-[#e5e7eb] px-4 py-2 hover:no-underline'>
				<Skeleton className='bg-muted-foreground h-4 w-full rounded-full' />
				<Spinner />
			</div>
		);

	return (
		<Accordion
			type='multiple'
			className='mx-2 space-y-2'
			value={openedCosts}
			onValueChange={setOpenedCosts}
		>
			<PlanedMaterialCost
				id={matchedOutput?.plannedMaterialCostId}
				plan={plan}
				output={matchedOutput}
				callback={handleRefreshExpandData}
				isOpen={openedCosts.includes(PLANED_MATERIAL_COST_VALUE)}
				reloadKey={reloadKey}
			/>

			<PlanedMaintainCost
				id={matchedOutput?.plannedMaintainCostId}
				plan={plan}
				output={matchedOutput}
				callback={handleRefreshExpandData}
				isOpen={openedCosts.includes(PLANED_MAINTAIN_COST_VALUE)}
				reloadKey={reloadKey}
			/>

			<PlanedElectricityCost
				id={matchedOutput?.plannedElectricityCostId}
				plan={plan}
				output={matchedOutput}
				callback={handleRefreshExpandData}
				isOpen={openedCosts.includes(PLANED_ELECTRICITY_COST_VALUE)}
				reloadKey={reloadKey}
			/>
		</Accordion>
	);
}
