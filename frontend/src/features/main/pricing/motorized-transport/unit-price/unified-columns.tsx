import { ColumnDef } from '@tanstack/react-table';
import { MechanizedTransportAssignmentGroup } from './types';

export const UNIFIED_MOTORIZED_TRANSPORT_COLUMNS: ColumnDef<MechanizedTransportAssignmentGroup>[] =
	[
		{
			accessorKey: 'assignmentCodeName',
			header: 'Nhóm vật tư, tài sản',
			cell: ({ row }) => row.original.assignmentCodeName || '-',
		},
	];
