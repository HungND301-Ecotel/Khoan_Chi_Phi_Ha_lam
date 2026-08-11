import { ColumnDef } from '@tanstack/react-table';

export type TransportDistanceParameter = {
	id: string;
	value?: string;
	name?: string;
	code?: string;
	distanceRange?: string;
};

export const CATALOG_TRANSPORT_DISTANCE_COLUMNS: ColumnDef<TransportDistanceParameter>[] = [
	{
		accessorKey: 'value',
		header: 'Cung độ vận tải (L)',
		cell: ({ row }) => row.original.value || row.original.name || row.original.distanceRange || '-',
	},
];
