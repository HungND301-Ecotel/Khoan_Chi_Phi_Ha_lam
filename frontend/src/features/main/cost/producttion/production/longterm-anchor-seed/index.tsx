import { DataTableEditDialog } from '@/components/datatable/edit';
import { DataTableImport } from '@/components/datatable/import';
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
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import {
	LongTermAnchorSeedDetail,
	LongTermAnchorSeedItem,
	LongTermAnchorSeedProcessGroupMetric,
} from '@/features/main/cost/producttion/production/longterm-anchor-seed/anchor-seed-types';
import { LongtermAnchorSeedForm } from '@/features/main/cost/producttion/production/longterm-anchor-seed/form';
import { FixedColumnDataTable } from '@/features/main/cost/producttion/production/longterm-material-cost/datatable';
import {
	LongtermMaterialDetailItem,
	LongTermTrackingProcessGroup,
} from '@/features/main/cost/producttion/production/longterm-material-cost/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import UploadIcon from '@mui/icons-material/FileUpload';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useMemo, useState } from 'react';

type LongtermAnchorSeedSectionProps = {
	departmentId: string;
};

function formatMonthLabel(value?: string) {
	if (!value) return 'Chưa có tháng hiệu lực';
	const [year, month] = value.split('-');
	if (!year || !month) return value;
	return `Hiệu lực từ ${month}/${year}`;
}

