import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { Material } from './type';

const getMaterialDetail = (material: Material) =>
	[
		material.hardnessName,
		material.passportName,
		material.insertItemName,
		material.supportStepName,
	]
		.filter(Boolean)
		.join(' | ');

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
		accessorFn: getMaterialDetail,
		id: 'materialDetail',
		header: 'Thông số',
		cell: ({ row }) => {
			return (
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
			);
		},
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
		{
			accessorKey: 'insert',
			header: () => <span className='text-black font-semibold'>Chèn</span>,
			cell: ({ row }) => {
				const { insert } = row.original;
				return <span className='text-black'>{insert?.value ?? '-'}</span>;
			},
		},
		{
			accessorKey: 'step',
			header: () => <span className='text-black font-semibold'>Bước chống</span>,
			cell: ({ row }) => {
				const { step } = row.original;
				return <span className='text-black'>{step?.value ?? '-'}</span>;
			},
		},
	];

export type ExpandMaterialCostRow = {
	rowType?: 'group-summary' | 'material-item';
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	materialId: string;
	materialCode: string;
	materialName: string;
	unitPrice?: number | null;
	norm: number | string;
	totalPrice: number;
};

export const MAIN_PRICING_MATERIAL_EXPAND_COLUMNS: ColumnDef<ExpandMaterialCostRow>[] =
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
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold text-black'>{row.original.assignmentCodeName}</span>
				) : (
					''
				),
		},
		{
			accessorKey: 'materialCode',
			header: () => <span className='text-black font-semibold'>Mã vật tư, tài sản</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					''
				) : (
					<span className='text-black'>{row.original.materialCode}</span>
				),
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
			accessorKey: 'unitPrice',
			header: () => <span className='text-black font-semibold'>Đơn giá (đ)</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ||
				row.original.unitPrice === null ||
				row.original.unitPrice === undefined ? (
					''
				) : (
					<span className='text-black'>{formatNumber(row.original.unitPrice)}</span>
				),
		},
		{
			accessorKey: 'norm',
			header: () => <span className='text-black font-semibold'>Định mức</span>,
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					''
				) : (
					<span className='text-black'>{String(row.original.norm)}</span>
				),
		},
		{
			accessorKey: 'totalPrice',
			header: () => <span className='text-black font-semibold'>Đơn giá vật liệu (đ/m)</span>,
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

export type ExpandMaterialAssignmentCost = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	totalPrice: number;
};

export const MAIN_PRICING_MATERIAL_EXPAND_SUMMARY_COLUMNS: ColumnDef<ExpandMaterialAssignmentCost>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: 'Nhóm vật tư, tài sản',
		},
		{
			accessorKey: 'assignmentCodeName',
			header: 'Tên nhóm vật tư, tài sản',
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá vật liệu (đ/m)',
			cell: ({ row }) => formatNumber(row.original.totalPrice),
		},
	];
