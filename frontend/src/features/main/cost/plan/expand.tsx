import { ActionDialogProps } from '@/components/datatable';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Skeleton } from '@/components/ui/skeleton';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { PlanedElectricityCost } from '@/features/main/cost/plan/planed-electricity-cost';
import { PlanedMaintainCost } from '@/features/main/cost/plan/planed-maintain-cost';
import { PlanedMaterialCost } from '@/features/main/cost/plan/planed-material-cost';
import {
	CostProduct,
	CostProductDetail,
} from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useCallback, useEffect, useState } from 'react';

const PLANED_MATERIAL_COST_VALUE = 'planed-material-cost';
const PLANED_MAINTAIN_COST_VALUE = 'planed-maintain-cost';
const PLANED_ELECTRICITY_COST_VALUE = 'planed-electricity-cost';

export function PlanExpand({ row, data }: ActionDialogProps<CostProduct>) {
	const [plan, setPlan] = useState<CostProductDetail>();
	const [openedOutputs, setOpenedOutputs] = useState<string[]>([]);
	const [openedCostsByOutput, setOpenedCostsByOutput] = useState<
		Record<string, string[]>
	>({});
	const [reloadKey, setReloadKey] = useState(0);
	const [loading, setLoading] = useState<boolean>(!!row);

	const loadPlanDetail = useCallback(async () => {
		if (!row?.id) return;
		setLoading(true);
		try {
			const planDetail = await api.get<CostProductDetail>(
				API.COST.PRODUCT.DETAIL_PLANNED(row.id),
			);
			setPlan(planDetail.result);
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

	useEffect(() => {
		if (!plan?.outputs?.length) return;

		const validOutputIds = new Set(plan.outputs.map((output) => output.id));
		setOpenedOutputs((prev) => prev.filter((id) => validOutputIds.has(id)));
		setOpenedCostsByOutput((prev) =>
			Object.fromEntries(
				Object.entries(prev).filter(([outputId]) =>
					validOutputIds.has(outputId),
				),
			),
		);
	}, [plan?.outputs]);

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
			className='mx-2 space-y-4'
			value={openedOutputs}
			onValueChange={setOpenedOutputs}
		>
			{plan?.outputs.map((output) => {
				const openedCosts = openedCostsByOutput[output.id] || [];

				return (
					<AccordionItem
						key={output.id}
						value={output.id}
						className='border-none'
					>
						<div className='flex h-10 items-center gap-8 rounded-sm bg-[#e5e7eb] px-4 py-2 hover:no-underline'>
							<span className='me-auto'>{formatDate(output.startMonth)}</span>
							<span className='w-24.5'>
								{formatNumber(output.productionMeters)}
							</span>
							<span className='w-24'>
								{formatNumber(output.totalPrice ?? 0)}
							</span>

							<AccordionTrigger className='group ms-17.5 cursor-pointer p-0'>
								<div className='group-data-[state=open]:hidden'>
									<VisibilityIcon />
								</div>
								<div className='hidden group-data-[state=open]:block'>
									<VisibilityOffIcon />
								</div>
							</AccordionTrigger>
						</div>

						<AccordionContent className='flex flex-col gap-2 p-0 pt-2'>
							<Accordion
								type='multiple'
								className='flex flex-col gap-2 px-2'
								value={openedCosts}
								onValueChange={(values) =>
									setOpenedCostsByOutput((prev) => ({
										...prev,
										[output.id]: values,
									}))
								}
							>
								<PlanedMaterialCost
									id={output.plannedMaterialCostId}
									plan={plan}
									output={output}
									callback={handleRefreshExpandData}
									isOpen={openedCosts.includes(PLANED_MATERIAL_COST_VALUE)}
									reloadKey={reloadKey}
								/>

								<PlanedMaintainCost
									id={output.plannedMaintainCostId}
									plan={plan}
									output={output}
									callback={handleRefreshExpandData}
									isOpen={openedCosts.includes(PLANED_MAINTAIN_COST_VALUE)}
									reloadKey={reloadKey}
								/>

								<PlanedElectricityCost
									id={output.plannedElectricityCostId}
									plan={plan}
									output={output}
									callback={handleRefreshExpandData}
									isOpen={openedCosts.includes(
										PLANED_ELECTRICITY_COST_VALUE,
									)}
									reloadKey={reloadKey}
								/>
							</Accordion>
						</AccordionContent>
					</AccordionItem>
				);
			})}
		</Accordion>
	);
}
