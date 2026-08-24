import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type MotorizedExcavatorDozerUnitPrice = {
	id: string;
	startMonth: string;
	endMonth: string;
	equipmentId?: string;
	assignmentCodeId?: string;
	equipmentName?: string;
	assignmentCodeName?: string;
	equipmentQuality: 'A' | 'B' | 'C' | string;
	productionProcessId?: string;
	productionProcess?: string;
	productionProcessName?: string;
	fuelUnitPrice?: number;
	maintenanceUnitPrice?: number;
	details?: Array<{
		id?: string;
		fuelUnitPrice?: number;
		maintenanceUnitPrice?: number;
	}>;
	allProcesses?: MotorizedExcavatorDozerUnitPrice[];
	allIds?: string[];
};

export const MOTORIZED_EXCAVATOR_DOZER_COLUMNS: ColumnDef<MotorizedExcavatorDozerUnitPrice>[] = [
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
		cell: ({ row }) =>
			row.original.assignmentCodeName ||
			row.original.equipmentName ||
			row.original.assignmentCodeId ||
			'-',
	},
];
