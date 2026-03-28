import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { cn, formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { Material } from './type';

export const MAIN_PRICING_MATERIAL_COLUMNS: ColumnDef<Material>[] = [
	{
		accessorKey: 'code',
		header: 'Mã định mức vật liệu',
	},
	{
		accessorKey: 'processName',
		header: 'Công đoạn sản xuất',
	},
	{
		id: 'materialDetail',
		header: 'Thông số',
		cell: ({ row }) => {
			const { passportName, hardnessName, insertItemName, supportStepName } =
				row.original;

			return (
				<div className='flex min-w-[360px] flex-wrap items-center gap-x-2 text-sm'>
					<span>{hardnessName}</span>
					<span>|</span>
					<span>{passportName}</span>
					<span>|</span>
					<span>{insertItemName}</span>
					<span>|</span>
					<span>{supportStepName}</span>
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
		header: 'Đơn giá vật liệu (đ/m)',
		cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
	},
];

export type ExpandMaterialDetail = {
	passport?: Passport;
	strength?: Strength;
	insert?: Insert;
	step?: Step;
};

export const MAIN_PRICING_MATERIAL_DETAIL_COLUMNS: ColumnDef<ExpandMaterialDetail>[] =
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
		{
			accessorKey: 'insert',
			header: 'Chèn',
			cell: ({ row }) => {
				const { insert } = row.original;
				return insert && insert.value;
			},
		},
		{
			accessorKey: 'step',
			header: 'Bước chống',
			cell: ({ row }) => {
				const { step } = row.original;
				return step && step.value;
			},
		},
	];

export type FlatMaterialCost = {
	isGroupRow: boolean;
	rowIndex?: number;
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
	otherMaterialValue?: number;
};

export const MAIN_PRICING_MATERIAL_EXPAND_COLUMNS: ColumnDef<FlatMaterialCost>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: () => <span>Mã giao khoán</span>,
			cell: ({ row }) => (
				<span className='font-semibold'>
					{row.original.isGroupRow && row.original.assignmentCode}
					{row.original.isGroupRow &&
						row.original.assignmentCodeName &&
						` - ${row.original.assignmentCodeName}`}
				</span>
			),
		},
		{
			accessorKey: 'materialCode',
			header: () => <span>Mã vật tư, tài sản</span>,
			cell: ({ row }) => row.original.materialCode ?? '',
		},
		{
			accessorKey: 'materialName',
			header: () => <span>Tên vật tư, tài sản</span>,
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
			accessorKey: 'cost',
			header: 'Đơn giá (đ)',
			cell: ({ row }) =>
				row.original.isGroupRow ? '' : formatNumber(row.original.cost ?? 0),
		},
		{
			accessorKey: 'quantity',
			header: 'Định mức',
			cell: ({ row }) => row.original.quantity ?? '',
		},
		{
			accessorKey: 'totalPrice',
			header: 'Thành tiền (đ)',
			cell: ({ row }) => (
				<span className={cn(row.original.isGroupRow && 'font-semibold')}>
					{formatNumber(Math.round(row.original.totalPrice))}
				</span>
			),
		},
	];
