import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type TransportLowValuePerishableSupplyUnitPrice = {
	id: string;
	departmentId: string;
	departmentCode: string;
	departmentName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export const TRANSPORT_LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS: ColumnDef<TransportLowValuePerishableSupplyUnitPrice>[] =
	[
		{
			accessorKey: 'departmentCode',
			header: 'Mã đơn vị',
		},
		{
			accessorKey: 'departmentName',
			header: 'Tên đơn vị',
		},
		{
			accessorKey: 'processGroupCode',
			header: 'Mã nhóm công đoạn',
		},
		{
			accessorKey: 'processGroupName',
			header: 'Tên nhóm công đoạn',
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá (đ/tháng)',
			cell: ({ row }) => formatNumber(row.original.totalPrice),
		},
		{
			accessorKey: 'time',
			header: 'Thời gian',
			cell: ({ row }) => (
				<span>
					<span>{formatDate(row.original.startMonth)}</span>
					<br />
					<span>{formatDate(row.original.endMonth)}</span>
				</span>
			),
		},
	];
