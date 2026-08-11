import { ColumnDef } from '@tanstack/react-table';
import { MechanizedTransportUnitPriceGroupDto } from './types';

export const UNIFIED_MOTORIZED_TRANSPORT_COLUMNS: ColumnDef<MechanizedTransportUnitPriceGroupDto>[] =
	[
		{
			accessorKey: 'timeRange',
			header: 'Thời gian',
			cell: ({ row }) => {
				const formatMonth = (val: string) => {
					if (!val) return '-';
					const parts = val.substring(0, 7).split('-');
					if (parts.length === 2) return `${parts[1]}/${parts[0]}`;
					return val;
				};
				const start = formatMonth(row.original.startMonth || '');
				const end = formatMonth(row.original.endMonth || '');
				return (
					<div className='text-xs'>
						<div>{start}</div>
						<div>{end}</div>
					</div>
				);
			},
		},
		{
			accessorKey: 'assignmentCodeName',
			header: 'Nhóm vật tư, tài sản',
			cell: ({ row }) => row.original.assignmentCodeName || '-',
		},
	];
