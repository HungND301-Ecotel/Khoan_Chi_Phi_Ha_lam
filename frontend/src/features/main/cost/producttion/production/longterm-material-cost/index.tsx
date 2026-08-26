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
import { usePermission } from '@/hooks/use-permission';
import { formatNumber } from '@/lib/utils';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { FixedColumnDataTable } from './datatable';
import { TrackingOnlyTable } from './tracking-only-table';

export function LongTermMaterialCosts({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const { hasPermission } = usePermission();
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

	const isVtlCodeOrName = (code?: string | null, name?: string | null): boolean => {
		const c = (code || '').toUpperCase();
		const n = (name || '').toLowerCase();
		return c.includes('VTL') || n.includes('vận tải lò') || n.includes('vtl');
	};

	const isVtlGroup = (group?: { processGroupCode?: string; processGroupName?: string; departmentCode?: string; departmentName?: string } | null): boolean => {
		if (!group) return false;
		return (
			isVtlCodeOrName(group.processGroupCode, group.processGroupName) ||
			isVtlCodeOrName(group.departmentCode, group.departmentName)
		);
	};

	const isDeptVtl = Boolean(
		isVtlCodeOrName(output?.departmentCode, output?.departmentName) ||
		(output as any)?.isTunnelTransport
	);

	const groupedItems: LongTermTrackingProcessGroup[] = (() => {
		const rawTrackingOnlyItems = additionalCostData?.trackingOnlyItems ?? [];
		const processGroups = additionalCostData?.processGroups ?? [];
		const allItems = additionalCostData?.items ?? [];

		// "VẬT TƯ THEO DÕI" chỉ áp dụng cho Vận tải lò (VTL)
		const vtlTrackingItems = rawTrackingOnlyItems.filter(
			(item) => isVtlGroup(item) || isDeptVtl,
		);

		const getTrackingForGroup = (group: { processGroupId: string; processGroupCode?: string; processGroupName?: string }) => {
			if (!isVtlGroup(group) && !isDeptVtl) {
				return [];
			}
			return vtlTrackingItems.filter(
				(item) =>
					item.processGroupId === group.processGroupId ||
					(!item.processGroupId && (isVtlGroup(group) || isDeptVtl)),
			);
		};

		// TRƯỜNG HỢP 1: Không có dòng hạch toán dài kỳ nào (allItems.length === 0)
		if (!allItems.length) {
			if (vtlTrackingItems.length > 0) {
				const vtlGroups = processGroups
					.filter((g) => isVtlGroup(g) || isDeptVtl)
					.map((g) => ({
						...g,
						items: [],
						trackingOnlyItems: getTrackingForGroup(g),
					}))
					.filter((g) => (g.trackingOnlyItems?.length ?? 0) > 0);

				if (vtlGroups.length > 0) {
					return vtlGroups;
				}

				return [
					{
						processGroupId: 'vtl',
						processGroupCode: 'VTL',
						processGroupName: output?.departmentName || 'Vận tải lò',
						items: [],
						trackingOnlyItems: vtlTrackingItems,
					},
				];
			}
			return [];
		}

		// TRƯỜNG HỢP 2: Có dòng hạch toán dài kỳ (allItems.length > 0)
		if (!processGroups.length) {
			return [
				{
					processGroupId: 'all',
					processGroupCode: '',
					processGroupName: 'Tất cả nhóm công đoạn',
					items: allItems,
					trackingOnlyItems: isDeptVtl ? vtlTrackingItems : [],
				},
			];
		}

		const ungroupedItems = allItems.filter(
			(item) => !item.processGroupId,
		);

		const groupsWithTracking = processGroups.map((group) => ({
			...group,
			trackingOnlyItems: getTrackingForGroup(group),
		}));

		// Nhóm "Chưa có nhóm công đoạn" CHỈ hiển thị khi có dòng vật tư hạch toán dài kỳ chưa phân nhóm
		return ungroupedItems.length > 0
			? [
					...groupsWithTracking,
					{
						processGroupId: 'ungrouped',
						processGroupCode: '',
						processGroupName: 'Chưa có nhóm công đoạn',
						items: ungroupedItems,
						trackingOnlyItems: [],
					},
				]
			: groupsWithTracking;
	})();

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
					{hasPermission('production.long-term-tracking.read') && (<AccordionTrigger
						disabled={false}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>)}
					{hasPermission('production.long-term-tracking.update') && (<DialogProvider>
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
					</DialogProvider>)}
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
												{hasPermission('production.long-term-tracking.read') && (<AccordionTrigger className='group p-0'>
													<div className='group-data-[state=open]:hidden'>
														<VisibilityIcon />
													</div>
													<div className='hidden group-data-[state=open]:block'>
														<VisibilityOffIcon />
													</div>
												</AccordionTrigger>)}
											</ItemActions>
										</div>
									</Item>
									<AccordionContent className='p-0 pt-2'>
										<div className='w-full min-w-0 overflow-x-auto'>
											{group.items.length > 0 && (
												<FixedColumnDataTable
													items={group.items}
													compact={true}
													loading={loading}
												/>
											)}
											{isVtlGroup(group) &&
												group.trackingOnlyItems &&
												group.trackingOnlyItems.length > 0 && (
													<TrackingOnlyTable
														items={group.trackingOnlyItems}
													/>
												)}
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
