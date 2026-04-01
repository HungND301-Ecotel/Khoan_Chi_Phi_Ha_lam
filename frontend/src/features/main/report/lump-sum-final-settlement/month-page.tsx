'use client';

import { LumpSumFinalSettlementMonthReportTable } from './month-datatable';

export function LumpSumFinalSettlementMonthPage() {
	return (
		<LumpSumFinalSettlementMonthReportTable
			enableSearch
			enablePagination={false}
		/>
	);
}
