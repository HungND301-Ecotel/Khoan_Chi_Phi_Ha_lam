import { formatDate } from '@/lib/utils';
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
		header: 'Thời gian',
		cell: ({ row }) => (
			<div className='flex flex-col text-xs font-medium'>
				<span>{formatDate(row.original.startMonth)}</span>
				<span className='text-gray-500'>
					{formatDate(row.original.endMonth)}
				</span>
			</div>
		),
	},
	{
		accessorKey: 'assignmentCodeName',
		header: 'Nhóm vật tư, tài sản',
		cell: ({ row }) => row.original.assignmentCodeName || '-',
	},
];
