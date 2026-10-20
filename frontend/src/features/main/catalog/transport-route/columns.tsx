import { ColumnDef } from '@tanstack/react-table';

export type TransportRoute = {
	id: string;
	code: string;
	name: string;
	note?: string;
	isSpecialLowVolume: boolean;
};

export const CATALOG_TRANSPORT_ROUTE_COLUMNS: ColumnDef<TransportRoute>[] = [
	{
		accessorKey: 'code',
		header: 'Mã tuyến vận tải',
	},
	{
		accessorKey: 'name',
		header: 'Tên tuyến vận tải',
	},
	{
		accessorKey: 'isSpecialLowVolume',
		header: 'Đơn giá tuyến vận tải phụ thuộc sản lượng',
		cell: ({ row }) => (row.original.isSpecialLowVolume ? 'Có' : 'Không'),
	},
	{
		accessorKey: 'note',
		header: 'Ghi chú',
	},
];
