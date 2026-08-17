import { CostProduct } from '@/features/main/cost/plan/types';
import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export { MAIN_COST_PLAN_COLUMNS } from './khai-thac/columns';
export { VTL_COST_PLAN_COLUMNS } from './van-tai-lo/columns';

export type DepartmentPlanGroup = {
	id: string;
	code: string;
	name: string;
	hasKhaiThac?: boolean;
	hasVanTaiLo?: boolean;
	startMonth?: string;
	endMonth?: string;
	productUnitPriceIds: string[];
};

export type DepartmentPlanMonthGroup = {
	id: string;
	time: string;
	productUnitPriceIds: string[];
	products: CostProduct[];
};

export const PLAN_DEPARTMENT_COLUMNS: ColumnDef<DepartmentPlanGroup>[] = [
	{
		accessorKey: 'code',
		header: () => <span className='whitespace-normal'>Mã đơn vị</span>,
	},
	{
		accessorKey: 'name',
		header: () => <span className='whitespace-normal'>Tên đơn vị</span>,
	},
	{
		id: 'time',
		header: () => <span className='whitespace-normal'>Thời gian</span>,
		cell: ({ row }) => {
			const { startMonth, endMonth } = row.original;

			if (!startMonth && !endMonth) return '-';
			if (!startMonth) return formatDate(endMonth!);
			if (!endMonth) return formatDate(startMonth);

			return `${formatDate(startMonth)} - ${formatDate(endMonth)}`;
		},
	},
];
