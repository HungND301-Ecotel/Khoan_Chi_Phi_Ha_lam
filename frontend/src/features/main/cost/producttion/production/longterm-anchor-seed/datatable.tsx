import { Button } from '@/components/ui/button';
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
import { cn, formatNumber } from '@/lib/utils';
import { DynamicIcon } from 'lucide-react/dynamic';
import { XIcon } from 'lucide-react';
import { useState } from 'react';
import { LongTermAnchorSeedItem } from './anchor-seed-types';

type LongTermAnchorSeedDataTableProps = {
	items?: LongTermAnchorSeedItem[];
	compact?: boolean;
	loading?: boolean;
};

export function LongTermAnchorSeedDataTable({
	items = [],
	compact = false,
	loading = false,
}: LongTermAnchorSeedDataTableProps) {
	const [isExpanded, setIsExpanded] = useState(false);

	if (loading) {
		return (
			<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
				<div className='text-center'>
					<div className='bg-muted mx-auto h-8 w-48 animate-pulse rounded' />
					<div className='bg-muted mx-auto mt-2 h-4 w-32 animate-pulse rounded' />
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

	const renderCodeName = (code?: string, name?: string) => {
		if (code && name) return `${code} - ${name}`;
		return code || name || '';
	};

	const totalPendingValue = items.reduce(
		(total, item) => total + (item.pendingValueStartPeriod ?? 0),
		0,
	);
	const totalUsageTime = items.reduce(
		(total, item) => total + (item.usageTime ?? 0),
		0,
	);
	const totalAllocatedTime = items.reduce(
		(total, item) => total + (item.allocatedTime ?? 0),
		0,
	);
	const totalRemainingTime = items.reduce(
		(total, item) => total + ((item.usageTime ?? 0) - (item.allocatedTime ?? 0)),
		0,
	);

	const renderContent = () => (
		<Table>
			<TableHeader className={cn('bg-[#fafafa]', !compact && 'h-14 text-base')}>
				<TableRow>
					<TableHead
						className='sticky left-0 z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							minWidth: '60px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							STT
						</div>
					</TableHead>
					<TableHead
						className='sticky left-[60px] z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							minWidth: '120px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Mã vật tư
						</div>
					</TableHead>
					<TableHead
						className='sticky left-[180px] z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							minWidth: '220px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Tên vật tư
						</div>
					</TableHead>
					<TableHead
						className='sticky left-[400px] z-20 bg-neutral-100 p-0 hover:bg-[#f0f0f0]'
						style={{
							minWidth: '80px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							ĐVT
						</div>
					</TableHead>
					<TableHead
						style={{
							minWidth: '220px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Mã nhóm công đoạn
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '220px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Nhóm vật tư, tài sản
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '220px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Lệnh sản xuất
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '180px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Tổng giá trị cần hạch toán (đ)
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '160px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian sử dụng (Ti)
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '160px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian đã phân bổ
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '160px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Thời gian còn lại
						</div>
					</TableHead>
					<TableHead style={{ minWidth: '220px' }}>
						<div className='inline-flex h-fit w-full items-center justify-between gap-2 px-4 py-2 font-bold'>
							Ghi chú
						</div>
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody>
				<TableRow className='bg-slate-100 font-bold'>
					<TableCell
						className='sticky left-0 z-10 bg-slate-100'
						style={{
							minWidth: '60px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					>
						Tổng
					</TableCell>
					<TableCell
						className='sticky left-[60px] z-10 bg-slate-100'
						style={{
							minWidth: '120px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					/>
					<TableCell
						className='sticky left-[180px] z-10 bg-slate-100'
						style={{
							minWidth: '220px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					/>
					<TableCell
						className='sticky left-[400px] z-10 bg-slate-100'
						style={{
							minWidth: '80px',
							boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
						}}
					/>
					<TableCell style={{ minWidth: '220px' }} />
					<TableCell style={{ minWidth: '220px' }} />
					<TableCell style={{ minWidth: '220px' }} />
					<TableCell style={{ minWidth: '180px' }}>
						{formatNumber(totalPendingValue)}
					</TableCell>
					<TableCell style={{ minWidth: '160px' }}>
						{formatNumber(totalUsageTime)}
					</TableCell>
					<TableCell style={{ minWidth: '160px' }}>
						{formatNumber(totalAllocatedTime)}
					</TableCell>
					<TableCell style={{ minWidth: '160px' }}>
						{formatNumber(totalRemainingTime)}
					</TableCell>
					<TableCell style={{ minWidth: '220px' }} />
				</TableRow>
				{items.map((item, index) => {
					const remainingTime =
						(item.usageTime ?? 0) - (item.allocatedTime ?? 0);

					return (
						<TableRow key={item.id}>
							<TableCell
								className='sticky left-0 z-10 bg-white'
								style={{
									minWidth: '60px',
									boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
								}}
							>
								{index + 1}
							</TableCell>
							<TableCell
								className='sticky left-[60px] z-10 bg-white'
								style={{
									minWidth: '120px',
									boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
								}}
							>
								{item.materialCode || item.partCode || ''}
							</TableCell>
							<TableCell
								className='sticky left-[180px] z-10 bg-white'
								style={{
									minWidth: '220px',
									boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
								}}
							>
								{item.materialName || item.partName || ''}
							</TableCell>
							<TableCell
								className='sticky left-[400px] z-10 bg-white'
								style={{
									minWidth: '80px',
									boxShadow: '2px 0 5px -2px rgba(0, 0, 0, 0.1)',
								}}
							>
								{item.unitOfMeasureName}
							</TableCell>
							<TableCell style={{ minWidth: '220px' }}>
								{item.processGroupCode
									? `${item.processGroupCode} - ${item.processGroupName || ''}`
									: item.processGroupName || ''}
							</TableCell>
							<TableCell style={{ minWidth: '220px' }}>
								{renderCodeName(
									item.categoryAssignmentCode,
									item.categoryAssignmentCodeName,
								)}
							</TableCell>
							<TableCell style={{ minWidth: '220px' }}>
								{renderCodeName(
									item.categoryProductionOrderCode,
									item.categoryProductionOrderName,
								)}
							</TableCell>
							<TableCell style={{ minWidth: '220px' }}>
								{formatNumber(item.pendingValueStartPeriod ?? 0)}
							</TableCell>
							<TableCell style={{ minWidth: '160px' }}>
								{formatNumber(item.usageTime ?? 0)}
							</TableCell>
							<TableCell style={{ minWidth: '160px' }}>
								{formatNumber(item.allocatedTime ?? 0)}
							</TableCell>
							<TableCell style={{ minWidth: '160px' }}>
								{formatNumber(remainingTime)}
							</TableCell>
							<TableCell style={{ minWidth: '220px' }}>{item.note || ''}</TableCell>
						</TableRow>
					);
				})}
			</TableBody>
		</Table>
	);

	return (
		<>
			<div className='relative overflow-hidden rounded-t-md border shadow'>
				<Button
					variant='ghost'
					size='sm'
					className='absolute top-2 left-2 z-30 h-8 w-8 p-0'
					onClick={() => setIsExpanded(true)}
				>
					<DynamicIcon name='maximize' className='size-4' />
				</Button>

				{renderContent()}
			</div>

			<Dialog open={isExpanded} onOpenChange={setIsExpanded}>
				<DialogContent
					showCloseButton={false}
					className='top-0 left-0 flex h-screen max-h-screen w-screen max-w-none! translate-x-0 translate-y-0 flex-col overflow-hidden rounded-lg border-0 p-0'
				>
					<DialogTitle hidden />
					<DialogDescription hidden />

					<div className='flex shrink-0 items-center gap-4 border-b bg-white px-6 py-4'>
						<h3 className='flex-1 text-lg font-semibold'>
							Bảng mốc gốc hạch toán dài kỳ
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
							{renderContent()}
						</div>
					</div>
				</DialogContent>
			</Dialog>
		</>
	);
}
