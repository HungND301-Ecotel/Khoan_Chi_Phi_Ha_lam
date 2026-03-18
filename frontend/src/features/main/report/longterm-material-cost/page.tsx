'use client';

import { LongtermMaterialCostDataTable } from './datatable';

export function LongtermMaterialCostPage() {
	return (
		<LongtermMaterialCostDataTable
			enableSearch
			enablePagination
			pageSize={10}
			largeText
		/>
	);
}
