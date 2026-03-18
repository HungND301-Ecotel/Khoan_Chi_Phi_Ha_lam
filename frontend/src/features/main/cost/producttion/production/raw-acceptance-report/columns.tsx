import { formatNumber } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { ColumnDef } from '@tanstack/react-table';
import { RawAcceptanceReportItem } from './types';

export const RAW_ACCEPTANCE_REPORT_EXPAND_COLUMNS: ColumnDef<RawAcceptanceReportItem>[] =
	[
		{
			accessorKey: 'materialCode',
			header: () => <span className='whitespace-normal'>{'Mã vật tư'}</span>,
		},
		{
			accessorKey: 'materialName',
			header: () => <span className='whitespace-normal'>{'Tên vật tư'}</span>,
		},
		{
			accessorKey: 'unit',
			header: () => <span className='h-fit whitespace-normal'>{'ĐVT'}</span>,
		},
		{
			id: 'issuedQuantity',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'Số lượng lĩnh trong kỳ'}
				</span>
			),
			cell: ({ row }) => formatNumber(row.original.issuedQuantity),
		},
		{
			id: 'shippedQuantity',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'Số lượng xuất trong kỳ'}
				</span>
			),
			cell: ({ row }) => formatNumber(row.original.shippedQuantity),
		},
		{
			id: 'type',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'Loại vật tư'}
				</span>
			),
			cell: ({ row }) => <Badge variant='secondary'>{row.original.type}</Badge>,
		},
		{
			id: 'materialsIncludedInContractRevenueQuantity',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'SL vật tư tính vào doanh thu'}
				</span>
			),
			cell: ({ row }) =>
				row.original.materialsIncludedInContractRevenueQuantity
					? formatNumber(
							row.original.materialsIncludedInContractRevenueQuantity,
						)
					: '-',
		},
		{
			id: 'additionalCostQuantity',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'SL bổ sung chi phí'}
				</span>
			),
			cell: ({ row }) =>
				row.original.additionalCostQuantity
					? formatNumber(row.original.additionalCostQuantity)
					: '-',
		},
		{
			id: 'quotaBasedMaterialQuantity',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'SL vật tư theo hạn mức'}
				</span>
			),
			cell: ({ row }) =>
				row.original.quotaBasedMaterialQuantity
					? formatNumber(row.original.quotaBasedMaterialQuantity)
					: '-',
		},
		{
			id: 'asset',
			header: () => (
				<span className='h-fit text-center whitespace-normal'>
					{'SL tài sản'}
				</span>
			),
			cell: ({ row }) =>
				row.original.asset ? formatNumber(row.original.asset) : '-',
		},
	];
