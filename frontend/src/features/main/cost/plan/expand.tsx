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
import { useEffect, useState } from 'react';

export function PlanExpand({ row, data }: ActionDialogProps<CostProduct>) {
	const [plan, setPlan] = useState<CostProductDetail>();
	const [opened, setOpened] = useState<string[]>([]);
	const [loading, setLoading] = useState<boolean>(!!row);

	useEffect(() => {
		if (!row) return;

		const promises = Promise.all([
			api.get<CostProductDetail>(API.COST.PRODUCT.DETAIL_PLANNED(row.id)),
		]);

		promises
			.then(([plan]) => {
				setPlan(plan.result);
			})
			.finally(() => setLoading(false));
	}, [row]);

	if (loading)
		return (
			<div className='mx-2 flex h-10 items-center gap-8 rounded-sm bg-[#e5e7eb] px-4 py-2 hover:no-underline'>
				<Skeleton className='bg-muted-foreground h-4 w-full rounded-full' />
				<Spinner />
			</div>
		);

	return (
		<Accordion type='multiple' className='mx-2 space-y-4'>
			{plan?.outputs.map((output) => {
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
								{formatNumber(Math.round(output.totalPrice ?? 0))}
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
								value={opened}
								onValueChange={setOpened}
							>
								<PlanedMaterialCost
									id={output.plannedMaterialCostId}
									plan={plan}
									output={output}
									callback={data.refresh}
									isOpen={opened.includes('planed-material-cost')}
								/>

								<PlanedMaintainCost
									id={output.plannedMaintainCostId}
									plan={plan}
									output={output}
									callback={data.refresh}
									isOpen={opened.includes('planed-maintain-cost')}
								/>

								<PlanedElectricityCost
									id={output.plannedElectricityCostId}
									plan={plan}
									output={output}
									callback={data.refresh}
									isOpen={opened.includes('planed-electricity-cost')}
								/>
							</Accordion>
						</AccordionContent>
					</AccordionItem>
				);
			})}
		</Accordion>
	);
}
