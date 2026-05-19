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
import { ProcessGroupType } from '@/constants/process-group';

import { AdjustmentCostExpandProps } from '@/features/main/cost/producttion/adjustment/adjustment-material-cost';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import {
	AdjustmentMaintainCostDetail,
	AdjustmentMaintainCostSummary,
	getAdjustmentMaintainCostColumns,
	getAdjustmentMaintainCostSummaryColumns,
} from './columns';

export function AdjustmentMaintainCost({
	id,
	isOpen,
	adjustment,
	productionOutput,
	multiplyByProductionMeters = true,
}: AdjustmentCostExpandProps) {
	const [adjustmentMaintainCost, setAdjustmentMaintainCost] =
		useState<AdjustmentMaintainCostDetail>();
	const [summary, setSummary] = useState<AdjustmentMaintainCostSummary[]>([]);
	const [adjustmentMaintainPrice, setAdjustmentMaintainPrice] =
		useState<number>(0);
	const [total, setTotal] = useState<number>(0);
	const [loading, setLoading] = useState<boolean>(!!id);

	useEffect(() => {
		if (!id) {
			setAdjustmentMaintainCost(undefined);
			setSummary([]);
			setAdjustmentMaintainPrice(0);
			setTotal(0);
			setLoading(false);
			return;
		}
		setLoading(true);
		api
			.get<AdjustmentMaintainCostDetail>(
				API.COST.ADJUSTMENT_MAINTAIN.DETAIL(id),
			)
			.then((res) => {
				setAdjustmentMaintainCost(res.result);
				setSummary([
					{
						akRatePercent: res.result.akRatePercent || 0,
					},
				]);
				let total = 0;
				res.result.costs.forEach((item) => {
					const { totalPrice } = item;
					total += totalPrice;
				});
				const trimmingCoefficient =
					adjustment?.fixedKeyType === ProcessGroupType.XL ? 1 : 1;
				setAdjustmentMaintainPrice(total * trimmingCoefficient);
				setTotal(
					multiplyByProductionMeters
						? total *
								(productionOutput?.productionMeters || 1) *
								trimmingCoefficient
						: total,
				);
			})
			.finally(() => setLoading(false));
	}, [
		id,
		adjustment?.fixedKeyType,
		productionOutput?.productionMeters,
		multiplyByProductionMeters,
	]);

	return (
		<AccordionItem value={'adjustment-maintain-cost'} className='border-none'>
			<Item
				variant={'outline'}
				size='sm'
				className='w-full flex-1 rounded-sm border-[#b8b8b8] bg-[#f3f4f6] py-2.5 shadow-none'
			>
				<ItemContent>
					<ItemTitle>Doanh thu SCTX điều chỉnh</ItemTitle>
				</ItemContent>

				<ItemContent className='me-2 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(adjustmentMaintainPrice)}
					</ItemTitle>
				</ItemContent>

				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(total ?? 0)}
					</ItemTitle>
				</ItemContent>

				<ItemActions className='gap-1'>
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
			<AccordionContent className='p-0 px-1 pt-1.5'>
				{id && isOpen && (
					<div className='space-y-2'>
						<DataTable
							columns={getAdjustmentMaintainCostSummaryColumns()}
							items={summary}
							compact={true}
							hasActions={false}
							hasPagination={false}
							hasSort={false}
							hasIndex={false}
						/>
						<DataTable
							columns={getAdjustmentMaintainCostColumns(
								adjustment?.fixedKeyType as ProcessGroupType | undefined,
							)}
							items={adjustmentMaintainCost?.costs}
							compact={true}
							hasActions={false}
							hasPagination={false}
							hasSort={false}
							hasIndex={false}
						/>
					</div>
				)}
			</AccordionContent>
		</AccordionItem>
	);
}
