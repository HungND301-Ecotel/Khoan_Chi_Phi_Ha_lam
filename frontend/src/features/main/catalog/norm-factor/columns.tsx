import { ColumnDef } from '@tanstack/react-table';

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
	affectAssignmentCodeIds: string[];
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
		accessorKey: 'value',
		header: 'Hệ số điều chỉnh định mức',
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.value}</span>
		),
	},
];
