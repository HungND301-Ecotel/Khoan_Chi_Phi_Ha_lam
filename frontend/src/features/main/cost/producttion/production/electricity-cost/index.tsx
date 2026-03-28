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
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { PRODUCTION_ELECTRICITY_COST_COLUMNS } from '@/features/main/cost/producttion/production/electricity-cost/columns';
import { ProductionElectricityCostForm } from '@/features/main/cost/producttion/production/electricity-cost/form';
import {
	ProductionActualElectricityCostDetail,
	ProductionElectricityCostItem,
} from '@/features/main/cost/producttion/production/electricity-cost/types';
import { API } from '@/constants/api-enpoint';
import { api, ErrorResponse } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useMemo, useState } from 'react';

const EMPTY_ITEMS: ProductionElectricityCostItem[] = [];

export function ProductionElectricityCost({
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [actualElectricityCostId, setActualElectricityCostId] = useState<
		string | undefined
	>(undefined);
	const [items, setItems] = useState<ProductionElectricityCostItem[]>([]);

	useEffect(() => {
		if (!output?.acceptanceReportId) {
			return;
		}

		api
			.get<ProductionActualElectricityCostDetail>(
				API.COST.ACTUAL_ELECTRICITY.DETAIL(output.acceptanceReportId),
			)
			.then((res) => {
				setActualElectricityCostId(res.result.id);
				setItems(
					res.result.equipments.map((item, index) => ({
						id: `${item.equipmentId}-${index}`,
						equipmentId: item.equipmentId,
						equipmentCode: item.equipmentCode,
						equipmentName: item.equipmentName,
						electricityUnitPrice: item.electricityUnitPrice || 0,
						electricityConsumption: item.actualElectricityConsumption || 0,
						electricityCost: item.totalPrice || 0,
					})),
				);
			})
			.catch((err) => {
				const maybeError = err as ErrorResponse;
				if (maybeError?.status === 404) {
					setActualElectricityCostId(undefined);
					setItems([]);
					return;
				}
				console.error('Failed to load actual electricity cost detail:', err);
			});
	}, [output?.acceptanceReportId, reloadKey]);

	const hasCreated = !!actualElectricityCostId && items.length > 0;
	const totalCost = useMemo(
		() => items.reduce((sum, item) => sum + item.electricityCost, 0),
		[items],
	);

	return (
		<AccordionItem
			value={'production-electricity-cost'}
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Chi phí điện năng</ItemTitle>
				</ItemContent>
				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>{formatNumber(Math.round(totalCost))}</ItemTitle>
				</ItemContent>
				<ItemActions>
					<DialogProvider>
						<DataTableEditDialog
							type='Tạo mới'
							crumb='Chi phí điện năng'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={hasCreated || !output?.acceptanceReportId}
								>
									<AddIcon />
								</Button>
							}
						>
							<ProductionElectricityCostForm
								id={undefined}
								plan={plan}
								output={output}
								callback={callback}
								initialItems={EMPTY_ITEMS}
								onSave={({ id, items }) => {
									setActualElectricityCostId(id);
									setItems(items);
								}}
							/>
						</DataTableEditDialog>
					</DialogProvider>

					<AccordionTrigger
						disabled={!hasCreated}
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
							crumb='Chi phí điện năng'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!hasCreated}
								>
									<CreateIcon />
								</Button>
							}
						>
							<ProductionElectricityCostForm
								id={actualElectricityCostId}
								plan={plan}
								output={output}
								callback={callback}
								initialItems={items}
								onSave={({ id, items }) => {
									setActualElectricityCostId(id);
									setItems(items);
								}}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			{hasCreated && isOpen && (
				<AccordionContent className='p-0 px-2 pt-2'>
					<DataTable
						columns={PRODUCTION_ELECTRICITY_COST_COLUMNS}
						items={items}
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
