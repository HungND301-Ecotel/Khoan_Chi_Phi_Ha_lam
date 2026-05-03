import { DataTable } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { getPlanedElectricityCostColumns } from '@/features/main/cost/plan/planed-electricity-cost/columns';
import { PlanElectricityCostForm } from '@/features/main/cost/plan/planed-electricity-cost/form';
import { PlanedElectricityCostDetail } from '@/features/main/cost/plan/planed-electricity-cost/types';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';

export function PlanedElectricityCost({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [planedElectricityCost, setPlanedElectricityCost] =
		useState<PlanedElectricityCostDetail>();
	const [total, setTotal] = useState<number>(0);
	const [plannedElectricityPrice, setPlannedElectricityPrice] =
		useState<number>(0);
	const [loading, setLoading] = useState<boolean>(!!id);

	useEffect(() => {
		if (!id) {
			setPlanedElectricityCost(undefined);
			setTotal(0);
			setPlannedElectricityPrice(0);
			setLoading(false);
			return;
		}
		setLoading(true);
		api
			.get<PlanedElectricityCostDetail>(API.COST.PLANNED_ELECTRICITY.DETAIL(id))
			.then((res) => {
				setPlanedElectricityCost(res.result);
				let total = 0;
				res.result.costs.forEach(({ totalPrice }) => {
					total += totalPrice;
				});
				const trimmingCoefficient =
					plan?.processGroupType === ProcessGroupType.XL
						? res.result.trimmingCoefficient || 1
						: 1;
				setTotal(total * (output?.productionMeters || 1) * trimmingCoefficient);
				setPlannedElectricityPrice(total * trimmingCoefficient);
			})
			.finally(() => setLoading(false));
	}, [id, reloadKey, output?.productionMeters, plan?.processGroupType]);

	return (
		<AccordionItem value={'planed-electricity-cost'} className='border-none'>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Doanh thu điện năng kế hoạch ban đầu</ItemTitle>
				</ItemContent>

				<ItemContent className='me-2 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(plannedElectricityPrice)}
					</ItemTitle>
				</ItemContent>

				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(total ?? 0)}
					</ItemTitle>
				</ItemContent>
				<ItemActions>
					<DialogProvider>
						<DataTableEditDialog
							type='Tạo mới'
							crumb='Doanh thu điện năng kế hoạch ban đầu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!!id}
								>
									<AddIcon />
								</Button>
							}
						>
							<PlanElectricityCostForm
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>

					<AccordionTrigger
						disabled={!id}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
					<DialogProvider>
						<DataTableEditDialog
							type='Chỉnh sửa'
							crumb='Doanh thu điện năng kế hoạch ban đầu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!id}
								>
									<CreateIcon />
								</Button>
							}
						>
							<PlanElectricityCostForm
								id={output?.plannedElectricityCostId}
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			{id && isOpen && (
				<AccordionContent className='p-0 px-2 pt-2'>
					<DataTable
						columns={getPlanedElectricityCostColumns(
							plan?.processGroupType as ProcessGroupType | undefined,
						)}
						items={planedElectricityCost?.costs}
						compact={true}
						hasActions={false}
						hasPagination={false}
						hasSort={false}
						hasIndex={false}
					/>
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
