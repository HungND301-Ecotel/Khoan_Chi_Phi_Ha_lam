import { type ColumnDef } from '@tanstack/react-table';
import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate, formatNumber } from '@/lib/utils';

export const VTL_COST_PLAN_COLUMNS: ColumnDef<CostProduct>[] = [
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
		header: () => <span className='whitespace-normal'>Mã nhóm VTTS</span>,
		cell: ({ row }) => row.original.contractCodeCode || '-',
	},
	{
		accessorKey: 'contractCodeName',
		header: () => <span className='whitespace-normal'>Tên nhóm VTTS</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.contractCodeName || '-'}
			</span>
		),
	},
	{
		accessorKey: 'routeDepartmentCode',
		header: () => <span className='whitespace-normal'>Mã đơn vị</span>,
		cell: ({ row }) => row.original.routeDepartmentCode || '-',
	},
	{
		accessorKey: 'routeDepartmentName',
		header: () => <span className='whitespace-normal'>Tên đơn vị</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.routeDepartmentName || '-'}
			</span>
		),
	},
	{
		accessorKey: 'equipmentQuality',
		header: () => <span className='whitespace-normal'>Chất lượng</span>,
		cell: ({ row }) => {
			const quality = row.original.equipmentQuality;
			if (!quality || quality === '-') return '-';
			return quality.startsWith('Loại') ? quality : `Loại ${quality}`;
		},
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: () => <span className='whitespace-normal'>Đơn vị tính</span>,
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
