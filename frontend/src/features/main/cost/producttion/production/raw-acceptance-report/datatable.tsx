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
import { DynamicIcon } from 'lucide-react/dynamic';
import { XIcon } from 'lucide-react';
import { cn, formatNumber } from '@/lib/utils';
import { RawAcceptanceReportItem } from './types';
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

// Helper functions to map enum values to display labels
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
		<Table className={cn('min-w-max', largeText && 'text-base')}>
			<TableHeader
				className={cn('bg-[#fafafa]', largeText && 'h-14 text-base')}
			>
				<TableRow>
					<TableHead
						className='bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '50px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							STT
						</div>
					</TableHead>

					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '50px',
							minWidth: '90px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Mã vật tư
						</div>
					</TableHead>

					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '140px',
							minWidth: '120px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Tên vật tư
						</div>
					</TableHead>

					<TableHead
						className='z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '260px',
							minWidth: '70px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							ĐVT
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '140px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Số chứng từ
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Ngày vào sổ
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '90px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Số lượng lĩnh
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '90px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Số lượng xuất
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '90px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Phân bổ
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Vật tư tính vào doanh thu khoán
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Bổ sung chi phí
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Vật tư theo hạn mức
						</div>
					</TableHead>

					<TableHead
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '80px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Tài sản
						</div>
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody className={cn(largeText && 'text-base')}>
				{items.map((item, index) => (
					<TableRow key={item.id}>
						<TableCell
							className='left-0 z-10 bg-inherit'
							style={{
								minWidth: '50px',
							}}
						>
							<div className='flex items-center justify-center px-4 py-2'>
								{index + 1}
							</div>
						</TableCell>

						<TableCell
							className='z-10 bg-inherit'
							style={{
								minWidth: '90px',
							}}
						>
							<div className='flex items-baseline justify-center px-4 py-2'>
								{getTrackedMaterialCode(item)}
							</div>
						</TableCell>

						<TableCell
							className='z-10 bg-inherit'
							style={{
								minWidth: '120px',
							}}
						>
							<div className='flex items-baseline justify-baseline px-4 py-2'>
								{getTrackedMaterialName(item)}
							</div>
						</TableCell>

						<TableCell
							className='z-10 bg-inherit'
							style={{
								minWidth: '70px',
							}}
						>
							<div className='flex items-center justify-center px-4 py-2'>
								{item.unitOfMeasureName || '-'}
							</div>
						</TableCell>

						<TableCell style={{ minWidth: '140px' }}>
							<div className='flex items-center justify-center px-4 py-2'>
								{item.documentNumber || '-'}
							</div>
						</TableCell>

						<TableCell style={{ minWidth: '120px' }}>
							<div className='flex items-center justify-center px-4 py-2'>
								{formatPostingDate(item.postingDate)}
							</div>
						</TableCell>

						{/* Số lượng lĩnh */}
						<TableCell style={{ minWidth: '90px' }}>
							<div className='flex items-center justify-center px-4 py-2'>
								{formatNumber(item.issuedQuantity || 0)}
							</div>
						</TableCell>

						{/* Số lượng xuất */}
						<TableCell style={{ minWidth: '90px' }}>
							<div className='flex items-center justify-center px-4 py-2'>
								{formatNumber(item.shippedQuantity || 0)}
							</div>
						</TableCell>

						<TableCell style={{ minWidth: '90px' }}>
							<div className='flex items-center justify-center px-4 py-2'>
								<Checkbox
									checked={Boolean(item.isLongTermTracking)}
									disabled
									className='[&_.lucide-check]:text-white'
								/>
							</div>
						</TableCell>

						{/* Vật tư tính vào doanh thu khoán */}
						<TableCell style={{ minWidth: '120px' }}>
							<div className='flex flex-col items-center justify-center gap-1 px-4 py-2'>
								{item.materialsIncludedInContractRevenue !==
									MaterialsIncludedInContractRevenue.None && (
									<>
										<Badge variant='secondary' className='text-xs'>
											{getMaterialsIncludedTypeLabel(item)}
										</Badge>
										<Badge variant='outline' className='text-xs'>
											{item.categoryAssignmentCodeLabel?.trim() ||
												DEFAULT_CATEGORY_ASSIGNMENT_LABEL}
										</Badge>
										<Badge variant='outline' className='text-xs'>
											{item.categoryProductionOrderLabel?.trim() ||
												DEFAULT_PRODUCTION_ORDER_LABEL}
										</Badge>
										<span className='text-xs text-slate-600'>
											SL:{' '}
											{formatNumber(
												item.materialsIncludedInContractRevenueQuantity || 0,
											)}
										</span>
									</>
								)}
							</div>
						</TableCell>

						{/* Bổ sung chi phí */}
						<TableCell style={{ minWidth: '120px' }}>
							<div className='flex flex-col items-center justify-center gap-1 px-4 py-2'>
								{item.additionalCost !== AdditionalCost.None && (
									<>
										<Badge variant='secondary' className='text-xs'>
											{getAdditionalCostLabel(item.additionalCost)}
										</Badge>
										<span className='text-xs text-slate-600'>
											SL: {formatNumber(item.additionalCostQuantity || 0)}
										</span>
									</>
								)}
							</div>
						</TableCell>

						{/* Vật tư theo hạn mức */}
						<TableCell style={{ minWidth: '120px' }}>
							<div className='flex flex-col items-center justify-center gap-1 px-4 py-2'>
								{item.quotaBasedMaterial !== QuotaBasedMaterial.None && (
									<>
										<Badge variant='secondary' className='text-xs'>
											{getQuotaBasedMaterialLabel(item.quotaBasedMaterial)}
										</Badge>
										{(item.quotaBasedMaterial ===
											QuotaBasedMaterial.MineSupport ||
											item.quotaBasedMaterial ===
												QuotaBasedMaterial.SupportAccessories) && (
											<Badge variant='outline' className='text-xs'>
												{getQuotaBasedMaterialTypeLabel(
													item.quotaBasedMaterialType,
												)}
											</Badge>
										)}
										<span className='text-xs text-slate-600'>
											SL: {formatNumber(item.quotaBasedMaterialQuantity || 0)}
										</span>
									</>
								)}
							</div>
						</TableCell>

						{/* Tài sản */}
						<TableCell style={{ minWidth: '80px' }}>
							<div className='flex flex-col items-center justify-center gap-1 px-4 py-2'>
								{item.asset !== Asset.None && (
									<>
										<Badge variant='secondary' className='text-xs'>
											Tài sản
										</Badge>
										<span className='text-xs text-slate-600'>
											SL: {formatNumber(item.assetMaterialQuantity || 0)}
										</span>
									</>
								)}
							</div>
						</TableCell>
					</TableRow>
				))}
			</TableBody>
		</Table>
	);

	return (
		<>
			<div className='relative overflow-x-hidden rounded-t-md border shadow'>
				{/* Expand button */}
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

			{/* Fullscreen Dialog */}
			<Dialog open={isExpanded} onOpenChange={setIsExpanded}>
				<DialogContent
					showCloseButton={false}
					className='top-0 left-0 flex h-screen max-h-screen w-screen max-w-none! translate-x-0 translate-y-0 flex-col overflow-hidden rounded-lg border-0 p-0'
				>
					<DialogTitle hidden />
					<DialogDescription hidden />

					{/* Header */}
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

					{/* Content */}
					<div className='flex-1 overflow-auto bg-gray-50 p-6'>
						<div className='inline-block min-w-full overflow-hidden rounded-t-md border shadow'>
							{renderTableContent()}
						</div>
					</div>
				</DialogContent>
			</Dialog>
		</>
	);
}
