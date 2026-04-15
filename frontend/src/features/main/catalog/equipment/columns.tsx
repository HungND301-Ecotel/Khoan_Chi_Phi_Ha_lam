import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { Badge } from '@/components/ui/badge';

export type Equipment = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	currentPrice: number;
	processGroups: Array<{
		id: string;
		code: string;
		type: number;
		name: string;
	}>;
};

export type EquipmentPartDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName: string;
	replacementTimeStandard: number;
	currentCost: number;
	actualAmount: number;
};

const getProcessGroupsSortValue = (processGroups: Equipment['processGroups']) =>
	(processGroups ?? [])
		.map((item) => `${item.code} ${item.name}`.trim())
		.sort((a, b) => a.localeCompare(b, 'vi', { sensitivity: 'base' }))
		.join(' | ');

export const CATALOG_EQUIPMENT_EXPAND_COLUMNS: ColumnDef<EquipmentPartDetail>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã phụ tùng',
		},
		{
			accessorKey: 'name',
			header: 'Tên phụ tùng',
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: 'Đơn vị tính',
		},
		{
			accessorKey: 'currentCost',
			header: 'Đơn giá kế hoạch (đ)',
			cell: ({ row }) => formatNumber(row.original.currentCost),
		},
		{
			accessorKey: 'actualAmount',
			header: 'Đơn giá thực tế (đ)',
			cell: ({ row }) => formatNumber(row.original.actualAmount),
		},
	];

export const CATALOG_EQUIPMENT_COLUMNS: ColumnDef<Equipment>[] = [
	{
		accessorKey: 'code',
		header: 'Mã thiết bị',
	},
	{
		accessorKey: 'name',
		header: 'Tên thiết bị',
	},
	{
		id: 'processGroups',
		accessorFn: (row) => getProcessGroupsSortValue(row.processGroups),
		header: 'Nhóm công đoạn sản xuất',
		sortingFn: (rowA, rowB, columnId) =>
			String(rowA.getValue(columnId)).localeCompare(
				String(rowB.getValue(columnId)),
				'vi',
				{ sensitivity: 'base' },
			),
		cell: ({ row }) => (
			<div className='flex flex-wrap gap-1'>
				{(row.original.processGroups ?? []).map((item) => (
					<Badge
						key={item.id}
						variant='secondary'
						className='whitespace-normal'
					>
						{item.code} - {item.name}
					</Badge>
				))}
			</div>
		),
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
	{
		accessorKey: 'currentPrice',
		header: 'Đơn giá điện năng (đ/kWh)',
		cell: ({ row }) => formatNumber(row.original.currentPrice),
	},
];
