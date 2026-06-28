import { formatNumber } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { ColumnDef } from '@tanstack/react-table';
import { RawAcceptanceReportItem } from './types';

const getQuotaBasedMaterialTypeLabel = (value: number): string => {
	switch (value) {
		case 1:
			return 'Lĩnh mới';
		case 2:
			return 'Tái sử dụng';
		default:
			return '';
	}
};

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
			accessorKey: 'documentNumber',
			header: () => <span className='whitespace-normal'>{'Số chứng từ'}</span>,
			cell: ({ row }) => row.original.documentNumber || '-',
		},
		{
			accessorKey: 'postingDate',
			header: () => <span className='whitespace-normal'>{'Ngày vào sổ'}</span>,
			cell: ({ row }) => row.original.postingDate || '-',
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
			cell: ({ row }) => {
				const total = row.original.quotaBasedMaterialQuantity;
				const details = (
					row.original.quotaBasedMaterialQuantities ?? []
				).filter(
					(detail) => detail.quantity != null && Number(detail.quantity) !== 0,
				);

				if (details.length === 0) {
					return total ? formatNumber(total) : '-';
				}

				return (
					<div className='flex flex-col items-center gap-1'>
						<div>{total ? formatNumber(total) : '-'}</div>
						<div className='text-muted-foreground flex flex-col gap-0.5 text-xs'>
							{details.map((detail) => (
								<div key={detail.type} className='whitespace-nowrap'>
									{getQuotaBasedMaterialTypeLabel(detail.type)}:{' '}
									{formatNumber(detail.quantity)}
								</div>
							))}
						</div>
					</div>
				);
			},
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
