import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Slide = {
	id: string;
	code: string;
	materialDetail?: string;
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

const getSlideMaterialDetail = (slide: Slide) =>
	[slide.hardnessName, slide.passportName].filter(Boolean).join(' | ');

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
		accessorFn: getSlideMaterialDetail,
		id: 'materialDetail',
		header: 'Thông số',
		cell: ({ row }) => (
			<div className='flex min-w-90 flex-wrap items-center gap-x-2 text-sm'>
				{String(row.getValue('materialDetail'))
					.split(' | ')
					.map((item, index, items) => (
						<div key={`${item}-${index}`} className='contents'>
							<span>{item}</span>
							{index < items.length - 1 && <span>|</span>}
						</div>
					))}
			</div>
		),
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
	assignmentCodeName?: string;
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

export type ExpandSlideCostRow = {
	rowType?: 'group-summary' | 'material-item';
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName?: string;
	materialId: string;
	materialCode: string;
	materialName: string;
	unitOfMeasureName?: string;
	unitPrice?: number | null;
	norm: number | string;
	totalPrice: number;
};

export const MAIN_PRICING_SLIDE_EXPAND_COLUMNS: ColumnDef<ExpandSlideCostRow>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: () => <span>Mã nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold'>{row.original.assignmentCode}</span>
				) : (
					''
				),
		},
		{
			accessorKey: 'assignmentCodeName',
			header: () => <span>Tên nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? row.original.assignmentCodeName ?? ''
					: '',
		},
		{
			accessorKey: 'materialCode',
			header: () => <span>Mã vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: row.original.materialCode,
		},
		{
			accessorKey: 'materialName',
			header: () => <span>Tên vật tư, tài sản</span>,
			cell: ({ row }) => (
				<span className='whitespace-normal'>
					{row.original.rowType === 'group-summary'
						? ''
						: row.original.materialName}
				</span>
			),
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: 'ĐVT',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: row.original.unitOfMeasureName ?? '',
		},
		{
			accessorKey: 'unitPrice',
			header: 'Đơn giá (đ)',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.unitPrice === null ||
				row.original.unitPrice === undefined
					? ''
					: formatNumber(row.original.unitPrice),
		},
		{
			accessorKey: 'norm',
			header: 'Định mức',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: String(row.original.norm),
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá máng trượt (đ/m)',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold'>
						{formatNumber(row.original.totalPrice)}
					</span>
				) : (
					formatNumber(row.original.totalPrice)
				),
		},
	];
