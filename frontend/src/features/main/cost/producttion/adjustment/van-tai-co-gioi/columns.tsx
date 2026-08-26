import { type ColumnDef } from '@tanstack/react-table';
import { ProductionAdjustment } from '@/features/main/cost/producttion/adjustment/columns';
import { formatDate, formatNumber } from '@/lib/utils';

export const VTCG_COST_ADJUSTMENT_COLUMNS: ColumnDef<ProductionAdjustment>[] = [
	{
		accessorKey: 'productionProcessCode',
		header: () => <span className='whitespace-normal'>Mã CĐSX</span>,
		cell: ({ row }) => row.original.productionProcessCode || '-',
	},
	{
		accessorKey: 'productionProcessName',
		header: () => <span className='whitespace-normal'>Tên CĐSX</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.productionProcessName || '-'}
			</span>
		),
	},
	{
		accessorKey: 'contractCodeCode',
		header: () => <span className='whitespace-normal'>Mã nhóm xe/VTTS</span>,
		cell: ({ row }) => row.original.contractCodeCode || '-',
	},
	{
		accessorKey: 'contractCodeName',
		header: () => <span className='whitespace-normal'>Tên nhóm xe/VTTS</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.contractCodeName || '-'}
			</span>
		),
	},
	{
		accessorKey: 'equipmentQuality',
		header: () => <span className='whitespace-normal'>Chất lượng</span>,
		cell: ({ row }) =>
			row.original.equipmentQuality
				? `Loại ${row.original.equipmentQuality}`
				: '-',
	},
	{
		accessorKey: 'cargoTypeName',
		header: () => <span className='whitespace-normal'>Loại hàng</span>,
		cell: ({ row }) => row.original.cargoTypeName || '-',
	},
	{
		id: 'locations',
		header: () => <span className='whitespace-normal'>Vị trí nhận → Đổ</span>,
		cell: ({ row }) => {
			const { receivingLocationName, dumpingLocationName } = row.original;
			if (!receivingLocationName && !dumpingLocationName) return '-';
			return `${receivingLocationName || '...'} → ${dumpingLocationName || '...'}`;
		},
	},
	{
		accessorKey: 'haulDistanceValue',
		header: () => <span className='whitespace-normal'>Cung độ</span>,
		cell: ({ row }) =>
			row.original.haulDistanceValue
				? `${row.original.haulDistanceValue} km`
				: '-',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: () => <span className='whitespace-normal'>ĐVT</span>,
		cell: ({ row }) => row.original.unitOfMeasureName || '-',
	},
	{
		accessorKey: 'startMonth',
		header: () => <span>Thời gian</span>,
		cell: ({ row }) => formatDate(row.original.startMonth),
	},
	{
		accessorKey: 'totalProductionMeters',
		header: () => (
			<span>
				Sản lượng <br /> thực tế
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.totalProductionMeters),
	},
	{
		accessorKey: 'adjustmentTotalCost',
		header: () => (
			<span>
				Doanh thu <br /> điều chỉnh (đ)
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.adjustmentTotalCost),
	},
];
