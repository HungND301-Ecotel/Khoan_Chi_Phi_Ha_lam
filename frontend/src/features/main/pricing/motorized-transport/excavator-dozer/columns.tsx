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
		cell: ({ row }) =>
			row.original.assignmentCodeName ||
			row.original.equipmentName ||
			row.original.assignmentCodeId ||
			'-',
	},
];
