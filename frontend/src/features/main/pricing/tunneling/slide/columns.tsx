import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type SlidePeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
};

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
	startMonth?: string;
	endMonth?: string;
	totalPrice?: number;
	periods?: SlidePeriod[];
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
];

export type ExpandSlideDetail = {
	passport?: Passport;
	strength?: Strength;
};

export const MAIN_PRICING_DETAIL_EXPAND_COLUMNS: ColumnDef<ExpandSlideDetail>[] =
	[
		{
			accessorKey: 'passport',
			header: () => <span className='text-black font-semibold'>Hộ chiếu, Sđ, Sc</span>,
			cell: ({ row }) => {
				const { passport } = row.original;
				return (
					<span className='text-black'>
						{passport ? `H/c ${passport.name}; ${passport.sd}; ${passport.sc}` : '-'}
					</span>
				);
			},
		},
		{
			accessorKey: 'strength',
			header: () => <span className='text-black font-semibold'>Độ kiên cố đá, than (f)</span>,
			cell: ({ row }) => {
				const { strength } = row.original;
				return <span className='text-black'>{strength?.value ?? '-'}</span>;
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
			header: () => <span className='text-black font-semibold'>Mã nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold text-black'>{row.original.assignmentCode}</span>
				) : (
					''
				),
		},
		{
			accessorKey: 'assignmentCodeName',
			header: () => <span className='text-black font-semibold'>Tên nhóm vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? <span className='font-semibold text-black'>{row.original.assignmentCodeName ?? ''}</span>
					: '',
		},
		{
			accessorKey: 'materialCode',
			header: () => <span className='text-black font-semibold'>Mã vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: <span className='text-black'>{row.original.materialCode}</span>,
		},
		{
			accessorKey: 'materialName',
			header: () => <span className='text-black font-semibold'>Tên vật tư, tài sản</span>,
			cell: ({ row }) => (
				<span className='whitespace-normal text-black'>
					{row.original.rowType === 'group-summary'
						? ''
						: row.original.materialName}
				</span>
			),
		},
		{
			accessorKey: 'unitOfMeasureName',
			header: () => <span className='text-black font-semibold'>ĐVT</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: <span className='text-black'>{row.original.unitOfMeasureName ?? ''}</span>,
		},
		{
			accessorKey: 'unitPrice',
			header: () => <span className='text-black font-semibold'>Đơn giá (đ)</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.unitPrice === null ||
				row.original.unitPrice === undefined
					? ''
					: <span className='text-black'>{formatNumber(row.original.unitPrice)}</span>,
		},
		{
			accessorKey: 'norm',
			header: () => <span className='text-black font-semibold'>Định mức</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: <span className='text-black'>{String(row.original.norm)}</span>,
		},
		{
			accessorKey: 'totalPrice',
			header: () => <span className='text-black font-semibold'>Đơn giá máng trượt (đ/m)</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold text-black'>
						{formatNumber(row.original.totalPrice)}
					</span>
				) : (
					<span className='text-black'>{formatNumber(row.original.totalPrice)}</span>
				),
		},
	];
