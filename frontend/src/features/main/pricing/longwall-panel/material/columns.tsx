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
	// Legacy field names for backward compatibility
	passportId?: string;
	cuttingthicknessId?: string;
	mValue?: string;
	technologyName?: string;
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
				technologyName,
				seamFaceName,
				mValue,
				longwallParameters,
				cuttingThickness,
			} = row.original;

			const longwallParameterText = longwallParameters
				? `Llc ${longwallParameters.llc}; Lkc ${longwallParameters.lkc}; Mk ${longwallParameters.mk}`
				: (mValue ?? '-');

			const cuttingThicknessText = cuttingThickness
				? cuttingThickness.value
				: '-';

			return (
				<div className='flex min-w-[360px] flex-wrap items-center gap-x-2 text-sm text-gray-600'>
					<span>{technologyName ?? ''}</span>
					<span className='text-gray-300'>|</span>
					<span>{seamFaceName ?? ''}</span>
					<span className='text-gray-300'>|</span>
					<span>{longwallParameterText}</span>
					<span className='text-gray-300'>|</span>
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
