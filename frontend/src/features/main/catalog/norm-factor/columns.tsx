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
	steelMeshType: number;
	affectAssignmentCodes: AffectAssignmentCode[];
	value: number;
	targetHardnessId: string;
	targetHardnessName: string;
};

const STEEL_MESH_TYPE_LABEL: Record<number, string> = {
	1: 'Không áp dụng',
	2: 'Trải 1 lớp lưới thép',
	3: 'Trải 2 lớp lưới thép',
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
		cell: ({ row }) => {
			const { hardnessName, steelMeshType } = row.original;

			if (!hardnessName && steelMeshType !== 1) {
				return (
					<span className='whitespace-normal'>
						{STEEL_MESH_TYPE_LABEL[steelMeshType] ?? 'Không xác định'}
					</span>
				);
			}

			return (
				<span className='whitespace-normal'>
					{hardnessName || 'Không áp dụng'}
				</span>
			);
		},
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
