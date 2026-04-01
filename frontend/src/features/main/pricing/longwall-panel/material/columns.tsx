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
			} = row.original;

			const longwallParameterText = longwallParameters
				? `Llc ${longwallParameters.llc}; Lkc ${longwallParameters.lkc}; Mk ${longwallParameters.mk}`
				: '-';
			const cuttingThicknessText = cuttingThickness
				? cuttingThickness.value
				: '-';

			return (
				<div className='flex min-w-[360px] flex-wrap items-center gap-x-2 text-sm'>
					<span>{technologyName ?? ''}</span>
					<span>|</span>
					<span>{seamFaceName ?? ''}</span>
					<span>|</span>
					<span>{longwallParameterText}</span>
					<span>|</span>
					<span>{cuttingThicknessText}</span>
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
