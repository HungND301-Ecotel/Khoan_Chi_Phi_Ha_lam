import { ColumnDef } from '@tanstack/react-table';
import { Badge } from '@/components/ui/badge';
import { formatNumber } from '@/lib/utils';

type AffectAssignmentCode = {
	id: string;
	code: string;
	name: string;
};

export type NormFactor = {
	id: string;
	processGroupId: string;
	processGroupCode: string;
	processGroupName: string;
	productionProcessId: string;
	productionProcessCode: string;
	productionProcessName: string;
	hardnessId: string;
	hardnessName: string;
	stoneClampRatioId: string;
	stoneClampRatioName: string;
	affectAssignmentCodes: AffectAssignmentCode[];
	value: number;
	targetHardnessId: string;
	targetHardnessName: string;
};

export const CATALOG_NORM_FACTOR_COLUMNS: ColumnDef<NormFactor>[] = [
	{
		accessorKey: 'productionProcessCode',
		header: 'Mã công đoạn sản xuất',
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.productionProcessCode} -{' '}
				{row.original.productionProcessName}
			</span>
		),
	},
	{
		accessorKey: 'hardnessName',
		header: 'Độ kiên cố than đá (f)',
	},
	{
		accessorKey: 'stoneClampRatioName',
		header: 'Tỷ lệ đá kẹp (Ckẹp)',
	},
	{
		accessorKey: 'affectAssignmentCodes',
		header: 'Thành phần điều chỉnh định mức',
		cell: ({ row }) => (
			<div className='flex flex-wrap gap-1'>
				{(row.original.affectAssignmentCodes ?? []).map((item) => (
					<Badge
						key={item.id}
						variant='secondary'
						className='whitespace-normal'
					>
						{item.code} - {item.name}
					</Badge>
				))}
			</div>
		),
	},
	{
		accessorKey: 'value',
		header: 'Hệ số điều chỉnh định mức',
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{formatNumber(row.original.value)}
			</span>
		),
	},
	{
		accessorKey: 'targetHardnessName',
		header: 'Định mức tham chiếu',
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.targetHardnessName || 'Định mức hiện tại'}
			</span>
		),
	},
];
