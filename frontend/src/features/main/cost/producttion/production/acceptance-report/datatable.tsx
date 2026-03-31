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

	const toRoman = (value: number): string => {
		const romanMap = [
			{ value: 1000, numeral: 'M' },
			{ value: 900, numeral: 'CM' },
			{ value: 500, numeral: 'D' },
			{ value: 400, numeral: 'CD' },
			{ value: 100, numeral: 'C' },
			{ value: 90, numeral: 'XC' },
			{ value: 50, numeral: 'L' },
			{ value: 40, numeral: 'XL' },
			{ value: 10, numeral: 'X' },
			{ value: 9, numeral: 'IX' },
			{ value: 5, numeral: 'V' },
			{ value: 4, numeral: 'IV' },
			{ value: 1, numeral: 'I' },
		];

		let remaining = value;
		let result = '';

		romanMap.forEach(({ value: romanValue, numeral }) => {
			while (remaining >= romanValue) {
				result += numeral;
				remaining -= romanValue;
			}
		});

		return result;
	};

	const buildSttByRowId = (rows: HierarchicalRow[]) => {
		const sttMap = new Map<string, string>();
		let categoryIndex = 0;
		let typeIndex = 0;
		let groupIndex = 0;
		let subGroupIndex = 0;

		rows.forEach((row) => {
			if (row.rowType === 'category') {
				categoryIndex += 1;
				typeIndex = 0;
				groupIndex = 0;
				sttMap.set(row.id, `${String.fromCharCode(64 + categoryIndex)}.`);
				return;
			}

			if (row.rowType === 'type') {
				typeIndex += 1;
				groupIndex = 0;
				subGroupIndex = 0;
				sttMap.set(row.id, `${toRoman(typeIndex)}.`);
				return;
			}

			if (row.rowType === 'group') {
				if (row.level >= 3) {
					subGroupIndex += 1;
					sttMap.set(
						row.id,
						`${toRoman(typeIndex)}.${groupIndex}.${subGroupIndex}`,
					);
					return;
				}
				groupIndex += 1;
				subGroupIndex = 0;
				sttMap.set(row.id, `${toRoman(typeIndex)}.${groupIndex}`);
				return;
			}

			sttMap.set(row.id, '');
		});

		return sttMap;
	};

	const sttByRowId = buildSttByRowId(data);

	// Get row styling based on row type
	const getRowClassName = (row: HierarchicalRow): string => {
		switch (row.rowType) {
			case 'category':
				return 'bg-primary/10 font-bold text-base';
			case 'type':
				return 'bg-muted font-semibold';
			case 'group':
				return row.level <= 2
					? 'bg-muted/40 font-medium text-foreground'
					: 'text-muted-foreground';
			case 'item':
				return '';
			default:
				return '';
		}
	};

	// Get indentation padding based on level
	const getIndentClass = (level: number): string => {
		const indents = ['pl-2', 'pl-6', 'pl-12', 'pl-20', 'pl-28'];
		return indents[level] || 'pl-2';
	};

	// Render danh mục column
	const renderCategoryColumn = (row: HierarchicalRow) => {
		if (row.rowType === 'item') {
			const itemLabel = row.itemCode
				? `${row.itemCode} - ${row.itemName || ''}`
				: row.itemName || '';
			return <span className={getIndentClass(row.level)}>{itemLabel}</span>;
		}
		return <span className={getIndentClass(row.level)}>{row.label}</span>;
	};

	// Amount-like fields rendered on totals rows (category/type/group)
	const quantityFields = [
		'openingBalanceTotalQty',
		'openingBalanceOnSiteQty',
		'openingBalancePendingQty',
		'openingBalanceContractQty',
		'receiptTotalQty',
		'receiptWithReceiptQty',
		'receiptBorrowedQty',
		'receiptReturnPrevMonthQty',
		'receiptHandoverQty',
		'issueTotalQty',
		'issueForProductionQty',
		'issueLongtermQty',
		'issueOtherQty',
		'issueContractQty',
		'closingBalanceTotalQty',
		'closingBalanceOnSiteQty',
		'closingBalancePendingQty',
		'closingBalanceContractQty',
	] as const;

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
		'closingBalanceContractAmount',
	] as const;

	const totalRowVisibleFields = [...quantityFields, ...amountFields];

	// Render numeric cell
	const renderFinancialCell = (
		row: HierarchicalRow,
		field: keyof NonNullable<HierarchicalRow['data']>,
	) => {
		if (!row.data) return null;

		if (
			row.rowType === 'category' ||
			row.rowType === 'type' ||
			row.rowType === 'group'
		) {
			if (
				row.rowType === 'category' &&
				quantityFields.includes(field as (typeof quantityFields)[number])
			) {
				return null;
			}

			if (
				!totalRowVisibleFields.includes(
					field as (typeof totalRowVisibleFields)[number],
				)
			)
				return null;
		}

		const value = row.data[field] as number;
		if (!value || value === 0) return null;

		const isQuantityField = quantityFields.includes(
			field as (typeof quantityFields)[number],
		);
		return formatNumber(value, {
			maximumFractionDigits: isQuantityField ? 3 : 0,
		});
	};

	const renderTableContent = () => (
		<Table className='text-xs'>
			<TableHeader>
				<TableRow>
					<TableHead
						rowSpan={3}
						className='min-w-14 border-r text-center font-bold'
					>
						STT
					</TableHead>
					<TableHead
						rowSpan={3}
						className='min-w-[220px] border-r text-center font-bold'
					>
						DANH MỤC VẬT TƯ, HÀNG HÓA
					</TableHead>
					<TableHead
						rowSpan={3}
						className='min-w-[60px] border-r text-center font-bold'
					>
						ĐVT
					</TableHead>
					<TableHead
						rowSpan={3}
						className='min-w-[90px] border-r text-center font-bold'
					>
						CÁCH TÍNH
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center font-bold'>
						ĐƠN GIÁ
					</TableHead>

					<TableHead colSpan={8} className='border-r text-center font-bold'>
						TỒN ĐẦU KỲ
					</TableHead>
					<TableHead colSpan={11} className='border-r text-center font-bold'>
						LĨNH TRONG KỲ
					</TableHead>
					<TableHead colSpan={10} className='border-r text-center font-bold'>
						XUẤT TRONG KỲ
					</TableHead>
					<TableHead colSpan={8} className='text-center font-bold'>
						TỒN CUỐI KỲ
					</TableHead>
				</TableRow>

				<TableRow>
					<TableHead rowSpan={2} className='min-w-[100px] border-r text-center'>
						Kế hoạch
					</TableHead>
					<TableHead rowSpan={2} className='min-w-[100px] border-r text-center'>
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
						Quyết định, giao khoán công trình
					</TableHead>

					<TableHead colSpan={2} className='border-r text-center'>
						Tổng cộng
					</TableHead>
					<TableHead colSpan={3} className='border-r text-center'>
						Lĩnh vật tư (Trả phiếu)
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Vay chưa trả phiếu
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Trả phiếu tháng trước
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center'>
						Lĩnh khác
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
						Xuất khác
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
					<TableHead colSpan={2} className='border-r text-center'>
						Chi phí chờ hạch toán
					</TableHead>
					<TableHead colSpan={2} className='text-center'>
						Quyết định, giao khoán công trình
					</TableHead>
				</TableRow>

				<TableRow>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>

					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền KH
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền KH
					</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền TT
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>

					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>

					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-[120px] text-center'>
						Thành tiền
					</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody>
				{data.length === 0 ? (
					<TableRow>
						<TableCell
							colSpan={43}
							className='text-muted-foreground py-8 text-center'
						>
							Không có dữ liệu
						</TableCell>
					</TableRow>
				) : (
					data.map((row) => (
						<TableRow key={row.id} className={getRowClassName(row)}>
							<TableCell className='border-r text-center'>
								{sttByRowId.get(row.id)}
							</TableCell>

							<TableCell className='border-r'>
								{renderCategoryColumn(row)}
							</TableCell>

							<TableCell className='border-r text-center'>
								{row.rowType === 'item' && row.unit}
							</TableCell>

							<TableCell className='border-r text-center' />
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'priceKH')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'priceTT')}
							</TableCell>

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
								{renderFinancialCell(row, 'receiptHandoverAmount')}
							</TableCell>

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
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalancePendingQty')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalancePendingAmount')}
							</TableCell>
							<TableCell className='border-r text-right'>
								{renderFinancialCell(row, 'closingBalanceContractQty')}
							</TableCell>
							<TableCell className='text-right'>
								{renderFinancialCell(row, 'closingBalanceContractAmount')}
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
