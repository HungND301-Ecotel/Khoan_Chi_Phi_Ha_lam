import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type MotorizedLowValueSupplyElectricityUnitPrice = {
	id: string;
	startMonth: string;
	endMonth: string;
	processGroupId?: string;
	processGroupName: string;
	lowValueSupplyUnitPrice?: number;
	lowValuePerishableSupplyUnitPrice?: number;
	electricityUnitPrice?: number;
};

export const MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS: ColumnDef<MotorizedLowValueSupplyElectricityUnitPrice>[] =
	[
		{
			accessorKey: 'startMonth',
			header: 'Thời gian',
			cell: ({ row }) => (
				<div className='flex flex-col text-xs font-medium'>
					<span>{formatDate(row.original.startMonth)}</span>
					<span className='text-black-600'>
						{formatDate(row.original.endMonth)}
					</span>
				</div>
			),
		},
		{
			accessorKey: 'processGroupName',
			header: 'Nhóm công đoạn sản xuất',
			cell: ({ row }) => row.original.processGroupName || '-',
		},
		{
			accessorKey: 'lowValueSupplyUnitPrice',
			header: 'Vật tư mau hỏng rẻ tiền (đ/tháng)',
			cell: ({ row }) =>
				(
					row.original.lowValuePerishableSupplyUnitPrice ??
					row.original.lowValueSupplyUnitPrice ??
					0
				).toLocaleString('vi-VN'),
		},
		{
			accessorKey: 'electricityUnitPrice',
			header: 'Điện năng (đ/tháng)',
			cell: ({ row }) =>
				(row.original.electricityUnitPrice ?? 0).toLocaleString('vi-VN'),
		},
	];
