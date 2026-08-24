import { type ColumnDef } from '@tanstack/react-table';
import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate, formatNumber } from '@/lib/utils';

export const VTCG_COST_PLAN_COLUMNS: ColumnDef<CostProduct>[] = [
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
		accessorKey: 'haulDistanceValue',
		header: () => <span className='whitespace-normal'>Cung độ</span>,
		cell: ({ row }) =>
			row.original.haulDistanceValue ? `${row.original.haulDistanceValue} km` : '-',
	},
	{
		accessorKey: 'fuelAdjustmentFactor',
		header: () => <span className='whitespace-normal'>Hệ số ĐC NL</span>,
		cell: ({ row }) =>
			row.original.fuelAdjustmentFactor !== undefined &&
			row.original.fuelAdjustmentFactor !== null
				? formatNumber(row.original.fuelAdjustmentFactor)
				: '-',
	},
	{
		accessorKey: 'equipmentQuality',
		header: () => <span className='whitespace-normal'>Chất lượng</span>,
		cell: ({ row }) =>
			row.original.equipmentQuality ? `Loại ${row.original.equipmentQuality}` : '-',
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
				Sản lượng <br /> kế hoạch <br /> ban đầu
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.totalProductionMeters),
	},
	{
		accessorKey: 'plannedTotalCost',
		header: () => (
			<span>
				Doanh thu <br /> kế hoạch <br /> ban đầu (đ)
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.plannedTotalCost),
	},
];
