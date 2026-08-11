import { ColumnDef } from '@tanstack/react-table';

export type ProcessStep = {
	id: string;
	code: string;
	name: string;
	processGroupId: string;
	processGroupName: string;
	unitOfMeasureId?: string;
	unitOfMeasureName?: string;
};

export const CATALOG_PROCESS_STEP_COLUMNS: ColumnDef<ProcessStep>[] = [
	{
		accessorKey: 'code',
		header: 'Mã công đoạn sản xuất',
	},
	{
		accessorKey: 'name',
		header: 'Tên công đoạn sản xuất',
	},
	{
		accessorKey: 'processGroupName',
		header: 'Nhóm công đoạn sản xuất',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
		cell: ({ row }) => row.original.unitOfMeasureName || '-',
	},
];
