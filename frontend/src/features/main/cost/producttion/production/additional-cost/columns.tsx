import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type AdditionalCostRowType = 'order' | 'group' | 'item';

export type AdditionalCostRow = {
	id: string;
	stt?: string;
	rowType: AdditionalCostRowType;
	code?: string;
	name?: string;
	unitOfMeasure?: string;
	quantity?: number;
};

function getTextClassName(rowType: AdditionalCostRowType) {
	switch (rowType) {
		case 'order':
			return 'font-bold text-foreground';
		case 'group':
			return 'font-semibold text-slate-600';
		default:
			return 'font-normal text-slate-500';
	}
}

export const ADDITIONAL_COST_COLUMNS: ColumnDef<AdditionalCostRow>[] = [
	{
		accessorKey: 'stt',
		header: () => <span className='whitespace-nowrap'>STT</span>,
		cell: ({ row }) =>
			row.original.stt ? (
				<span className={getTextClassName(row.original.rowType)}>
					{row.original.stt}
				</span>
			) : (
				''
			),
		size: 80,
	},
	{
		accessorKey: 'code',
		header: () => <span className='whitespace-normal'>Mã vật tư</span>,
		cell: ({ row }) => (
			<span className={getTextClassName(row.original.rowType)}>
				{row.original.code ?? ''}
			</span>
		),
		size: 220,
	},
	{
		accessorKey: 'name',
		header: () => <span className='whitespace-normal'>Tên vật tư</span>,
		cell: ({ row }) => (
			<span
				className={`whitespace-normal ${getTextClassName(row.original.rowType)}`}
			>
				{row.original.name ?? ''}
			</span>
		),
		size: 360,
	},
	{
		accessorKey: 'unitOfMeasure',
		header: () => <span className='whitespace-normal'>ĐVT</span>,
		cell: ({ row }) => (row.original.rowType === 'item' ? row.original.unitOfMeasure ?? '' : ''),
		size: 100,
	},
	{
		accessorKey: 'quantity',
		header: () => <span className='whitespace-normal'>Số lượng</span>,
		cell: ({ row }) =>
			row.original.rowType === 'item' &&
			row.original.quantity !== undefined &&
			row.original.quantity !== null
				? formatNumber(row.original.quantity)
				: '',
		size: 120,
	},
];
