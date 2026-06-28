import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogTitle,
} from '@/components/ui/dialog';
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from '@/components/ui/tooltip';
import { DynamicIcon } from 'lucide-react/dynamic';
import { XIcon } from 'lucide-react';
import { cn, formatNumber } from '@/lib/utils';
import { RawAcceptanceReportItem } from './types';
import { ClientPagination } from '@/components/datatable/client-pagination';
import {
	MaterialsIncludedInContractRevenue,
	AdditionalCost,
	QuotaBasedMaterial,
	Asset,
	MaterialType,
} from './types';

interface RawAcceptanceReportDataTableProps {
	items: RawAcceptanceReportItem[];
	largeText?: boolean;
}

const DEFAULT_CATEGORY_ASSIGNMENT_LABEL = 'Không thuộc nhóm vật tư, tài sản';
const DEFAULT_PRODUCTION_ORDER_LABEL = 'Không theo lệnh sản xuất';

const formatPostingDate = (value?: string | null): string => {
	if (!value) return '-';
	const [year, month, day] = value.split('-');
	if (year && month && day) {
		return `${day}/${month}/${year}`;
	}
	return value;
};

function OverflowTooltipText({
	text,
	className,
}: {
	text?: string | null;
	className?: string;
}) {
	if (!text) return <span>-</span>;
	return (
		<Tooltip>
			<TooltipTrigger asChild>
				<div className={cn('min-w-0 truncate', className)}>{text}</div>
			</TooltipTrigger>
			<TooltipContent
				side='top'
				align='start'
				className='max-w-80 whitespace-pre-wrap'
			>
				{text}
			</TooltipContent>
		</Tooltip>
	);
}

const getMaterialsIncludedTypeLabel = (
	item: RawAcceptanceReportItem,
): string => {
	switch (item.materialsIncludedInContractRevenueType) {
		case MaterialType.Material:
			return 'Vật liệu';
		case MaterialType.SparePart:
			return 'SCTX';
		default:
			switch (item.materialsIncludedInContractRevenue) {
				case MaterialsIncludedInContractRevenue.Material:
					return 'Vật liệu';
				case MaterialsIncludedInContractRevenue.Maintain:
					return 'SCTX';
				default:
					return '';
			}
	}
};

const getAdditionalCostLabel = (value: number): string => {
	switch (value) {
		case AdditionalCost.Material:
			return 'Vật liệu';
		case AdditionalCost.Maintain:
			return 'Chi phí SCTX';
		case AdditionalCost.OtherMaterial:
			return 'Vật tư khác';
		default:
			return '';
	}
};

const getQuotaBasedMaterialLabel = (value: number): string => {
	switch (value) {
		case QuotaBasedMaterial.MineSupport:
			return 'Vì chống lò';
		case QuotaBasedMaterial.SupportAccessories:
			return 'Phụ kiện';
		case QuotaBasedMaterial.MineTimber:
			return 'Gỗ lò';
		default:
			return '';
	}
};

const getQuotaBasedMaterialTypeLabel = (value: number): string => {
	switch (value) {
		case 1:
			return 'Lĩnh mới';
		case 2:
			return 'Tái sử dụng';
		default:
			return '';
	}
};

