import { ColumnDef } from '@tanstack/react-table';

export type Clamp = {
	id: string;
	value: string;
	coefficientValue: number;
	hardnessId: string;
	hardnessValue: string;
	processId: string;
	processName: string;
	processCode: string;
};

export const CATALOG_PARAMETER_CLAMP_COLUMNS: ColumnDef<Clamp>[] = [
	{
		accessorKey: 'processCode',
		header: 'Mã công đoạn sản xuất',
	},
	{
		accessorKey: 'hardnessValue',
		header: 'Độ kiên cố than, đá (f)',
	},
	{
		accessorKey: 'value',
		header: 'Tỷ lệ đá kẹp (Ckẹp)',
	},
	{
		accessorKey: 'coefficientValue',
		header: 'Hệ số điều chỉnh định mức',
	},
];
