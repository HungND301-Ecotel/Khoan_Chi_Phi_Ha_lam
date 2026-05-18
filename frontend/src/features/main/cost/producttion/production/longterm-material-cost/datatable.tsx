import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { cn, formatNumber } from '@/lib/utils';
import { LongtermMaterialDetailItem } from './types';
import { useState } from 'react';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogTitle,
} from '@/components/ui/dialog';
import { DynamicIcon } from 'lucide-react/dynamic';
import { XIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';

type FixedColumnDataTableProps = {
	items?: LongtermMaterialDetailItem[];
	compact?: boolean;
	loading?: boolean;
};

export function FixedColumnDataTable({
	items = [],
	compact = false,
	loading = false,
}: FixedColumnDataTableProps) {
	const [isExpanded, setIsExpanded] = useState(false);

	if (loading) {
		return (
			<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
				<div className='text-center'>
					<Skeleton className='mx-auto h-8 w-48' />
					<Skeleton className='mx-auto mt-2 h-4 w-32' />
				</div>
			</div>
		);
	}

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

	// Calculate totals for summary row
	const totalOpeningBalance = items.reduce(
		(sum, item) => sum + (item.pendingValueStartPeriod ?? 0),
		0,
	);
	const totalAmount = items.reduce(
		(sum, item) => sum + (item.totalAmount ?? 0),
		0,
	);
	const totalAccountingValue = items.reduce(
		(sum, item) => sum + (item.totalValueToAccount ?? 0),
		0,
	);
	const totalOriginalPrice = items.reduce(
		(sum, item) => sum + (item.originAmount ?? 0),
		0,
	);
	const totalQuotaAccountingValue = items.reduce(
		(sum, item) => sum + (item.valueByStandard ?? 0),
		0,
	);
	const totalEndingBalance = items.reduce(
		(sum, item) => sum + (item.pendingValueEndPeriod ?? 0),
		0,
	);

	const renderTableContent = () => (
		<Table>
			<TableHeader className={cn('bg-[#fafafa]', !compact && 'h-14 text-base')}>
				{/* Header Row 1: Main headers */}
				<TableRow>
					{/* STT Column */}
					<TableHead
						rowSpan={2}
						className='sticky left-0 z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							minWidth: '60px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							STT
						</div>
					</TableHead>

					{/* Fixed columns */}
					<TableHead
						rowSpan={2}
						className='sticky z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '60px',
							minWidth: '120px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Mã vật tư
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='sticky z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '180px',
							minWidth: '200px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Tên vật tư
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='sticky z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							left: '380px',
							minWidth: '80px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							ĐVT
						</div>
					</TableHead>

					{/* Scrollable columns */}
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '150px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Giá trị chờ hạch toán đầu kỳ (đ)
						</div>
					</TableHead>
					<TableHead
						colSpan={3}
						className='border-border border-l p-0 hover:bg-[#f0f0f0]'
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-center gap-2 px-4 py-2 font-bold'>
							Giá trị phát sinh trong kỳ
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='border-border border-l p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '150px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Tổng giá trị cần hạch toán (đ)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Nguyên giá (đ)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian sử dụng (Ti)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian đã phân bổ
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '120px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian còn lại
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '180px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Giá trị cần hạch toán theo định mức (đ)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '100px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Tỷ lệ phân bổ
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '180px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Giá trị dài kỳ hạch toán kỳ này (đ)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '180px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Giá trị cuối kỳ chờ hạch toán kỳ sau (đ)
						</div>
					</TableHead>
					<TableHead
						rowSpan={2}
						className='p-0 hover:bg-[#f0f0f0]'
						style={{ minWidth: '150px' }}
					>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Ghi chú
						</div>
					</TableHead>
				</TableRow>

				{/* Header Row 2: Sub-headers for grouped columns */}
				<TableRow>
					<TableHead className='border-border border-l p-0 hover:bg-[#f0f0f0]'>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Số lượng
						</div>
					</TableHead>
					<TableHead className='p-0 hover:bg-[#f0f0f0]'>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Đơn giá
						</div>
					</TableHead>
					<TableHead className='p-0 hover:bg-[#f0f0f0]'>
						<div className='inline-flex h-fit w-full flex-nowrap items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thành tiền
						</div>
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody>
				{/* Summary Row */}
				<TableRow className='bg-slate-100 font-bold'>
					{/* STT Column */}
					<TableCell
						className='sticky left-0 z-10 bg-slate-100'
						style={{
							minWidth: '60px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						Tổng
					</TableCell>

					{/* Fixed columns */}
					<TableCell
						className='sticky z-10 bg-slate-100'
						style={{
							left: '60px',
							minWidth: '120px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					></TableCell>
					<TableCell
						className='sticky z-10 bg-slate-100'
						style={{
							left: '180px',
							minWidth: '200px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					></TableCell>
					<TableCell
						className='sticky z-10 bg-slate-100'
						style={{
							left: '380px',
							minWidth: '80px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					></TableCell>

					{/* Scrollable columns */}
					<TableCell style={{ minWidth: '150px' }}>
						{formatNumber(totalOpeningBalance)}
					</TableCell>
					<TableCell
						className='border-border border-l'
						style={{ minWidth: '100px' }}
					></TableCell>
					<TableCell style={{ minWidth: '100px' }}></TableCell>
					<TableCell style={{ minWidth: '120px' }}>
						{formatNumber(totalAmount)}
					</TableCell>
					<TableCell
						className='border-border border-l'
						style={{ minWidth: '150px' }}
					>
						{formatNumber(totalAccountingValue)}
					</TableCell>
					<TableCell style={{ minWidth: '120px' }}>
						{formatNumber(totalOriginalPrice)}
					</TableCell>
					<TableCell style={{ minWidth: '120px' }}></TableCell>
					<TableCell style={{ minWidth: '120px' }}></TableCell>
					<TableCell style={{ minWidth: '120px' }}></TableCell>
					<TableCell style={{ minWidth: '180px' }}>
						{formatNumber(totalQuotaAccountingValue)}
					</TableCell>
					<TableCell style={{ minWidth: '100px' }}></TableCell>
					<TableCell style={{ minWidth: '180px' }}></TableCell>
					<TableCell style={{ minWidth: '180px' }}>
						{formatNumber(totalEndingBalance)}
					</TableCell>
					<TableCell style={{ minWidth: '150px' }}></TableCell>
				</TableRow>

				{items.map((item, index) => (
					<TableRow key={index}>
						{/* STT Column */}
						<TableCell
							className='sticky left-0 z-10 bg-white'
							style={{
								minWidth: '60px',
								boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
							}}
						>
							{index + 1}
						</TableCell>

						{/* Fixed columns */}
						<TableCell
							className='sticky z-10 bg-white'
							style={{
								left: '60px',
								minWidth: '120px',
								boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
							}}
						>
							{item.partCode}
						</TableCell>
						<TableCell
							className='sticky z-10 bg-white'
							style={{
								left: '180px',
								minWidth: '200px',
								boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
							}}
						>
							{item.partName}
						</TableCell>
						<TableCell
							className='sticky z-10 bg-white'
							style={{
								left: '380px',
								minWidth: '80px',
								boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
							}}
						>
							{item.unitOfMeasureName}
						</TableCell>

						{/* Scrollable columns */}
						<TableCell style={{ minWidth: '150px' }}>
							{formatNumber(item.pendingValueStartPeriod ?? 0)}
						</TableCell>
						<TableCell
							className='border-border border-l'
							style={{ minWidth: '100px' }}
						>
							{formatNumber(item.issuedQuantity ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '100px' }}>
							{formatNumber(item.unitPrice ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '120px' }}>
							{formatNumber(item.totalAmount ?? 0)}
						</TableCell>
						<TableCell
							className='border-border border-l'
							style={{ minWidth: '150px' }}
						>
							{formatNumber(item.totalValueToAccount ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '120px' }}>
							{formatNumber(item.originAmount ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '120px' }}>
							{formatNumber(item.usageTime ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '120px' }}>
							{formatNumber(item.allocatedTime ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '120px' }}>
							{formatNumber(item.remainingTime ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '180px' }}>
							{formatNumber(item.valueByStandard ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '100px' }}>
							{formatNumber(item.allocationRatio ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '180px' }}>
							{formatNumber(item.accountedValueThisPeriod ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '180px' }}>
							{formatNumber(item.pendingValueEndPeriod ?? 0)}
						</TableCell>
						<TableCell style={{ minWidth: '150px' }}>
							{item.note || ''}
						</TableCell>
					</TableRow>
				))}
			</TableBody>
		</Table>
	);

	return (
		<>
			<div className='relative overflow-hidden rounded-t-md border shadow'>
				{/* Expand button */}
				<Button
					variant='ghost'
					size='sm'
					className='absolute top-2 left-2 z-30 h-8 w-8 p-0'
					onClick={() => setIsExpanded(true)}
				>
					<DynamicIcon name='maximize' className='size-4' />
				</Button>

				{renderTableContent()}
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
							Bảng hạch toán chi phí vật tư dài kỳ
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
