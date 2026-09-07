import { ColumnDef } from '@tanstack/react-table';
import { SupportAndDrillingMaterial } from './type';

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
	];

export type ExpandSupportAndDrillingDetail = {
	technologyName?: string;
	passportText?: string;
	hardnessName?: string;
};

export const MAIN_PRICING_SUPPORT_AND_DRILLING_DETAIL_COLUMNS: ColumnDef<ExpandSupportAndDrillingDetail>[] =
	[
		{
			accessorKey: 'technologyName',
			header: 'Công nghệ',
		},
		{
			accessorKey: 'passportText',
			header: 'Hộ chiếu, Sđ, Sc',
		},
		{
			accessorKey: 'hardnessName',
			header: 'Độ kiên cố than đá (f)',
		},
	];
