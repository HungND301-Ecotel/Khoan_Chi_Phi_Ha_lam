'use client';

import { RawAcceptanceReportTable } from './datatable';

export function RawAcceptanceReportPage() {
	return (
		<RawAcceptanceReportTable
			enableSearch
			enablePagination
			pageSize={10}
			largeText
		/>
	);
}
