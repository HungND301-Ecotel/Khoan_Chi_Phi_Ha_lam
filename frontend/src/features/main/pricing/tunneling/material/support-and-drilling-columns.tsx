import { ColumnDef } from '@tanstack/react-table';
import { SupportAndDrillingMaterial } from './type';
import { formatDate, formatNumber } from '@/lib/utils';

export const MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS: ColumnDef<SupportAndDrillingMaterial>[] =
	[
		{
			accessorKey: 'code',
			header: 'Mã đơn giá',
		},
		{
			accessorKey: 'processName',
			header: 'Công đoạn sản xuất',
		},
		{
			accessorKey: 'technologyName',
			header: 'Công nghệ',
		},
		{
			accessorKey: 'passportName',
			header: 'Hộ chiếu',
		},
		{
			accessorKey: 'hardnessName',
			header: 'Độ kiên cố than đá',
		},
		{
			id: 'time',
			header: 'Thời gian',
			cell: ({ row }) => (
				<span>
					<span>{formatDate(row.original.startMonth)}</span>
					<br />
					<span>{formatDate(row.original.endMonth)}</span>
				</span>
			),
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá vật liệu (đ/m)',
			cell: ({ row }) => formatNumber(row.original.totalPrice),
		},
	];
