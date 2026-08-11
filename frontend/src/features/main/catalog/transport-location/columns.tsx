import { ColumnDef } from '@tanstack/react-table';

export const LocationType = {
	Receiving: 1,
	Dumping: 2,
} as const;

export type LocationType = (typeof LocationType)[keyof typeof LocationType];

export type TransportLocation = {
	id: string;
	code: string;
	name: string;
	note?: string;
	locationType: LocationType;
};

export const CATALOG_TRANSPORT_LOCATION_COLUMNS: ColumnDef<TransportLocation>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã vị trí',
		},
		{
			accessorKey: 'name',
			header: 'Tên vị trí',
		},
		{
			accessorKey: 'locationType',
			header: 'Loại vị trí',
			cell: ({ row }) => {
				const type = row.original.locationType;
				if (type === LocationType.Receiving) return 'Vị trí nhận';
				if (type === LocationType.Dumping) return 'Vị trí đổ';
				return '-';
			},
		},
		{
			accessorKey: 'note',
			header: 'Ghi chú',
			cell: ({ row }) => row.original.note || '-',
		},
	];
