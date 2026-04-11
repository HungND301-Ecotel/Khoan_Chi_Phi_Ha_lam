import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

type AffectAssignmentCode = {
	id: string;
	code: string;
	name: string;
};

type NormFactorAssignmentCode = {
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	value: number;
	targetHardnessId?: string;
	targetHardnessName?: string;
};

export type NormFactorExpandItem = NormFactorAssignmentCode;

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
	assignmentCodes?: NormFactorAssignmentCode[];
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
];

export const CATALOG_NORM_FACTOR_EXPAND_COLUMNS: ColumnDef<NormFactorExpandItem>[] =
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
			accessorKey: 'value',
			header: 'Hệ số điều chỉnh định mức',
			cell: ({ row }) => formatNumber(row.original.value),
		},
		{
			accessorKey: 'targetHardnessName',
			header: 'Định mức tham chiếu',
			cell: ({ row }) => row.original.targetHardnessName || 'Định mức hiện tại',
		},
	];
