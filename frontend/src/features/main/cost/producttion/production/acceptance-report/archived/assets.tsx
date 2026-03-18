/**
 * @deprecated This component is archived and no longer in use.
 * The acceptance report now uses a single unified hierarchical table.
 * This file is kept for reference only and may have type errors.
 */

import { useEffect } from 'react';
import { AcceptanceReportDataTable } from '../datatable';
import { AssetCategory } from '../types';
import { flattenAssetGroups } from '../utils';

type AssetsProps = {
	id: string;
	data: AssetCategory | null;
	isOpen: boolean;
};

export function Assets({ id, data, isOpen }: AssetsProps) {
	// Only fetch/process data when open
	useEffect(() => {
		if (!isOpen || !id) return;

		// Data is already passed from parent
		// If you need to fetch data here, add API call
	}, [isOpen, id]);

	if (!data) return null;

	const assetData = flattenAssetGroups(data.assetGroups);

	return (
		<div className='space-y-4 p-4'>
			<div className='space-y-2'>
				<h4 className='text-sm font-semibold'>Tài sản</h4>
				{/* @ts-expect-error - Archived component using old API */}
				<AcceptanceReportDataTable data={assetData} type='asset' />
			</div>
		</div>
	);
}
