import { ColumnDef } from '@tanstack/react-table';

export type WasteSuctionTruckUnitPriceDetail = {
	id?: string;
	haulDistanceId?: string;
	haulDistanceValue?: string;
	fuelUnitPrice: number;
	maintenanceUnitPrice: number;
};

export type MotorizedVacuumTruckUnitPrice = {
	id: string;
	assignmentCodeId: string;
	assignmentCodeName: string;
	equipmentQuality: string;
	productionProcessId: string;
	productionProcessName: string;
	startMonth: string;
	endMonth: string;
	details: WasteSuctionTruckUnitPriceDetail[];
	allProcesses?: MotorizedVacuumTruckUnitPrice[];
	allIds?: string[];
};

export const MOTORIZED_VACUUM_TRUCK_COLUMNS: ColumnDef<MotorizedVacuumTruckUnitPrice>[] = [
	{
		accessorKey: 'startMonth',
		header: 'Thời gian bắt đầu',
		cell: ({ row }) => row.original.startMonth?.substring(0, 7) || row.original.startMonth,
	},
	{
		accessorKey: 'endMonth',
		header: 'Thời gian kết thúc',
		cell: ({ row }) => row.original.endMonth?.substring(0, 7) || row.original.endMonth,
	},
	{
		accessorKey: 'assignmentCodeName',
		header: 'Nhóm vật tư, tài sản',
		cell: ({ row }) => row.original.assignmentCodeName || '-',
	},
];
