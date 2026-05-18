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
import { cn, formatNumber } from '@/lib/utils';
import { HierarchicalRow } from './types';
import { memo, useEffect, useMemo, useState } from 'react';
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

type OverflowTooltipTextProps = {
	text?: string;
	className?: string;
	tooltipClassName?: string;
};

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

function OverflowTooltipText({
	text,
	className,
	tooltipClassName,
}: OverflowTooltipTextProps) {
	if (!text) return null;

	return (
		<Tooltip>
			<TooltipTrigger asChild>
				<div className={cn('min-w-0 truncate', className)}>{text}</div>
			</TooltipTrigger>
			<TooltipContent
				side='top'
				align='start'
				className={cn(
					'max-w-96 break-words whitespace-pre-wrap',
					tooltipClassName,
				)}
			>
				{text}
			</TooltipContent>
		</Tooltip>
	);
}

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
	let groupPath: number[] = [];

	rows.forEach((row) => {
		if (row.rowType === 'category') {
			categoryIndex += 1;
			typeIndex = 0;
			groupPath = [];
			sttMap.set(row.id, `${String.fromCharCode(64 + categoryIndex)}.`);
			return;
		}

		if (row.rowType === 'type') {
			typeIndex += 1;
			groupPath = [];
			sttMap.set(row.id, `${toRoman(typeIndex)}.`);
			return;
		}

		if (row.rowType === 'group') {
			const depth = Math.max(1, row.level - 1);
			groupPath = groupPath.slice(0, depth);
			while (groupPath.length < depth) {
				groupPath.push(0);
			}
			groupPath[depth - 1] += 1;
			sttMap.set(row.id, `${toRoman(typeIndex)}.${groupPath.join('.')}`);
			return;
		}

		sttMap.set(row.id, '');
	});

	return sttMap;
};

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

const getRowCode = (row: HierarchicalRow) => row.code ?? row.itemCode ?? '';
const getRowName = (row: HierarchicalRow) => row.name ?? row.itemName ?? row.label;

const AcceptanceReportTableContent = memo(function AcceptanceReportTableContent({
	data,
}: {
	data: HierarchicalRow[];
}) {
	const sttByRowId = useMemo(() => buildSttByRowId(data), [data]);

	const renderCodeColumn = (row: HierarchicalRow) => (
		<OverflowTooltipText
			text={getRowCode(row)}
			className='w-24 max-w-24'
			tooltipClassName='max-w-80'
		/>
	);

	const renderNameColumn = (row: HierarchicalRow) => (
		<OverflowTooltipText text={getRowName(row)} className='w-56 max-w-56' />
	);

	const renderMergedHeaderColumn = (row: HierarchicalRow) => (
		<OverflowTooltipText text={getRowName(row)} className='w-80 max-w-80' />
	);

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
			) {
				return null;
			}
		}

		const value = row.data[field] as number;
		if (!value || value === 0) return null;

		return formatNumber(value);
	};

	return (
		<Table className='text-xs'>
			<TableHeader>
				<TableRow>
					<TableHead
						rowSpan={3}
						className='min-w-14 border-r text-center font-bold'
					>
						STT
					</TableHead>
					<TableHead colSpan={2} className='border-r text-center font-bold'>
						DANH MỤC VẬT TƯ, HÀNG HÓA
					</TableHead>
					<TableHead
						rowSpan={3}
						className='min-w-15 border-r text-center font-bold'
					>
						ĐVT
					</TableHead>
					<TableHead
						rowSpan={3}
						className='min-w-22.5 border-r text-center font-bold'
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
					<TableHead rowSpan={2} className='min-w-24 border-r text-center'>
						Mã vật tư
					</TableHead>
					<TableHead rowSpan={2} className='min-w-56 border-r text-center'>
						Tên vật tư
					</TableHead>
					<TableHead rowSpan={2} className='min-w-25 border-r text-center'>
						Kế hoạch
					</TableHead>
					<TableHead rowSpan={2} className='min-w-25 border-r text-center'>
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
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền KH
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền KH
					</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền TT
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 border-r text-center'>
						Thành tiền
					</TableHead>
					<TableHead className='min-w-20 border-r text-center'>SL</TableHead>
					<TableHead className='min-w-30 text-center'>Thành tiền</TableHead>
				</TableRow>
			</TableHeader>

			<TableBody>
				{data.length === 0 ? (
					<TableRow>
						<TableCell
							colSpan={44}
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

							{row.rowType === 'category' || row.rowType === 'type' ? (
								<TableCell colSpan={2} className='border-r'>
									{renderMergedHeaderColumn(row)}
								</TableCell>
							) : (
								<>
									<TableCell className='border-r'>
										{renderCodeColumn(row)}
									</TableCell>
									<TableCell className='border-r'>
										{renderNameColumn(row)}
									</TableCell>
								</>
							)}

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
});

export function AcceptanceReportDataTable({ data, className }: DataTableProps) {
	const [isExpanded, setIsExpanded] = useState(false);
	const [shouldRenderExpandedTable, setShouldRenderExpandedTable] =
		useState(false);

	useEffect(() => {
		if (!isExpanded) {
			setShouldRenderExpandedTable(false);
			return;
		}

		const frameId = window.requestAnimationFrame(() => {
			setShouldRenderExpandedTable(true);
		});

		return () => window.cancelAnimationFrame(frameId);
	}, [isExpanded]);

	return (
		<>
			<div
				className={cn('relative overflow-auto rounded-md border', className)}
			>
				<Button
					variant='ghost'
					size='sm'
					className='absolute top-2 left-2 z-30 h-8 w-8 p-0'
					onClick={() => setIsExpanded(true)}
				>
					<DynamicIcon name='maximize' className='size-4' />
				</Button>

				<AcceptanceReportTableContent data={data} />
			</div>

			<Dialog open={isExpanded} onOpenChange={setIsExpanded}>
				<DialogContent
					showCloseButton={false}
					className='top-0! left-0! flex h-screen! max-h-screen! w-screen! max-w-none! translate-x-0! translate-y-0! flex-col overflow-hidden rounded-lg border-0 p-0'
				>
					<DialogTitle hidden />
					<DialogDescription hidden />

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

					<div className='flex-1 overflow-auto bg-gray-50 p-6'>
						<div className='inline-block min-w-full overflow-hidden rounded-md border shadow'>
							{shouldRenderExpandedTable ? (
								<AcceptanceReportTableContent data={data} />
							) : (
								<div className='flex min-h-40 items-center justify-center bg-white text-sm text-muted-foreground'>
									Đang tải bảng...
								</div>
							)}
						</div>
					</div>
				</DialogContent>
			</Dialog>
		</>
	);
}
