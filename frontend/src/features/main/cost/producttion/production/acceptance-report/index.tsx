import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { AcceptanceReportDataTable } from './datatable';
import { HierarchicalRow } from './types';
import { flattenHierarchicalData } from './utils';
import { useAcceptanceReportDetail } from './use-acceptance-report-detail';

export function AcceptanceReport({
	id,
	output: _output,
	plan: _plan,
	callback: _callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [flattenedData, setFlattenedData] = useState<HierarchicalRow[]>([]);

	// Fetch data from API
	const {
		data: hierarchicalData,
		loading,
		error,
	} = useAcceptanceReportDetail(id, isOpen, reloadKey);

	// Flatten and update data when hierarchical data changes
	useEffect(() => {
		if (hierarchicalData) {
			const flattened = flattenHierarchicalData(hierarchicalData);
			setFlattenedData(flattened);
		}
	}, [hierarchicalData]);

	return (
		<AccordionItem value={'acceptance-report'} className='border-none'>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Bảng nghiệm thu vật tư và kết chuyển chi phí</ItemTitle>
				</ItemContent>
				<ItemActions>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<AccordionTrigger
						disabled={false}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='p-0 px-2 pt-2'>
					{loading ? (
						<div className='flex justify-center py-8'>
							<Spinner />
						</div>
					) : error ? (
						<div className='flex justify-center py-8 text-red-500'>
							<p>Lỗi tải dữ liệu: {error.message}</p>
						</div>
					) : (
						<AcceptanceReportDataTable data={flattenedData} className='mt-2' />
					)}{' '}
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