export function RawAcceptanceReportDataTable({
	items,
	largeText = false,
}: RawAcceptanceReportDataTableProps) {
	const [isExpanded, setIsExpanded] = useState(false);
	const [pageIndex, setPageIndex] = useState(0);
	const [pageSize, setPageSize] = useState(10);

	const pageCount = Math.ceil(items.length / pageSize);
	const safePageIndex = Math.min(pageIndex, Math.max(pageCount - 1, 0));
	const paginatedItems = items.slice(
		safePageIndex * pageSize,
		(safePageIndex + 1) * pageSize,
	);
	const getTrackedMaterialCode = (item: RawAcceptanceReportItem) =>
		item.materialCode || item.trackedMaterialCode || item.partCode || '-';

	const getTrackedMaterialName = (item: RawAcceptanceReportItem) =>
		item.materialName || item.trackedMaterialName || item.partName || '-';

	if (!items || items.length === 0) {
		return (
			<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
				<div className='text-muted-foreground text-center'>
					<p className='text-lg font-medium'>Không có dữ liệu</p>
					<p className='text-sm'>Chưa có dữ liệu để hiển thị</p>
				</div>
			</div>
		);
	}

	const renderTableContent = () => (
		<Table className={cn('min-w-max text-sm', largeText && 'text-base')}>
			<TableHeader
				className={cn('bg-[#fafafa]', largeText && 'h-14 text-base')}
			>
				<TableRow>
					<TableHead
						className='bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '50px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							STT
						</div>
					</TableHead>
					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '50px',
							minWidth: '90px',
							boxShadow: '2px 0 5px -2px rgba(0,0,0,0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Mã vật tư
						</div>
					</TableHead>
					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '140px',
							minWidth: '150px',
							boxShadow: '2px 0 5px -2px rgba(0,0,0,0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Tên vật tư
						</div>
					</TableHead>
					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '290px',
							minWidth: '60px',
							boxShadow: '2px 0 5px -2px rgba(0,0,0,0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							ĐVT
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Số chứng từ
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '100px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Ngày vào sổ
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '80px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							SL lĩnh
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '80px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							SL xuất
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '70px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Phân bổ
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '110px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 text-center font-bold'>
							VT tính vào DT khoán
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '100px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Bổ sung CP
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '100px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							VT theo hạn mức
						</div>
					</TableHead>
					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '70px' }}
					>
						<div className='inline-flex h-fit w-full items-center justify-center px-3 py-2 font-bold'>
							Tài sản
						</div>
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody className={cn(largeText && 'text-base')}>
				{paginatedItems.map((item, index) => (
					<TableRow key={item.id} className='h-9'>
						<TableCell
							className='left-0 z-10 bg-inherit px-3 py-1 text-center'
							style={{ minWidth: '50px' }}
						>
							{safePageIndex * pageSize + index + 1}
						</TableCell>

						<TableCell
							className='z-10 bg-inherit px-3 py-1'
							style={{ minWidth: '90px', maxWidth: '120px' }}
						>
							<OverflowTooltipText
								text={getTrackedMaterialCode(item)}
								className='w-24'
							/>
						</TableCell>

						<TableCell
							className='z-10 bg-inherit px-3 py-1'
							style={{ minWidth: '150px', maxWidth: '200px' }}
						>
							<OverflowTooltipText
								text={getTrackedMaterialName(item)}
								className='w-44'
							/>
						</TableCell>

						<TableCell
							className='z-10 bg-inherit px-3 py-1 text-center'
							style={{ minWidth: '60px' }}
						>
							{item.unitOfMeasureName || '-'}
						</TableCell>

						<TableCell
							className='px-3 py-1 text-center'
							style={{ minWidth: '120px' }}
						>
							{item.documentNumber || '-'}
						</TableCell>

						<TableCell
							className='px-3 py-1 text-center'
							style={{ minWidth: '100px' }}
						>
							{formatPostingDate(item.postingDate)}
						</TableCell>

						<TableCell
							className='px-3 py-1 text-center'
							style={{ minWidth: '80px' }}
						>
							{formatNumber(item.issuedQuantity || 0)}
						</TableCell>

						<TableCell
							className='px-3 py-1 text-center'
							style={{ minWidth: '80px' }}
						>
							{formatNumber(item.shippedQuantity || 0)}
						</TableCell>

						<TableCell
							className='px-3 py-1 text-center'
							style={{ minWidth: '70px' }}
						>
							<Checkbox
								checked={Boolean(item.isLongTermTracking)}
								disabled
								className='[&_.lucide-check]:text-white'
							/>
						</TableCell>

						{/* Vật tư tính vào doanh thu khoán */}
						<TableCell
							className='px-3 py-1'
							style={{ minWidth: '140px', maxWidth: '200px' }}
						>
							{item.materialsIncludedInContractRevenue !==
								MaterialsIncludedInContractRevenue.None && (
								<Tooltip>
									<TooltipTrigger asChild>
										<div className='flex cursor-default flex-col gap-0.5'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getMaterialsIncludedTypeLabel(item)}
											</Badge>
											<span className='truncate text-sm text-slate-600'>
												{item.categoryAssignmentCodeLabel?.trim() ||
													DEFAULT_CATEGORY_ASSIGNMENT_LABEL}
											</span>
											<span className='truncate text-sm text-slate-600'>
												{item.categoryProductionOrderLabel?.trim() ||
													DEFAULT_PRODUCTION_ORDER_LABEL}
											</span>
											<span className='text-sm font-medium text-slate-700'>
												SL:{' '}
												{formatNumber(
													item.materialsIncludedInContractRevenueQuantity || 0,
												)}
											</span>
										</div>
									</TooltipTrigger>
									<TooltipContent side='top' align='start' className='max-w-96'>
										<div className='flex flex-col gap-1 text-sm'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getMaterialsIncludedTypeLabel(item)}
											</Badge>
											<span>
												{item.categoryAssignmentCodeLabel?.trim() ||
													DEFAULT_CATEGORY_ASSIGNMENT_LABEL}
											</span>
											<span>
												{item.categoryProductionOrderLabel?.trim() ||
													DEFAULT_PRODUCTION_ORDER_LABEL}
											</span>
											<span className='font-medium'>
												SL:{' '}
												{formatNumber(
													item.materialsIncludedInContractRevenueQuantity || 0,
												)}
											</span>
										</div>
									</TooltipContent>
								</Tooltip>
							)}
						</TableCell>

						{/* Bổ sung chi phí */}
						<TableCell
							className='px-3 py-1'
							style={{ minWidth: '140px', maxWidth: '200px' }}
						>
							{item.additionalCost !== AdditionalCost.None && (
								<Tooltip>
									<TooltipTrigger asChild>
										<div className='flex cursor-default flex-col gap-0.5'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getAdditionalCostLabel(item.additionalCost)}
											</Badge>
											<span className='text-sm font-medium text-slate-700'>
												SL: {formatNumber(item.additionalCostQuantity || 0)}
											</span>
										</div>
									</TooltipTrigger>
									<TooltipContent side='top' align='start' className='max-w-96'>
										<div className='flex flex-col gap-1 text-sm'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getAdditionalCostLabel(item.additionalCost)}
											</Badge>
											<span className='font-medium'>
												SL: {formatNumber(item.additionalCostQuantity || 0)}
											</span>
										</div>
									</TooltipContent>
								</Tooltip>
							)}
						</TableCell>

						{/* Vật tư theo hạn mức */}
						<TableCell
							className='px-3 py-1'
							style={{ minWidth: '140px', maxWidth: '200px' }}
						>
							{item.quotaBasedMaterial !== QuotaBasedMaterial.None && (
								<Tooltip>
									<TooltipTrigger asChild>
										<div className='flex cursor-default flex-col gap-0.5'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getQuotaBasedMaterialLabel(item.quotaBasedMaterial)}
											</Badge>
											{(item.quotaBasedMaterial ===
												QuotaBasedMaterial.MineSupport ||
												item.quotaBasedMaterial ===
													QuotaBasedMaterial.SupportAccessories) && (
												<span className='truncate text-sm text-slate-600'>
													{getQuotaBasedMaterialTypeLabel(
														item.quotaBasedMaterialType,
													)}
												</span>
											)}
											<span className='text-sm font-medium text-slate-700'>
												SL: {formatNumber(item.quotaBasedMaterialQuantity || 0)}
											</span>
										</div>
									</TooltipTrigger>
									<TooltipContent side='top' align='start' className='max-w-96'>
										<div className='flex flex-col gap-1 text-sm'>
											<Badge variant='secondary' className='w-fit text-sm'>
												{getQuotaBasedMaterialLabel(item.quotaBasedMaterial)}
											</Badge>
											{(item.quotaBasedMaterial ===
												QuotaBasedMaterial.MineSupport ||
												item.quotaBasedMaterial ===
													QuotaBasedMaterial.SupportAccessories) && (
												<span>
													{getQuotaBasedMaterialTypeLabel(
														item.quotaBasedMaterialType,
													)}
												</span>
											)}
											<span className='font-medium'>
												SL: {formatNumber(item.quotaBasedMaterialQuantity || 0)}
											</span>
										</div>
									</TooltipContent>
								</Tooltip>
							)}
						</TableCell>

						{/* Tài sản */}
						<TableCell
							className='px-3 py-1'
							style={{ minWidth: '100px', maxWidth: '140px' }}
						>
							{item.asset !== Asset.None && (
								<Tooltip>
									<TooltipTrigger asChild>
										<div className='flex cursor-default flex-col gap-0.5'>
											<Badge variant='secondary' className='w-fit text-sm'>
												Tài sản
											</Badge>
											<span className='text-sm font-medium text-slate-700'>
												SL: {formatNumber(item.assetMaterialQuantity || 0)}
											</span>
										</div>
									</TooltipTrigger>
									<TooltipContent side='top' align='start' className='max-w-96'>
										<div className='flex flex-col gap-1 text-sm'>
											<Badge variant='secondary' className='w-fit text-sm'>
												Tài sản
											</Badge>
											<span className='font-medium'>
												SL: {formatNumber(item.assetMaterialQuantity || 0)}
											</span>
										</div>
									</TooltipContent>
								</Tooltip>
							)}
						</TableCell>
					</TableRow>
				))}
			</TableBody>
		</Table>
	);

	return (
		<>
			<div className='relative overflow-x-hidden rounded-t-md border shadow'>
				<Button
					variant='ghost'
					size='sm'
					className='absolute top-2 left-2 z-30 h-8 w-8 p-0'
					onClick={() => setIsExpanded(true)}
				>
					<DynamicIcon name='maximize' className='size-4' />
				</Button>
				<div className='w-full overflow-x-auto overflow-y-visible'>
					{renderTableContent()}
				</div>
			</div>
			{items.length > 0 && (
				<ClientPagination
					totalItems={items.length}
					pageIndex={safePageIndex}
					pageSize={pageSize}
					onPageIndexChange={setPageIndex}
					onPageSizeChange={(nextPageSize) => {
						setPageSize(nextPageSize);
						setPageIndex(0);
					}}
					className='py-2'
				/>
			)}

			<Dialog open={isExpanded} onOpenChange={setIsExpanded}>
				<DialogContent
					showCloseButton={false}
					className='top-0 left-0 flex h-screen max-h-screen w-screen max-w-none! translate-x-0 translate-y-0 flex-col overflow-hidden rounded-lg border-0 p-0'
				>
					<DialogTitle hidden />
					<DialogDescription hidden />
					<div className='flex shrink-0 items-center gap-4 border-b bg-white px-6 py-4'>
						<h3 className='flex-1 text-lg font-semibold'>
							Báo cáo nghiệm thu nguyên vật liệu
						</h3>
						<DynamicIcon
							name='minimize'
							className='size-4 cursor-pointer'
							onClick={() => setIsExpanded(false)}
						/>
						<XIcon
							className='size-5 cursor-pointer'
							onClick={() => setIsExpanded(false)}
						/>
					</div>
					<div className='flex-1 overflow-auto bg-gray-50 p-6'>
						<div className='inline-block min-w-full overflow-hidden rounded-t-md border shadow'>
							{renderTableContent()}
						</div>
					</div>
					{items.length > 0 && (
						<div className='shrink-0 border-t bg-white py-2'>
							<ClientPagination
								totalItems={items.length}
								pageIndex={safePageIndex}
								pageSize={pageSize}
								onPageIndexChange={setPageIndex}
								onPageSizeChange={(nextPageSize) => {
									setPageSize(nextPageSize);
									setPageIndex(0);
								}}
								className='py-2'
							/>
						</div>
					)}
				</DialogContent>
			</Dialog>
		</>
	);
}
