import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

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
		totalPrice: number;
	}>;
	otherMaterialValue?: number;
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
		id: 'materialDetail',
		header: 'Thông số',
		cell: ({ row }) => {
			const {
				seamFaceName,
				longwallParameters,
				cuttingThickness,
				technologyName,
				hardnessName,
				powerName,
			} = row.original;

			const longwallParameterText = longwallParameters
				? `Llc ${longwallParameters.llc}; Lkc ${longwallParameters.lkc}; Mk ${longwallParameters.mk}`
				: '-';
			const cuttingThicknessText = cuttingThickness
				? cuttingThickness.value
				: '-';
			const hardnessOrPowerText =
				powerName?.trim() || hardnessName?.trim() || '';
			const displayParts = [
				technologyName?.trim(),
				hardnessOrPowerText,
				seamFaceName?.trim(),
				longwallParameterText,
				cuttingThicknessText,
			].filter(Boolean);

			return (
				<div className='flex min-w-[360px] flex-wrap items-center gap-x-2 text-sm'>
					{displayParts.map((part, index) => (
						<span key={`${String(part)}-${index}`}>
							{index > 0 ? ` | ${part}` : part}
						</span>
					))}
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
		header: 'Đơn giá vật liệu (đ/tấn)',
		cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
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

export type ExpandLongwallMaterialAssignmentCost = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	totalPrice: number;
};

export const LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS: ColumnDef<ExpandLongwallMaterialAssignmentCost>[] =
	[
		{
			accessorKey: 'assignmentCode',
			header: 'Mã giao khoán',
		},
		{
			accessorKey: 'assignmentCodeName',
			header: 'Tên giao khoán',
		},
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá vật liệu (đ/tấn)',
			cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
		},
	];
