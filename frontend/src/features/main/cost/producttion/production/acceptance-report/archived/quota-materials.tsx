/**
 * @deprecated This component is archived and no longer in use.
 * The acceptance report now uses a single unified hierarchical table.
 * This file is kept for reference only and may have type errors.
 */

import { useEffect } from 'react';
import { AcceptanceReportDataTable } from '../datatable';
import { QuotaCategory } from '../types';
import { flattenQuotaMaterialGroups } from '../utils';

type QuotaMaterialsProps = {
	id: string;
	data: QuotaCategory | null;
	isOpen: boolean;
};

export function QuotaMaterials({ id, data, isOpen }: QuotaMaterialsProps) {
	// Only fetch/process data when open
	useEffect(() => {
		if (!isOpen || !id) return;

		// Data is already passed from parent
		// If you need to fetch data here, add API call
	}, [isOpen, id]);

	if (!data) return null;

	const supportBeamData = flattenQuotaMaterialGroups(data.supportBeamGroups);
	const accessoriesData = flattenQuotaMaterialGroups(data.accessoriesGroups);
	const woodData = flattenQuotaMaterialGroups(data.woodGroups);

	return (
		<div className='space-y-4 p-4'>
			{/* Vì chống lò Section */}
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>Vì chống lò</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={supportBeamData} type='quota' />
			</div>

			{/* Phụ kiện chống lò Section */}
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>Phụ kiện chống lò</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={accessoriesData} type='quota' />
			</div>

			{/* Gỗ lò Section */}
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>Gỗ lò</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={woodData} type='quota' />
			</div>
		</div>
	);
}
