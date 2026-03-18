/**
 * @deprecated This component is archived and no longer in use.
 * The acceptance report now uses a single unified hierarchical table.
 * This file is kept for reference only and may have type errors.
 */

import { useEffect } from 'react';
import { AcceptanceReportDataTable } from '../datatable';
import { ContractedRevenueCategory } from '../types';
import { flattenMaterialGroups, flattenSCTXGroups } from '../utils';

type ContractedRevenueProps = {
	id: string;
	data: ContractedRevenueCategory | null;
	isOpen: boolean;
};

export function ContractedRevenue({
	id,
	data,
	isOpen,
}: ContractedRevenueProps) {
	// Only fetch/process data when open
	useEffect(() => {
		if (!isOpen || !id) return;

		// Data is already passed from parent
		// If you need to fetch data here, add API call
	}, [isOpen, id]);

	if (!data) return null;

	const materialData = flattenMaterialGroups(data.materialGroups);
	const sctxData = flattenSCTXGroups(data.sctxGroups);

	return (
		<div className='space-y-4 p-4'>
			{/* Vật liệu Section */}
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>Vật liệu</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={materialData} type='material' />
			</div>

			{/* SCTX Section */}
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>SCTX</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={sctxData} type='sctx' />
			</div>
		</div>
	);
}
