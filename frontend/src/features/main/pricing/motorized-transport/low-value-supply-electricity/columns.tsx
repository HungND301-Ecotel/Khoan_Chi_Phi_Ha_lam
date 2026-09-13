import { ColumnDef } from '@tanstack/react-table';

export type MotorizedLowValuePeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	lowValueSupplyUnitPrice: number;
	lowValuePerishableSupplyUnitPrice?: number;
	electricityUnitPrice: number;
};

export type MotorizedLowValueSupplyElectricityUnitPrice = {
	id: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	processGroupId?: string;
	processGroupCode?: string;
	processGroupName?: string;
	startMonth?: string;
	endMonth?: string;
	lowValueSupplyUnitPrice?: number;
	lowValuePerishableSupplyUnitPrice?: number;
	electricityUnitPrice?: number;
	periods?: MotorizedLowValuePeriod[];
};

export const MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_COLUMNS: ColumnDef<MotorizedLowValueSupplyElectricityUnitPrice>[] =
	[
		{
			accessorKey: 'departmentCode',
			header: 'Mã đơn vị',
			cell: ({ row }) => row.original.departmentCode || '-',
		},
		{
			accessorKey: 'departmentName',
			header: 'Tên đơn vị',
			cell: ({ row }) => row.original.departmentName || '-',
		},
		{
			accessorKey: 'processGroupCode',
			header: 'Mã nhóm công đoạn',
			cell: ({ row }) => row.original.processGroupCode || '-',
		},
		{
			accessorKey: 'processGroupName',
			header: 'Tên nhóm công đoạn',
			cell: ({ row }) => row.original.processGroupName || '-',
		},
	];
