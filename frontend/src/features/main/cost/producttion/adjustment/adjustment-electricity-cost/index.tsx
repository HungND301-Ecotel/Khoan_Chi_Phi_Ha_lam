import { DataTable } from '@/components/datatable';
import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { AdjustmentCostExpandProps } from '@/features/main/cost/producttion/adjustment/adjustment-material-cost';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { AdjustmentElectricityCostDetail } from './types';
import { ADJUSTMENT_ELECTRICITY_COST_COLUMNS } from './columns';

export function AdjustmentElectricityCost({
	id,
	isOpen,
	productionOutput,
	multiplyByProductionMeters = true,
}: AdjustmentCostExpandProps) {
	const [adjustmentElectricityCost, setAdjustmentElectricityCost] =
		useState<AdjustmentElectricityCostDetail>();
	const [total, setTotal] = useState<number>(0);
	const [loading, setLoading] = useState<boolean>(!!id);

	useEffect(() => {
		if (!id) {
			setAdjustmentElectricityCost(undefined);
			setTotal(0);
			setLoading(false);
			return;
		}
		setLoading(true);
		api
			.get<AdjustmentElectricityCostDetail>(
				API.COST.ADJUSTMENT_ELECTRICITY.DETAIL(id),
			)
			.then((res) => {
				setAdjustmentElectricityCost(res.result);
				let total = 0;
				res.result.costs.forEach(({ totalPrice }) => {
					total += totalPrice;
				});
				setTotal(
					multiplyByProductionMeters
						? total * (productionOutput?.productionMeters || 1)
						: total,
				);
			})
			.finally(() => setLoading(false));
	}, [id, productionOutput?.productionMeters, multiplyByProductionMeters]);

	return (
		<AccordionItem
			value={'adjustment-electricity-cost'}
			className='border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Doanh thu điện năng điều chỉnh</ItemTitle>
				</ItemContent>
				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(Math.round(total ?? 0))}
					</ItemTitle>
				</ItemContent>
				<ItemActions>
					<div className='size-5' />
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
					<div className='size-5' />{' '}
				</ItemActions>
			</Item>
			{id && isOpen && (
				<AccordionContent className='p-0 px-2 pt-2'>
					<DataTable
						columns={ADJUSTMENT_ELECTRICITY_COST_COLUMNS}
						items={adjustmentElectricityCost?.costs}
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
