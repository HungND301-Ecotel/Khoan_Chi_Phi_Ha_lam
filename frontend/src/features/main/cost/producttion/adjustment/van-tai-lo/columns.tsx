import { type ColumnDef } from '@tanstack/react-table';
import { ProductionAdjustment } from '@/features/main/cost/producttion/adjustment/columns';
import { formatDate, formatNumber } from '@/lib/utils';

// Song song VTL_COST_PLAN_COLUMNS bên Kế hoạch sản xuất — chỉ khác 2 cột cuối lấy Sản lượng
// thực tế / Doanh thu điều chỉnh thay vì Sản lượng kế hoạch ban đầu / Doanh thu kế hoạch ban đầu.
export const VTL_COST_ADJUSTMENT_COLUMNS: ColumnDef<ProductionAdjustment>[] = [
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
		header: () => <span className='whitespace-normal'>Loại</span>,
		cell: ({ row }) => row.original.equipmentQuality || '-',
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
