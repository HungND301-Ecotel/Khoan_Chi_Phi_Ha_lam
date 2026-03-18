'use client';

import { LumpSumFinalSettlementReportTable } from './datatable';

export function LumpSumFinalSettlementPage() {
	return (
		<LumpSumFinalSettlementReportTable
			enableSearch
			enablePagination
			pageSize={10}
		/>
	);
}
