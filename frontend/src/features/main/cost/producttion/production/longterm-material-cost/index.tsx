import { DataTableEditDialog } from '@/components/datatable/edit';
import {
	Accordion,
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
import { API } from '@/constants/api-enpoint';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { LongtermMaterialCostForm } from '@/features/main/cost/producttion/production/longterm-material-cost/form';
import {
	LongtermMaterialCostDetail,
	LongTermTrackingProcessGroup,
	LongTermTrackingResponse,
} from '@/features/main/cost/producttion/production/longterm-material-cost/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { FixedColumnDataTable } from './datatable';

export function LongTermMaterialCosts({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [additionalCostData, setAdditionalCostData] =
		useState<LongtermMaterialCostDetail>();
	const [loading, setLoading] = useState<boolean>(false);

	useEffect(() => {
		if (!isOpen || !output?.acceptanceReportId) {
			return;
		}

		const fetchLongTermTracking = async () => {
			setLoading(true);
			try {
				const response = await api.get<LongTermTrackingResponse>(
					API.PRODUCTION.ACCEPTANCE_REPORT.LONG_TERM_TRACKING_LIST(
						output.acceptanceReportId!,
					),
				);

				if (response.result) {
					setAdditionalCostData(response.result);
				}
			} catch (err) {
				console.error('Failed to fetch long-term tracking:', err);
				setAdditionalCostData(undefined);
			} finally {
				setLoading(false);
			}
		};

		fetchLongTermTracking();
	}, [isOpen, output?.acceptanceReportId, reloadKey]);

	const groupedItems: LongTermTrackingProcessGroup[] =
		additionalCostData?.processGroups &&
		additionalCostData.processGroups.length > 0
			? additionalCostData.processGroups
			: additionalCostData?.items && additionalCostData.items.length > 0
				? [
						{
							processGroupId: 'all',
							processGroupCode: '',
							processGroupName: 'Tất cả nhóm công đoạn',
							items: additionalCostData.items,
						},
					]
				: [];

	return (
		<AccordionItem
			value={'longterm-material-cost'}
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Bảng hạch toán chi phí vật tư dài kỳ</ItemTitle>
				</ItemContent>
				<ItemActions>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<AccordionTrigger
						disabled={false}
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
							crumb='Bảng hạch toán chi phí vật tư dài kỳ'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={false}
								>
									<CreateIcon />
								</Button>
							}
						>
							<LongtermMaterialCostForm
								id={output?.acceptanceReportId || id}
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='max-h-96 overflow-hidden overflow-y-auto p-0 px-2 pt-2'>
					<Accordion
						type='multiple'
						className='flex w-full min-w-0 flex-col gap-2'
					>
						{groupedItems.map((group) => {
							const totalAccountedValueThisPeriod = group.items.reduce(
								(total, item) => total + (item.accountedValueThisPeriod ?? 0),
								0,
							);

							return (
								<AccordionItem
									key={group.processGroupId}
									value={group.processGroupId}
									className='min-w-0 overflow-hidden border-none'
								>
									<Item
										variant={'outline'}
										className='relative w-full flex-1 rounded-sm bg-gray-300 py-2'
									>
										<div className='flex w-full items-center gap-4'>
											<div className='flex flex-1 items-center'>
												<ItemTitle className='text-sm font-semibold'>
													{group.processGroupCode
														? `${group.processGroupCode} - ${group.processGroupName || ''}`
														: group.processGroupName || 'Không xác định'}
												</ItemTitle>
											</div>
											<div className='me-40 text-sm font-semibold'>
												{formatNumber(totalAccountedValueThisPeriod)}
											</div>
											<ItemActions>
												<AccordionTrigger className='group p-0'>
													<div className='group-data-[state=open]:hidden'>
														<VisibilityIcon />
													</div>
													<div className='hidden group-data-[state=open]:block'>
														<VisibilityOffIcon />
													</div>
												</AccordionTrigger>
											</ItemActions>
										</div>
									</Item>
									<AccordionContent className='p-0 pt-2'>
										<div className='w-full min-w-0 overflow-x-auto'>
											<FixedColumnDataTable
												items={group.items}
												compact={true}
												loading={loading}
											/>
										</div>
									</AccordionContent>
								</AccordionItem>
							);
						})}
					</Accordion>
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
