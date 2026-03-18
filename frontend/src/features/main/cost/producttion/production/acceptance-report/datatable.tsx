import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { cn, formatNumber } from '@/lib/utils';
import { HierarchicalRow } from './types';
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

type DataTableProps = {
	data: HierarchicalRow[];
	className?: string;
};

export function AcceptanceReportDataTable({ data, className }: DataTableProps) {
	const [isExpanded, setIsExpanded] = useState(false);
	// Get row styling based on row type
	const getRowClassName = (row: HierarchicalRow): string => {
		switch (row.rowType) {
			case 'category':
				return 'bg-primary/10 font-bold text-base';
			case 'type':
				return 'bg-muted font-semibold';
			case 'group':
				return 'text-muted-foreground';
			case 'item':
				return '';
			default:
				return '';
		}
	};

	// Get indentation padding based on level
	const getIndentClass = (level: number): string => {
		const indents = ['pl-2', 'pl-6', 'pl-10', 'pl-14'];
		return indents[level] || 'pl-2';
	};

	// Render first column (Mã Vật tư or label)
	const renderFirstColumn = (row: HierarchicalRow) => {
		if (row.rowType === 'item') {
			return <span className={getIndentClass(row.level)}>{row.itemCode}</span>;
		}
		// Category, type, or group rows show label
		return <span className={getIndentClass(row.level)}>{row.label}</span>;
	};

	// List of "Thành tiền" (amount) fields that should be shown in totals
	const amountFields = [
		'openingBalanceTotalAmount',
		'openingBalanceOnSiteAmount',
		'openingBalancePendingAmount',
		'openingBalanceContractAmount',
		'receiptTotalAmountKH',
		'receiptWithReceiptAmountKH',
		'receiptWithReceiptAmountTT',
		'receiptBorrowedAmount',
		'receiptReturnPrevMonthAmount',
		'receiptHandoverAmount',
		'issueTotalAmount',
		'issueForProductionAmount',
		'issueLongtermAmount',
		'issueOtherAmount',
		'issueContractAmount',
		'closingBalanceTotalAmount',
		'closingBalanceOnSiteAmount',
		'closingBalancePendingAmount',
	];

	// Render financial cell
	const renderFinancialCell = (
		row: HierarchicalRow,
		field: keyof NonNullable<HierarchicalRow['data']>,
	) => {
		// Empty if no data
		if (!row.data) return null;

		// Group rows: show totals only if group has data (showTotals flag)
		// Category/Type rows: only show "Thành tiền" (amount) columns
		if (
			row.rowType === 'category' ||
			row.rowType === 'type' ||
			row.rowType === 'group'
		) {
			if (!amountFields.includes(field)) return null;
		}

		// Get value
		const value = row.data[field] as number;

		// Don't show 0 values (leave empty)
		if (!value || value === 0) return null;

		return formatNumber(value, { maximumFractionDigits: 0 });
	};

	const renderTableContent = () => (
		<Table className='text-xs'>
			<TableHeader>
				{/* Header Row 1: Main groups */}
				<TableRow>
					<TableHead rowSpan={3} className='min-w-[120px] border-r text-center'>
						MÃ VẬT TƯ
					</TableHead>
					<TableHead rowSpan={3} className='min-w-[180px] border-r text-center'>
						TÊN VẬT TƯ
					</TableHead>
					<TableHead rowSpan={3} className='min-w-[60px] border-r text-center'>
						ĐVT
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center font-bold'>
						ĐƠN GIÁ
					</TableHead>
					<TableHead colSpan={8} className='border-r text-center font-bold'>
						TỒN ĐẦU KỲ
					</TableHead>
					<TableHead colSpan={12} className='border-r text-center font-bold'>
						LĨNH TRONG KỲ
					</TableHead>
					<TableHead colSpan={10} className='border-r text-center font-bold'>
						XUẤT TRONG KỲ
					</TableHead>
					<TableHead colSpan={6} className='text-center font-bold'>
						TỒN CUỐI KỲ
					</TableHead>
				</TableRow>

				{/* Header Row 2: Sub-groups */}
				<TableRow>
					<TableHead rowSpan={2} className='min-w-[90px] border-r text-center'>
						Kế hoạch
					</TableHead>
					<TableHead rowSpan={2} className='min-w-[90px] border-r text-center'>
						Thực tế
					</TableHead>

					<TableHead colSpan={2} className='border-r text-center'>
						Tổng cộng
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Tồn tại khai trường
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Chi phí chờ hạch toán
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Quyết định giao khoán công trình
					</TableHead>

					<TableHead colSpan={2} className='border-r text-center'>
						Tổng cộng
					</TableHead>
					<TableHead colSpan={3} className='border-r text-center'>
						Lĩnh vật tư (trả phiếu)
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Vay chưa trả phiếu
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Trả phiếu tháng trước
					</TableHead>
					<TableHead colSpan={3} className='border-r text-center'>
						Nhận bàn giao
					</TableHead>

					<TableHead colSpan={2} className='border-r text-center'>
						Tổng cộng
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Xuất cho sản xuất
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Chi phí vật tư dài kỳ hạch toán
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Xuất khác số
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Quyết định, giao khoán công trình
					</TableHead>

					<TableHead colSpan={2} className='border-r text-center'>
						Tổng cộng
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Tồn tại khai trường
					</TableHead>
					<TableHead colSpan={2} className='text-center'>
						Giá trị cuối kỳ chờ hạch toán
					</TableHead>
				</TableRow>

				{/* Header Row 3: Final columns */}
				<TableRow>
					{/* Tồn đầu kỳ - Tổng cộng */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn đầu kỳ - Tồn tại khai trường */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn đầu kỳ - Chi phí chờ hạch toán */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn đầu kỳ - Quyết định giao khoán công trình */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Lĩnh trong kỳ - Tổng cộng */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền KH
					</TableHead>

					{/* Lĩnh trong kỳ - Lĩnh vật tư (trả phiếu) */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền KH
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>
						Thành tiền TT
					</TableHead>

					{/* Lĩnh trong kỳ - Vay chưa trả phiếu */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Lĩnh trong kỳ - Trả phiếu tháng trước */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Lĩnh trong kỳ - Nhận bàn giao */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-20 border-r text-center'>
						% còn lại
					</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Xuất trong kỳ - Tổng cộng */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Xuất trong kỳ - Xuất cho sản xuất */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Xuất trong kỳ - Chi phí vật tư dài kỳ hạch toán */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Xuất trong kỳ - Xuất khác số */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Xuất trong kỳ - Quyết định, giao khoán công trình */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn cuối kỳ - Tổng cộng */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn cuối kỳ - Tồn tại khai trường */}
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] border-r text-center'>
						Thành tiền
					</TableHead>

					{/* Tồn cuối kỳ - Giá trị cuối kỳ chờ hạch toán */}
					<TableHead className='min-w-20 text-center'>SL</TableHead>
					<TableHead className='min-w-[100px] text-center'>
						Thành tiền
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody>
				{data.length === 0 ? (
					<TableRow>
						<TableCell
							colSpan={41}
							className='text-muted-foreground py-8 text-center'
						>
							Không có dữ liệu
						</TableCell>
					</TableRow>
				) : (
					data.map((row) => (
						<TableRow key={row.id} className={getRowClassName(row)}>
							{/* Column 1: Mã Vật tư (or label for category/type/group) */}
							<TableCell className='border-r'>
								{renderFirstColumn(row)}
							</TableCell>

							{/* Column 2: Tên Vật tư (only for items) */}
							<TableCell className='border-r'>
								{row.rowType === 'item' && row.itemName}
							</TableCell>

							{/* Column 3: ĐVT (only for items) */}
							<TableCell className='border-r text-center'>
								{row.rowType === 'item' && row.unit}
							</TableCell>

							{/* Đơn giá */}
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'priceKH')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'priceTT')}
							</TableCell>

							{/* Tồn đầu kỳ */}
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceTotalQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceTotalAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceOnSiteQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceOnSiteAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalancePendingQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalancePendingAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceContractQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'openingBalanceContractAmount')}
							</TableCell>

							{/* Lĩnh trong kỳ */}
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptTotalQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptTotalAmountKH')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptWithReceiptQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptWithReceiptAmountKH')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptWithReceiptAmountTT')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptBorrowedQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptBorrowedAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptReturnPrevMonthQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptReturnPrevMonthAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptHandoverQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptHandoverPercent')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'receiptHandoverAmount')}
							</TableCell>

							{/* Xuất trong kỳ */}
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueTotalQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueTotalAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueForProductionQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueForProductionAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueLongtermQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueLongtermAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueOtherQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueOtherAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueContractQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'issueContractAmount')}
							</TableCell>

							{/* Tồn cuối kỳ */}
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalanceTotalQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalanceTotalAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalanceOnSiteQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalanceOnSiteAmount')}
							</TableCell>
							<TableCell className='text-right'>
								{renderFinancialCell(row, 'closingBalancePendingQty')}
							</TableCell>
							<TableCell className='text-right'>
								{renderFinancialCell(row, 'closingBalancePendingAmount')}
							</TableCell>
						</TableRow>
					))
				)}
			</TableBody>
		</Table>
	);

	return (
		<>
			<div
				className={cn('relative overflow-auto rounded-md border', className)}
			>
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
					className='top-0! left-0! flex h-screen! max-h-screen! w-screen! max-w-none! translate-x-0! translate-y-0! flex-col overflow-hidden rounded-lg border-0 p-0'
				>
					<DialogTitle hidden />
					<DialogDescription hidden />

					{/* Header */}
					<div className='flex shrink-0 items-center gap-4 border-b bg-white px-6 py-4'>
						<h3 className='flex-1 text-lg font-semibold'>
							Bảng nghiệm thu vật tư và kết chuyển chi phí
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
						<div className='inline-block min-w-full overflow-hidden rounded-md border shadow'>
							{renderTableContent()}
						</div>
					</div>
				</DialogContent>
			</Dialog>
		</>
	);
}
