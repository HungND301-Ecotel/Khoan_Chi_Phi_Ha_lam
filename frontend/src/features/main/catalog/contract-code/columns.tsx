import { ColumnDef } from '@tanstack/react-table';

export type ContractCode = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId?: string | null;
	unitOfMeasureName?: string | null;
};

export const CATALOG_CONTRACT_CODE_COLUMNS: ColumnDef<ContractCode>[] = [
	{
		accessorKey: 'code',
		header: 'Mã giao khoán',
	},
	{
		accessorKey: 'name',
		header: 'Tên giao khoán',
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'Đơn vị tính',
	},
];
