import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Slide = {
	id: string;
	code: string;
	processGroupId: string;
	processGroupName: string;
	passportId: string;
	passportName: string;
	hardnessId: string;
	hardnessName: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

export const MAIN_PRICING_SLIDE_COLUMNS: ColumnDef<Slide>[] = [
	{
		accessorKey: 'code',
		header: 'Mã định mức máng trượt',
	},
	{
		accessorKey: 'processGroupName',
		header: 'Nhóm công đoạn sản xuất',
	},
	{
		id: 'materialDetail',
		header: 'Thông số',
		cell: ({ row }) => {
			const { passportName, hardnessName } = row.original;

			return (
				<div className='flex min-w-[360px] flex-wrap items-center gap-x-2 text-sm'>
					<span>{hardnessName}</span>
					<span>|</span>
					<span>{passportName}</span>
				</div>
			);
		},
	},
	{
		accessorKey: 'startMonth',
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
		header: 'Đơn giá máng trượt (đ/m)',
		cell: ({ row }) => formatNumber(row.original.totalPrice),
	},
];

export type ExpandSlideDetail = {
	passport?: Passport;
	strength?: Strength;
};

export const MAIN_PRICING_DETAIL_EXPAND_COLUMNS: ColumnDef<ExpandSlideDetail>[] =
	[
		{
			accessorKey: 'passport',
			header: 'Hộ chiếu, Sđ, Sc',
			cell: ({ row }) => {
				const { passport } = row.original;
				return (
					passport && `H/c ${passport.name}; ${passport.sd}; ${passport.sc}`
				);
			},
		},
		{
			accessorKey: 'strength',
			header: 'Độ kiên cố đá, than (f)',
			cell: ({ row }) => {
				const { strength } = row.original;
				return strength && strength.value;
			},
		},
	];

export type SlideDetail = {
	id: string;
	code: string;
	name: string;
	startDate: string;
	endDate: string;
	materialCost: SlideDetailMaterial[];
};

export type SlideDetailMaterial = {
	assignmentCodeId: string;
	assignmentCode: string;
	costs: SlideDetailMaterialCost[];
};

export type SlideDetailMaterialCost = {
	id: string;
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName: string;
	cost: number;
	amount: number;
};

export type FlatSlideCost = {
	isGroupRow: boolean;
	rowIndex: number;
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName?: string;
	materialId?: string;
	materialCode?: string;
	materialName?: string;
	unitOfMeasureName?: string;
	cost?: number;
	quantity?: number;
	totalPrice: number;
};

export const MAIN_PRICING_SLIDE_EXPAND_COLUMNS: ColumnDef<FlatSlideCost>[] = [
	{
		accessorKey: 'assignmentCode',
		header: () => (
			<span className='leading-tight whitespace-normal'>Mã giao khoán</span>
		),
		cell: ({ row }) => (
			<span className='font-semibold'>
				{row.original.isGroupRow &&
					`${row.original.assignmentCode}${row.original.assignmentCodeName ? ` - ${row.original.assignmentCodeName}` : ''}`}
			</span>
		),
	},
	{
		accessorKey: 'materialCode',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				Mã vật tư, tài sản
			</span>
		),
		cell: ({ row }) => row.original.materialCode ?? '',
	},
	{
		accessorKey: 'materialName',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				Tên vật tư, tài sản
			</span>
		),
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.materialName ?? ''}
			</span>
		),
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'ĐVT',
		cell: ({ row }) => row.original.unitOfMeasureName ?? '',
	},
	{
		accessorKey: 'totalPrice',
		header: 'Đơn giá máng trượt (đ/m)',
		cell: ({ row }) => (
			<span className={cn(row.original.isGroupRow && 'font-semibold')}>
				{formatNumber(row.original.totalPrice)}
			</span>
		),
	},
];
