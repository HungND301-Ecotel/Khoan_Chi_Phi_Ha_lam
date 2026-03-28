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
import {
	getPlanedMaintainCostColumns,
	PlanedMaintainCostDetail,
} from '@/features/main/cost/plan/planed-maintain-cost/columns';
import { PlanMaintainCostForm } from '@/features/main/cost/plan/planed-maintain-cost/form';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';

export function PlanedMaintainCost({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [planedMaintainCost, setPlanedMaintainCost] =
		useState<PlanedMaintainCostDetail>();
	const [total, setTotal] = useState<number>(0);
	const [loading, setLoading] = useState<boolean>(!!id);

	useEffect(() => {
		if (!id) {
			setPlanedMaintainCost(undefined);
			setTotal(0);
			setLoading(false);
			return;
		}
		setLoading(true);
		api
			.get<PlanedMaintainCostDetail>(API.COST.PLANNED_MAINTAIN.DETAIL(id))
			.then((res) => {
				setPlanedMaintainCost(res.result);
				let total = 0;
				res.result.costs.forEach((item) => {
					const { totalPrice } = item;
					total += totalPrice;
				});
				setTotal(total * (output?.productionMeters || 1));
			})
			.finally(() => setLoading(false));
	}, [id, reloadKey, output?.productionMeters]);

	return (
		<AccordionItem value={'planed-maintain-cost'} className='border-none'>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Chi phí SCTX kế hoạch ban đầu</ItemTitle>
				</ItemContent>

				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(Math.round(total ?? 0))}
					</ItemTitle>
				</ItemContent>

				<ItemActions>
					<DialogProvider>
						<DataTableEditDialog
							type='Tạo mới'
							crumb='Chi phí SCTX kế hoạch ban đầu'
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
							<PlanMaintainCostForm
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
							crumb='Chi phí SCTX kế hoạch ban đầu'
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
							<PlanMaintainCostForm
								id={output?.plannedMaintainCostId}
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			<AccordionContent className='p-0 px-2 pt-2'>
				{id && isOpen && (
					<DataTable
						columns={getPlanedMaintainCostColumns(
							plan?.processGroupType as ProcessGroupType | undefined,
						)}
						items={planedMaintainCost?.costs}
						compact={true}
						hasActions={false}
						hasPagination={false}
						hasSort={false}
						hasIndex={false}
					/>
				)}
			</AccordionContent>
		</AccordionItem>
	);
}
