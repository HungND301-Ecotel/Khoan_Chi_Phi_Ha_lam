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
import { AdjustmentMaterialCost } from '@/features/main/cost/producttion/adjustment/adjustment-material-cost/index';
import {
	AdjustmentCostProductDetail,
	AdjustmentOutput,
	AdjustmentProductionOutput,
} from './type';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { AdjustmentMaintainCost } from './adjustment-maintain-cost';
import { AdjustmentElectricityCost } from './adjustment-electricity-cost';
import { ProductionAdjustment } from './columns';

const ADJUSTMENT_MATERIAL_COST_VALUE = 'adjustment-material-cost';
const ADJUSTMENT_MAINTAIN_COST_VALUE = 'adjustment-maintain-cost';
const ADJUSTMENT_ELECTRICITY_COST_VALUE = 'adjustment-electricity-cost';

export function AdjustmentExpand({
	row,
	data,
}: ActionDialogProps<ProductionAdjustment>) {
	const [adjustment, setAdjustment] = useState<AdjustmentCostProductDetail>();
	const [openedOutputs, setOpenedOutputs] = useState<string[]>([]);
	const [openedCostsByOutput, setOpenedCostsByOutput] = useState<
		Record<string, string[]>
	>({});
	const [loading, setLoading] = useState<boolean>(!!row);

	useEffect(() => {
		if (!row?.id) return;
		setLoading(true);

		const promises = Promise.all([
			api.get<AdjustmentCostProductDetail>(
				API.COST.PRODUCT.DETAIL_ADJUSTMENT(row.id),
			),
		]);

		promises
			.then(([adjustment]) => {
				setAdjustment(adjustment.result);
			})
			.finally(() => setLoading(false));
	}, [row?.id]);

	useEffect(() => {
		const items =
			adjustment?.outputs && adjustment.outputs.length > 0
				? adjustment.outputs
				: adjustment?.productionOutputs;
		if (!items?.length) return;

		const validIds = new Set(items.map((item) => item.id));
		setOpenedOutputs((prev) => prev.filter((id) => validIds.has(id)));
		setOpenedCostsByOutput((prev) =>
			Object.fromEntries(
				Object.entries(prev).filter(([outputId]) => validIds.has(outputId)),
			),
		);
	}, [adjustment?.outputs, adjustment?.productionOutputs]);

	const hasOutputs = adjustment?.outputs && adjustment.outputs.length > 0;
	const displayItems = hasOutputs
		? adjustment?.outputs
		: adjustment?.productionOutputs;

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
			{displayItems?.map((item) => {
				const openedCosts = openedCostsByOutput[item.id] || [];
				const matchedProductionOutput = hasOutputs
					? adjustment?.productionOutputs.find(
							(po) =>
								po.startMonth === item.startMonth &&
								po.endMonth === item.endMonth,
						)
					: (item as AdjustmentProductionOutput);
				const displayStartMonth = item.startMonth;
				const displayMeters =
					matchedProductionOutput?.productionMeters ??
					(item as AdjustmentOutput).productionMeters;

				return (
					<AccordionItem key={item.id} value={item.id} className='border-none'>
						<div className='flex h-10 items-center gap-8 rounded-sm bg-[#e5e7eb] px-4 py-2 hover:no-underline'>
							<span className='me-auto'>{formatDate(displayStartMonth)}</span>
							<span className='w-24.5'>{formatNumber(displayMeters || 0)}</span>
							<span className='w-24'>
								{formatNumber(matchedProductionOutput?.adjTotalPrice || 0)}
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
										[item.id]: values,
									}))
								}
							>
								<AdjustmentMaterialCost
									id={item.id}
									adjustment={adjustment!}
									output={hasOutputs ? (item as AdjustmentOutput) : undefined}
									productionOutput={matchedProductionOutput}
									callback={data.refresh}
									isOpen={openedCosts.includes(
										ADJUSTMENT_MATERIAL_COST_VALUE,
									)}
								/>

								<AdjustmentMaintainCost
									id={item.id}
									adjustment={adjustment!}
									output={hasOutputs ? (item as AdjustmentOutput) : undefined}
									productionOutput={matchedProductionOutput}
									callback={data.refresh}
									isOpen={openedCosts.includes(
										ADJUSTMENT_MAINTAIN_COST_VALUE,
									)}
								/>

								<AdjustmentElectricityCost
									id={item.id}
									adjustment={adjustment!}
									output={hasOutputs ? (item as AdjustmentOutput) : undefined}
									productionOutput={matchedProductionOutput}
									callback={data.refresh}
									isOpen={openedCosts.includes(
										ADJUSTMENT_ELECTRICITY_COST_VALUE,
									)}
								/>
							</Accordion>
						</AccordionContent>
					</AccordionItem>
				);
			})}
		</Accordion>
	);
}