export function LongtermAnchorSeedSection({
	departmentId,
}: LongtermAnchorSeedSectionProps) {
	const [detail, setDetail] = useState<LongTermAnchorSeedDetail>();
	const [loading, setLoading] = useState(false);
	const [refreshKey, setRefreshKey] = useState(0);
	const [isExporting, setIsExporting] = useState(false);

	const fetchDetail = async () => {
		setLoading(true);
		try {
			const response = await api.get<LongTermAnchorSeedDetail>(
				API.PRODUCTION.LONG_TERM_ANCHOR_SEED.DETAIL(departmentId),
			);
			setDetail(response.result);
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		fetchDetail();
	}, [departmentId, refreshKey]);

	const groupedItems = useMemo<LongTermTrackingProcessGroup[]>(
		() =>
			(detail?.items ?? []).reduce<LongTermTrackingProcessGroup[]>(
				(groups, item) => {
					const processGroupId = item.processGroupId;
					const existingGroup = groups.find(
						(group) => group.processGroupId === processGroupId,
					);

					if (existingGroup) {
						existingGroup.items.push(
							mapSeedItemToTableItem(item, detail?.processGroupMetrics ?? []),
						);
						return groups;
					}

					groups.push({
						processGroupId,
						processGroupCode: item.processGroupCode,
						processGroupName: item.processGroupName,
						items: [
							mapSeedItemToTableItem(item, detail?.processGroupMetrics ?? []),
						],
					});
					return groups;
				},
				[],
			),
		[detail?.items, detail?.processGroupMetrics],
	);

	const handleExport = async () => {
		setIsExporting(true);
		try {
			await api.export(
				API.PRODUCTION.LONG_TERM_ANCHOR_SEED.EXPORT(departmentId),
			);
		} finally {
			setIsExporting(false);
		}
	};

	const handleImport = async (file: File) => {
		await api.uploadFile<boolean>(
			API.PRODUCTION.LONG_TERM_ANCHOR_SEED.UPLOAD_FILE(departmentId),
			file,
		);
		setRefreshKey((prev) => prev + 1);
	};

	return (
		<AccordionItem
			value='longterm-anchor-seed'
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant='outline' className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Mốc gốc hạch toán dài kỳ</ItemTitle>
					<div className='text-muted-foreground text-xs'>
						{formatMonthLabel(detail?.effectiveMonth)}
					</div>
				</ItemContent>
				<ItemActions>
					<DialogProvider>
						<DataTableEditDialog
							type='Tải lên'
							crumb='Mốc gốc hạch toán dài kỳ'
							trigger={
								<Button
									variant='ghost'
									size='icon-sm'
									className='size-5 rounded-full bg-transparent'
								>
									<UploadIcon />
								</Button>
							}
						>
							<DataTableImport onImport={handleImport} />
						</DataTableEditDialog>
					</DialogProvider>
					<Button
						variant='ghost'
						size='icon-sm'
						className='size-5 rounded-full bg-transparent'
						disabled={isExporting}
						onClick={handleExport}
					>
						{isExporting ? <Spinner /> : <DownloadIcon />}
					</Button>
					<DialogProvider>
						<DataTableEditDialog
							type='Chỉnh sửa'
							crumb='Mốc gốc hạch toán dài kỳ'
							trigger={
								<Button
									variant='ghost'
									size='icon-sm'
									className='size-5 rounded-full bg-transparent'
								>
									<CreateIcon />
								</Button>
							}
						>
							<LongtermAnchorSeedForm
								departmentId={departmentId}
								callback={async () => setRefreshKey((prev) => prev + 1)}
							/>
						</DataTableEditDialog>
					</DialogProvider>
					<AccordionTrigger
						className='group p-0'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
				</ItemActions>
			</Item>

			<AccordionContent className='max-h-96 overflow-hidden overflow-y-auto p-0 px-2 pt-2'>
				<Accordion
					type='multiple'
					className='flex w-full min-w-0 flex-col gap-2'
				>
					{!loading && groupedItems.length === 0 ? (
						<div className='border-border flex h-40 items-center justify-center rounded-sm border bg-white shadow'>
							<div className='text-muted-foreground text-center'>
								<p className='text-lg font-medium'>Không có dữ liệu</p>
								<p className='text-sm'>Chưa có dữ liệu mốc gốc để hiển thị</p>
							</div>
						</div>
					) : (
						groupedItems.map((group) => {
							const totalGroupValue = group.items.reduce(
								(total, item) => total + (item.totalValueToAccount ?? 0),
								0,
							);

							return (
								<AccordionItem
									key={group.processGroupId}
									value={group.processGroupId}
									className='min-w-0 overflow-hidden border-none'
								>
									<Item
										variant='outline'
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
												{formatNumber(totalGroupValue)}
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
						})
					)}
				</Accordion>
			</AccordionContent>
		</AccordionItem>
	);
}

function mapSeedItemToTableItem(
	item: LongTermAnchorSeedItem,
	processGroupMetrics: LongTermAnchorSeedProcessGroupMetric[],
): LongtermMaterialDetailItem {
	const metric = processGroupMetrics.find(
		(processGroupMetric) => processGroupMetric.processGroupId === item.processGroupId,
	);
	const plannedOutput = metric?.plannedOutput ?? 0;
	const standardOutput = metric?.standardOutput ?? 0;
	const valueByStandard =
		item.usageTime > 0 && standardOutput > 0
			? (item.totalValueToAccount / item.usageTime) *
				(plannedOutput / standardOutput)
			: 0;
	const accountedValueThisPeriod =
		Math.abs(item.remainingTime) < 0.0001
			? item.totalValueToAccount
			: Math.min(item.totalValueToAccount, valueByStandard * item.allocationRatio);
	const pendingValueEndPeriod = item.totalValueToAccount - accountedValueThisPeriod;

	return {
		id: item.id,
		acceptanceReportItemId: item.id,
		processGroupId: item.processGroupId,
		processGroupCode: item.processGroupCode,
		processGroupName: item.processGroupName,
		partCode: item.materialCode || item.partCode,
		partName: item.materialName || item.partName,
		unitOfMeasureName: item.unitOfMeasureName,
		pendingValueStartPeriod: item.pendingValueStartPeriod,
		issuedQuantity: item.issuedQuantity,
		unitPrice: item.unitPrice,
		totalAmount: item.totalAmount,
		originAmount: item.originAmount,
		totalValueToAccount: item.totalValueToAccount,
		usageTime: item.usageTime,
		allocatedTime: item.allocatedTime,
		remainingTime: item.remainingTime,
		plannedOutput,
		standardOutput,
		valueByStandard,
		allocationRatio: item.allocationRatio,
		isFullAccounting: false,
		accountedValueThisPeriod,
		pendingValueEndPeriod,
		isAnchorSeed: true,
		note: item.note,
	};
}
