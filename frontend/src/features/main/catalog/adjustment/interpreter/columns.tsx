import { ColumnDef } from '@tanstack/react-table';

export type Interpreter = {
	id: string;
	description: string;
	adjustmentFactorId: string;
	adjustmentFactorCode: string;
	maintenanceAdjustmentValue: number;
	electricityAdjustmentValue?: number;
};

export const CATALOG_ADJUSTMENT_INTERPRETER_COLUMNS: ColumnDef<Interpreter>[] =
	[
		{
			accessorKey: 'adjustmentFactorCode',
			header: 'Mã hệ số điều chỉnh',
		},
		{
			accessorKey: 'description',
			header: 'Diễn giải hệ số điều chỉnh',
		},
		{
			accessorKey: 'maintenanceAdjustmentValue',
			header: 'Trị số điều chỉnh SCTX',
		},
		{
			accessorKey: 'electricityAdjustmentValue',
			header: 'Trị số điều chỉnh điện năng',
		},
	];
