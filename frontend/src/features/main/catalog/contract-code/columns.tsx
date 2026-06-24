import { ColumnDef } from '@tanstack/react-table';
import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from '@/components/ui/tooltip';
import { formatNumber } from '@/lib/utils';

export type ContractCode = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId?: string | null;
	unitOfMeasureName?: string | null;
	currentPrice?: number | null;
};

export type ContractCodeMaterialDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName?: string | null;
	materialType?: number | null;
	costAmount?: number | null;
	actualAmount?: number | null;
};

type OverflowTooltipTextProps = {
	text?: string | null;
	className?: string;
};

function renderOverflowTooltipText({
	text,
	className,
}: OverflowTooltipTextProps) {
	if (!text) return null;

	return (
		<Tooltip>
			<TooltipTrigger asChild>
				<div
					className={`block w-full min-w-0 overflow-hidden text-ellipsis whitespace-nowrap ${className ?? ''}`}
				>
					{text}
				</div>
			</TooltipTrigger>
			<TooltipContent
				side='top'
				align='start'
				className='max-w-96 wrap-break-word whitespace-pre-wrap'
			>
				{text}
			</TooltipContent>
		</Tooltip>
	);
}

export const CATALOG_CONTRACT_CODE_COLUMNS: ColumnDef<ContractCode>[] = [
	{
		accessorKey: 'code',
		header: 'Nhóm vật tư, tài sản',
	},
	{
		accessorKey: 'name',
		header: 'Tên nhóm vật tư, tài sản',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'currentPrice',
		header: 'Đơn giá điện năng (đ/kWh)',
		cell: ({ row }) => formatNumber(Number(row.original.currentPrice ?? 0)),
	},
];

export const CATALOG_CONTRACT_CODE_EXPAND_COLUMNS: ColumnDef<ContractCodeMaterialDetail>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã vật tư, tài sản',
			size: 260,
			cell: ({ row }) =>
				renderOverflowTooltipText({
					text: row.original.code,
					className: 'max-w-[260px]',
				}),
		},
		{
			accessorKey: 'name',
			header: 'Tên vật tư, tài sản',
			size: 320,
			cell: ({ row }) =>
				renderOverflowTooltipText({
					text: row.original.name,
					className: 'max-w-[320px]',
				}),
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: 'Đơn vị tính',
			size: 120,
			cell: ({ row }) =>
				renderOverflowTooltipText({
					text: row.original.unitOfMeasureName,
					className: 'max-w-[120px]',
				}),
		},
		{
			accessorKey: 'costAmount',
			header: 'Đơn giá kế hoạch (đ)',
			size: 180,
			cell: ({ row }) => formatNumber(Number(row.original.costAmount ?? 0)),
		},
		{
			accessorKey: 'actualAmount',
			header: 'Đơn giá thực tế (đ)',
			size: 180,
			cell: ({ row }) => formatNumber(Number(row.original.actualAmount ?? 0)),
		},
	];
