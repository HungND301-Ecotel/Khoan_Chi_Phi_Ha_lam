import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import type { LongwallMaterialDetailCost } from './type';

export type LongwallParameters = {
	id: string;
	llc: string;
	lkc: number;
	mk: number;
};

export type CuttingThickness = {
	id: string;
	value: string;
};

export type LongwallMaterial = {
	id: string;
	code: string;
	materialDetail?: string;
	longwallParametersId: string;
	cuttingThicknessId: string;
	seamFaceId: string;
	technologyId: string;
	processId: string;
	powerId?: string | null;
	hardnessId?: string | null;
	isLongwallMaterialUnitPriceCGH?: boolean;
	processName?: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
	// Nested objects from API
	longwallParameters?: LongwallParameters;
	cuttingThickness?: CuttingThickness;
	seamFaceName?: string;
	cuttingthicknessId?: string;
	technologyName?: string;
	hardnessName?: string;
	powerName?: string;

	costs: Array<{
		assignmentCodeId: string;
		materialId?: string;
		norm?: number;
		totalPrice: number;
	}>;
	otherMaterialValue?: number;
};

export const getLongwallMaterialDetail = (material: LongwallMaterial) => {
	const longwallParameterText = material.longwallParameters
		? `${material.longwallParameters.llc}; ${material.longwallParameters.lkc}; ${material.longwallParameters.mk}`
		: '-';
	const cuttingThicknessText = material.cuttingThickness?.value ?? '-';
	const hardnessOrPowerText =
		material.powerName?.trim() || material.hardnessName?.trim() || '';

	return [
		material.technologyName?.trim(),
		hardnessOrPowerText,
		material.seamFaceName?.trim(),
		longwallParameterText,
		cuttingThicknessText,
	]
		.filter(Boolean)
		.join(' | ');
};

export const LONGWALL_MATERIAL_COLUMNS: ColumnDef<LongwallMaterial>[] = [
	{
		accessorKey: 'code',
		header: 'Mã định mức vật liệu',
	},
	{
		accessorKey: 'processName',
		header: 'Công đoạn sản xuất',
	},
	{
		accessorFn: getLongwallMaterialDetail,
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
		header: 'Đơn giá vật liệu (đ/1000 tấn)',
		cell: ({ row }) => formatNumber(row.original.totalPrice),
	},
];

export type ExpandLongwallMaterialDetail = {
	technologyName?: string;
	powerOrHardnessValue?: string;
	longwallParametersValue?: string;
	cuttingThicknessValue?: string;
	seamFaceValue?: string;
};

export const LONGWALL_MATERIAL_DETAIL_CGH_COLUMNS: ColumnDef<ExpandLongwallMaterialDetail>[] =
	[
		{
			accessorKey: 'technologyName',
			header: 'Công nghệ khai thác',
		},
		{
			accessorKey: 'powerOrHardnessValue',
			header: 'Công suất',
		},
		{
			accessorKey: 'longwallParametersValue',
			header: 'Thông số lò chợ',
		},
		{
			accessorKey: 'cuttingThicknessValue',
			header: 'Chiều dày lớp khấu',
		},
		{
			accessorKey: 'seamFaceValue',
			header: 'Mặt vỉa (m)',
		},
	];

export const LONGWALL_MATERIAL_DETAIL_NON_CGH_COLUMNS: ColumnDef<ExpandLongwallMaterialDetail>[] =
	[
		{
			accessorKey: 'technologyName',
			header: 'Công nghệ khai thác',
		},
		{
			accessorKey: 'powerOrHardnessValue',
			header: 'Độ kiên cố than đá (f)',
		},
		{
			accessorKey: 'longwallParametersValue',
			header: 'Thông số lò chợ',
		},
		{
			accessorKey: 'cuttingThicknessValue',
			header: 'Chiều dày lớp khấu',
		},
		{
			accessorKey: 'seamFaceValue',
			header: 'Mặt vỉa (m)',
		},
	];

export type ExpandLongwallMaterialCostRow = Omit<
	LongwallMaterialDetailCost,
	'norm' | 'unitPrice'
> & {
	rowType?: 'group-summary' | 'material-item';
	norm: number | string;
	unitPrice?: number | null;
};

export const LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS: ColumnDef<ExpandLongwallMaterialCostRow>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: 'Mã nhóm vật tư, tài sản',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary' ? (
					<span className='font-semibold'>{row.original.assignmentCode}</span>
				) : (
					''
				),
		},
		{
			accessorKey: 'assignmentCodeName',
			header: 'Tên nhóm vật tư, tài sản',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? row.original.assignmentCodeName
					: '',
		},
		{
			accessorKey: 'materialCode',
			header: 'Mã vật tư, tài sản',
			cell: ({ row }) =>
				row.original.rowType === 'group-summary'
					? ''
					: row.original.materialCode,
		},
		{
			accessorKey: 'materialName',
			header: 'Tên vật tư, tài sản',
			cell: ({ row }) => (
				<span className='whitespace-normal'>
					{row.original.rowType === 'group-summary'
						? ''
						: row.original.materialName}
				</span>
			),
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
			header: 'Đơn giá vật liệu (đ/1000 tấn)',
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
