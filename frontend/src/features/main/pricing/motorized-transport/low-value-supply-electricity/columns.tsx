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
			header: 'Thời gian bắt đầu',
			cell: ({ row }) =>
				row.original.startMonth?.substring(0, 7) || row.original.startMonth,
		},
		{
			accessorKey: 'endMonth',
			header: 'Thời gian kết thúc',
			cell: ({ row }) =>
				row.original.endMonth?.substring(0, 7) || row.original.endMonth,
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
