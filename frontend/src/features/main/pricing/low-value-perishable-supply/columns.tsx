import { ColumnDef } from '@tanstack/react-table';

export type LowValuePerishablePeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export type LowValuePerishableSupplyUnitPrice = {
	id: string;
	departmentId: string;
	departmentCode: string;
	departmentName: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	startMonth?: string;
	endMonth?: string;
	totalPrice?: number;
	periods?: LowValuePerishablePeriod[];
};

export const LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS: ColumnDef<LowValuePerishableSupplyUnitPrice>[] =
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
	];
